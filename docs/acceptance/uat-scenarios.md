# Kịch bản Kiểm thử Chấp nhận Người dùng (UAT Scenarios)

> Tham chiếu: [SPEC.md — Mục 2.3, 2.5](../../SPEC.md#23-yêu-cầu-chức-năng)  
> Mục tiêu: Xác minh 13 yêu cầu chức năng (FR-01 → FR-13) và các ràng buộc phi chức năng.

---

### UAT-01: Xác thực & Quản lý Phiên (FR-01)
- **Kịch bản 1**: Đăng nhập với username/mật khẩu đúng → Thiết lập Cookie HttpOnly `SessionId` và non-HttpOnly `XSRF-TOKEN` → API `/api/v1/auth/me` trả về thông tin user và các role scope.
- **Kịch bản 2**: Gửi request POST/PUT không có header `X-XSRF-TOKEN` → API trả về `400 Bad Request` hoặc `403 Forbidden`.
- **Kịch bản 3**: Khóa tài khoản hoặc thu hồi quyền → Phiên đăng nhập hiện tại bị từ chối truy cập trong vòng tối đa 5 phút.

### UAT-02: Phân quyền theo Scope & Đơn vị (FR-02)
- **Kịch bản 1**: Cán bộ Đơn vị A (`UnitEditor` tại A) cố tình đọc/sửa hồ sơ thuộc Đơn vị B (truyền ID của B) → API trả về `404 Not Found` (không làm lộ sự tồn tại của bản ghi).
- **Kịch bản 2**: Cán bộ có quyền `deployments.write` tại Đơn vị A và `deployments.read` tại Đơn vị B → Cho phép tạo/sửa tại A, nhưng từ chối khi thực hiện tạo/sửa tại B.

### UAT-03: Cây Tổ chức & Lịch sử Sáp nhập (FR-03)
- **Kịch bản 1**: Đơn vị A đổi tên thành A' vào ngày `2026-06-01` → Truy vấn báo cáo với mốc `asOf = 2026-01-01` vẫn hiển thị tên A; mốc `asOf = 2026-07-01` hiển thị tên A'.
- **Kịch bản 2**: Cập nhật đơn vị cha tạo chu trình (A là con B, đặt B làm con A) → Bị hệ thống từ chối `400 Bad Request`.

### UAT-04: Danh mục & Luồng Đề xuất (FR-04)
- **Kịch bản 1**: `UnitEditor` gửi đề xuất phần mềm mới kèm mô tả → Trạng thái đề xuất là `Pending`.
- **Kịch bản 2**: `CatalogManager` phê duyệt đề xuất (`Accept`) → Tự động tạo phần mềm mới trong danh mục chung và cập nhật trạng thái đề xuất thành `Accepted`.

### UAT-05: Hồ sơ Triển khai, Revision & Chống Ghi đè Đồng thời (FR-05, FR-06)
- **Kịch bản 1**: Cán bộ tạo hồ sơ nháp (`Draft`), điền thông tin và tiến hành `Submit` → Trạng thái chuyển thành `Submitted`, các trường thông tin và tệp đính kèm bị khóa không cho chỉnh sửa trực tiếp.
- **Kịch bản 2 (Cấm tự duyệt)**: Người gửi (`submitted_by`) đăng nhập bằng tài khoản duyệt và cố tình gọi API `/approve` → Hệ thống chặn lại với lỗi nghiệp vụ.
- **Kịch bản 3 (Từ chối & Mở lại)**: Người duyệt bấm `Reject` và nhập lý do → Hồ sơ chuyển thành `Rejected`. Cán bộ tạo nháp bấm `Reopen` → Hồ sơ mở lại thành `Draft` để sửa.
- **Kịch bản 4 (Xung đột ETag)**: Người dùng A và B cùng mở form sửa Revision (Version 5). A bấm lưu trước (thành công, version lên 6). B bấm lưu sau với If-Match="5" → Bị từ chối `412 Precondition Failed`.

### UAT-06: Hợp đồng & Phân bổ Hạn mức License (FR-07)
- **Kịch bản 1**: Hợp đồng có hạn mức `Seat = 10`. Đơn vị phân bổ 6 license cho Deployment 1, 4 license cho Deployment 2 → Hợp lệ.
- **Kịch bản 2 (Cấp vượt quota)**: Phân bổ thêm 1 license cho Deployment 3 → Bị từ chối vì vượt quá 10 license.
- **Kịch bản 3 (Giao dịch đồng thời)**: 2 request đồng thời cùng xin 2 license cuối cùng → Nhờ khóa bi quan `FOR UPDATE`, chỉ đúng 1 request thành công, request còn lại bị từ chối.

### UAT-07: Nhập/Xuất Excel (FR-10, FR-11)
- **Kịch bản 1 (All-or-Nothing)**: File Excel 100 dòng có 1 dòng sai định dạng ngày → Toàn bộ 100 dòng bị từ chối, hệ thống trả về bảng danh sách chi tiết lỗi theo dòng/cột.
- **Kịch bản 2 (Commit an toàn)**: File Excel 50 dòng hợp lệ → Bấm xác nhận commit → 50 bản ghi nháp (`Draft`) được tạo trong cùng một transaction.
