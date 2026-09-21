using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using LaoCai.SoftwareManagement.Api.Controllers;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LaoCai.SoftwareManagement.IntegrationTests;

public class ContractApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ContractApiIntegrationTests(CustomWebApplicationFactory factory)
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
    public async Task GetContracts_ByCoordinator_ShouldReturnFinancialDetails()
    {
        var client = CreateClientWithCookies();
        await LoginAsync(client, "coordinator");

        var response = await client.GetAsync("/api/v1/contracts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        var items = json!["items"]!.AsArray();
        items.Should().NotBeEmpty();

        var first = items[0]!.AsObject();
        first["totalAmount"].Should().NotBeNull();
        first["totalAmount"]!.GetValue<decimal>().Should().Be(250000000m);
    }

    [Fact]
    public async Task GetContracts_ByEditorWithoutContractPermission_ShouldHideFinancials()
    {
        var client = CreateClientWithCookies();
        await LoginAsync(client, "editor_baothang");

        var response = await client.GetAsync("/api/v1/contracts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        var items = json!["items"]!.AsArray();
        if (items.Count > 0)
        {
            var first = items[0]!.AsObject();
            first["totalAmount"].Should().BeNull();
        }
    }

    [Fact]
    public async Task UpdateContract_WithValidIfMatch_ShouldSucceed_And_StaleIfMatch_ShouldReturn412()
    {
        var client = CreateClientWithCookies();
        await LoginAsync(client, "coordinator");

        var getResp = await client.GetAsync("/api/v1/contracts");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await getResp.Content.ReadFromJsonAsync<JsonObject>();
        var first = json!["items"]!.AsArray()[0]!.AsObject();
        var contractId = Guid.Parse(first["id"]!.ToString());

        var detailResp = await client.GetAsync($"/api/v1/contracts/{contractId}");
        detailResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var etag = detailResp.Headers.ETag!.Tag;

        var updateReq = new UpdateContractRequest(
            "HD-01/2026/STTTT-VNPT-EDITED",
            Guid.Parse(first["owningOrganizationId"]!.ToString()),
            Guid.Parse(first["vendorId"]!.ToString()),
            "Active",
            DateOnly.Parse(first["signedDate"]!.ToString()),
            DateOnly.Parse(first["startDate"]!.ToString()),
            DateOnly.Parse(first["endDate"]!.ToString()),
            300000000m,
            "VND"
        );

        // 1. Valid If-Match
        using var putMsg1 = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/contracts/{contractId}");
        putMsg1.Headers.Add("If-Match", etag);
        putMsg1.Content = JsonContent.Create(updateReq);
        var putResp1 = await client.SendAsync(putMsg1);
        putResp1.StatusCode.Should().Be(HttpStatusCode.OK);

        // 2. Stale If-Match => 412
        using var putMsg2 = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/contracts/{contractId}");
        putMsg2.Headers.Add("If-Match", etag); // stale
        putMsg2.Content = JsonContent.Create(updateReq);
        var putResp2 = await client.SendAsync(putMsg2);
        putResp2.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
    }
}
