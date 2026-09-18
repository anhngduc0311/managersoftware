using LaoCai.SoftwareManagement.Application.Common.Interfaces;
using LaoCai.SoftwareManagement.Domain.Entities.Catalog;
using LaoCai.SoftwareManagement.Domain.Entities.Iam;
using LaoCai.SoftwareManagement.Domain.Entities.Organizations;
using Microsoft.EntityFrameworkCore;

namespace LaoCai.SoftwareManagement.Infrastructure.Persistence;

public static class DataSeeder
{
    public static async Task SeedAsync(
        AppDbContext context,
        IPasswordHasherService passwordHasher,
        IDateTimeProvider dateTimeProvider)
    {
        var nowUtc = dateTimeProvider.UtcNow;

        // 1. Seed Permissions
        var permissions = new List<(string Code, string Name, string Group)>
        {
            ("access.manage", "Quản lý người dùng, tài khoản và cấp quyền", "Hệ thống & Tài khoản"),
            ("settings.manage", "Quản lý cấu hình tham số hệ thống", "Hệ thống & Tài khoản"),
            ("jobs.manage", "Quản trị tác vụ ngầm và tiến trình nền", "Hệ thống & Tài khoản"),
            ("organizations.read", "Xem danh sách và cây tổ chức đơn vị", "Đơn vị & Tổ chức"),
            ("organizations.manage", "Thêm, sửa đơn vị và quan hệ sáp nhập", "Đơn vị & Tổ chức"),
            ("catalog.read", "Xem danh mục phần mềm và phiên bản", "Danh mục phần mềm"),
            ("catalog.manage", "Quản lý danh mục phần mềm dùng chung", "Danh mục phần mềm"),
            ("catalog.propose", "Gửi đề xuất bổ sung phần mềm mới", "Danh mục phần mềm"),
            ("deployments.read", "Xem các bản triển khai chính thức", "Hồ sơ Triển khai"),
            ("deployments.read_drafts", "Xem các bản nháp và chờ duyệt", "Hồ sơ Triển khai"),
            ("deployments.write", "Tạo và chỉnh sửa hồ sơ triển khai", "Hồ sơ Triển khai"),
            ("deployments.approve", "Phê duyệt hoặc từ chối hồ sơ triển khai", "Hồ sơ Triển khai"),
            ("contracts.read", "Xem hợp đồng và giá trị kinh phí", "Hợp đồng & Bản quyền"),
            ("contracts.write", "Tạo và cập nhật hợp đồng", "Hợp đồng & Bản quyền"),
            ("licenses.allocate", "Phân bổ hạn mức bản quyền phần mềm", "Hợp đồng & Bản quyền"),
            ("reports.read", "Xem Dashboard và các báo cáo tổng hợp", "Báo cáo & Tiện ích"),
            ("reports.import", "Nhập hồ sơ triển khai từ tệp Excel", "Báo cáo & Tiện ích"),
            ("reports.export", "Yêu cầu và tải xuống tệp Excel xuất dữ liệu", "Báo cáo & Tiện ích"),
            ("audit.read", "Tra cứu nhật ký kiểm toán hệ thống", "Báo cáo & Tiện ích")
        };

        var existingPermissions = await context.Permissions.ToListAsync();
        var permissionEntities = new Dictionary<string, Permission>(StringComparer.OrdinalIgnoreCase);

        foreach (var p in permissions)
        {
            var entity = existingPermissions.FirstOrDefault(ep => ep.Code.Equals(p.Code, StringComparison.OrdinalIgnoreCase));
            if (entity == null)
            {
                entity = new Permission
                {
                    Id = Guid.NewGuid(),
                    Code = p.Code,
                    Name = p.Name,
                    GroupName = p.Group
                };
                context.Permissions.Add(entity);
            }
            permissionEntities[p.Code] = entity;
        }

        // 2. Seed Roles
        var rolesDef = new Dictionary<string, (string Name, string Description, string[] Permissions)>
        {
            ["SystemAdmin"] = (
                "Quản trị viên Hệ thống",
                "Quản lý tài khoản, phân quyền, cấu hình và tiến trình vận hành",
                new[] { "access.manage", "settings.manage", "jobs.manage", "organizations.read", "organizations.manage", "catalog.read" }
            ),
            ["CatalogManager"] = (
                "Quản trị Danh mục",
                "Quản lý danh mục phần mềm dùng chung toàn tỉnh, phiên bản, nhà cung cấp và duyệt đề xuất",
                new[] { "organizations.read", "catalog.read", "catalog.manage" }
            ),
            ["Coordinator"] = (
                "Điều phối viên",
                "Tổng hợp, rà soát và theo dõi tình hình ứng dụng phần mềm toàn tỉnh",
                new[] { "organizations.read", "catalog.read", "deployments.read", "deployments.read_drafts", "deployments.approve", "contracts.read", "reports.read", "reports.export" }
            ),
            ["UnitEditor"] = (
                "Cán bộ Cập nhật Đơn vị",
                "Tạo và chỉnh sửa hồ sơ triển khai, gửi duyệt, đề xuất phần mềm mới tại đơn vị",
                new[] { "organizations.read", "catalog.read", "catalog.propose", "deployments.read", "deployments.read_drafts", "deployments.write", "reports.read", "reports.import" }
            ),
            ["UnitApprover"] = (
                "Lãnh đạo Phê duyệt Đơn vị",
                "Kiểm tra và phê duyệt/từ chối hồ sơ triển khai phần mềm tại đơn vị",
                new[] { "organizations.read", "catalog.read", "deployments.read", "deployments.read_drafts", "deployments.approve", "reports.read" }
            ),
            ["Viewer"] = (
                "Người xem Báo cáo",
                "Xem số liệu Dashboard và hồ sơ triển khai chính thức trong phạm vi",
                new[] { "organizations.read", "catalog.read", "deployments.read", "reports.read" }
            ),
            ["Auditor"] = (
                "Kiểm toán viên",
                "Tra cứu nhật ký thay đổi và kiểm tra tính toàn vẹn dữ liệu",
                new[] { "organizations.read", "catalog.read", "deployments.read", "contracts.read", "audit.read" }
            )
        };

        var existingRoles = await context.Roles.Include(r => r.RolePermissions).ToListAsync();
        var roleEntities = new Dictionary<string, Role>(StringComparer.OrdinalIgnoreCase);

        foreach (var r in rolesDef)
        {
            var role = existingRoles.FirstOrDefault(er => er.Code.Equals(r.Key, StringComparison.OrdinalIgnoreCase));
            if (role == null)
            {
                role = new Role
                {
                    Id = Guid.NewGuid(),
                    Code = r.Key,
                    Name = r.Value.Name,
                    Description = r.Value.Description
                };
                context.Roles.Add(role);
            }
            else
            {
                role.Name = r.Value.Name;
                role.Description = r.Value.Description;
            }

            roleEntities[r.Key] = role;

            // Map permissions to role
            foreach (var permCode in r.Value.Permissions)
            {
                if (permissionEntities.TryGetValue(permCode, out var perm))
                {
                    var existingRp = role.RolePermissions.FirstOrDefault(rp => rp.PermissionId == perm.Id);
                    if (existingRp == null)
                    {
                        context.RolePermissions.Add(new RolePermission
                        {
                            RoleId = role.Id,
                            PermissionId = perm.Id,
                            Role = role,
                            Permission = perm
                        });
                    }
                }
            }
        }

        await context.SaveChangesAsync();

        // 3. Seed Organizations
        var orgStttId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var orgBaoThangId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var orgSaPaId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var orgTpLaoCaiId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        if (!await context.Organizations.AnyAsync(o => o.Id == orgStttId))
        {
            var orgSttt = new Organization
            {
                Id = orgStttId,
                Code = "STTT",
                IsActive = true,
                CreatedAt = nowUtc
            };
            context.Organizations.Add(orgSttt);
            context.OrganizationVersions.Add(new OrganizationVersion
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgStttId,
                Name = "Sở Thông tin và Truyền thông tỉnh Lào Cai",
                ParentId = null,
                ValidFrom = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                ValidTo = null,
                CreatedAt = nowUtc
            });
        }

