# Sổ tay Vận hành Hệ thống (System Operations Runbook)

> **Hệ thống Quản lý Phần mềm Chuyển đổi số tỉnh Lào Cai**  
> **Dành cho**: Cán bộ Quản trị Hệ thống (SystemAdmin) & Đội ngũ Vận hành (DevOps / IT Support)

---

## 1. Giám sát Tiến trình Nền (Background Worker Monitoring)

Tiến trình Worker chịu trách nhiệm xử lý các tác vụ bất đồng bộ quan trọng:
- Quét an toàn tệp tin (`document.scan`)
- Thẩm định dữ liệu Excel (`deployment.import.validate`)
- Xuất báo cáo Excel (`deployment.export`)
- Quét thông báo nhắc hạn hợp đồng / license 30-15-7 ngày (`reminder.expiration`)
- Gửi thông báo luồng duyệt (`workflow.notification`)

### Lệnh kiểm tra trạng thái hàng đợi Job trong PostgreSQL:
```sql
-- Kiểm tra tổng số job theo trạng thái
SELECT status, type, COUNT(*) AS total
FROM jobs.background_jobs
GROUP BY status, type
ORDER BY status, total DESC;

-- Kiểm tra các job đang chạy (Running) và thời hạn lease
SELECT id, type, lease_owner, lease_until, attempts, updated_at
FROM jobs.background_jobs
WHERE status = 'Running'
ORDER BY updated_at DESC;

-- Kiểm tra các job bị lỗi (Failed)
SELECT id, type, attempts, error_message, updated_at
FROM jobs.background_jobs
WHERE status = 'Failed'
ORDER BY updated_at DESC
LIMIT 20;
```

### Xử lý Job bị kẹt (Stuck Job Recovery):
Cơ chế **Lease Token** tự động thu hồi job sau 2 phút nếu worker gặp sự cố (crash hoặc bị kill). Để reset thủ công các job bị lỗi sang trạng thái hàng đợi:
```sql
UPDATE jobs.background_jobs
SET status = 'Queued', lease_owner = NULL, lease_token = NULL, next_run_at = NOW()
WHERE status = 'Running' AND lease_until < NOW();
```

---

## 2. Quản lý Kho tệp và Quét An toàn (File Scanner)

Hệ thống áp dụng kiến trúc lưu trữ 2 vùng cách ly:
- **Vùng Cách ly (Quarantine)**: `App_Data/Storage/quarantine/` — Tệp tải lên tạm thời, client không thể tải xuống.
- **Vùng Sạch (Clean)**: `App_Data/Storage/clean/` — Tệp đã vượt qua bộ lọc mã độc và kiểm tra chữ ký định dạng.

### Lệnh đối soát tính toàn vẹn kho tệp định kỳ:
Chạy script tự động hàng ngày:
```powershell
.\scripts\audit-blob-integrity.ps1
```

### Xử lý tệp bị từ chối (Rejected Files):
Khi người dùng phản ánh tệp văn bản hợp lệ nhưng bị từ chối:
1. Tra cứu lý do từ chối trong CSDL:
   ```sql
   SELECT id, original_name, scan_status, scan_message, created_at
   FROM documents.documents
   WHERE scan_status = 'Rejected'
   ORDER BY created_at DESC LIMIT 10;
   ```
2. Nếu tệp vi phạm quy tắc an toàn (chứa macro VBA, tỷ lệ nén zip-bomb > 50:1, header PE/DOS), hướng dẫn cán bộ lưu tệp dưới định dạng chuẩn PDF hoặc XLSX thuần túy không chứa script.

---

## 3. Lịch trình Sao lưu Định kỳ (Backup Schedule)

| Loại sao lưu | Tần suất | Thời điểm | Thời gian lưu trữ (Retention) | Lệnh thực thi |
| :--- | :--- | :--- | :---: | :--- |
| **WAL Continuous** | Liên tục | Khi đầy 16MB WAL | 30 ngày | Cấu hình tự động `archive_command` |
| **Base Backup DB** | Hàng ngày | 01:00 AM UTC | 90 ngày | `.\scripts\backup-pitr.ps1` |
| **Storage Clean** | Hàng ngày | 01:30 AM UTC | 90 ngày | Tích hợp trong `backup-pitr.ps1` |
| **Diễn tập Restore** | Hàng quý | Ngày đầu quý | Biên bản lưu 1 năm | `.\scripts\restore-drill.ps1` |

---

## 4. Kiểm toán Bảo mật (Audit Log Inspection)

Hệ thống áp dụng cơ chế **Append-Only** (không thể sửa hoặc xóa nhật ký kiểm toán trong CSDL):
- Tra cứu hành động của người dùng cụ thể:
  ```sql
  SELECT action, entity_type, entity_id, actor_user_name, correlation_id, occurred_at
  FROM audit.audit_logs
  WHERE actor_user_name = 'editor_baothang'
  ORDER BY occurred_at DESC LIMIT 50;
  ```
- Kiểm tra các thao tác can thiệp phân quyền hoặc phê duyệt hồ sơ:
  ```sql
  SELECT action, entity_type, entity_id, actor_user_name, occurred_at
  FROM audit.audit_logs
  WHERE action IN ('Approve', 'Reject', 'AssignRole', 'Revoke')
  ORDER BY occurred_at DESC;
  ```

---

## 5. Xử lý Sự cố Thường gặp (Troubleshooting Guide)

### Sự cố 1: Người dùng gặp lỗi `412 Precondition Failed` khi lưu form
- **Nguyên nhân**: Dữ liệu hồ sơ triển khai đã được cập nhật bởi một phiên làm việc khác (xung đột ETag).
- **Cách xử lý**: Hướng dẫn người dùng bấm nút "Tải lại dữ liệu mới nhất" trên giao diện. Form Angular sẽ đối chiếu dữ liệu cục bộ và dữ liệu mới nhất để người dùng cập nhật mà không bị mất nội dung đã nhập.

### Sự cố 2: Worker bị dừng hoặc không nhận job
- **Kiểm tra**:
  ```bash
  docker logs laocai_prod_worker --tail 100
  ```
- **Khắc phục**: Khởi động lại container worker:
  ```bash
  docker compose -f docker-compose.prod.yml restart backend-worker
  ```

### Sự cố 3: Không thể gửi đề xuất hoặc phê duyệt hồ sơ
- **Kiểm tra**: Cán bộ có đúng phạm vi Scope tại đơn vị hay không. Cán bộ chỉ có thể thao tác với các hồ sơ thuộc cơ quan mình được phân quyền trong bảng `iam.user_role_scopes`.
