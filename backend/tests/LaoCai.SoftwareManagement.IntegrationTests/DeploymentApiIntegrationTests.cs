using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using LaoCai.SoftwareManagement.Api.Controllers;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LaoCai.SoftwareManagement.IntegrationTests;

public class DeploymentApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DeploymentApiIntegrationTests(CustomWebApplicationFactory factory)
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
    public async Task GetDeployments_ShouldReturnSeededDeployments()
    {
        var client = CreateClientWithCookies();
        await LoginAsync(client, "editor_baothang");

        var response = await client.GetAsync("/api/v1/deployments");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("VNPT_IOFFICE");
    }

    [Fact]
    public async Task CreateDeployment_Submit_SelfApproveBlocked_ApproveByOther_Lifecycle()
    {
        var editorClient = CreateClientWithCookies();
        await LoginAsync(editorClient, "editor_baothang");

        // 1. Get Software and Organizations
        var softwareResp = await editorClient.GetAsync("/api/v1/software");
        softwareResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var softwareJson = await softwareResp.Content.ReadFromJsonAsync<JsonObject>();
        var softwareArray = softwareJson!["items"]!.AsArray();
        var softwareId = Guid.Parse(softwareArray[0]!["id"]!.ToString());

        var orgsResp = await editorClient.GetAsync("/api/v1/organizations");
        orgsResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var orgsJson = await orgsResp.Content.ReadFromJsonAsync<JsonObject>();
        var orgsArray = orgsJson!["items"]!.AsArray();
        var orgBaoThang = orgsArray.First(o => o!["code"]!.ToString() == "UBND_BAOTHANG");
        var orgId = Guid.Parse(orgBaoThang!["id"]!.ToString());

        // Get Releases for software
        var releasesResp = await editorClient.GetAsync($"/api/v1/software/{softwareId}/releases");
        releasesResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var releases = await releasesResp.Content.ReadFromJsonAsync<JsonArray>();
        var releaseId = Guid.Parse(releases![0]!["id"]!.ToString());

        // Get Current User (editor)
        var meResp = await editorClient.GetAsync("/api/v1/auth/me");
        meResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var meJson = await meResp.Content.ReadFromJsonAsync<JsonObject>();
        var editorUserId = Guid.Parse(meJson!["id"]!.ToString());

        // 2. Create new Deployment
        var uniqueInstanceKey = $"test_{Guid.NewGuid().ToString("N")[..6]}";
        var createDto = new CreateDeploymentDto(
            softwareId,
            orgId,
            "Development",
            uniqueInstanceKey,
            releaseId,
            "Active",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 2, 1),
            editorUserId);

        var createResp = await editorClient.PostAsJsonAsync("/api/v1/deployments", createDto);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdDep = await createResp.Content.ReadFromJsonAsync<JsonObject>();
        var deploymentId = Guid.Parse(createdDep!["id"]!.ToString());
        var rev1 = createdDep["activeRevision"]!.AsObject();
        var rev1Id = Guid.Parse(rev1["id"]!.ToString());
        var rev1Version = long.Parse(rev1["version"]!.ToString());

        // 3. Submit Revision
        using var submitMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/deployment-revisions/{rev1Id}/submit");
        submitMsg.Headers.Add("If-Match", $"\"{rev1Version}\"");
        var submitResp = await editorClient.SendAsync(submitMsg);
        submitResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var submittedRev = await submitResp.Content.ReadFromJsonAsync<JsonObject>();
        var submittedVersion = long.Parse(submittedRev!["version"]!.ToString());
        submittedRev["workflowStatus"]!.ToString().Should().Be("Submitted");

        // 4. Anti-Self-Approval: Editor tries to approve own submission -> Must fail with 403 Forbidden!
        using var selfApproveMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/deployment-revisions/{rev1Id}/approve");
        selfApproveMsg.Headers.Add("If-Match", $"\"{submittedVersion}\"");
        var selfApproveResp = await editorClient.SendAsync(selfApproveMsg);
        selfApproveResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 5. Approver logs in and approves
        var approverClient = CreateClientWithCookies();
        await LoginAsync(approverClient, "approver_baothang");

        using var approveMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/deployment-revisions/{rev1Id}/approve");
        approveMsg.Headers.Add("If-Match", $"\"{submittedVersion}\"");
        var approveResp = await approverClient.SendAsync(approveMsg);
        approveResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var approvedRev = await approveResp.Content.ReadFromJsonAsync<JsonObject>();
        approvedRev!["workflowStatus"]!.ToString().Should().Be("Approved");

        // Verify deployment's currentApprovedRevisionId is updated
        var checkDepResp = await approverClient.GetAsync($"/api/v1/deployments/{deploymentId}");
        checkDepResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var checkDepJson = await checkDepResp.Content.ReadFromJsonAsync<JsonObject>();
        checkDepJson!["currentApprovedRevisionId"]!.ToString().Should().Be(rev1Id.ToString());
    }
}
