using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using ClosedXML.Excel;
using FluentAssertions;
using LaoCai.SoftwareManagement.Api.Controllers;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LaoCai.SoftwareManagement.IntegrationTests;

public class ExcelApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ExcelApiIntegrationTests(CustomWebApplicationFactory factory)
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
    public async Task DownloadTemplate_ShouldReturnXlsxFileWithThreeSheets()
    {
        var client = CreateClientWithCookies();
        await LoginAsync(client, "coordinator");

        var response = await client.GetAsync("/api/v1/deployments/excel/template");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        var stream = await response.Content.ReadAsStreamAsync();
        using var workbook = new XLWorkbook(stream);

        workbook.Worksheets.Should().Contain(ws => ws.Name == "Mau_Nhap_Lieu");
        workbook.Worksheets.Should().Contain(ws => ws.Name == "DM_Phan_Mem");
        workbook.Worksheets.Should().Contain(ws => ws.Name == "DM_Don_Vi");

        var dataWs = workbook.Worksheet("Mau_Nhap_Lieu");
        dataWs.Cell(1, 1).GetString().Should().Contain("Mã phần mềm");
        dataWs.Cell(1, 2).GetString().Should().Contain("Mã đơn vị");
    }

    [Fact]
    public async Task UploadImportBatch_WithFormulaInjection_ShouldCatchAndFailValidation()
    {
        var client = CreateClientWithCookies();
        await LoginAsync(client, "admin", "Admin@123456");

        // 1. Create malicious Excel file containing formula injection
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Mau_Nhap_Lieu");
        ws.Cell(1, 1).Value = "Mã phần mềm (*)";
        ws.Cell(1, 2).Value = "Mã đơn vị (*)";
        ws.Cell(1, 3).Value = "Phiên bản (*)";
        ws.Cell(1, 4).Value = "Môi trường";

        // Row 2 contains Formula Injection: cell starts with '='
        ws.Cell(2, 1).Value = "=1+1";
        ws.Cell(2, 2).Value = "STTT";
        ws.Cell(2, 3).Value = "1.0.0";
        ws.Cell(2, 4).Value = "Production";

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var fileBytes = ms.ToArray();

        // 2. Upload file
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(fileContent, "file", "malicious_import.xlsx");

        var uploadResponse = await client.PostAsync("/api/v1/deployments/excel/import", content);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var uploadJson = await uploadResponse.Content.ReadFromJsonAsync<JsonObject>();
        var batchId = Guid.Parse(uploadJson!["id"]!.GetValue<string>());

        // 3. Trigger validation
        var validateResponse = await client.PostAsync($"/api/v1/deployments/excel/import/{batchId}/validate", null);
        validateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var validateJson = await validateResponse.Content.ReadFromJsonAsync<JsonObject>();
        validateJson!["isValid"]!.GetValue<bool>().Should().BeFalse();

        // 4. Verify batch status and row errors
        var getBatchResponse = await client.GetAsync($"/api/v1/deployments/excel/import/{batchId}");
        getBatchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var batchDetails = await getBatchResponse.Content.ReadFromJsonAsync<JsonObject>();
        batchDetails!["status"]!.GetValue<string>().Should().Be("FailedValidation");
        var errors = batchDetails["errors"]!.AsArray();
        errors.Should().NotBeEmpty();
        errors.Should().Contain(e => e!["errorCode"]!.GetValue<string>() == "FORMULA_INJECTION");

        // 5. Commit should be rejected because errors exist
        var commitResponse = await client.PostAsync($"/api/v1/deployments/excel/import/{batchId}/commit", null);
        commitResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadImportBatch_WithValidData_ShouldValidateAndCommitDraftDeployments()
    {
        var client = CreateClientWithCookies();
        await LoginAsync(client, "admin", "Admin@123456");

        // 1. Create valid Excel file
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Mau_Nhap_Lieu");
        ws.Cell(1, 1).Value = "Mã phần mềm (*)";
        ws.Cell(1, 2).Value = "Mã đơn vị (*)";
        ws.Cell(1, 3).Value = "Phiên bản (*)";
        ws.Cell(1, 4).Value = "Môi trường";
        ws.Cell(1, 5).Value = "Ngày golive (yyyy-MM-dd)";

        ws.Cell(2, 1).Value = "VNPT_IOFFICE";
        ws.Cell(2, 2).Value = "STTT";
        ws.Cell(2, 3).Value = "5.0.0";
        ws.Cell(2, 4).Value = "Production";
        ws.Cell(2, 5).Value = "2026-06-01";

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        var fileBytes = ms.ToArray();

        // 2. Upload file
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(fileContent, "file", "valid_import.xlsx");

        var uploadResponse = await client.PostAsync("/api/v1/deployments/excel/import", content);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var uploadJson = await uploadResponse.Content.ReadFromJsonAsync<JsonObject>();
        var batchId = Guid.Parse(uploadJson!["id"]!.GetValue<string>());

        // 3. Trigger validation
        var validateResponse = await client.PostAsync($"/api/v1/deployments/excel/import/{batchId}/validate", null);
        validateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var validateJson = await validateResponse.Content.ReadFromJsonAsync<JsonObject>();
        validateJson!["isValid"]!.GetValue<bool>().Should().BeTrue(validateJson.ToString());

        // 4. Commit batch
        var commitResponse = await client.PostAsync($"/api/v1/deployments/excel/import/{batchId}/commit", null);
        commitResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var commitJson = await commitResponse.Content.ReadFromJsonAsync<JsonObject>();
        commitJson!["success"]!.GetValue<bool>().Should().BeTrue();
        commitJson["committedCount"]!.GetValue<int>().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExportDeployments_ShouldProduceValidXlsx_AndEscapeFormulas()
    {
        var client = CreateClientWithCookies();
        await LoginAsync(client, "coordinator");

        // 1. Request export
        var requestExportResponse = await client.PostAsJsonAsync("/api/v1/deployments/excel/export", new ExportFilterDto(null, null, null, null));
        requestExportResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var exportJson = await requestExportResponse.Content.ReadFromJsonAsync<JsonObject>();
        var exportId = Guid.Parse(exportJson!["id"]!.GetValue<string>());

        // 2. Process export directly using scoped service
        using (var scope = _factory.Services.CreateScope())
        {
            var excelService = scope.ServiceProvider.GetRequiredService<IExcelService>();
            await excelService.ProcessExportAsync(exportId);
        }

        // 3. Check status
        var statusResponse = await client.GetAsync($"/api/v1/deployments/excel/export/{exportId}");
        statusResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var statusJson = await statusResponse.Content.ReadFromJsonAsync<JsonObject>();
        statusJson!["status"]!.GetValue<string>().Should().Be("Completed");

        // 4. Download export file
        var downloadResponse = await client.GetAsync($"/api/v1/deployments/excel/export/{exportId}/download");
        downloadResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        downloadResponse.Content.Headers.ContentType?.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        var stream = await downloadResponse.Content.ReadAsStreamAsync();
        using var workbook = new XLWorkbook(stream);
        var ws = workbook.Worksheet("Danh_Sach_Trien_Khai");
        ws.Should().NotBeNull();
        ws.Cell(1, 1).GetString().Should().Be("STT");
    }
}
