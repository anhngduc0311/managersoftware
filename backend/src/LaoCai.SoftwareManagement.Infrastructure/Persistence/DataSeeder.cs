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
                new[] { "access.manage", "settings.manage", "jobs.manage", "organizations.read", "organizations.manage", "catalog.read", "contracts.read", "contracts.write", "licenses.allocate" }
            ),
            ["CatalogManager"] = (
                "Quản trị Danh mục",
                "Quản lý danh mục phần mềm dùng chung toàn tỉnh, phiên bản, nhà cung cấp và duyệt đề xuất",
                new[] { "organizations.read", "catalog.read", "catalog.manage" }
            ),
            ["Coordinator"] = (
                "Điều phối viên",
                "Tổng hợp, rà soát và theo dõi tình hình ứng dụng phần mềm toàn tỉnh",
                new[] { "organizations.read", "catalog.read", "deployments.read", "deployments.read_drafts", "deployments.approve", "contracts.read", "contracts.write", "licenses.allocate", "reports.read", "reports.export" }
            ),
            ["UnitEditor"] = (
                "Cán bộ Cập nhật Đơn vị",
                "Tạo và chỉnh sửa hồ sơ triển khai, gửi duyệt, đề xuất phần mềm mới tại đơn vị",
                new[] { "organizations.read", "catalog.read", "catalog.propose", "deployments.read", "deployments.read_drafts", "deployments.write", "licenses.allocate", "reports.read", "reports.import" }
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

        // 6. Seed Sample Deployments & Revisions
        var dep1Id = Guid.Parse("11112222-3333-4444-5555-666677778888");
        var dep2Id = Guid.Parse("22223333-4444-5555-6666-777788889999");

        var editorUser = await context.Users.FirstOrDefaultAsync(u => u.NormalizedUserName == "EDITOR_BAOTHANG");
        var approverUser = await context.Users.FirstOrDefaultAsync(u => u.NormalizedUserName == "APPROVER_BAOTHANG");
        var releaseIoffice = await context.SoftwareReleases.FirstOrDefaultAsync(r => r.SoftwareId == swIofficeId);
        var releaseIgate = await context.SoftwareReleases.FirstOrDefaultAsync(r => r.SoftwareId == swIgateId);

        if (!await context.Deployments.AnyAsync(d => d.Id == dep1Id))
        {
            var rev1Id = Guid.NewGuid();
            var dep1 = new LaoCai.SoftwareManagement.Domain.Entities.Deployments.Deployment
            {
                Id = dep1Id,
                SoftwareId = swIofficeId,
                OrganizationId = orgBaoThangId,
                Environment = "Production",
                InstanceKey = "default",
                CurrentApprovedRevisionId = rev1Id,
                Version = 2,
                CreatedAt = nowUtc.AddMonths(-6),
                CreatedBy = editorUser?.Id
            };

            var rev1 = new LaoCai.SoftwareManagement.Domain.Entities.Deployments.DeploymentRevision
            {
                Id = rev1Id,
                DeploymentId = dep1Id,
                RevisionNo = 1,
                ReleaseId = releaseIoffice?.Id,
                OperationalStatus = "Active",
                StartDate = new DateOnly(2024, 1, 1),
                GoLiveDate = new DateOnly(2024, 2, 1),
                ResponsibleUserId = editorUser?.Id,
                WorkflowStatus = "Approved",
                SubmittedBy = editorUser?.Id,
                SubmittedAt = nowUtc.AddMonths(-6),
                ApprovedAt = nowUtc.AddMonths(-6).AddDays(1),
                Version = 2,
                CreatedAt = nowUtc.AddMonths(-6),
                CreatedBy = editorUser?.Id
            };

            var dec1 = new LaoCai.SoftwareManagement.Domain.Entities.Deployments.ApprovalDecision
            {
                Id = Guid.NewGuid(),
                DeploymentRevisionId = rev1Id,
                Decision = "Approved",
                Reason = null,
                ActorId = approverUser?.Id ?? Guid.NewGuid(),
                DecidedAt = nowUtc.AddMonths(-6).AddDays(1)
            };

            context.Deployments.Add(dep1);
            context.DeploymentRevisions.Add(rev1);
            context.ApprovalDecisions.Add(dec1);
        }

        if (!await context.Deployments.AnyAsync(d => d.Id == dep2Id))
        {
            var rev2Id = Guid.NewGuid();
            var dep2 = new LaoCai.SoftwareManagement.Domain.Entities.Deployments.Deployment
            {
                Id = dep2Id,
                SoftwareId = swIgateId,
                OrganizationId = orgBaoThangId,
                Environment = "Production",
                InstanceKey = "default",
                CurrentApprovedRevisionId = null,
                Version = 1,
                CreatedAt = nowUtc.AddDays(-2),
                CreatedBy = editorUser?.Id
            };

            var rev2 = new LaoCai.SoftwareManagement.Domain.Entities.Deployments.DeploymentRevision
            {
                Id = rev2Id,
                DeploymentId = dep2Id,
                RevisionNo = 1,
                ReleaseId = releaseIgate?.Id,
                OperationalStatus = "Active",
                StartDate = new DateOnly(2025, 1, 1),
                GoLiveDate = new DateOnly(2025, 3, 1),
                ResponsibleUserId = editorUser?.Id,
                WorkflowStatus = "Submitted",
                SubmittedBy = editorUser?.Id,
                SubmittedAt = nowUtc.AddDays(-1),
                Version = 1,
                CreatedAt = nowUtc.AddDays(-2),
                CreatedBy = editorUser?.Id
            };

            context.Deployments.Add(dep2);
            context.DeploymentRevisions.Add(rev2);

            if (approverUser != null)
            {
                context.Notifications.Add(new LaoCai.SoftwareManagement.Domain.Entities.Notifications.Notification
                {
                    Id = Guid.NewGuid(),
                    RecipientUserId = approverUser.Id,
                    Type = "WorkflowSubmitted",
                    Title = "Yêu cầu phê duyệt: Hệ thống iGate",
                    Message = "Hồ sơ triển khai phần mềm 'Hệ thống Thông tin Giải quyết Thủ tục Hành chính (iGate)' đã được gửi duyệt. Vui lòng kiểm tra và phê duyệt.",
                    TargetRoute = $"/deployments/{dep2Id}",
                    DeduplicationKey = $"wf:submitted:{rev2Id}:{approverUser.Id}",
                    CreatedAt = nowUtc.AddDays(-1)
                });
            }
        }

        // 8. Seed Contracts and License Allocations
        var contractId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        if (!await context.Contracts.AnyAsync(c => c.Id == contractId))
        {
            var orgSoTttt = await context.Organizations.FirstOrDefaultAsync(o => o.Code == "STTT");
            var vendorVnpt = await context.Vendors.FirstOrDefaultAsync(v => v.Code == "VNPT_LAOCAI");
            var swIoffice = await context.Software.FirstOrDefaultAsync(s => s.Code == "VNPT_IOFFICE");
            var swIgate = await context.Software.FirstOrDefaultAsync(s => s.Code == "VNPT_IGATE");

            if (orgSoTttt != null && vendorVnpt != null && swIoffice != null)
            {
                var contract = new LaoCai.SoftwareManagement.Domain.Entities.Contracts.Contract
                {
                    Id = contractId,
                    ContractNo = "HD-01/2026/STTTT-VNPT",
                    OwningOrganizationId = orgSoTttt.Id,
                    VendorId = vendorVnpt.Id,
                    Status = "Active",
                    SignedDate = new DateOnly(2026, 1, 10),
                    StartDate = new DateOnly(2026, 1, 15),
                    EndDate = new DateOnly(2027, 1, 15),
                    TotalAmount = 250000000m,
                    CurrencyCode = "VND",
                    MaintenanceStartDate = new DateOnly(2026, 1, 15),
                    MaintenanceEndDate = new DateOnly(2027, 1, 15),
                    Version = 1,
                    CreatedAt = nowUtc.AddMonths(-2),
                    CreatedBy = adminUser?.Id
                };

                var item1Id = Guid.NewGuid();
                var item1 = new LaoCai.SoftwareManagement.Domain.Entities.Contracts.ContractItem
                {
                    Id = item1Id,
                    ContractId = contractId,
                    SoftwareId = swIoffice.Id,
                    Description = "Bản quyền phần mềm Quản lý văn bản và điều hành VNPT iOffice",
                    Amount = 180000000m
                };

                var ent1Id = Guid.NewGuid();
                var ent1 = new LaoCai.SoftwareManagement.Domain.Entities.Contracts.LicenseEntitlement
                {
                    Id = ent1Id,
                    ContractItemId = item1Id,
                    LicenseType = "Seat",
                    Quantity = 50,
                    ValidFrom = new DateOnly(2026, 1, 15),
                    ValidTo = new DateOnly(2027, 1, 15)
                };

                var alloc1 = new LaoCai.SoftwareManagement.Domain.Entities.Contracts.LicenseAllocation
                {
                    Id = Guid.NewGuid(),
                    EntitlementId = ent1Id,
                    DeploymentId = dep1Id,
                    Quantity = 20,
                    AllocatedAt = nowUtc.AddMonths(-1)
                };

                context.Contracts.Add(contract);
                context.ContractItems.Add(item1);
                context.LicenseEntitlements.Add(ent1);
                context.LicenseAllocations.Add(alloc1);

                if (swIgate != null)
                {
                    var item2Id = Guid.NewGuid();
                    var item2 = new LaoCai.SoftwareManagement.Domain.Entities.Contracts.ContractItem
                    {
                        Id = item2Id,
                        ContractId = contractId,
                        SoftwareId = swIgate.Id,
                        Description = "Hệ thống Một cửa điện tử dùng chung toàn tỉnh",
                        Amount = 70000000m
                    };

                    var ent2 = new LaoCai.SoftwareManagement.Domain.Entities.Contracts.LicenseEntitlement
                    {
                        Id = Guid.NewGuid(),
                        ContractItemId = item2Id,
                        LicenseType = "Unlimited",
                        Quantity = null,
                        ValidFrom = new DateOnly(2026, 1, 15),
                        ValidTo = new DateOnly(2027, 1, 15)
                    };

                    context.ContractItems.Add(item2);
                    context.LicenseEntitlements.Add(ent2);
                }
            }
        }

        await context.SaveChangesAsync();
    }
}
