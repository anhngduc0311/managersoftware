using FluentAssertions;
using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Application.Services;
using LaoCai.SoftwareManagement.Domain.Entities.Catalog;
using LaoCai.SoftwareManagement.Domain.Entities.Iam;
using LaoCai.SoftwareManagement.Domain.Entities.Organizations;
using LaoCai.SoftwareManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LaoCai.SoftwareManagement.Domain.Tests;

public class CatalogTests
{
    private readonly DbContextOptions<AppDbContext> _dbOptions;
    private readonly MockCurrentUserService _currentUserService = new();

    public CatalogTests()
    {
        _dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    private AppDbContext CreateDbContext(IDateTimeProvider dtProvider)
    {
        return new AppDbContext(_dbOptions, _currentUserService, dtProvider);
    }

    [Fact]
    public async Task IsSoftwareCodeUniqueAsync_WhenCodeExists_ShouldReturnFalse()
    {
        var dtProvider = new MockDateTimeProvider();
        using var context = CreateDbContext(dtProvider);
        var catalogService = new CatalogService(context, dtProvider);

        var catId = Guid.NewGuid();
        var vendorId = Guid.NewGuid();

        context.SoftwareCategories.Add(new SoftwareCategory { Id = catId, Code = "E_GOV", Name = "E-Gov" });
        context.Vendors.Add(new Vendor { Id = vendorId, Code = "VNPT", Name = "VNPT" });
        context.Software.Add(new Software
        {
            Id = Guid.NewGuid(),
            Code = "VNPT_IGATE",
            Name = "iGate",
            CategoryId = catId,
            VendorId = vendorId
        });
        await context.SaveChangesAsync();

        var isUnique = await catalogService.IsSoftwareCodeUniqueAsync("vnpt_igate");
        isUnique.Should().BeFalse();
    }

    [Fact]
    public async Task AcceptProposalAsync_WhenCreatingNewSoftware_ShouldCreateSoftwareAndMarkAccepted()
    {
        var dtProvider = new MockDateTimeProvider();
        using var context = CreateDbContext(dtProvider);
        var catalogService = new CatalogService(context, dtProvider);

        var orgId = Guid.NewGuid();
        var proposerId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();
        var catId = Guid.NewGuid();
        var vendorId = Guid.NewGuid();

        context.SoftwareCategories.Add(new SoftwareCategory { Id = catId, Code = "HEALTH", Name = "Health" });
        context.Vendors.Add(new Vendor { Id = vendorId, Code = "VIETTEL", Name = "Viettel" });
        context.Users.AddRange(
            new User { Id = proposerId, UserName = "proposer", DisplayName = "Proposer" },
            new User { Id = reviewerId, UserName = "reviewer", DisplayName = "Reviewer" }
        );
        context.Organizations.Add(new Organization { Id = orgId, Code = "BV_DK" });

        var proposal = new CatalogProposal
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            ProposedByUserId = proposerId,
            SoftwareName = "Phần mềm Quản lý Bệnh viện HIS",
            Description = "Hệ thống HIS",
            Status = "Pending",
            CreatedAt = dtProvider.UtcNow
        };
        context.CatalogProposals.Add(proposal);
        await context.SaveChangesAsync();

        var software = await catalogService.AcceptProposalAsync(
            proposal.Id,
            reviewerId,
            null,
            "VIETTEL_HIS",
            catId,
            vendorId);

        software.Should().NotBeNull();
        software.Code.Should().Be("VIETTEL_HIS");
        proposal.Status.Should().Be("Accepted");
        proposal.CreatedSoftwareId.Should().Be(software.Id);
        proposal.ReviewedByUserId.Should().Be(reviewerId);
    }

    [Fact]
    public async Task RejectProposalAsync_WhenRejected_ShouldMarkStatusWithReason()
    {
        var dtProvider = new MockDateTimeProvider();
        using var context = CreateDbContext(dtProvider);
        var catalogService = new CatalogService(context, dtProvider);

        var orgId = Guid.NewGuid();
        var proposerId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();

        context.Users.AddRange(
            new User { Id = proposerId, UserName = "proposer", DisplayName = "Proposer" },
            new User { Id = reviewerId, UserName = "reviewer", DisplayName = "Reviewer" }
        );
        context.Organizations.Add(new Organization { Id = orgId, Code = "BV_DK" });

        var proposal = new CatalogProposal
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            ProposedByUserId = proposerId,
            SoftwareName = "Phần mềm Trùng lặp",
            Status = "Pending",
            CreatedAt = dtProvider.UtcNow
        };
        context.CatalogProposals.Add(proposal);
        await context.SaveChangesAsync();

        await catalogService.RejectProposalAsync(proposal.Id, reviewerId, "Đã có phần mềm tương đương trong danh mục dùng chung.");

        proposal.Status.Should().Be("Rejected");
        proposal.RejectionReason.Should().Be("Đã có phần mềm tương đương trong danh mục dùng chung.");
        proposal.ReviewedByUserId.Should().Be(reviewerId);
    }
}
