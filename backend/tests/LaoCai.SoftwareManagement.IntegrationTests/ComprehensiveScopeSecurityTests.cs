using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using LaoCai.SoftwareManagement.Api.Controllers;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LaoCai.SoftwareManagement.IntegrationTests;

public class ComprehensiveScopeSecurityTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ComprehensiveScopeSecurityTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string username, string password = "User@123456")
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });

        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, password));
        loginRes.StatusCode.Should().Be(HttpStatusCode.OK);
        return client;
    }

    [Fact]
    public async Task HealthProbes_LiveAndReady_ShouldReturn200Ok()
    {
        var client = _factory.CreateClient();

        var liveRes = await client.GetAsync("/healthz/live");
        liveRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var liveBody = await liveRes.Content.ReadAsStringAsync();
        liveBody.Should().Contain("Healthy");

        var readyRes = await client.GetAsync("/healthz/ready");
        readyRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var readyBody = await readyRes.Content.ReadAsStringAsync();
        readyBody.Should().Contain("Healthy");
    }

    [Fact]
    public async Task ScopeIsolation_UserAtBaoThang_CannotViewDeploymentsOutsideScope()
    {
        // editor_baothang only has scope at Huyện Bảo Thắng
        var client = await CreateAuthenticatedClientAsync("editor_baothang", "User@123456");

        var listRes = await client.GetAsync("/api/v1/deployments");
        listRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await listRes.Content.ReadFromJsonAsync<JsonObject>();
        var items = json!["items"]!.AsArray();
        
        // All returned deployments must belong to Bao Thang organization
        foreach (var item in items)
        {
            item!["organizationCode"]!.ToString().Should().Be("UBND_BAOTHANG");
        }
    }

    [Fact]
    public async Task ScopeIsolation_UserWithoutContractsRead_HidesFinancialInformation()
    {
        // Login as editor_baothang who has deployments.read but NOT contracts.read
        var client = await CreateAuthenticatedClientAsync("editor_baothang", "User@123456");

        var response = await client.GetAsync("/api/v1/dashboard/overview");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        // Financial summary must be null / hidden
        content.Should().Contain("\"totalContractAmount\":null");
    }

    [Fact]
    public async Task SessionRevocation_WhenSecurityStampRotated_SubsequentRequestsAreRejected()
    {
        var client = await CreateAuthenticatedClientAsync("admin", "Admin@123456");

        // Request succeeds initially
        var meRes = await client.GetAsync("/api/v1/auth/me");
        meRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // Rotate security stamp in DB (simulating password change or session revocation)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var adminUser = await db.Users.FirstAsync(u => u.UserName == "admin");
            adminUser.SecurityStamp = Guid.NewGuid().ToString("N");
            await db.SaveChangesAsync();
        }

        // Next request with same cookie must be rejected
        var afterRes = await client.GetAsync("/api/v1/auth/me");
        afterRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
