# HỆ THỐNG QUẢN LÝ PHẦN MỀM - TỈNH LÀO CAI

Hệ thống chuyên trách Quản lý Danh mục Phần mềm, Nhóm phân loại, Nhà cung cấp / Đối tác và Phiên bản phát hành dùng chung phục vụ công cuộc Chuyển đổi số tỉnh Lào Cai.

---

## 🚀 Tính năng Cốt lõi

1. **Quản lý Danh mục Phần mềm Dùng chung**:
   - Tra cứu, tìm kiếm phần mềm theo Nhóm phân loại, Nhà cung cấp và Trạng thái vòng đời (*Active*, *Deprecated*, *Retired*).
   - Form thông minh với gợi ý mẫu 1-click, tự động sinh mã code chuẩn hóa, thêm nhanh Nhóm/NCC inline.
   - Quản lý phiên bản phát hành (Releases Timeline) theo từng phần mềm.
2. **Quản lý Nhóm phân loại**:
   - Phân cấp nhóm phần mềm chuyên ngành (Chính quyền số, Kinh tế số, Xã hội số,...).
3. **Quản lý Nhà cung cấp & Đối tác**:
   - Lưu trữ danh bạ, mã định danh, hotline và email hỗ trợ của các đối tác công nghệ.
4. **Quản trị Người dùng & Phân quyền Thẩm quyền (Scope-based RBAC)**:
   - Quản lý tài khoản, đặt lại mật khẩu, khóa/mở khóa nhanh.
   - Gán quyền linh hoạt theo phạm vi: Toàn tỉnh (`Global`) hoặc Đơn vị (`Organization`).
5. **Giao diện Tinh gọn & Chuẩn Enterprise**:
   - Sử dụng hệ thống Vector SVG Icons chuẩn sắc nét, thao tác Icon-only trực quan.

---

## 🛠️ Công nghệ Sử dụng

- **Backend**: .NET 8 Web API, Entity Framework Core 8, PostgreSQL, Redis, Clean Architecture, CQRS (MediatR), FluentValidation.
- **Frontend**: Angular 19, TypeScript, Reactive Forms, Signals, SCSS, SVG Vector Icons, Vanilla Theme.
- **Bảo mật**: Session-based Cookie HttpOnly + CSRF Protection (`XSRF-TOKEN`), Data Protection, Password Hashing.

---

## 🔑 Tài khoản Truy cập Mặc định

Thông tin cấu hình tài khoản quản trị viên và chuỗi kết nối lưu tại [`.env`](file:///d:/Project/managersoftware/.env):

| Tài khoản | Mật khẩu | Quyền hạn |
| :--- | :--- | :--- |
| **`admin`** | **`Admin@123456`** | Quản trị viên Hệ thống (SystemAdmin - Toàn quyền) |

---

## ⚙️ Hướng dẫn Chạy Hệ thống

### 1. Khởi động Cơ sở dữ liệu (PostgreSQL)
```bash
docker compose up -d
```

### 2. Khởi chạy Backend API
```bash
cd backend/src/LaoCai.SoftwareManagement.Api
dotnet run
```
*Backend API chạy tại: `http://localhost:5105` (Swagger: `http://localhost:5105/swagger`)*

### 3. Khởi chạy Frontend
```bash
cd frontend
npm start
```
*Frontend chạy tại: `http://localhost:4200`*

---

## 📚 Tài liệu Chi tiết
- [Tài liệu Hướng dẫn sử dụng chi tiết](file:///d:/Project/managersoftware/HUONG_DAN_SU_DUNG.md)
- [Tài liệu Hướng dẫn người dùng (docs/manuals/user-guide.md)](file:///d:/Project/managersoftware/docs/manuals/user-guide.md)
