# Thông cáo Phát hành Phiên bản Chính thức (Release Notes v1.0.0)

> **Tên sản phẩm**: Hệ thống Quản lý Phần mềm Chuyển đổi số tỉnh Lào Cai  
> **Phiên bản**: v1.0.0 (Bản phát hành chính thức - Production Release)  
> **Ngày phát hành**: 2026-09-21  
> **Cơ quan chủ quản**: Sở Thông tin và Truyền thông tỉnh Lào Cai  

---

## 1. Tổng quan Sản phẩm Bàn giao

Hệ thống Quản lý Phần mềm Chuyển đổi số tỉnh Lào Cai phiên bản v1.0.0 được xây dựng hoàn chỉnh theo kiến trúc Monolith hiện đại:
- **Backend**: .NET 10 LTS, C# 14, Entity Framework Core 10, PostgreSQL 18.
- **Frontend**: Angular 22 với Angular Signals, Vanilla SCSS Design System hiện đại, hỗ trợ Responsive toàn diện.
- **Worker**: Tiến trình xử lý tác vụ nền Transactional Outbox trên nền tảng PostgreSQL `SKIP LOCKED` với cơ chế bảo vệ lease token.
- **An toàn Thông tin**: Lưu trữ tệp tin cách ly ngoài webroot, bộ quét mã độc đa tầng, chống Formula Injection và CSDL kiểm toán bất biến (Append-Only).

---

## 2. Các Tính năng Chính theo Từng Giai đoạn Phát triển

### Giai đoạn 1 & 2: Nền tảng Hạt nhân & Quản trị Danh mục
- Xác thực Cookie bảo mật (`HttpOnly`, `SameSite=Lax`) kèm token chống tấn công CSRF (`X-XSRF-TOKEN`).
- Phân quyền theo Phạm vi Đơn vị (**Scope-Based Authorization**) với 3 cấp độ: `Global`, `Organization`, `Descendants`.
- Quản lý Cây đơn vị hành chính theo thời gian nửa mở `[valid_from, valid_to)` hỗ trợ lịch sử sáp nhập/đổi tên.
- Danh mục phần mềm dùng chung toàn tỉnh, phiên bản phát hành, nhà cung cấp và luồng đề xuất bổ sung danh mục.

### Giai đoạn 3 & 4: Hồ sơ Triển khai, Quy trình Phê duyệt & Quản lý Bản quyền
- Hồ sơ triển khai đa phiên bản (**Revisions**): Quản lý vòng đời `Draft → Submitted → Approved / Rejected → Reopen`.
- Chống ghi đè đồng thời bằng **ETag Optimistic Concurrency Control (OCC)** (mã lỗi HTTP 412).
- Cấm tự phê duyệt (**Anti-Self-Approval**): Người tạo hồ sơ không được phép tự duyệt hồ sơ của mình.
- Quản lý hợp đồng mua sắm, bảo trì và hạn mức bản quyền (`Seat` và `Unlimited`).
- **Khóa bi quan (Pessimistic Locking `FOR UPDATE`)** ngăn ngừa triệt để việc cấp phát vượt quá quota còn lại.
- Kho lưu trữ tệp tin cách ly (`Quarantine` vs `Clean`) kèm bộ lọc kiểm tra chữ ký PE/ELF, chống Zip-bomb và macro độc hại.

### Giai đoạn 5 & 6: Báo cáo asOf, Nhập/Xuất Excel, Tự động Hóa & An ninh Vận hành
- **Dashboard Điều hành**: Báo cáo tổng quan thời gian thực và tại bất kỳ thời điểm quá khứ nào (`asOf`), tỷ lệ bao phủ theo danh mục đủ điều kiện (`CoverageEligibility`), tự động che giấu số liệu tài chính đối với người không có quyền.
- **Nhập / Xuất Excel Hàng loạt**: Mẫu chuẩn ClosedXML, chống tấn công **Formula Injection** (`=`, `+`, `-`, `@`), cơ chế **All-or-Nothing** (1 dòng lỗi hủy toàn bộ batch, 0 dòng lỗi mới commit nháp).
- **Xuất báo cáo nền**: Xử lý qua Worker, file tạm được bảo vệ với thời hạn lưu trữ TTL 24 giờ.
- **Tự động Nhắc hạn**: Quét hợp đồng/license/bảo trì sắp hết hạn tại các mốc **30 ngày**, **15 ngày**, **7 ngày** kèm Deduplication Key chống lặp thông báo.
- **Nhật ký Kiểm toán Bất biến (Append-Only Audit UI)**: Tra cứu lịch sử thay đổi, xem Before/After JSON diff trực quan đã lọc sạch thông tin nhạy cảm.
- **Hạ tầng Staging/Production**: Docker multi-stage (API, Worker, Frontend SPA/Nginx), kịch bản sao lưu PITR và diễn tập phục hồi thảm họa đạt RPO < 15 phút, RTO < 2 giờ.

---

## 3. Danh mục Kiểm tra Khói sau Triển khai (Post-Deployment Smoke Test)

Sau khi triển khai lên môi trường Production, thực hiện lần lượt các bước kiểm tra sau:

- [x] **Kiểm tra Health Probes**:
  ```bash
  curl -k https://localhost/healthz/live   # Kỳ vọng: HTTP 200 Healthy
  curl -k https://localhost/healthz/ready  # Kỳ vọng: HTTP 200 Healthy
  ```
- [x] **Kiểm tra Đăng nhập & CSRF**: Đăng nhập bằng tài khoản Quản trị `admin` và tài khoản cán bộ đơn vị `editor_baothang`.
- [x] **Kiểm tra Dashboard**: Tải trang Dashboard điều hành, kiểm tra hiển thị số liệu KPI và thay đổi bộ lọc ngày `asOf`.
- [x] **Kiểm tra Hồ sơ Triển khai**: Tạo thử 1 hồ sơ triển khai nháp, gửi duyệt và đăng nhập tài khoản Lãnh đạo để phê duyệt.
- [x] **Kiểm tra Worker Job**: Kiểm tra log Worker xử lý job quét tệp và gửi thông báo in-app:
  ```bash
  docker logs laocai_prod_worker --tail 30
  ```
- [x] **Kiểm tra Tải mẫu Excel**: Vào chức năng Nhập/Xuất Excel, tải file mẫu `.xlsx` và mở kiểm tra sheet danh mục tham chiếu.
- [x] **Kiểm tra Nhật ký Kiểm toán**: Vào menu Nhật ký Kiểm toán, tra cứu bản ghi thao tác vừa thực hiện và mở modal xem Before/After diff.

---

## 4. Hướng dẫn Hỗ trợ Kỹ thuật
- **Đầu mối tiếp nhận yêu cầu hỗ trợ**: Trung tâm Công nghệ Thông tin và Truyền thông — Sở TT&TT tỉnh Lào Cai.
- **Hệ thống Ticket/Hỗ trợ nội bộ**: `hotro.phanmem@laocai.gov.vn`
