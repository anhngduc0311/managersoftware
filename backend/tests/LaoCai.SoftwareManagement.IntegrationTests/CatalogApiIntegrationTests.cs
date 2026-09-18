using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using LaoCai.SoftwareManagement.Api.Controllers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LaoCai.SoftwareManagement.IntegrationTests;

public class CatalogApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _catalogClient;

    public CatalogApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _catalogClient = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
    }

    private async Task EnsureLoggedInAsync()
    {
        await _catalogClient.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("catalog_mgr", "User@123456"));
    }

    [Fact]
    public async Task GetSoftware_ShouldReturnSeededSoftware()
    {
        await EnsureLoggedInAsync();

        var response = await _catalogClient.GetAsync("/api/v1/software");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("VNPT_IGATE");
        content.Should().Contain("VNPT_IOFFICE");
    }

    [Fact]
    public async Task CreateSoftware_WithCatalogManager_ShouldReturnCreated()
    {
        await EnsureLoggedInAsync();

        var categoriesResp = await _catalogClient.GetAsync("/api/v1/software-categories");
        categoriesResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var categories = await categoriesResp.Content.ReadFromJsonAsync<JsonArray>();
        var categoryId = Guid.Parse(categories![0]!["id"]!.ToString());

        var vendorsResp = await _catalogClient.GetAsync("/api/v1/vendors");
        vendorsResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var vendors = await vendorsResp.Content.ReadFromJsonAsync<JsonArray>();
        var vendorId = Guid.Parse(vendors![0]!["id"]!.ToString());

        var newCode = $"SW_{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
        var request = new CreateSoftwareRequest(
            newCode,
            "Phần mềm Quản lý Cây xanh Đô thị",
            categoryId,
            vendorId,
            "Mô tả phần mềm",
            "Active");

        var response = await _catalogClient.PostAsJsonAsync("/api/v1/software", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
