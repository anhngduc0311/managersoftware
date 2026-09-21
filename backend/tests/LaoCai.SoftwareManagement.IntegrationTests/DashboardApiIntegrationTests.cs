using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using LaoCai.SoftwareManagement.Api.Controllers;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LaoCai.SoftwareManagement.IntegrationTests;

public class DashboardApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DashboardApiIntegrationTests(CustomWebApplicationFactory factory)
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
    public async Task GetOverview_AsCoordinator_ShouldReturnKPIs_AndFinancialAmounts()
    {
        var client = CreateClientWithCookies();
        await LoginAsync(client, "coordinator");

        var response = await client.GetAsync("/api/v1/dashboard/overview");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        json.Should().NotBeNull();
        json!["totalSoftwareCount"]!.GetValue<int>().Should().BeGreaterThan(0);
        json["totalDeploymentsCount"]!.GetValue<int>().Should().BeGreaterThan(0);
        json["activeOrganizationsCount"]!.GetValue<int>().Should().BeGreaterThan(0);

        // Coordinator has contracts.read permission -> totalContractAmount is NOT null
        json["totalContractAmount"].Should().NotBeNull();
        json["currencyCode"]!.GetValue<string>().Should().Be("VND");
    }

    [Fact]
    public async Task GetOverview_AsViewer_WithoutContractPermission_ShouldHideFinancials()
    {
        var client = CreateClientWithCookies();
        await LoginAsync(client, "viewer_prov");

        var response = await client.GetAsync("/api/v1/dashboard/overview");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        json.Should().NotBeNull();

        // Viewer does not have contracts.read permission -> totalContractAmount is NULL (protected)
        json!["totalContractAmount"].Should().BeNull();
    }

    [Fact]
    public async Task GetOverview_WithHistoricalAsOfDate_ShouldCalculateHistoricalMetrics()
    {
        var client = CreateClientWithCookies();
        await LoginAsync(client, "coordinator");

        // Query historical state as of 2020-01-01 (before recent deployments)
        var response = await client.GetAsync("/api/v1/dashboard/overview?asOf=2020-01-01");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        json.Should().NotBeNull();
        json!["asOfDate"]!.GetValue<string>().Should().Be("2020-01-01");
    }

    [Fact]
    public async Task CoverageEligibility_GetAndSet_ShouldWorkCorrectly()
    {
        var client = CreateClientWithCookies();
        await LoginAsync(client, "coordinator");

        // 1. Get initial coverage eligibilities
        var getResponse = await client.GetAsync("/api/v1/dashboard/coverage-eligibility");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var items = await getResponse.Content.ReadFromJsonAsync<JsonArray>();
        items.Should().NotBeNull();

        // 2. Set new coverage eligibility
        var orgBaoThangId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var swIofficeId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

        var createReq = new CreateCoverageEligibilityRequest(
            orgBaoThangId,
            swIofficeId,
            new DateOnly(2026, 1, 1),
            new DateOnly(2027, 12, 31),
            true,
            "Đơn vị bắt buộc sử dụng hệ thống văn bản điện tử");

        var postResponse = await client.PostAsJsonAsync("/api/v1/dashboard/coverage-eligibility", createReq);
        postResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var postJson = await postResponse.Content.ReadFromJsonAsync<JsonObject>();
        postJson.Should().NotBeNull();
        postJson!["isEligible"]!.GetValue<bool>().Should().BeTrue();
    }
}
