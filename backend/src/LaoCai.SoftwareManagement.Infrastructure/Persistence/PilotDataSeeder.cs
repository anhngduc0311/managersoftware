using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Domain.Entities.Catalog;
using LaoCai.SoftwareManagement.Domain.Entities.Contracts;
using LaoCai.SoftwareManagement.Domain.Entities.Deployments;
using LaoCai.SoftwareManagement.Domain.Entities.Iam;
using LaoCai.SoftwareManagement.Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;

namespace LaoCai.SoftwareManagement.Infrastructure.Persistence;

public static class PilotDataSeeder
{
    public static async Task SeedPilotDataAsync(
        AppDbContext context,
        IPasswordHasherService passwordHasher,
        IDateTimeProvider dateTimeProvider)
    {
        var nowUtc = dateTimeProvider.UtcNow;

        // 1. Seed Provincial Departments & District People's Committees
        var pilotOrgs = new List<(string Code, string Name, string Level)>
        {
            ("STTT", "Sở Thông tin và Truyền thông tỉnh Lào Cai", "Department"),
            ("SYT", "Sở Y tế tỉnh Lào Cai", "Department"),
            ("SGDDT", "Sở Giáo dục và Đào tạo tỉnh Lào Cai", "Department"),
            ("SNV", "Sở Nội vụ tỉnh Lào Cai", "Department"),
            ("TP_LAOCAI", "Ủy ban nhân dân Thành phố Lào Cai", "District"),
            ("TX_SAPA", "Ủy ban nhân dân Thị xã Sa Pa", "District"),
            ("H_BAOTHANG", "Ủy ban nhân dân Huyện Bảo Thắng", "District"),
            ("H_BATXAT", "Ủy ban nhân dân Huyện Bát Xát", "District"),
            ("H_VANBAN", "Ủy ban nhân dân Huyện Văn Bàn", "District")
        };

        var orgMap = new Dictionary<string, Organization>(StringComparer.OrdinalIgnoreCase);

        foreach (var po in pilotOrgs)
        {
            var existing = await context.Organizations.FirstOrDefaultAsync(o => o.Code == po.Code);
            if (existing == null)
            {
                var orgId = Guid.NewGuid();
                var org = new Organization
                {
                    Id = orgId,
                    Code = po.Code,
                    IsActive = true,
                    CreatedAt = nowUtc
                };
                context.Organizations.Add(org);
                context.OrganizationVersions.Add(new OrganizationVersion
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = orgId,
                    Name = po.Name,
                    ParentId = null,
                    ValidFrom = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    ValidTo = null,
                    CreatedAt = nowUtc
                });
                orgMap[po.Code] = org;
            }
            else
            {
                orgMap[po.Code] = existing;
            }
        }

        await context.SaveChangesAsync();

        // 2. Seed Software Vendors
        var vendors = new List<(string Code, string Name)>
        {
            ("VNPT_LC", "Trung tâm Kinh doanh VNPT - Lào Cai"),
            ("VIETTEL_LC", "Viettel Lào Cai - Chi nhánh Tập đoàn Công nghiệp - Viễn thông Quân đội"),
            ("FPT_IS", "Công ty TNHH Hệ thống Thông tin FPT (FPT IS)"),
            ("MISA", "Công ty Cổ phần MISA")
        };

        var vendorMap = new Dictionary<string, Vendor>(StringComparer.OrdinalIgnoreCase);
        foreach (var v in vendors)
        {
            var existing = await context.Vendors.FirstOrDefaultAsync(x => x.Code == v.Code);
            if (existing == null)
            {
                var vendor = new Vendor
                {
                    Id = Guid.NewGuid(),
                    Code = v.Code,
                    Name = v.Name,
                    IsActive = true,
                    CreatedAt = nowUtc
                };
                context.Vendors.Add(vendor);
                vendorMap[v.Code] = vendor;
            }
            else
            {
                vendorMap[v.Code] = existing;
            }
        }

        await context.SaveChangesAsync();

        // 3. Seed Software Categories
        var catEgov = await context.SoftwareCategories.FirstOrDefaultAsync(c => c.Code == "EGOV");
        var catHealth = await context.SoftwareCategories.FirstOrDefaultAsync(c => c.Code == "HEALTH");
        var catEdu = await context.SoftwareCategories.FirstOrDefaultAsync(c => c.Code == "EDU");

        if (catHealth == null)
        {
            catHealth = new SoftwareCategory { Id = Guid.NewGuid(), Code = "HEALTH", Name = "Y tế số", IsActive = true, CreatedAt = nowUtc };
            context.SoftwareCategories.Add(catHealth);
        }
        if (catEdu == null)
        {
            catEdu = new SoftwareCategory { Id = Guid.NewGuid(), Code = "EDU", Name = "Giáo dục số", IsActive = true, CreatedAt = nowUtc };
            context.SoftwareCategories.Add(catEdu);
        }
        await context.SaveChangesAsync();

        // 4. Seed Pilot Software Catalog
        var pilotSoftware = new List<(string Code, string Name, Guid CategoryId, string VendorCode, string VersionName)>
        {
            ("LGSP_LAOCAI", "Nền tảng Tích hợp, Chia sẻ Dữ liệu tỉnh Lào Cai (LGSP)", catEgov!.Id, "VNPT_LC", "v2.0.0"),
            ("VNPT_HMIS", "Hệ thống Quản lý Y tế Cơ sở tỉnh Lào Cai (HMIS)", catHealth.Id, "VNPT_LC", "v3.1.0"),
            ("VNEDU_LC", "Hệ sinh thái Giáo dục và Quản lý Trường học (vnEdu)", catEdu.Id, "VNPT_LC", "v4.0.0"),
            ("QLCBCC_LC", "Phần mềm Quản lý Cán bộ, Công chức, Viên chức tỉnh Lào Cai", catEgov.Id, "MISA", "v2026.1")
        };

        foreach (var ps in pilotSoftware)
        {
            if (!await context.Software.AnyAsync(s => s.Code == ps.Code))
            {
                var sw = new Software
                {
                    Id = Guid.NewGuid(),
                    Code = ps.Code,
                    Name = ps.Name,
                    CategoryId = ps.CategoryId,
                    VendorId = vendorMap[ps.VendorCode].Id,
                    Description = $"Phần mềm triển khai thí điểm theo chương trình chuyển đổi số tỉnh Lào Cai.",
                    LifecycleStatus = "Active",
                    Version = 1,
                    CreatedAt = nowUtc
                };
                context.Software.Add(sw);

                context.SoftwareReleases.Add(new SoftwareRelease
                {
                    Id = Guid.NewGuid(),
                    SoftwareId = sw.Id,
                    VersionName = ps.VersionName,
                    ReleaseDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    SupportEndDate = new DateTime(2028, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                    CreatedAt = nowUtc
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
