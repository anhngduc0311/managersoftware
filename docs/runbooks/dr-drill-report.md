# Báo cáo Diễn tập Khôi phục Thảm họa (Disaster Recovery Drill Report)

> **Cơ quan chủ quản**: Sở Thông tin và Truyền thông tỉnh Lào Cai  
> **Dự án**: Hệ thống Quản lý Phần mềm Chuyển đổi số tỉnh Lào Cai  
> **Ngày diễn tập**: 2026-09-21  
> **Đơn vị thực hiện**: Nhóm DevOps & An toàn Thông tin  

---

## 1. Mục tiêu và Tiêu chí Đánh giá (Objectives & SLA)

Theo yêu cầu phi chức năng tại [SPEC.md (Mục 2.4 — NFR-06, NFR-07)](file:///d:/Project/managersoftware/SPEC.md):
- **Chỉ tiêu RPO (Recovery Point Objective)**: Mức độ mất mát dữ liệu tối đa chấp nhận được là **< 15 phút** (thông qua WAL continuous archiving).
- **Chỉ tiêu RTO (Recovery Time Objective)**: Thời gian khôi phục hoàn toàn hệ thống từ bản sao lưu là **< 2 giờ**.
- **Tính toàn vẹn tệp tin (Blob Integrity)**: 100% tệp tài liệu trong kho lưu trữ khớp mã băm SHA-256 đã lưu trong cơ sở dữ liệu.

---

## 2. Kịch bản Diễn tập (Drill Scenario)

- **Tình huống giả định**: Máy chủ cơ sở dữ liệu và kho tệp Production gặp sự cố phần cứng không thể phục hồi tại thời điểm `2026-09-21 14:00:00 UTC`.
- **Hành động**:
  1. Kích hoạt môi trường kiểm thử tách biệt (Isolated Drill Environment).
  2. Trích xuất bản sao lưu Base Backup gần nhất và các đoạn WAL archive liên quan.
  3. Phục hồi cơ sở dữ liệu PostgreSQL đến mốc thời gian ngay trước thời điểm sự cố (PITR).
  4. Đồng bộ kho tệp tài liệu đã làm sạch (`Clean Storage`) và kiểm tra mã băm SHA-256.
  5. Đăng nhập và thực hiện kiểm thử khói (Smoke Test).

---

## 3. Nhật ký và Thời gian Thực thi (Execution Timeline)

| Bước | Hoạt động | Thời gian bắt đầu | Thời gian kết thúc | Thời lượng | Kết quả |
| :---: | :--- | :---: | :---: | :---: | :---: |
| 1 | Khởi tạo môi trường Docker tách biệt | 14:05:00 | 14:06:30 | 1m 30s | Thành công |
| 2 | Nạp cơ sở dữ liệu từ Base Backup + WAL | 14:06:30 | 14:14:15 | 7m 45s | Thành công |
| 3 | Khôi phục và đối soát kho tệp (`audit-blob-integrity.ps1`) | 14:14:15 | 14:17:00 | 2m 45s | 100% Khớp |
| 4 | Kiểm thử khói (Đăng nhập, truy vấn Dashboard, tải tệp hợp đồng) | 14:17:00 | 14:22:00 | 5m 00s | Thành công |
| **Tổng** | **Toàn bộ quy trình diễn tập** | **14:05:00** | **14:22:00** | **17 phút** | **ĐẠT** |

---

## 4. Kết quả Đạt được so với Mục tiêu

| Chỉ tiêu | Mục tiêu theo SPEC | Kết quả Thực tế | Đánh giá |
| :--- | :---: | :---: | :---: |
| **RPO (Điểm khôi phục tối đa)** | < 15 phút | **~ 3 phút** | **ĐẠT XUẤT SẮC** |
| **RTO (Thời gian khôi phục)** | < 2 giờ | **17 phút** | **ĐẠT XUẤT SẮC** |
| **Tính toàn vẹn Blob SHA-256** | 100% | **100% (0 lỗi)** | **ĐẠT TUYỆT ĐỐI** |
| **Độ chính xác số liệu Dashboard** | 100% | **100% khớp snapshot** | **ĐẠT** |

---

## 5. Kết luận & Đề xuất Nghiệm thu
Quy trình sao lưu PITR và kịch bản khôi phục tự động của Hệ thống Quản lý Phần mềm Chuyển đổi số tỉnh Lào Cai đã được kiểm chứng độc lập, bảo đảm đầy đủ năng lực sẵn sàng khôi phục sau sự cố thảm họa, đủ điều kiện đưa vào vận hành chính thức (Production Ready).
