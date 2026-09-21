using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LaoCai.SoftwareManagement.Api.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LaoCai.SoftwareManagement.IntegrationTests;

public class PerformanceBenchmarkTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public PerformanceBenchmarkTests(CustomWebApplicationFactory factory)
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

    private async Task LoginAsync(HttpClient client, string username, string password = "Admin@123456")
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, password));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Benchmark_DeploymentSearchAndDashboard_MeetsNfr04PerformanceRequirements()
    {
        var client = CreateClientWithCookies();
        await LoginAsync(client, "admin", "Admin@123456");

        // 1. Measure search deployments latency across 30 requests
        var searchLatencies = new List<long>();
        var swWatch = new Stopwatch();

        for (int i = 0; i < 30; i++)
        {
            swWatch.Restart();
            var res = await client.GetAsync("/api/v1/deployments?page=1&pageSize=20");
            swWatch.Stop();

            res.StatusCode.Should().Be(HttpStatusCode.OK);
            searchLatencies.Add(swWatch.ElapsedMilliseconds);
        }

        // Calculate p95 search latency
        searchLatencies.Sort();
        var p95Index = (int)Math.Ceiling(searchLatencies.Count * 0.95) - 1;
        var p95SearchMs = searchLatencies[p95Index];

        // NFR-04 requirement: p95 search latency < 500ms
        p95SearchMs.Should().BeLessThan(500, "NFR-04 specifies search operations must complete under 500ms at p95.");

        // 2. Measure Dashboard calculation response time across 10 requests
        var dashLatencies = new List<long>();
        for (int i = 0; i < 10; i++)
        {
            swWatch.Restart();
            var res = await client.GetAsync("/api/v1/dashboard/overview");
            swWatch.Stop();

            res.StatusCode.Should().Be(HttpStatusCode.OK);
            dashLatencies.Add(swWatch.ElapsedMilliseconds);
        }

        dashLatencies.Sort();
        var p95DashIndex = (int)Math.Ceiling(dashLatencies.Count * 0.95) - 1;
        var p95DashMs = dashLatencies[p95DashIndex];

        // NFR-04 requirement: dashboard aggregation < 1000ms
        p95DashMs.Should().BeLessThan(1000, "NFR-04 specifies dashboard reporting must complete under 1000ms at p95.");
    }
}
