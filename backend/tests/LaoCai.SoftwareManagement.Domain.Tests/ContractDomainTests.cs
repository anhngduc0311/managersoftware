using System.ComponentModel.DataAnnotations;
using System.Text;
using FluentAssertions;
using LaoCai.SoftwareManagement.Domain.Entities.Contracts;
using LaoCai.SoftwareManagement.Domain.Entities.Documents;
using LaoCai.SoftwareManagement.Infrastructure.Services;
using Xunit;

namespace LaoCai.SoftwareManagement.Domain.Tests;

public class ContractDomainTests
{
    [Fact]
    public void Contract_Validation_ShouldThrow_WhenDatesAreInvalid()
    {
        var contract = new Contract
        {
            ContractNo = "HD-01",
            StartDate = new DateOnly(2026, 5, 1),
            EndDate = new DateOnly(2026, 4, 1), // Invalid: EndDate < StartDate
            TotalAmount = 1000000
        };

        var act = () => contract.Validate();
        act.Should().Throw<ValidationException>().WithMessage("*không được trước ngày bắt đầu*");
    }

    [Fact]
    public void Contract_Validation_ShouldThrow_WhenTotalAmountIsNegative()
    {
        var contract = new Contract
        {
            ContractNo = "HD-01",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
            TotalAmount = -500
        };

        var act = () => contract.Validate();
        act.Should().Throw<ValidationException>().WithMessage("*không được âm*");
    }

    [Fact]
    public void Contract_Validation_ShouldThrow_WhenMaintenanceDatesAreInvalid()
    {
        var contract = new Contract
        {
            ContractNo = "HD-01",
            StartDate = new DateOnly(2026, 1, 1),
            EndDate = new DateOnly(2026, 12, 31),
            TotalAmount = 500000,
            MaintenanceStartDate = new DateOnly(2026, 6, 1),
            MaintenanceEndDate = new DateOnly(2026, 5, 1) // Invalid
        };

        var act = () => contract.Validate();
        act.Should().Throw<ValidationException>().WithMessage("*kết thúc bảo trì*");
    }

    [Fact]
    public void LicenseEntitlement_Validation_SeatMustHaveQuantity()
    {
        var ent = new LicenseEntitlement
        {
            LicenseType = "Seat",
            Quantity = null, // Invalid for Seat
            ValidFrom = new DateOnly(2026, 1, 1),
            ValidTo = new DateOnly(2026, 12, 31)
        };

        var act = () => ent.Validate();
        act.Should().Throw<ValidationException>().WithMessage("*phải có số lượng*");
    }

    [Fact]
    public void LicenseEntitlement_Validation_UnlimitedClearsQuantity()
    {
        var ent = new LicenseEntitlement
        {
            LicenseType = "Unlimited",
            Quantity = 100,
            ValidFrom = new DateOnly(2026, 1, 1),
            ValidTo = new DateOnly(2026, 12, 31)
        };

        ent.Validate();
        ent.Quantity.Should().BeNull();
    }

    [Fact]
    public void LicenseAllocation_Validation_QuantityMustBePositive()
    {
        var alloc = new LicenseAllocation
        {
            Quantity = 0
        };

        var act = () => alloc.Validate();
        act.Should().Throw<ValidationException>().WithMessage("*phải lớn hơn 0*");
    }

    [Fact]
    public void Document_Validation_ShouldEnforceSizeLimit()
    {
        var doc = new Document
        {
            StorageKey = "test.pdf",
            OriginalName = "test.pdf",
            ContentType = "application/pdf",
            ChecksumSha256 = "dummy",
            SizeBytes = 25 * 1024 * 1024 // 25 MB > 20 MB limit
        };

        var act = () => doc.Validate();
        act.Should().Throw<ValidationException>().WithMessage("*tối đa 20 MB*");
    }

    [Fact]
    public async Task RuleBasedFileScanner_ShouldDetectCleanPdf()
    {
        var scanner = new RuleBasedFileScanner();
        var pdfBytes = Encoding.ASCII.GetBytes("%PDF-1.4 sample pdf content for testing unit");
        using var stream = new MemoryStream(pdfBytes);

        var result = await scanner.ScanAsync(stream, "sample.pdf", "application/pdf");
        result.IsClean.Should().BeTrue();
    }

    [Fact]
    public async Task RuleBasedFileScanner_ShouldRejectFakePdfWithExeHeader()
    {
        var scanner = new RuleBasedFileScanner();
        // MZ header disguised as pdf
        var fakePdfBytes = new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00 };
        using var stream = new MemoryStream(fakePdfBytes);

        var result = await scanner.ScanAsync(stream, "virus.pdf", "application/pdf");
        result.IsClean.Should().BeFalse();
        result.Message.Should().Contain("thực thi");
    }

    [Fact]
    public async Task RuleBasedFileScanner_ShouldRejectDisallowedExtension()
    {
        var scanner = new RuleBasedFileScanner();
        var bytes = Encoding.UTF8.GetBytes("echo hello");
        using var stream = new MemoryStream(bytes);

        var result = await scanner.ScanAsync(stream, "script.bat", "application/x-bat");
        result.IsClean.Should().BeFalse();
        result.Message.Should().Contain("không được phép");
    }
}
