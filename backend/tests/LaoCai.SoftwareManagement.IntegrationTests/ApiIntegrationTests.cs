using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace LaoCai.SoftwareManagement.IntegrationTests;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCsrfToken_ShouldReturnOk_AndSetXsrfCookie_AndCorrelationIdHeader()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/auth/csrf");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey("X-Correlation-ID");

        var setCookieHeaders = response.Headers.GetValues("Set-Cookie");
        setCookieHeaders.Should().Contain(c => c.StartsWith("XSRF-TOKEN="));
    }

    [Fact]
    public async Task GetCurrentUser_ShouldReturnOk_WithExpectedPermissions()
    {
        // Act
        var response = await _client.GetAsync("/api/v1/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("SystemAdmin");
        content.Should().Contain("access.manage");
    }
}
