using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using LaoCai.SoftwareManagement.Api.Controllers;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LaoCai.SoftwareManagement.IntegrationTests;

public class WorkerNotificationIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public WorkerNotificationIntegrationTests(CustomWebApplicationFactory factory)
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
    public async Task NotificationsApi_GetNotifications_And_MarkAsRead_ShouldWork()
    {
        var client = CreateClientWithCookies();
        await LoginAsync(client, "approver_baothang");

        // Get notifications
        var notifsResp = await client.GetAsync("/api/v1/notifications");
        notifsResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var notifsJson = await notifsResp.Content.ReadFromJsonAsync<JsonObject>();
        var items = notifsJson!["items"]!.AsArray();
        items.Count.Should().BeGreaterThan(0);

        var firstNotifId = Guid.Parse(items[0]!["id"]!.ToString());

        // Mark single as read
        var readResp = await client.PostAsync($"/api/v1/notifications/{firstNotifId}/read", null);
        readResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Mark all as read
        var readAllResp = await client.PostAsync("/api/v1/notifications/read-all", null);
        readAllResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify unread count is 0
        var unreadCountResp = await client.GetAsync("/api/v1/notifications/unread-count");
        unreadCountResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var unreadJson = await unreadCountResp.Content.ReadFromJsonAsync<JsonObject>();
        unreadJson!["count"]!.GetValue<int>().Should().Be(0);
    }
}
