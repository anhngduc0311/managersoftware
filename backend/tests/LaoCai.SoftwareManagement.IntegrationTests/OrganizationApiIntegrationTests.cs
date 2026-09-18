using System.Net;
using System.Net.Http.Json;
using LaoCai.SoftwareManagement.Api.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LaoCai.SoftwareManagement.IntegrationTests;

public class OrganizationApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _adminClient;

    public OrganizationApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _adminClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
    }

    private async Task EnsureLoggedInAsync()
    {
        await _adminClient.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin", "Admin@123456"));
    }

    [Fact]
    public async Task GetOrganizations_ShouldReturnSeedData()
    {
        await EnsureLoggedInAsync();

        var response = await _adminClient.GetAsync("/api/v1/organizations");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("STTT");
        content.Should().Contain("UBND_BAOTHANG");
    }

    [Fact]
    public async Task GetOrganizationTree_ShouldReturnHierarchy()
    {
        await EnsureLoggedInAsync();

        var response = await _adminClient.GetAsync("/api/v1/organizations/tree");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("STTT");
    }

    [Fact]
    public async Task CreateOrganization_WithDuplicateCode_ShouldReturnConflict()
    {
        await EnsureLoggedInAsync();

        var request = new CreateOrganizationRequest(
            "STTT", // already seeded
            "Sở Thông tin Lào Cai Trùng",
            null,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            null);

        var response = await _adminClient.PostAsJsonAsync("/api/v1/organizations", request);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateOrganization_Valid_ShouldReturnCreated()
    {
        await EnsureLoggedInAsync();

        var newCode = $"ORG_{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
        var request = new CreateOrganizationRequest(
            newCode,
            "Trung tâm Chuyển đổi số tỉnh Lào Cai",
            null,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            null);

        var response = await _adminClient.PostAsJsonAsync("/api/v1/organizations", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
