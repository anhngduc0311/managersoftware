# Từ điển Dữ liệu (Data Dictionary)

> Tham chiếu: [SPEC.md — Mục 3.3, 3.4, 3.5](../SPEC.md#33-d-03--mô-hình-dữ-liệu)  
> Quy ước: Khóa chính UUIDv7/UUID; tên bảng/cột `snake_case`; thời gian `timestamptz` UTC; tiền tệ `numeric(18,2)`.

---

## 1. Schema: `iam` (Identity & Access Management)

### Bảng `users`
- `id` (UUID, PK): Định danh người dùng.
- `username` (VARCHAR(100), UNIQUE, NOT NULL): Tên đăng nhập chuẩn hóa (lowercase, trimmed).
- `display_name` (VARCHAR(200), NOT NULL): Họ và tên hiển thị.
- `email` (VARCHAR(256), NULL): Email liên hệ (tùy chọn trong MVP).
- `is_active` (BOOLEAN, NOT NULL, DEFAULT true): Trạng thái tài khoản.
- `security_stamp` (VARCHAR(256), NOT NULL): Dấu bảo mật phục vụ hủy phiên khi đổi quyền.
- `created_at` (TIMESTAMPTZ, NOT NULL), `updated_at` (TIMESTAMPTZ, NOT NULL).

### Bảng `roles` & `permissions`
- `roles`: `id` (UUID, PK), `code` (VARCHAR(50), UNIQUE), `name` (VARCHAR(200)), `description` (TEXT).
- `permissions`: `id` (UUID, PK), `code` (VARCHAR(100), UNIQUE), `name` (VARCHAR(200)), `group_name` (VARCHAR(100)).
- `role_permissions`: `role_id` (UUID, FK), `permission_id` (UUID, FK) -> PK `(role_id, permission_id)`.

### Bảng `user_role_scopes`
- `id` (UUID, PK): Định danh bản cấp quyền.
- `user_id` (UUID, FK -> users.id, NOT NULL).
- `role_id` (UUID, FK -> roles.id, NOT NULL).
- `scope_type` (VARCHAR(20), NOT NULL): `Global` hoặc `Organization`.
- `organization_id` (UUID, FK -> organizations.id, NULL khi `scope_type = Global`, NOT NULL khi `scope_type = Organization`).
- `include_descendants` (BOOLEAN, NOT NULL, DEFAULT false): Áp dụng cho cả đơn vị con trực thuộc.
- `valid_from` (TIMESTAMPTZ, NOT NULL): Bắt đầu hiệu lực.
- `valid_to` (TIMESTAMPTZ, NULL): Hết hiệu lực (NULL = vô hạn).

---

## 2. Schema: `organizations` (Đơn vị & Cơ cấu)

### Bảng `organizations`
- `id` (UUID, PK): Định danh ổn định của đơn vị.
- `code` (VARCHAR(50), UNIQUE, NOT NULL): Mã định danh chuẩn hóa (uppercase, no space).
- `is_active` (BOOLEAN, NOT NULL, DEFAULT true).
- `created_at` (TIMESTAMPTZ), `updated_at` (TIMESTAMPTZ).

### Bảng `organization_versions`
- `id` (UUID, PK): Bản ghi phiên bản theo thời gian.
- `organization_id` (UUID, FK -> organizations.id, NOT NULL).
- `name` (VARCHAR(255), NOT NULL): Tên cơ quan / đơn vị tại thời điểm hiệu lực.
- `parent_id` (UUID, FK -> organizations.id, NULL): Đơn vị cấp trên trực tiếp (cấm parent_id = organization_id).
- `valid_from` (DATE, NOT NULL): Ngày bắt đầu có hiệu lực.
- `valid_to` (DATE, NULL): Ngày kết thúc hiệu lực (NULL = hiện hành).

### Bảng `organization_successions`
- `id` (UUID, PK).
- `predecessor_id` (UUID, FK -> organizations.id, NOT NULL): Đơn vị tiền nhiệm.
- `successor_id` (UUID, FK -> organizations.id, NOT NULL): Đơn vị kế nhiệm.
- `effective_date` (DATE, NOT NULL): Ngày sáp nhập/chuyển giao.
- `note` (TEXT, NULL).

---

## 3. Schema: `catalog` (Danh mục phần mềm dùng chung)

### Bảng `software_categories` & `vendors`
- `software_categories`: `id` (UUID, PK), `code` (VARCHAR(50), UNIQUE), `name` (VARCHAR(255)), `is_active` (BOOLEAN).
- `vendors`: `id` (UUID, PK), `code` (VARCHAR(50), UNIQUE), `name` (VARCHAR(255)), `contact_info` (TEXT), `is_active` (BOOLEAN).

### Bảng `software`
- `id` (UUID, PK).
- `code` (VARCHAR(50), UNIQUE, NOT NULL): Mã phần mềm chuẩn hóa.
- `name` (VARCHAR(255), NOT NULL): Tên phần mềm.
- `category_id` (UUID, FK -> software_categories.id, NOT NULL).
- `vendor_id` (UUID, FK -> vendors.id, NOT NULL).
- `description` (TEXT, NULL).
- `lifecycle_status` (VARCHAR(50), NOT NULL, DEFAULT 'Active'): `Active | Deprecated | Retired`.
- `created_at` (TIMESTAMPTZ), `updated_at` (TIMESTAMPTZ).

### Bảng `software_releases`
- `id` (UUID, PK).
- `software_id` (UUID, FK -> software.id, NOT NULL).
- `version_name` (VARCHAR(50), NOT NULL): Tên phiên bản (VD: "v2.1.0").
- `release_date` (DATE, NOT NULL).
- `support_end_date` (DATE, NULL).
- Unique Constraint: `(software_id, version_name)`.

### Bảng `catalog_proposals`
- `id` (UUID, PK).
- `organization_id` (UUID, FK -> organizations.id, NOT NULL): Đơn vị đề xuất.
- `proposed_by_user_id` (UUID, FK -> users.id, NOT NULL).
- `software_name` (VARCHAR(255), NOT NULL), `description` (TEXT, NULL).
- `status` (VARCHAR(30), NOT NULL, DEFAULT 'Pending'): `Pending | Accepted | Rejected`.
- `rejection_reason` (TEXT, NULL).
- `created_software_id` (UUID, FK -> software.id, NULL).
- `reviewed_by_user_id` (UUID, FK -> users.id, NULL), `reviewed_at` (TIMESTAMPTZ, NULL).

---

## 4. Schema: `deployments` (Hồ sơ Triển khai & Phê duyệt)

### Bảng `deployments`
- `id` (UUID, PK).
- `software_id` (UUID, FK -> software.id, NOT NULL).
- `organization_id` (UUID, FK -> organizations.id, NOT NULL).
- `environment` (VARCHAR(50), NOT NULL): `Production | Test | Development`.
- `instance_key` (VARCHAR(50), NOT NULL, DEFAULT 'default'): Phân biệt các bản cài đặt cùng môi trường.
- `current_approved_revision_id` (UUID, NULL, FK -> deployment_revisions.id): Con trỏ bản đã duyệt hiện hành.
- `version` (BIGINT, NOT NULL, DEFAULT 1): Concurrency version token.
- Unique Constraint: `(software_id, organization_id, environment, instance_key)`.

### Bảng `deployment_revisions`
- `id` (UUID, PK).
- `deployment_id` (UUID, FK -> deployments.id, NOT NULL).
- `revision_no` (INT, NOT NULL): Số thứ tự revision (1, 2, 3...).
- `release_id` (UUID, FK -> software_releases.id, NULL khi Draft, NOT NULL khi Submit).
- `operational_status` (VARCHAR(50), NOT NULL, DEFAULT 'Planned'): `Planned | Piloting | Active | Suspended | Retired`.
- `progress_percent` (INT, NOT NULL, DEFAULT 0, CHECK 0 <= progress_percent <= 100).
- `start_date` (DATE, NULL khi Draft, NOT NULL khi Submit).
- `go_live_date` (DATE, NULL): Bắt buộc khi `operational_status = Active`.
- `responsible_user_id` (UUID, FK -> users.id, NULL khi Draft, NOT NULL khi Submit).
- `workflow_status` (VARCHAR(30), NOT NULL, DEFAULT 'Draft'): `Draft | Submitted | Approved | Rejected`.
- `submitted_by` (UUID, FK -> users.id, NULL), `submitted_at` (TIMESTAMPTZ, NULL).
- `approved_at` (TIMESTAMPTZ, NULL).
- `version` (BIGINT, NOT NULL, DEFAULT 1).
- Unique Constraint: `(deployment_id, revision_no)`.

### Bảng `deployment_milestones`
- `id` (UUID, PK).
- `deployment_revision_id` (UUID, FK -> deployment_revisions.id, NOT NULL).
- `name` (VARCHAR(255), NOT NULL), `due_date` (DATE, NOT NULL), `completed_at` (TIMESTAMPTZ, NULL).

### Bảng `approval_decisions`
- `id` (UUID, PK).
- `deployment_revision_id` (UUID, FK -> deployment_revisions.id, NOT NULL).
- `decision` (VARCHAR(30), NOT NULL): `Approved | Rejected`.
- `reason` (TEXT, NULL khi Approved, Bắt buộc NOT NULL khi Rejected).
- `actor_id` (UUID, FK -> users.id, NOT NULL).
- `decided_at` (TIMESTAMPTZ, NOT NULL).

---

## 5. Schema: `contracts` (Hợp đồng & Bản quyền)

### Bảng `contracts`
- `id` (UUID, PK).
- `contract_no` (VARCHAR(100), NOT NULL).
- `owning_organization_id` (UUID, FK -> organizations.id, NOT NULL).
- `vendor_id` (UUID, FK -> vendors.id, NOT NULL).
- `status` (VARCHAR(30), NOT NULL, DEFAULT 'Active'): `Draft | Active | Closed | Cancelled`.
- `signed_date` (DATE, NOT NULL), `start_date` (DATE, NOT NULL), `end_date` (DATE, NOT NULL).
- `total_amount` (NUMERIC(18,2), NOT NULL, CHECK total_amount >= 0).
- `currency_code` (VARCHAR(3), NOT NULL, DEFAULT 'VND').
- `maintenance_start_date` (DATE, NULL), `maintenance_end_date` (DATE, NULL).
- `version` (BIGINT, NOT NULL, DEFAULT 1).

### Bảng `contract_items`
- `id` (UUID, PK), `contract_id` (UUID, FK -> contracts.id, NOT NULL).
- `software_id` (UUID, FK -> software.id, NOT NULL).
- `description` (VARCHAR(500), NULL), `amount` (NUMERIC(18,2), NOT NULL, DEFAULT 0).

### Bảng `license_entitlements`
- `id` (UUID, PK), `contract_item_id` (UUID, FK -> contract_items.id, NOT NULL).
- `license_type` (VARCHAR(50), NOT NULL): `Seat | Unlimited`.
- `quantity` (INT, NULL khi Unlimited, NOT NULL CHECK quantity >= 0 khi Seat).
- `valid_from` (DATE, NOT NULL), `valid_to` (DATE, NOT NULL).

### Bảng `license_allocations`
- `id` (UUID, PK).
- `entitlement_id` (UUID, FK -> license_entitlements.id, NOT NULL).
- `deployment_id` (UUID, FK -> deployments.id, NOT NULL).
- `quantity` (INT, NOT NULL, CHECK quantity >= 0).
- Unique Constraint: `(entitlement_id, deployment_id)`.

---

## 6. Schema: `documents`, `audit`, `notifications`, `reporting`

- **documents**: `id`, `storage_key`, `original_name`, `content_type`, `size_bytes`, `checksum_sha256`, `scan_status ('Pending'|'Scanning'|'Clean'|'Rejected')`, `uploaded_by`, `created_at`.
- **audit_logs**: `id`, `actor_id`, `action`, `entity_type`, `entity_id`, `organization_id`, `before_json`, `after_json`, `occurred_at`, `correlation_id`.
- **notifications**: `id`, `recipient_user_id`, `type`, `title`, `message`, `target_route`, `deduplication_key` (UNIQUE), `read_at`, `created_at`.
- **background_jobs**: `id`, `type`, `payload_json`, `status ('Queued'|'Running'|'Succeeded'|'Failed')`, `attempts`, `lease_owner`, `lease_token`, `lease_until`, `next_run_at`, `created_at`.
- **import_batches**: `id`, `requester_user_id`, `organization_id`, `file_document_id`, `status ('Uploaded'|'Validating'|'Ready'|'Invalid'|'Committing'|'Committed'|'Failed')`, `total_rows`, `valid_rows`, `error_rows`, `created_at`.
- **import_row_errors**: `id`, `batch_id`, `row_index`, `column_name`, `error_code`, `error_message`.
- **export_jobs**: `id`, `requester_user_id`, `filters_json`, `status ('Queued'|'Running'|'Succeeded'|'Failed'|'Expired')`, `document_id`, `expires_at`, `created_at`.
