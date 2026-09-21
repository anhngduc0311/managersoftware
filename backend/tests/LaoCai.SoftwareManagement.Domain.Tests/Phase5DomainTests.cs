using FluentAssertions;
using LaoCai.SoftwareManagement.Domain.Entities.Reports;
using Xunit;

namespace LaoCai.SoftwareManagement.Domain.Tests;

public class Phase5DomainTests
{
    [Fact]
    public void CoverageEligibility_Validation_ShouldThrow_WhenDatesAreInvalid()
    {
        var eligibility = new CoverageEligibility
        {
            OrganizationId = Guid.NewGuid(),
            SoftwareId = Guid.NewGuid(),
            ValidFrom = new DateOnly(2026, 6, 1),
            ValidTo = new DateOnly(2026, 5, 1) // Invalid: ValidTo < ValidFrom
        };

        var act = () => eligibility.Validate();
        act.Should().Throw<InvalidOperationException>().WithMessage("*không được trước ngày bắt đầu*");
    }

    [Fact]
    public void CoverageEligibility_Validation_ShouldSucceed_WhenValid()
    {
        var eligibility = new CoverageEligibility
        {
            OrganizationId = Guid.NewGuid(),
            SoftwareId = Guid.NewGuid(),
            ValidFrom = new DateOnly(2026, 1, 1),
            ValidTo = new DateOnly(2026, 12, 31),
            IsEligible = true
        };

        var act = () => eligibility.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void ImportBatch_Validation_ShouldThrow_WhenRowsExceed5000()
    {
        var batch = new ImportBatch
        {
            DocumentId = Guid.NewGuid(),
            RequestedByUserId = Guid.NewGuid(),
            TotalRows = 5001 // Limit is 5000 per FR-10
        };

        var act = () => batch.Validate();
        act.Should().Throw<InvalidOperationException>().WithMessage("*vượt quá giới hạn tối đa*");
    }

    [Fact]
    public void ImportBatch_Validation_ShouldSucceed_Within5000Rows()
    {
        var batch = new ImportBatch
        {
            DocumentId = Guid.NewGuid(),
            RequestedByUserId = Guid.NewGuid(),
            TotalRows = 5000
        };

        var act = () => batch.Validate();
        act.Should().NotThrow();
    }

    [Fact]
    public void ExportRequest_Validation_ShouldThrow_WhenExpiresAtIsInvalid()
    {
        var req = new ExportRequest
        {
            ExportType = "Deployments",
            RequestedByUserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-10) // Invalid
        };

        var act = () => req.Validate();
        act.Should().Throw<InvalidOperationException>().WithMessage("*Thời hạn tệp xuất (TTL) phải lớn hơn*");
    }

    [Fact]
    public void ExportRequest_Validation_ShouldSucceed_WhenValid()
    {
        var req = new ExportRequest
        {
            ExportType = "Deployments",
            RequestedByUserId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        var act = () => req.Validate();
        act.Should().NotThrow();
    }
}
