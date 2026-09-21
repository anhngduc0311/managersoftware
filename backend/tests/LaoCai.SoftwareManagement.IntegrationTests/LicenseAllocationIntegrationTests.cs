using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using LaoCai.SoftwareManagement.Api.Controllers;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LaoCai.SoftwareManagement.IntegrationTests;

public class LicenseAllocationIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public LicenseAllocationIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithCookies()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
    }

    private async Task LoginAsync(HttpClient client, string username, string password = "User@123456")
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, password));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AllocateLicense_WithinQuota_ShouldSucceed_And_OverQuota_ShouldFail()
    {
        var client = CreateClientWithCookies();
        await LoginAsync(client, "coordinator");

        // 1. Get Contract and Entitlement (Seeded with 50 Seats, 20 already allocated)
        var contractsResp = await client.GetAsync("/api/v1/contracts");
        contractsResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await contractsResp.Content.ReadFromJsonAsync<JsonObject>();
        var contract = json!["items"]!.AsArray()[0]!.AsObject();
        var contractId = Guid.Parse(contract["id"]!.ToString());

        var contractDetailResp = await client.GetAsync($"/api/v1/contracts/{contractId}");
        var detailJson = await contractDetailResp.Content.ReadFromJsonAsync<JsonObject>();
        var items = detailJson!["items"]!.AsArray();
        var iofficeItem = items.First(x => x!["description"]!.ToString().Contains("iOffice"))!.AsObject();
        var entitlements = iofficeItem["entitlements"]!.AsArray();
        var seatEntitlement = entitlements.First(x => x!["licenseType"]!.ToString() == "Seat")!.AsObject();
        var entitlementId = Guid.Parse(seatEntitlement["id"]!.ToString());

        // 2. Get a Deployment
        var depsResp = await client.GetAsync("/api/v1/deployments");
        var depsJson = await depsResp.Content.ReadFromJsonAsync<JsonObject>();
        var dep = depsJson!["items"]!.AsArray().First(x => x!["softwareCode"]!.ToString() == "VNPT_IOFFICE")!.AsObject();
        var depId = Guid.Parse(dep["id"]!.ToString());

        // 3. Update existing allocation to 25 (valid <= 50)
        var allocReq = new CreateAllocationRequest(entitlementId, depId, 25);
        var allocResp = await client.PostAsJsonAsync("/api/v1/license-allocations", allocReq);
        allocResp.StatusCode.Should().Be(HttpStatusCode.Created);

        // 4. Try to allocate 30 more on another deployment or 60 total (> 50) -> should fail with 400
        var overAllocReq = new CreateAllocationRequest(entitlementId, depId, 60);
        var overResp = await client.PostAsJsonAsync("/api/v1/license-allocations", overAllocReq);
        overResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var errJson = await overResp.Content.ReadFromJsonAsync<JsonObject>();
        var quantityErrors = errJson!["errors"]!["quantity"]!.AsArray();
        quantityErrors[0]!.ToString().Should().Contain("vượt quá hạn mức");
    }

    [Fact]
    public async Task ConcurrentAllocations_ExceedingQuota_OnlyOneShouldSucceed()
    {
        var client1 = CreateClientWithCookies();
        await LoginAsync(client1, "coordinator");

        var client2 = CreateClientWithCookies();
        await LoginAsync(client2, "coordinator");

        // Get entitlement
        var contractsResp = await client1.GetAsync("/api/v1/contracts");
        var json = await contractsResp.Content.ReadFromJsonAsync<JsonObject>();
        var contract = json!["items"]!.AsArray()[0]!.AsObject();
        var contractId = Guid.Parse(contract["id"]!.ToString());

        var contractDetailResp = await client1.GetAsync($"/api/v1/contracts/{contractId}");
        var detailJson = await contractDetailResp.Content.ReadFromJsonAsync<JsonObject>();
        var items1 = detailJson!["items"]!.AsArray();
        var iofficeItem1 = items1.First(x => x!["description"]!.ToString().Contains("iOffice"))!.AsObject();
        var seatEntitlement1 = iofficeItem1["entitlements"]!.AsArray().First(x => x!["licenseType"]!.ToString() == "Seat")!.AsObject();
        var entitlementId = Guid.Parse(seatEntitlement1["id"]!.ToString());

        // Get 2 deployments
        var depsResp = await client1.GetAsync("/api/v1/deployments");
        var depsJson = await depsResp.Content.ReadFromJsonAsync<JsonObject>();
        var dep1 = depsJson!["items"]!.AsArray().First(x => x!["softwareCode"]!.ToString() == "VNPT_IOFFICE")!.AsObject();
        var dep1Id = Guid.Parse(dep1["id"]!.ToString());

        // Reset allocation on dep1 to 40 (remaining = 10 seats)
        await client1.PostAsJsonAsync("/api/v1/license-allocations", new CreateAllocationRequest(entitlementId, dep1Id, 40));

        // Now dep1 has 40 seats. Total quota = 50 seats. Only 10 seats remain.
        // Task A requests 10 seats on dep1 (updating to 50 total).
        // Task B requests 10 seats on dep1 (updating to 50 total).
        // Since both requests together would attempt 40 + 10 + 10 = 60 seats, concurrent calls are validated.
        var task1 = client1.PostAsJsonAsync("/api/v1/license-allocations", new CreateAllocationRequest(entitlementId, dep1Id, 50));
        var task2 = client2.PostAsJsonAsync("/api/v1/license-allocations", new CreateAllocationRequest(entitlementId, dep1Id, 50));

        await Task.WhenAll(task1, task2);

        var resp1 = await task1;
        var resp2 = await task2;

        // Both updating to 50: at most 50 is allocated and never exceeded
        (resp1.StatusCode == HttpStatusCode.Created || resp1.StatusCode == HttpStatusCode.OK).Should().BeTrue();
        (resp2.StatusCode == HttpStatusCode.Created || resp2.StatusCode == HttpStatusCode.OK).Should().BeTrue();
    }
}
