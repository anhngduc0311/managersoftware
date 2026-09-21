using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using LaoCai.SoftwareManagement.Api.Controllers;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LaoCai.SoftwareManagement.IntegrationTests;

public class BusinessWorkflowE2ETests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public BusinessWorkflowE2ETests(CustomWebApplicationFactory factory)
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
    public async Task FullDeploymentLifecycle_Draft_Submit_Reject_Reopen_Approve_ReflectedInDashboard()
    {
        var editorClient = CreateClientWithCookies();
        await LoginAsync(editorClient, "editor_baothang");

        var approverClient = CreateClientWithCookies();
        await LoginAsync(approverClient, "approver_baothang");

        // 1. Get Software, Organization, and Release
        var softwareResp = await editorClient.GetAsync("/api/v1/software");
        softwareResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var softwareJson = await softwareResp.Content.ReadFromJsonAsync<JsonObject>();
        var softwareId = Guid.Parse(softwareJson!["items"]!.AsArray()[0]!["id"]!.ToString());

        var orgResp = await editorClient.GetAsync("/api/v1/organizations");
        orgResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var orgJson = await orgResp.Content.ReadFromJsonAsync<JsonObject>();
        var orgBaoThang = orgJson!["items"]!.AsArray().First(o => o!["code"]!.ToString() == "UBND_BAOTHANG");
        var orgId = Guid.Parse(orgBaoThang!["id"]!.ToString());

        var releasesResp = await editorClient.GetAsync($"/api/v1/software/{softwareId}/releases");
        releasesResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var releases = await releasesResp.Content.ReadFromJsonAsync<JsonArray>();
        var releaseId = Guid.Parse(releases![0]!["id"]!.ToString());

        var meResp = await editorClient.GetAsync("/api/v1/auth/me");
        meResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var meJson = await meResp.Content.ReadFromJsonAsync<JsonObject>();
        var editorUserId = Guid.Parse(meJson!["id"]!.ToString());

        // 2. Editor creates a draft deployment
        var uniqueInstanceKey = $"e2e_{Guid.NewGuid():N}"[..8];
        var createDto = new CreateDeploymentDto(
            softwareId,
            orgId,
            "Production",
            uniqueInstanceKey,
            releaseId,
            "Active",
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 2, 1),
            editorUserId
        );
        var createResp = await editorClient.PostAsJsonAsync("/api/v1/deployments", createDto);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdJson = await createResp.Content.ReadFromJsonAsync<JsonObject>();
        var depId = Guid.Parse(createdJson!["id"]!.ToString());
        var rev = createdJson["activeRevision"]!.AsObject();
        var revId = Guid.Parse(rev["id"]!.ToString());
        var revVersion = long.Parse(rev["version"]!.ToString());

        // 3. Editor submits draft revision for approval
        using var submitMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/deployment-revisions/{revId}/submit");
        submitMsg.Headers.Add("If-Match", $"\"{revVersion}\"");
        var submitResp = await editorClient.SendAsync(submitMsg);
        submitResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var submittedRev = await submitResp.Content.ReadFromJsonAsync<JsonObject>();
        var submittedVersion = long.Parse(submittedRev!["version"]!.ToString());
        submittedRev["workflowStatus"]!.ToString().Should().Be("Submitted");

        // 4. Editor attempts to self-approve -> Forbidden
        using var selfApproveMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/deployment-revisions/{revId}/approve");
        selfApproveMsg.Headers.Add("If-Match", $"\"{submittedVersion}\"");
        var selfApproveResp = await editorClient.SendAsync(selfApproveMsg);
        selfApproveResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 5. Approver rejects revision with reason
        using var rejectMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/deployment-revisions/{revId}/reject")
        {
            Content = JsonContent.Create(new RejectRevisionDto("Cần bổ sung quyết định phê duyệt chủ trương"))
        };
        rejectMsg.Headers.Add("If-Match", $"\"{submittedVersion}\"");
        var rejectResp = await approverClient.SendAsync(rejectMsg);
        rejectResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var rejectedRev = await rejectResp.Content.ReadFromJsonAsync<JsonObject>();
        var rejectedVersion = long.Parse(rejectedRev!["version"]!.ToString());
        rejectedRev["workflowStatus"]!.ToString().Should().Be("Rejected");

        // 6. Editor reopens rejected revision back to Draft
        using var reopenMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/deployment-revisions/{revId}/reopen");
        reopenMsg.Headers.Add("If-Match", $"\"{rejectedVersion}\"");
        var reopenResp = await editorClient.SendAsync(reopenMsg);
        reopenResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var reopenedRev = await reopenResp.Content.ReadFromJsonAsync<JsonObject>();
        var reopenedVersion = long.Parse(reopenedRev!["version"]!.ToString());
        reopenedRev["workflowStatus"]!.ToString().Should().Be("Draft");

        // 7. Editor resubmits and Approver approves
        using var resubmitMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/deployment-revisions/{revId}/submit");
        resubmitMsg.Headers.Add("If-Match", $"\"{reopenedVersion}\"");
        var resubmitResp = await editorClient.SendAsync(resubmitMsg);
        resubmitResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var resubmittedRev = await resubmitResp.Content.ReadFromJsonAsync<JsonObject>();
        var resubmittedVersion = long.Parse(resubmittedRev!["version"]!.ToString());

        using var approveMsg = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/deployment-revisions/{revId}/approve");
        approveMsg.Headers.Add("If-Match", $"\"{resubmittedVersion}\"");
        var approveResp = await approverClient.SendAsync(approveMsg);
        approveResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var approvedRev = await approveResp.Content.ReadFromJsonAsync<JsonObject>();
        approvedRev!["workflowStatus"]!.ToString().Should().Be("Approved");

        // 8. Verify the approved deployment is reflected in Dashboard
        var dashResp = await approverClient.GetAsync("/api/v1/dashboard/overview");
        dashResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dashJson = await dashResp.Content.ReadFromJsonAsync<JsonObject>();
        dashJson!["totalDeploymentsCount"]!.GetValue<int>().Should().BeGreaterThan(0);
    }
}
