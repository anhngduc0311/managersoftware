using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using FluentAssertions;
using LaoCai.SoftwareManagement.Api.Controllers;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Worker.Handlers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LaoCai.SoftwareManagement.IntegrationTests;

public class DocumentApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public DocumentApiIntegrationTests(CustomWebApplicationFactory factory)
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
    public async Task Upload_Scan_Download_Lifecycle_ShouldEnforceSecurityRules()
    {
        var client = CreateClientWithCookies();
        await LoginAsync(client, "coordinator");

        // 1. Get Contract Id
        var contractsResp = await client.GetAsync("/api/v1/contracts");
        var json = await contractsResp.Content.ReadFromJsonAsync<JsonObject>();
        var contract = json!["items"]!.AsArray()[0]!.AsObject();
        var contractId = Guid.Parse(contract["id"]!.ToString());

        // 2. Upload valid PDF
        using var form = new MultipartFormDataContent();
        var pdfBytes = Encoding.ASCII.GetBytes("%PDF-1.5 test document content for contract integration test");
        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
        form.Add(fileContent, "file", "contract_appendix.pdf");
        form.Add(new StringContent("Contract"), "entityType");
        form.Add(new StringContent(contractId.ToString()), "entityId");

        var uploadResp = await client.PostAsync("/api/v1/documents/upload", form);
        uploadResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var uploadJson = await uploadResp.Content.ReadFromJsonAsync<JsonObject>();
        var documentId = Guid.Parse(uploadJson!["documentId"]!.ToString());
        var attachmentId = Guid.Parse(uploadJson!["attachmentId"]!.ToString());
        uploadJson["scanStatus"]!.ToString().Should().Be("Pending");

        // 3. Attempt download while Pending => 403 Forbidden
        var earlyDownloadResp = await client.GetAsync($"/api/v1/documents/{documentId}/download");
        earlyDownloadResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        // 4. Run Scanner Handler directly to simulate worker scanning
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
            var scanner = scope.ServiceProvider.GetRequiredService<IFileScanner>();
            var handler = new DocumentScanJobHandler(db, storage, scanner, NullLogger<DocumentScanJobHandler>.Instance);

            var payload = $"{{\"documentId\": \"{documentId}\"}}";
            await handler.HandleAsync(payload, CancellationToken.None);
        }

        // 5. Download after Clean => 200 OK with security header
        var cleanDownloadResp = await client.GetAsync($"/api/v1/documents/{documentId}/download");
        cleanDownloadResp.StatusCode.Should().Be(HttpStatusCode.OK);
        cleanDownloadResp.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");

        // 6. Unlink attachment => 204 NoContent
        var unlinkResp = await client.DeleteAsync($"/api/v1/documents/attachments/{attachmentId}");
        unlinkResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}
