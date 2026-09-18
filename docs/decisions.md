# Ghi nhận Quyết định & Điểm cần xác nhận (Decisions Log)

> Tham chiếu: [SPEC.md — Mục 2.1](../SPEC.md#21-giả-định-và-điểm-cần-xác-nhận)  
> Ngày lập: 18/09/2026 | Trạng thái: Cơ sở để phát triển và kiểm thử kỹ thuật.

| Mã | Nội dung cần xác nhận | Trạng thái | Mặc định để phát triển thử nghiệm | Bên xác nhận đề xuất | Ngày cập nhật |
| --- | --- | --- | --- | --- | --- |
| **Q-01** | Quản lý phần mềm đã hoàn thành; biểu mẫu nhập và trường bắt buộc | Người dùng đã xác nhận phạm vi; biểu mẫu còn cần chốt | Quản lý danh mục, sử dụng/vận hành; bỏ progress_percent và milestone phát triển; D-04 và Excel v1 theo phạm vi mới | Người dùng xác nhận phạm vi; chủ quản, đại diện đơn vị chốt biểu mẫu | 18/09/2026 |
| **Q-02** | Nguồn mã đơn vị, cây tổ chức, lịch sử sáp nhập/chia tách | Dữ liệu giả lập | Sinh dữ liệu fixture A/B/C có quan hệ cha-con và sáp nhập; không seed phỏng đoán | Đầu mối dữ liệu tổ chức | 18/09/2026 |
| **Q-03** | Ma trận quyền, quyền hợp đồng và phạm vi con | Tạm khóa theo SPEC | 7 vai trò chuẩn + ma trận vai trò tại `docs/access-matrix.md`; cấm cấp Global ngầm | Chủ quản, Quản trị truy cập | 18/09/2026 |
| **Q-04** | Quy trình duyệt 1 hay nhiều cấp, ký số, tự duyệt | Tạm khóa theo SPEC | Phê duyệt 1 cấp; cấm tự duyệt (`actor != submitted_by`); không ngoại lệ trong MVP | Chủ quy trình | 18/09/2026 |
| **Q-05** | Hạ tầng, scanner, SSO, SMTP, giám sát | Đã xác định cho MVP | Docker Compose Linux; tài khoản nội bộ (ASP.NET Identity + Cookie); chưa dùng email | Vận hành | 18/09/2026 |
| **Q-06** | Quy mô tài khoản, dữ liệu, tệp, người dùng đồng thời | Tạm khóa theo NFR-04 | 100 người đồng thời, 100.000 deployment, 1 triệu audit log | Chủ quản, Vận hành | 18/09/2026 |
| **Q-07** | Mẫu Excel, định nghĩa kỳ báo cáo, đơn vị đủ điều kiện | Tạm khóa theo D-09 | Mẫu Excel 1 sheet; chỉ số Overview theo D-08; asOf theo thời điểm duyệt | Đầu mối báo cáo | 18/09/2026 |
| **Q-08** | Phân loại dữ liệu, lưu trữ, backup, RPO/RTO | Tạm khóa theo NFR-06 | RPO ≤ 1 giờ, RTO ≤ 4 giờ; PostgreSQL base backup + WAL/PITR; kho tệp riêng biệt | Chủ quản, Vận hành | 18/09/2026 |
| **Q-09** | Loại license, cấp license chéo đơn vị, bảo trì | Tạm khóa theo D-05 | Loại `Seat` (có quota số nguyên) và `Unlimited` (null quota); chỉ cấp trong đơn vị sở hữu | Chủ nghiệp vụ hợp đồng | 18/09/2026 |
| **Q-10** | Quyền xem nháp, tạo hợp đồng, quản lý đơn vị, đề xuất danh mục | Đã xác định theo D-02 | Tách riêng permission: `deployments.read_drafts`, `contracts.read`, `catalog.propose` | Chủ quản | 18/09/2026 |
