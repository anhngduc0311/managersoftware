# Quy trình Rollback Ứng dụng và Cơ sở Dữ liệu (Rollback Runbook)

> **Hệ thống Quản lý Phần mềm Chuyển đổi số tỉnh Lào Cai**  
> **Áp dụng cho**: Môi trường Staging và Production  
> **Mục tiêu**: Hồi phục trạng thái hoạt động ổn định của hệ thống trong thời gian ngắn nhất (< 15 phút) khi phát sinh sự cố nghiêm trọng sau triển khai.

---

## 1. Nguyên tắc Phân loại Sự cố & Điều kiện Rollback

| Mức độ | Hiện tượng | Biện pháp xử lý | Cần Rollback DB? |
| :--- | :--- | :--- | :--- |
| **P1 — Nghiêm trọng** | Toàn bộ hệ thống sập, API trả về 500 liên tục, mất kết nối cơ sở dữ liệu, rò rỉ dữ liệu hoặc lỗi phân quyền Scope. | Kích hoạt Rollback khẩn cấp toàn bộ hệ thống. | Tùy thuộc vào Schema Migration. |
| **P2 — Trung bình** | Lỗi một tính năng phụ (ví dụ: Job xuất Excel bị chậm, thông báo in-app gửi trễ). | Hotfix container Worker hoặc cấu hình, không rollback API. | Không. |
| **P3 — Nhẹ** | Lỗi hiển thị giao diện Frontend (UI layout, CSS). | Rollback container Frontend hoặc cập nhật bản build SPA mới. | Không. |

---

## 2. Quy tắc Tương thích Cơ sở Dữ liệu (Backward-Compatible Schema)

Theo nguyên tắc thiết kế tại [SPEC.md](file:///d:/Project/managersoftware/SPEC.md), mọi thay đổi Migration cơ sở dữ liệu phải bảo đảm **tương thích ngược (Backward-Compatible)**:
1. **Chỉ thêm mới cột (Add Column)** với giá trị mặc định hoặc cho phép `NULL`, không xóa cột hoặc đổi tên cột trong cùng một phiên bản triển khai.
2. **Không xóa bảng (Drop Table)** khi phiên bản ứng dụng trước đó vẫn đang tham chiếu.
3. Nếu migration đã thêm cột hoặc bảng mới: Việc rollback ứng dụng Backend/Frontend về image trước đó **hoàn toàn an toàn và không cần rollback cơ sở dữ liệu**.

---

## 3. Các bước Thực hiện Rollback Ứng dụng (Container Rollback)

### Bước 1: Xác định phiên bản ổn định gần nhất (Known Good Version)
```bash
# Kiểm tra lịch sử tag image Docker
docker images | grep laocai
# Ví dụ: Phiên bản lỗi là v1.0.1, phiên bản ổn định là v1.0.0
```

### Bước 2: Chuyển hướng traffic sang phiên bản trước (Zero-Downtime Rollback)
Sử dụng `docker-compose.prod.yml`:
```bash
# 1. Cập nhật biến môi trường IMAGE_TAG về phiên bản ổn định
export IMAGE_TAG=v1.0.0

# 2. Khởi động lại các container Backend API, Worker và Frontend Gateway
docker compose -f docker-compose.prod.yml up -d --no-deps backend-api backend-worker frontend-gateway

# 3. Kiểm tra tính sẵn sàng của hệ thống (Readiness Probe)
curl -k https://localhost/healthz/ready
```

### Bước 3: Xác minh kiểm tra khói (Smoke Test)
1. Đăng nhập với tài khoản `admin` và tài khoản cán bộ đơn vị.
2. Kiểm tra tải trang Dashboard: `GET /api/v1/dashboard/overview`.
3. Kiểm tra danh sách hồ sơ triển khai: `GET /api/v1/deployments`.
4. Kiểm tra worker claim jobs: `docker logs laocai_prod_worker --tail 50`.

---

## 4. Quy trình Rollback Cơ sở Dữ liệu (Khi bắt buộc)

> [!CAUTION]
> Rollback Migration cơ sở dữ liệu có thể dẫn đến mất các dữ liệu mới phát sinh trong khoảng thời gian từ lúc triển khai đến lúc rollback. Chỉ thực hiện khi có phê duyệt của Trưởng nhóm Kỹ thuật và đã tạo bản sao lưu khẩn cấp trước đó.

```powershell
# 1. Tạo bản sao lưu khẩn cấp dữ liệu hiện tại trước khi can thiệp
.\scripts\backup-pitr.ps1 -BackupDir "backups/pre_rollback"

# 2. Revert migration về phiên bản trước bằng EF Core CLI
cd backend/src/LaoCai.SoftwareManagement.Infrastructure
dotnet ef database update <TenMigrationTruocDo> -s ../LaoCai.SoftwareManagement.Api

# 3. Khởi động lại API và Worker
docker compose -f docker-compose.prod.yml restart backend-api backend-worker
```

---

## 5. Báo cáo Sự cố sau Rollback (Post-Mortem)
Trong vòng 24 giờ sau khi rollback thành công, nhóm vận hành phải lập biên bản sự cố gồm:
- Nguyên nhân gốc rễ (Root Cause).
- Thời gian gián đoạn dịch vụ (Downtime).
- Biện pháp khắc phục trong mã nguồn và kịch bản kiểm thử bổ sung nhằm ngăn chặn tái diễn.
