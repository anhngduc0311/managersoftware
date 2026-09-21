using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using LaoCai.SoftwareManagement.Api.Controllers;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Application.Services;
using LaoCai.SoftwareManagement.Domain.Entities.Audit;
using LaoCai.SoftwareManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LaoCai.SoftwareManagement.IntegrationTests;

public class AuditApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuditApiIntegrationTests(CustomWebApplicationFactory factory)
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
    public async Task GetAuditLogs_WithoutPermission_ShouldReturn403Forbidden()
    {
        var client = CreateClientWithCookies();
        // editor_baothang does not have audit.read
        await LoginAsync(client, "editor_baothang");

        var response = await client.GetAsync("/api/v1/audit-logs");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetAuditLogs_AsAuditor_ShouldReturnAuditTrail()
    {
        var client = CreateClientWithCookies();
        await LoginAsync(client, "auditor");

        var response = await client.GetAsync("/api/v1/audit-logs");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        json.Should().NotBeNull();
        json!.ContainsKey("items").Should().BeTrue();
        json["items"]!.AsArray().Should().NotBeNull();
    }

    [Fact]
    public void AuditService_ScrubJson_ShouldMaskSensitiveKeys()
    {
        var rawJson = "{\"UserName\":\"test_user\",\"PasswordHash\":\"$2a$11$secretHash123\",\"SecurityStamp\":\"stamp-xyz-123\",\"Secret\":\"superSecretKey\"}";
        var scrubbed = AuditService.ScrubJson(rawJson);

        scrubbed.Should().NotBeNull();
        scrubbed.Should().NotContain("$2a$11$secretHash123");
        scrubbed.Should().NotContain("stamp-xyz-123");
        scrubbed.Should().NotContain("superSecretKey");
        scrubbed.Should().Contain("\"PasswordHash\":\"***\"");
        scrubbed.Should().Contain("\"SecurityStamp\":\"***\"");
        scrubbed.Should().Contain("\"Secret\":\"***\"");
        scrubbed.Should().Contain("\"UserName\":\"test_user\"");
    }

    [Fact]
    public async Task AuditLog_AppendOnly_ShouldThrowOnModificationOrDeletion()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Add a new audit log
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            Action = "TestCreate",
            EntityType = "TestEntity",
            EntityId = "123",
            OccurredAt = DateTime.UtcNow
        };
        context.AuditLogs.Add(log);
        await context.SaveChangesAsync();

        // Attempt to modify the audit log
        log.Action = "ModifiedAction";
        var actModify = async () => await context.SaveChangesAsync();
        await actModify.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Nhật ký kiểm toán (AuditLog) là dữ liệu chỉ ghi (Append-Only)*");
    }
}
