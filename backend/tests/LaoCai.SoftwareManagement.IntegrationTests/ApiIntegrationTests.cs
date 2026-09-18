using System.Net;
using System.Net.Http.Json;
using LaoCai.SoftwareManagement.Api.Controllers;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LaoCai.SoftwareManagement.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var descriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(AppDbContext)).ToList();

            foreach (var d in descriptors)
            {
                services.Remove(d);
            }

            var inMemoryOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(_dbName)
                .Options;

            services.AddSingleton(inMemoryOptions);
            services.AddScoped(sp => new AppDbContext(
                inMemoryOptions,
                sp.GetRequiredService<ICurrentUserService>(),
                sp.GetRequiredService<IDateTimeProvider>()));
            services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasherService>();
            var dt = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();
            DataSeeder.SeedAsync(db, hasher, dt).GetAwaiter().GetResult();
        });
    }
}

public class ApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true
        });
    }

    [Fact]
    public async Task GetCsrfToken_ShouldReturnOk_AndSetXsrfCookie_AndCorrelationIdHeader()
    {
        var response = await _client.GetAsync("/api/v1/auth/csrf");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Should().ContainKey("X-Correlation-ID");

        var setCookieHeaders = response.Headers.GetValues("Set-Cookie");
        setCookieHeaders.Should().Contain(c => c.StartsWith("XSRF-TOKEN="));
    }

    [Fact]
    public async Task GetCurrentUser_WhenUnauthenticated_ShouldReturn401()
    {
        var unauthenticatedClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false
        });

        var response = await unauthenticatedClient.GetAsync("/api/v1/auth/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnOk_AndAllowAccessToMe()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin", "Admin@123456"));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var meResponse = await _client.GetAsync("/api/v1/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await meResponse.Content.ReadAsStringAsync();
        content.Should().Contain("SystemAdmin");
        content.Should().Contain("access.manage");
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturn401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin", "WrongPassword!2026"));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
