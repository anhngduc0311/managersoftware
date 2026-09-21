# Ma trận Phân quyền (Access Matrix)

> Tham chiếu: [SPEC.md — Mục 2.2, 3.2](../SPEC.md#22-vai-trò-và-nguyên-tắc-quyền)  
> Nguyên tắc cốt lõi: Một hành động được phép khi và chỉ khi có **một bản cấp vai trò (UserRoleScope) đang hiệu lực** thỏa mãn đồng thời: `Quyền thao tác (Permission) + Phạm vi đơn vị (Scope) + Trạng thái bản ghi`.

---

## 1. Danh sách Quyền chuẩn hóa (Permissions)

| Nhóm | Permission Code | Ý nghĩa |
| --- | --- | --- |
| **Hệ thống & Tài khoản** | `access.manage` | Quản lý người dùng, tạo/khóa tài khoản, gán vai trò & scope |
| | `settings.manage` | Quản lý cấu hình tham số hệ thống, đơn vị đủ điều kiện báo cáo |
| | `jobs.manage` | Quản trị tác vụ ngầm, xem trạng thái & retry job vận hành |
| **Đơn vị & Tổ chức** | `organizations.read` | Xem danh sách, chi tiết và cây tổ chức |
| | `organizations.manage` | Thêm, sửa đơn vị, cơ cấu cha-con, ghi nhận sáp nhập/chia tách |
| **Danh mục phần mềm** | `catalog.read` | Xem danh mục phần mềm, phiên bản release, nhà cung cấp, nhóm |
| | `catalog.manage` | Thêm, sửa danh mục dùng chung (chỉ cấp cho cấp tỉnh/Sở TTTT) |
| | `catalog.propose` | Gửi đề xuất bổ sung phần mềm mới từ cấp đơn vị |
| **Hồ sơ Triển khai** | `deployments.read` | Xem các bản triển khai chính thức (`Approved`) trong scope |
| | `deployments.read_drafts` | Xem các bản nháp (`Draft`) hoặc đang chờ duyệt (`Submitted`) trong scope |
| | `deployments.write` | Tạo hồ sơ triển khai mới, chỉnh sửa bản nháp, gửi duyệt (`Submit`) |
| | `deployments.approve` | Phê duyệt (`Approve`) hoặc từ chối (`Reject`) hồ sơ (cấm tự duyệt) |
| **Hợp đồng & Bản quyền** | `contracts.read` | Xem thông tin hợp đồng, giá trị tiền, thời hạn bảo trì |
| | `contracts.write` | Tạo, cập nhật hồ sơ hợp đồng, hạng mục phần mềm |
| | `licenses.allocate` | Phân bổ hạn mức bản quyền vào hồ sơ triển khai |
| **Báo cáo & Tiện ích** | `reports.read` | Xem Dashboard tổng quan KPI và các báo cáo tổng hợp |
| | `reports.import` | Tải lên và commit tệp Excel nhập hồ sơ triển khai hàng loạt |
| | `reports.export` | Yêu cầu và tải xuống tệp Excel xuất dữ liệu |
| | `audit.read` | Tra cứu nhật ký kiểm toán (`AuditLog`) trong phạm vi được giao |

---

## 2. Ma trận Vai trò × Quyền mặc định

| Permission | SystemAdmin | CatalogManager | Coordinator | UnitEditor | UnitApprover | Viewer | Auditor |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| `access.manage` | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| `settings.manage` | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| `jobs.manage` | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| `organizations.read` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `organizations.manage` | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| `catalog.read` | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `catalog.manage` | ✅ | ✅ | ❌ | ❌ | ❌ | ❌ | ❌ |
| `catalog.propose` | ✅ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| `deployments.read` | ❌ | ❌ | ✅ | ✅ | ✅ | ✅ | ✅ |
| `deployments.read_drafts`| ❌ | ❌ | ✅ | ✅ | ✅ | ❌ | ❌ |
| `deployments.write` | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| `deployments.approve` | ❌ | ❌ | ✅ | ❌ | ✅ | ❌ | ❌ |
| `contracts.read` | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ | ✅ |
| `contracts.write` | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| `licenses.allocate` | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ |
| `reports.read` | ❌ | ❌ | ✅ | ✅ | ✅ | ✅ | ❌ |
| `reports.import` | ❌ | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ |
| `reports.export` | ❌ | ❌ | ✅ | ❌ | ❌ | ❌ | ❌ |
| `audit.read` | ❌ | ❌ | ❌ | ❌ | ❌ | ❌ | ✅ |

> [!NOTE]
> - `SystemAdmin` không tự động có quyền đọc thông tin tài chính hợp đồng hoặc duyệt hồ sơ triển khai.
> - Người dùng có thể được gán nhiều vai trò với các Scope khác nhau (ví dụ: `UnitEditor` tại Đơn vị A và `Viewer` tại Đơn vị B). Quyền không bị trộn chéo giữa các đơn vị.
