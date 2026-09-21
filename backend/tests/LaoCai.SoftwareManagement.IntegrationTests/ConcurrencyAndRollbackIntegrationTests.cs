using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using LaoCai.SoftwareManagement.Api.Controllers;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LaoCai.SoftwareManagement.IntegrationTests;

public class ConcurrencyAndRollbackIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ConcurrencyAndRollbackIntegrationTests(CustomWebApplicationFactory factory)
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
    public async Task OptimisticConcurrency_WhenUpdatingWithStaleETag_ShouldReturn412PreconditionFailed()
    {
        var client = CreateClientWithCookies();
        await LoginAsync(client, "editor_baothang");

        // 1. Get Software and Org
        var softwareResp = await client.GetAsync("/api/v1/software");
        var softwareJson = await softwareResp.Content.ReadFromJsonAsync<JsonObject>();
        var softwareId = Guid.Parse(softwareJson!["items"]!.AsArray()[0]!["id"]!.ToString());

        var orgResp = await client.GetAsync("/api/v1/organizations");
        var orgJson = await orgResp.Content.ReadFromJsonAsync<JsonObject>();
        var orgBaoThang = orgJson!["items"]!.AsArray().First(o => o!["code"]!.ToString() == "UBND_BAOTHANG");
        var orgId = Guid.Parse(orgBaoThang!["id"]!.ToString());

        var releasesResp = await client.GetAsync($"/api/v1/software/{softwareId}/releases");
        var releases = await releasesResp.Content.ReadFromJsonAsync<JsonArray>();
        var releaseId = Guid.Parse(releases![0]!["id"]!.ToString());

        // 2. Create a new deployment with draft revision
        var uniqueInstanceKey = $"occ_{Guid.NewGuid():N}"[..8];
        var createDto = new CreateDeploymentDto(
            softwareId,
            orgId,
            "Production",
            uniqueInstanceKey,
            releaseId,
            "Active",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 2, 1),
            null
        );
        var createResp = await client.PostAsJsonAsync("/api/v1/deployments", createDto);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdJson = await createResp.Content.ReadFromJsonAsync<JsonObject>();
        var rev = createdJson!["activeRevision"]!.AsObject();
        var revId = Guid.Parse(rev["id"]!.ToString());
        var initialVersion = long.Parse(rev["version"]!.ToString());

        // 3. Client A updates with initialVersion -> Should succeed
        using var reqA = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/deployment-revisions/{revId}")
        {
            Content = JsonContent.Create(new UpdateDraftRevisionDto(
                releaseId,
                "Active",
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 2, 1),
                null
            ))
        };
        reqA.Headers.Add("If-Match", $"\"{initialVersion}\"");

        var resA = await client.SendAsync(reqA);
        resA.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Client B attempts to update with same stale initialVersion -> Must fail with 412 Precondition Failed
        using var reqB = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/deployment-revisions/{revId}")
        {
            Content = JsonContent.Create(new UpdateDraftRevisionDto(
                releaseId,
                "Suspended",
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 2, 1),
                null
            ))
        };
        reqB.Headers.Add("If-Match", $"\"{initialVersion}\"");

        var resB = await client.SendAsync(reqB);
        resB.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }

    [Fact]
    public async Task LicenseQuota_PessimisticConcurrency_PreventsOverAllocationUnderConcurrentLoad()
    {
        var client1 = CreateClientWithCookies();
        await LoginAsync(client1, "coordinator");

        // 1. Get Contract and Entitlement (50 Seats total, 20 already allocated in seed, 30 remaining)
        var contractsResp = await client1.GetAsync("/api/v1/contracts");
        contractsResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await contractsResp.Content.ReadFromJsonAsync<JsonObject>();
        var contract = json!["items"]!.AsArray()[0]!.AsObject();
        var contractId = Guid.Parse(contract["id"]!.ToString());

        var contractDetailResp = await client1.GetAsync($"/api/v1/contracts/{contractId}");
        var detailJson = await contractDetailResp.Content.ReadFromJsonAsync<JsonObject>();
        var items = detailJson!["items"]!.AsArray();
        var iofficeItem = items.First(x => x!["description"]!.ToString().Contains("iOffice"))!.AsObject();
        var entitlements = iofficeItem["entitlements"]!.AsArray();
        var seatEntitlement = entitlements.First(x => x!["licenseType"]!.ToString() == "Seat")!.AsObject();
        var entitlementId = Guid.Parse(seatEntitlement["id"]!.ToString());

        // 2. Get existing deployment
        var depsResp = await client1.GetAsync("/api/v1/deployments");
        var depsJson = await depsResp.Content.ReadFromJsonAsync<JsonObject>();
        var dep = depsJson!["items"]!.AsArray().First(x => x!["softwareCode"]!.ToString() == "VNPT_IOFFICE")!.AsObject();
        var depId = Guid.Parse(dep["id"]!.ToString());

        // 3. Attempting to allocate 60 seats (Total quota is 50) -> Must fail with BadRequest
        var overReq = new CreateAllocationRequest(entitlementId, depId, 60);
        var overResp = await client1.PostAsJsonAsync("/api/v1/license-allocations", overReq);
        overResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
