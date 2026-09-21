# TÀI LIỆU HƯỚNG DẪN SỬ DỤNG
## HỆ THỐNG QUẢN LÝ PHẦN MỀM - TỈNH LÀO CAI

---

### 1. Thông tin Đăng nhập & Cấu hình Hệ thống
- **Địa chỉ truy cập**: `http://localhost:4200` (hoặc cổng cấu hình nội bộ).
- **Tài khoản Quản trị viên Mặc định (System Admin)**:
  - **Tên đăng nhập**: `admin`
  - **Mật khẩu**: `Admin@123456`
  - **Email**: `admin@laocai.gov.vn`
  - *(Thông tin đã được lưu trữ an toàn trong tệp `.env` và `.env.example`).*

---

### 2. Các Phân hệ Chức năng Cốt lõi

#### 2.1. Quản lý Danh mục Phần mềm Dùng chung (`/software`)
Hệ thống được thiết kế tập trung 100% cho công tác quản trị phần mềm của tỉnh với 3 tab chính:
1. **Tab Phần mềm**:
   - Tra cứu, lọc theo Nhóm phần mềm, Nhà cung cấp, Trạng thái vòng đời (*Đang duy trì*, *Cảnh báo thay thế*, *Ngừng hỗ trợ*).
   - Tìm kiếm nhanh tức thì theo tên hoặc mã phần mềm.
   - Bấm **Thêm Phần mềm mới** để mở Form thông minh:
     - *Gợi ý mẫu 1-click*: Điền nhanh dữ liệu mẫu các phần mềm dùng chung phổ biến (Hệ thống QLVB, Dịch vụ công, Một cửa điện tử,...).
     - *Tự động sinh mã (Code)*: Chuẩn hóa mã phần mềm theo Tên và Nhà cung cấp.
     - *Thêm nhanh Nhóm / NCC*: Bổ sung tức thì nhóm hoặc nhà cung cấp mới ngay trong form mà không cần tải lại trang.
     - *Tạo kèm Phiên bản phát hành ban đầu*: Khởi tạo số hiệu phiên bản v1.0.0, ngày phát hành và hạn hỗ trợ.
2. **Tab Nhóm phân loại**:
   - Quản lý danh mục nhóm phân loại phần mềm (Chính quyền số, Kinh tế số, Xã hội số,...).
   - Thao tác: Thêm nhóm mới, Chỉnh sửa, Xoá nhóm (có kiểm tra ràng buộc dữ liệu an toàn).
3. **Tab Nhà cung cấp & Đối tác**:
   - Quản lý thông tin các nhà cung cấp / đối tác công nghệ (VNPT, Viettel, FPT, MISA,...).
   - Lưu trữ mã NCC, tên đơn vị, thông tin hotline/email hỗ trợ.

#### 2.2. Ngăn Chi tiết Phần mềm & Quản lý Phiên bản (Drawer Timeline)
- Bấm vào một dòng hoặc nút **Xem** (`👁`) để mở ngăn chi tiết bên phải.
- Xem tổng quan thông tin phân loại, nhà sản xuất, mô tả chức năng.
- **Quản lý Phiên bản Phát hành (Releases)**:
  - Xem danh sách phiên bản theo dòng thời gian (Timeline).
  - Thêm phiên bản mới (`+ Thêm Phiên bản`): Số hiệu version (v1.0.0, v2.1.0), ngày phát hành, hạn hỗ trợ kỹ thuật.
  - Xoá phiên bản cũ không còn hiệu lực.

#### 2.3. Quản lý Tài khoản & Phân quyền Người dùng (`/admin/users`)
- Xem danh sách toàn bộ cán bộ, quản trị viên sử dụng hệ thống.
- **Thêm Người dùng Mới**: Khởi tạo tài khoản với username, họ tên, email và mật khẩu khởi tạo.
- **Phân quyền Phạm vi (Scope-based Access Control)**:
  - Gán vai trò (`SystemAdmin`, `CatalogManager`, `Coordinator`, `UnitEditor`, `Viewer`,...).
  - Cấu hình phạm vi áp dụng: Toàn tỉnh (`Global`) hoặc Cấp Đơn vị (`Organization` kèm tùy chọn kế thừa cấp dưới).
  - Đặt thời hạn hiệu lực từ ngày - đến ngày hoặc vô thời hạn.
- **Thao tác nhanh**: Đặt lại mật khẩu, Khóa / Mở khóa tài khoản chỉ với 1 click.

---

### 3. Quy ước Biểu tượng Thao tác (Icon-Only Actions)
Tất cả các bảng dữ liệu sử dụng nút thao tác dạng icon chuẩn 32x32px với tooltip hướng dẫn khi rê chuột:
- `[ 👁 ]` **Xem**: Mở ngăn trượt xem chi tiết thông tin và phiên bản phát hành.
- `[ ✏️ ]` **Sửa**: Mở hộp thoại chỉnh sửa thông tin phần mềm, nhóm, nhà cung cấp hoặc tài khoản.
- `[ 🗑️ ]` **Xoá**: Mở hộp thoại xác nhận xoá an toàn (màu đỏ cảnh báo).
- `[ 🛡️ ]` **Phân quyền**: Quản lý gán vai trò và phạm vi thẩm quyền của người dùng.
- `[ 🔒 / 🔓 ]` **Khóa / Mở khóa**: Tạm dừng hoặc kích hoạt lại quyền đăng nhập tài khoản.

---

### 4. Hướng dẫn Khởi chạy Dự án

```bash
# 1. Khởi chạy Backend (.NET 8 Web API)
cd backend/src/LaoCai.SoftwareManagement.Api
dotnet run

# 2. Khởi chạy Frontend (Angular 19)
cd frontend
npm start
```
Truy cập trình duyệt: **`http://localhost:4200`** và đăng nhập bằng tài khoản `admin` / `Admin@123456`.