        if (!await context.Organizations.AnyAsync(o => o.Id == orgBaoThangId))
        {
            var orgBaoThang = new Organization
            {
                Id = orgBaoThangId,
                Code = "UBND_BAOTHANG",
                IsActive = true,
                CreatedAt = nowUtc
            };
            context.Organizations.Add(orgBaoThang);
            context.OrganizationVersions.Add(new OrganizationVersion
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgBaoThangId,
                Name = "UBND Huyện Bảo Thắng",
                ParentId = null,
                ValidFrom = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                ValidTo = null,
                CreatedAt = nowUtc
            });
        }

        if (!await context.Organizations.AnyAsync(o => o.Id == orgSaPaId))
        {
            var orgSaPa = new Organization
            {
                Id = orgSaPaId,
                Code = "UBND_SAPA",
                IsActive = true,
                CreatedAt = nowUtc
            };
            context.Organizations.Add(orgSaPa);
            context.OrganizationVersions.Add(new OrganizationVersion
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgSaPaId,
                Name = "UBND Thị xã Sa Pa",
                ParentId = null,
                ValidFrom = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                ValidTo = null,
                CreatedAt = nowUtc
            });
        }

        if (!await context.Organizations.AnyAsync(o => o.Id == orgTpLaoCaiId))
        {
            var orgTpLaoCai = new Organization
            {
                Id = orgTpLaoCaiId,
                Code = "UBND_TPLAOCAI",
                IsActive = true,
                CreatedAt = nowUtc
            };
            context.Organizations.Add(orgTpLaoCai);
            context.OrganizationVersions.Add(new OrganizationVersion
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgTpLaoCaiId,
                Name = "UBND Thành phố Lào Cai",
                ParentId = null,
                ValidFrom = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                ValidTo = null,
                CreatedAt = nowUtc
            });
        }

        await context.SaveChangesAsync();

        // 4. Seed Users
        var adminUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var adminUser = await context.Users.Include(u => u.RoleScopes).FirstOrDefaultAsync(u => u.Id == adminUserId);
        if (adminUser == null)
        {
            adminUser = new User
            {
                Id = adminUserId,
                UserName = "admin",
                NormalizedUserName = "ADMIN",
                DisplayName = "Quản trị viên Hệ thống",
                Email = "admin@laocai.gov.vn",
                NormalizedEmail = "ADMIN@LAOCAI.GOV.VN",
                PasswordHash = passwordHasher.HashPassword("Admin@123456"),
                SecurityStamp = Guid.NewGuid().ToString(),
                IsActive = true,
                CreatedAt = nowUtc
            };
            context.Users.Add(adminUser);

            context.UserRoleScopes.Add(new UserRoleScope
            {
                Id = Guid.NewGuid(),
                UserId = adminUserId,
                RoleId = roleEntities["SystemAdmin"].Id,
                ScopeType = "Global",
                OrganizationId = null,
                IncludeDescendants = true,
                ValidFrom = nowUtc.AddYears(-1),
                ValidTo = null
            });
        }

        // Seed Sample Role Users
        var usersToSeed = new (string Username, string DisplayName, string RoleCode, string ScopeType, Guid? OrgId)[]
        {
            ("catalog_mgr", "Cán bộ Quản lý Danh mục Sở", "CatalogManager", "Global", null),
            ("coordinator", "Điều phối viên Sở TTTT", "Coordinator", "Global", null),
            ("editor_baothang", "Chuyên viên CNTT Huyện Bảo Thắng", "UnitEditor", "Organization", orgBaoThangId),
            ("approver_baothang", "Lãnh đạo UBND Huyện Bảo Thắng", "UnitApprover", "Organization", orgBaoThangId),
            ("viewer_prov", "Cán bộ Giám sát Tỉnh", "Viewer", "Global", null)
        };

        foreach (var u in usersToSeed)
        {
            var userEntity = await context.Users.FirstOrDefaultAsync(x => x.NormalizedUserName == u.Username.ToUpperInvariant());
            if (userEntity == null)
            {
                userEntity = new User
                {
                    Id = Guid.NewGuid(),
                    UserName = u.Username,
                    NormalizedUserName = u.Username.ToUpperInvariant(),
                    DisplayName = u.DisplayName,
                    Email = $"{u.Username}@laocai.gov.vn",
                    NormalizedEmail = $"{u.Username.ToUpperInvariant()}@LAOCAI.GOV.VN",
                    PasswordHash = passwordHasher.HashPassword("User@123456"),
                    SecurityStamp = Guid.NewGuid().ToString(),
                    IsActive = true,
                    CreatedAt = nowUtc
                };
                context.Users.Add(userEntity);

                if (roleEntities.TryGetValue(u.RoleCode, out var role))
                {
                    context.UserRoleScopes.Add(new UserRoleScope
                    {
                        Id = Guid.NewGuid(),
                        UserId = userEntity.Id,
                        RoleId = role.Id,
                        ScopeType = u.ScopeType,
                        OrganizationId = u.OrgId,
                        IncludeDescendants = true,
                        ValidFrom = nowUtc.AddYears(-1),
                        ValidTo = null
                    });
                }
            }
        }

        await context.SaveChangesAsync();

        // 5. Seed Catalog Categories, Vendors, Software
        var catEgovId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var catHealthId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        if (!await context.SoftwareCategories.AnyAsync(c => c.Id == catEgovId))
        {
            context.SoftwareCategories.Add(new SoftwareCategory
            {
                Id = catEgovId,
                Code = "E_GOV",
                Name = "Chính quyền số & Dịch vụ công",
                IsActive = true,
                CreatedAt = nowUtc
            });
        }

        if (!await context.SoftwareCategories.AnyAsync(c => c.Id == catHealthId))
        {
            context.SoftwareCategories.Add(new SoftwareCategory
            {
                Id = catHealthId,
                Code = "DIGITAL_HEALTH",
                Name = "Y tế số & Quản lý bệnh viện",
                IsActive = true,
                CreatedAt = nowUtc
            });
        }

        var vendorVnptId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var vendorViettelId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        if (!await context.Vendors.AnyAsync(v => v.Id == vendorVnptId))
        {
            context.Vendors.Add(new Vendor
            {
                Id = vendorVnptId,
                Code = "VNPT_LAOCAI",
                Name = "Tập đoàn VNPT - Chi nhánh Lào Cai",
                ContactInfo = "Hotline: 0214.3888888 | Email: vnpt.laocai@vnpt.vn",
                IsActive = true,
                CreatedAt = nowUtc
            });
        }

        if (!await context.Vendors.AnyAsync(v => v.Id == vendorViettelId))
        {
            context.Vendors.Add(new Vendor
            {
                Id = vendorViettelId,
                Code = "VIETTEL_LAOCAI",
                Name = "Viettel Lào Cai - Chi nhánh Tập đoàn Công nghiệp - Viễn thông Quân đội",
                ContactInfo = "Hotline: 0214.6250178 | Email: cskh@viettel.com.vn",
                IsActive = true,
                CreatedAt = nowUtc
            });
        }

        await context.SaveChangesAsync();

        // Seed Sample Software
        var swIgateId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var swIofficeId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

        if (!await context.Software.AnyAsync(s => s.Id == swIgateId))
        {
            var swIgate = new Software
            {
                Id = swIgateId,
                Code = "VNPT_IGATE",
                Name = "Hệ thống Thông tin Giải quyết Thủ tục Hành chính (iGate)",
                CategoryId = catEgovId,
                VendorId = vendorVnptId,
                Description = "Hệ thống một cửa điện tử và cổng dịch vụ công trực tuyến tích hợp toàn tỉnh.",
                LifecycleStatus = "Active",
                Version = 1,
                CreatedAt = nowUtc
            };
            context.Software.Add(swIgate);

            context.SoftwareReleases.Add(new SoftwareRelease
            {
                Id = Guid.NewGuid(),
                SoftwareId = swIgateId,
                VersionName = "v3.2.0",
                ReleaseDate = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                SupportEndDate = new DateTime(2028, 12, 31, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = nowUtc
            });
        }

        if (!await context.Software.AnyAsync(s => s.Id == swIofficeId))
        {
            var swIoffice = new Software
            {
                Id = swIofficeId,
                Code = "VNPT_IOFFICE",
                Name = "Hệ thống Quản lý Văn bản và Điều hành (VNPT iOffice)",
                CategoryId = catEgovId,
                VendorId = vendorVnptId,
                Description = "Phần mềm quản lý văn bản đi/đến, hồ sơ công việc và ký số điện tử.",
                LifecycleStatus = "Active",
                Version = 1,
                CreatedAt = nowUtc
            };
            context.Software.Add(swIoffice);

            context.SoftwareReleases.Add(new SoftwareRelease
            {
                Id = Guid.NewGuid(),
                SoftwareId = swIofficeId,
                VersionName = "v4.5.1",
                ReleaseDate = new DateTime(2025, 9, 15, 0, 0, 0, DateTimeKind.Utc),
                SupportEndDate = new DateTime(2029, 6, 30, 0, 0, 0, DateTimeKind.Utc),
                CreatedAt = nowUtc
            });
        }

        await context.SaveChangesAsync();
    }
}
