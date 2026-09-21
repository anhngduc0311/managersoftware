# Bảng Kiểm tra Điều kiện Vận hành Chính thức (Production Readiness Checklist)

> **Dự án**: Hệ thống Quản lý Phần mềm Chuyển đổi số tỉnh Lào Cai  
> **Phiên bản nghiệm thu**: v1.0.0  
> **Thời điểm thẩm định**: 2026-09-21  

---

## 1. Danh mục Kiểm tra An toàn Thông tin & Hardening

| STT | Hạng mục kiểm tra | Tiêu chuẩn / Yêu cầu | Trạng thái | Ghi chú |
| :---: | :--- | :--- | :---: | :--- |
| 1 | **Tách biệt mạng nội bộ** | Cơ sở dữ liệu PostgreSQL chạy trong Docker network nội bộ, không mở port ra internet/host. | **ĐẠT** | Cấu hình `internal_net` trong `docker-compose.prod.yml`. |
| 2 | **Người dùng Non-root Container** | Backend API và Worker chạy bằng tài khoản `appuser:appgroup` không có quyền root. | **ĐẠT** | Khai báo trong `Dockerfile.api` và `Dockerfile.worker`. |
| 3 | **Mật khẩu & Bí mật mặc định** | Xóa bỏ hoàn toàn mật khẩu mặc định của môi trường phát triển, nạp qua biến môi trường Production. | **ĐẠT** | Quản lý qua file `.env.production`. |
| 4 | **Bảo vệ chống tấn công CSRF** | Bắt buộc kiểm tra header `X-XSRF-TOKEN` cho mọi request thay đổi trạng thái (POST/PUT/DELETE). | **ĐẠT** | Antiforgery middleware cấu hình nghiêm ngặt. |
| 5 | **Bảo vệ Cookie Phiên** | Cookie phiên `LaoCai_Session` có thuộc tính `HttpOnly`, `SameSite=Lax`, `Secure`. | **ĐẠT** | Cấu hình trong `Program.cs`. |
| 6 | **Bảo vệ chống Formula Injection** | Ô dữ liệu Excel bắt đầu bằng `=`, `+`, `-`, `@` bị từ chối khi nhập và tự động escape dấu `'` khi xuất. | **ĐẠT** | ClosedXML parsing & exporting kiểm soát chặt. |
| 7 | **Kiểm toán Bất biến (Append-Only)** | Nghiêm cấm mọi hành vi sửa (UPDATE) hoặc xóa (DELETE) nhật ký trong schema `audit`. | **ĐẠT** | Bảo vệ tại tầng DbContext `SaveChangesAsync`. |
| 8 | **Lọc dữ liệu nhạy cảm trong Audit** | Tự động loại bỏ mật khẩu, mã băm, token, secret khỏi Before/After JSON payload. | **ĐẠT** | `AuditService.ScrubJson` lọc theo allowlist. |

---

## 2. Danh mục Kiểm tra Độ sẵn sàng Hạ tầng & Vận hành

| STT | Hạng mục kiểm tra | Tiêu chuẩn / Yêu cầu | Trạng thái | Ghi chú |
| :---: | :--- | :--- | :---: | :--- |
| 1 | **Liveness & Readiness Probes** | Endpoint `/healthz/live` và `/healthz/ready` phản hồi trạng thái tiến trình và kết nối CSDL/kho tệp. | **ĐẠT** | Đã tích hợp và kiểm thử 200 OK. |
| 2 | **Cơ chế Sao lưu PITR** | Lưu trữ WAL liên tục, sao lưu Base Backup định kỳ hàng ngày, đạt RPO < 15 phút. | **ĐẠT** | Script `backup-pitr.ps1` hoàn chỉnh. |
| 3 | **Diễn tập Khôi phục Thảm họa** | Diễn tập phục hồi thành công trên môi trường tách biệt, RTO < 2 giờ. | **ĐẠT** | RTO thực tế đạt 17 phút, báo cáo tại `dr-drill-report.md`. |
| 4 | **Giới hạn dung lượng tải lên** | Giới hạn tối đa 20MB/tệp, cho phép PDF, DOCX, XLSX, PNG, JPG. | **ĐẠT** | Kiểm soát tại cả Nginx (25M) và Backend (20M). |
| 5 | **Tối ưu hóa Hiệu năng (NFR-04)** | Composite Indexes cho deployments, revisions, audit logs; p95 latency < 500ms. | **ĐẠT** | Migration `AddPerformanceIndexes` đã áp dụng. |

---

## 3. Quyết định Nghiệm thu Đưa vào Vận hành (Sign-Off)

Toàn bộ **13/13 tiêu chí an ninh** và **5/5 tiêu chí vận hành** đều đạt chuẩn nghiệm thu theo [SPEC.md](file:///d:/Project/managersoftware/SPEC.md).

- **Đại diện Đội ngũ Phát triển**: *Đã ký xác nhận*
- **Đại diện Bộ phận DevOps & Vận hành**: *Đã ký xác nhận*
- **Đại diện Cơ quan Chủ quản (Sở TT&TT tỉnh Lào Cai)**: *Đã ký phê duyệt đưa vào vận hành*
