# SỔ TAY HƯỚNG DẪN SỬ DỤNG HỆ THỐNG
## HỆ THỐNG QUẢN LÝ PHẦN MỀM - TỈNH LÀO CAI

---

> **Cơ quan phát hành**: Sở Thông tin và Truyền thông tỉnh Lào Cai  
> **Phiên bản tài liệu**: 2.1.0  
> **Hệ thống áp dụng**: Cổng thông tin Quản lý phần mềm dùng chung (`http://localhost:4200` tại môi trường nội bộ)  

---

## MỤC LỤC TỔNG QUAN

1. [Tổng quan Hệ thống & Tài khoản Truy cập](#1-tổng-quan-hệ-thống--tài-khoản-truy-cập)
2. [Đăng nhập & Điều hướng Chung](#2-đăng-nhập--điều-hướng-chung)
3. [Chức năng 1: Quản lý Danh mục Phần mềm](#3-chức-năng-1-quản-lý-danh-mục-phần-mềm)
4. [Chức năng 2: Quản lý Nhóm phân loại Phần mềm](#4-chức-năng-2-quản-lý-nhóm-phân-loại-phần-mềm)
5. [Chức năng 3: Quản lý Nhà cung cấp & Đối tác](#5-chức-năng-3-quản-lý-nhà-cung-cấp--đối-tác)
6. [Chức năng 4: Quản lý Phiên bản Phát hành (Releases)](#6-chức-năng-4-quản-lý-phiên-bản-phát-hành-releases)
7. [Chức năng 5: Quản trị Tài khoản & Phân quyền Người dùng](#7-chức-năng-5-quản-trị-tài-khoản--phân-quyền-người-dùng)
8. [Quy ước Biểu tượng Thao tác (Icon-Only Guide)](#8-quy-ước-biểu-tượng-thao-tác-icon-only-guide)

---

## 1. TỔNG QUAN HỆ THỐNG & TÀI KHOẢN TRUY CẬP

### 1.1. Mục tiêu hệ thống
Hệ thống là nền tảng quản trị tập trung toàn tỉnh Lào Cai nhằm:
- Thống nhất danh mục các phần mềm, hệ thống thông tin dùng chung cấp tỉnh.
- Theo dõi thông tin phân loại, nhà sản xuất/đối tác công nghệ cung ứng.
- Kiểm soát các phiên bản phát hành (Releases Timeline) và thời hạn hỗ trợ kỹ thuật.
- Phân quyền quản trị linh hoạt và an toàn dữ liệu.

### 1.2. Tài khoản Quản trị viên Mặc định (System Admin)
Thông tin cấu hình lưu tại tệp `.env`:
- **Tên đăng nhập**: `admin`
- **Mật khẩu**: `Admin@123456`
- **Email**: `admin@laocai.gov.vn`
- **Quyền hạn**: Quản trị viên Hệ thống (`SystemAdmin` - Toàn quyền trên toàn tỉnh).

---

## 2. ĐĂNG NHẬP & ĐIỀU HƯỚNG CHUNG

### 2.1. Đăng nhập Hệ thống
1. Mở trình duyệt web và truy cập `http://localhost:4200`.
2. Nhập **Tên đăng nhập** (`admin`) và **Mật khẩu** (`Admin@123456`).
3. Bấm **Đăng nhập hệ thống**. Sau khi xác thực thành công, hệ thống tự động đưa người dùng vào trang **Danh mục Phần mềm**.

### 2.2. Đổi Mật khẩu & Đăng xuất
1. Nhấp vào tên tài khoản ở góc trên bên phải màn hình để mở menu dropdown.
2. Chọn **Đổi mật khẩu** để cập nhật mật khẩu mới khi cần thiết.
3. Chọn **Đăng xuất** để đóng phiên làm việc an toàn.

### 2.3. Thanh Menu Điều hướng (Sidebar)
Thanh bên trái hỗ trợ chuyển đổi nhanh giữa 4 mục quản trị cốt lõi:
- **Danh mục Phần mềm**: Xem và quản lý danh sách phần mềm.
- **Nhóm phân loại**: Quản lý các nhóm danh mục nghiệp vụ.
- **Nhà cung cấp & Đối tác**: Quản lý thông tin đơn vị phát triển.
- **Quản lý Tài khoản**: Phân quyền và quản trị người dùng.

---

## 3. CHỨC NĂNG 1: QUẢN LÝ DANH MỤC PHẦN MỀM
*Đường dẫn: `/software?tab=software`*

### 3.1. Tìm kiếm & Lọc dữ liệu
- **Ô tìm kiếm**: Gõ từ khóa tên phần mềm hoặc mã để lọc tức thì.
- **Bộ lọc đa tiêu chí**:
  - Lọc theo **Nhóm phần mềm**.
  - Lọc theo **Nhà cung cấp**.
  - Lọc theo **Trạng thái vòng đời**: *Đang hoạt động (Active)*, *Cảnh báo thay thế (Deprecated)*, *Ngừng hỗ trợ (Retired)*.

### 3.2. Thêm mới Phần mềm (Smart Form)
Bấm nút **Thêm Phần mềm mới** ở góc phải:
1. **Gợi ý mẫu 1-click**: Chọn các phần mềm mẫu phổ biến (Hệ thống QLVB, Cổng Dịch vụ công, HT Một cửa điện tử, Phần mềm Đánh giá cán bộ,...) để tự động điền các thông tin chuẩn.
2. **Tự động sinh mã Code**: Nút *Tự động sinh mã* giúp chuẩn hóa mã code theo cú pháp chuẩn tỉnh (VD: `VNPT_IOFFICE`).
3. **Thêm nhanh Nhóm/NCC inline**: Có thể thêm ngay Nhóm mới hoặc Nhà cung cấp mới trực tiếp từ form mà không cần thoát ra.
4. **Tạo kèm Phiên bản phát hành ban đầu**: Tích chọn để nhập ngay số hiệu phiên bản khởi tạo (VD: `v1.0.0`) cùng ngày phát hành.

### 3.3. Chỉnh sửa & Xoá Phần mềm
- Bấm nút **Sửa** (`✏️`) trên từng dòng để cập nhật thông tin phần mềm.
- Bấm nút **Xoá** (`🗑️`) để xoá phần mềm (hệ thống có cảnh báo xác nhận an toàn trước khi thực hiện).

---

## 4. CHỨC NĂNG 2: QUẢN LÝ NHÓM PHÂN LOẠI PHẦN MỀM
*Đường dẫn: `/software?tab=categories`*

- **Xem danh sách nhóm**: Hiển thị mã nhóm, tên nhóm phân loại, ngày tạo và trạng thái.
- **Thêm Nhóm mới**: Nhập Mã nhóm (VD: `E_GOV`) và Tên nhóm (VD: `Chính quyền số`).
- **Chỉnh sửa Nhóm**: Cập nhật tên nhóm hoặc trạng thái hoạt động (*Hoạt động* / *Tạm dừng*).
- **Xoá Nhóm**: Chỉ cho phép xoá khi chưa có phần mềm nào đang thuộc nhóm này.

---

## 5. CHỨC NĂNG 3: QUẢN LÝ NHÀ CUNG CẤP & ĐỐI TÁC
*Đường dẫn: `/software?tab=vendors`*

- **Xem danh bạ đối tác**: Hiển thị mã nhà cung cấp, tên đơn vị, thông tin hotline/email liên hệ.
- **Thêm Nhà cung cấp mới**: Nhập Mã NCC (VD: `VNPT_LAOCAI`), Tên NCC (VD: `VNPT Lào Cai`), thông tin hotline/địa chỉ.
- **Chỉnh sửa thông tin**: Cập nhật hotline, email hỗ trợ hoặc trạng thái hợp tác.
- **Xoá Nhà cung cấp**: Có xác nhận an toàn, chống xoá nhầm khi đối tác đang có phần mềm trong danh mục.

---

## 6. CHỨC NĂNG 4: QUẢN LÝ PHIÊN BẢN PHÁT HÀNH (RELEASES)

1. Nhấp vào bất kỳ dòng phần mềm nào hoặc nút **Xem** (`👁`) để mở ngăn chi tiết bên phải (Drawer).
2. **Dòng thời gian phiên bản (Timeline)**:
   - Liệt kê toàn bộ lịch sử các bản phát hành từ mới nhất đến cũ nhất.
   - Hiển thị ngày phát hành và hạn hỗ trợ kỹ thuật.
3. **Thêm Phiên bản mới**:
   - Bấm nút **+ Thêm Phiên bản**.
   - Nhập số hiệu phiên bản (VD: `v2.0.0`), ngày phát hành và ngày kết thúc hỗ trợ (tùy chọn).
   - Bấm **Lưu Phiên bản**.
4. **Xoá phiên bản**: Nhấp vào biểu tượng thùng rác nhỏ tại từng thẻ phiên bản để gỡ bỏ phiên bản lỗi thời.

---

## 7. CHỨC NĂNG 5: QUẢN TRỊ TÀI KHOẢN & PHÂN QUYỀN NGƯỜI DÙNG
*Đường dẫn: `/admin/users`*

### 7.1. Quản lý Người dùng
- **Thêm người dùng mới**: Nhập tên đăng nhập (Username), họ tên hiển thị, email và mật khẩu ban đầu.
- **Chỉnh sửa thông tin**: Cập nhật tên hiển thị, email.
- **Đặt lại Mật khẩu**: Cho phép quản trị viên cấp mật khẩu mới trực tiếp khi người dùng quên mật khẩu.
- **Khóa / Mở khóa**: Tạm ngưng quyền truy cập của người dùng ngay tức thì.

### 7.2. Phân quyền Phạm vi (Scope-based RBAC)
Bấm nút **Phân quyền** (`🛡️`) trên dòng người dùng:
1. Chọn **+ Thêm Vai trò & Phạm vi**.
2. Chọn vai trò cần cấp: *Quản trị viên Hệ thống*, *Quản trị Danh mục*, *Điều phối viên*, *Cán bộ Đơn vị*,...
3. Chọn loại phạm vi:
   - **Toàn tỉnh (Global)**: Có quyền trên toàn bộ dữ liệu phần mềm của tỉnh.
   - **Cấp Đơn vị (Organization)**: Chọn cơ quan/đơn vị áp dụng (VD: *UBND Huyện Bảo Thắng*) và tùy chọn *Kế thừa tất cả đơn vị cấp dưới*.
4. Thiết lập ngày bắt đầu và ngày kết thúc hiệu lực quyền.
5. Bấm **Xác nhận Gán Quyền**.
6. Quản trị viên có thể **Thu hồi quyền** bất kỳ lúc nào chỉ với 1 click.

---

## 8. QUY ƯỚC BIỂU TƯỢNG THAO TÁC (ICON-ONLY GUIDE)

Toàn bộ ứng dụng sử dụng các nút thao tác dạng icon vector chuẩn `32x32px`:

| Biểu tượng | Tên thao tác | Ý nghĩa chức năng |
| :---: | :--- | :--- |
| `👁` | **Xem chi tiết** | Mở ngăn trượt bên phải hiển thị chi tiết phần mềm và lịch sử phiên bản |
| `✏️` | **Chỉnh sửa** | Mở cửa sổ sửa thông tin phần mềm, nhóm, nhà cung cấp hoặc tài khoản |
| `🗑️` | **Xoá** | Mở hộp thoại xác nhận xoá an toàn (màu đỏ cảnh báo khi hover) |
| `🛡️` | **Phân quyền** | Mở bảng phân quyền vai trò và phạm vi thẩm quyền của người dùng |
| `🔒` | **Khóa tài khoản** | Khóa tài khoản đang hoạt động |
| `🔓` | **Mở khóa** | Kích hoạt lại tài khoản đang bị khóa |
| `✕` | **Đóng** | Đóng cửa sổ modal hoặc ngăn chi tiết drawer |
