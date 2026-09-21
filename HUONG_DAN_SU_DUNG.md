# TÀI LIỆU HƯỚNG DẪN SỬ DỤNG TỪNG CHỨC NĂNG
## HỆ THỐNG QUẢN LÝ PHẦN MỀM CHUYỂN ĐỔI SỐ TỈNH LÀO CAI

Tài liệu hướng dẫn chi tiết và đầy đủ nhất đã được biên soạn và cập nhật tại:
👉 **[docs/manuals/user-guide.md](file:///d:/Project/managersoftware/docs/manuals/user-guide.md)**

---

### Tóm tắt các phân hệ chức năng & Đường dẫn nhanh:

1. **[Tài khoản Kiểm thử & Đăng nhập](file:///d:/Project/managersoftware/docs/manuals/user-guide.md#1-tổng-quan-hệ-thống--tài-khoản-truy-cập)**
   - Tài khoản Admin: `admin` / `Admin@123456`
   - Tài khoản Cán bộ Đơn vị: `editor_baothang` / `User@123456`
   - Tài khoản Lãnh đạo duyệt: `approver_baothang` / `User@123456`
   - Tài khoản Quản lý hợp đồng & License: `coordinator` / `User@123456`
   - Tài khoản Quản lý danh mục: `catalog_mgr` / `User@123456`
   - Tài khoản Kiểm toán viên: `auditor` / `User@123456`

2. **[Dashboard & Trung tâm Báo cáo](file:///d:/Project/managersoftware/docs/manuals/user-guide.md#3-chức-năng-1-dashboard--trung-tâm-báo-cáo-chỉ-số)** (`/dashboard`):
   - 4 chỉ số KPI thời gian thực.
   - Biểu đồ độ phủ, cơ cấu phần mềm, trạng thái triển khai.

3. **[Quản lý Danh mục Phần mềm Dùng chung](file:///d:/Project/managersoftware/docs/manuals/user-guide.md#4-chức-năng-2-quản-lý-danh-mục-phần-mềm-dùng-chung)** (`/software`):
   - Tra cứu, tìm kiếm phần mềm theo danh mục và nhà sản xuất.
   - Quản lý phiên bản phát hành (Releases & Changelog).

4. **[Đề xuất Bổ sung Phần mềm Mới](file:///d:/Project/managersoftware/docs/manuals/user-guide.md#5-chức-năng-3-đề-xuất-bổ-sung-phần-mềm-mới)** (`/software/proposals`):
   - Cán bộ đơn vị gửi đề xuất phần mềm mới.
   - Sở TT&TT thẩm định, duyệt tự động tạo phần mềm hoặc từ chối kèm lý do.

5. **[Quản lý Hồ sơ Triển khai & Cài đặt](file:///d:/Project/managersoftware/docs/manuals/user-guide.md#6-chức-năng-4-quản-lý-hồ-sơ-triển-khai--cài-đặt)** (`/deployments`):
   - Tạo hồ sơ triển khai tại các môi trường `Production`, `Staging`, `DR`.
   - Vòng đời phê duyệt: `Draft` ➔ `Submitted` ➔ `Approved` / `Rejected` (kèm `Reopen`) ➔ `Decommissioned`.
   - Đính kèm biên bản nghiệm thu, quyết định (hỗ trợ quét virus an toàn).
   - Xem lịch sử sửa đổi (Revisions Before/After Diff).

6. **[Tiện ích Nhập / Xuất Excel Chuẩn](file:///d:/Project/managersoftware/docs/manuals/user-guide.md#7-chức-năng-5-tiện-ích-nhập--xuất-dữ-liệu-excel-all-or-nothing)** (`/deployments/excel`):
   - Tải file mẫu `.xlsx` tích hợp danh mục chuẩn của tỉnh.
   - Kiểm tra lỗi trước khi nhập (Validation Preview).
   - Cơ chế cam kết an toàn **All-or-Nothing Transaction** (chỉ nhập khi 100% dòng hợp lệ).
   - Xuất dữ liệu báo cáo toàn tỉnh.

7. **[Quản lý Hợp đồng & Phân bổ Bản quyền (License)](file:///d:/Project/managersoftware/docs/manuals/user-guide.md#8-chức-năng-6-quản-lý-hợp-đồng--bản-quyền-license)** (`/contracts`):
   - Quản lý số hợp đồng, đối tác, giá trị, hạn bảo hành.
   - Thiết lập gói bản quyền dạng `Seat` hoặc `Unlimited`.
   - Cấp phát bản quyền và kiểm soát Quota chống cấp phát vượt quá số lượng.

8. **[Quản lý Cơ quan & Đơn vị Hành chính](file:///d:/Project/managersoftware/docs/manuals/user-guide.md#9-chức-năng-7-quản-lý-cơ-quan--đơn-vị-hành-chính)** (`/organizations`):
   - Danh sách và sơ đồ cây tổ chức (Organization Tree view) từ cấp tỉnh đến cấp xã.

9. **[Quản trị Tài khoản & Phân quyền Thẩm quyền](file:///d:/Project/managersoftware/docs/manuals/user-guide.md#10-chức-năng-8-quản-trị-người-dùng--phân-quyền-phạm-vi-scope-based)** (`/admin/users`, `/admin/roles`):
   - Quản lý người dùng, khóa/mở khóa, đặt lại mật khẩu.
   - Gán quyền theo phạm vi: Toàn cục (`Global`) hoặc Đơn vị (`Organization`) có/không kế thừa cấp dưới (`IncludeDescendants`).

10. **[Nhật ký Kiểm toán Hệ thống](file:///d:/Project/managersoftware/docs/manuals/user-guide.md#11-chức-năng-9-nhật-ký-kiểm-toán-hệ-thống-audit-logs)** (`/admin/audit`):
    - Tra cứu dấu vết mọi thao tác thay đổi dữ liệu.
    - So sánh chi tiết Trước / Sau (Before / After) có che mờ dữ liệu nhạy cảm (Data Masking).

11. **[Quản trị Tiến trình Nền Worker Jobs](file:///d:/Project/managersoftware/docs/manuals/user-guide.md#12-chức-năng-10-quản-trị-tiến-trình-nền-worker-background-jobs)** (`/admin/jobs`):
    - Giám sát trạng thái, nhật ký và kích hoạt chạy ngay các tác vụ định kỳ.

12. **[Ma trận Phân quyền theo Vai trò](file:///d:/Project/managersoftware/docs/manuals/user-guide.md#13-phụ-lục-ma-trận-phân-quyền-thao-tác-theo-vai-trò)**:
    - Bảng tổng hợp quyền hạn của từng nhóm người dùng.
