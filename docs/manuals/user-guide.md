# Sổ tay Hướng dẫn Sử dụng (User Guide)

> **HỆ THỐNG QUẢN LÝ PHẦN MỀM CHUYỂN ĐỔI SỐ TỈNH LÀO CAI**  
> **Phiên bản tài liệu**: 1.0.0  
> **Cơ quan phát hành**: Sở Thông tin và Truyền thông tỉnh Lào Cai  

---

## Chương 1: Đăng nhập & Điều hướng Cơ bản

### 1.1. Đăng nhập Hệ thống
1. Truy cập địa chỉ hệ thống qua trình duyệt web: `https://phanmem.laocai.gov.vn` (hoặc `http://localhost:4200` trên môi trường nội bộ).
2. Nhập **Tên đăng nhập** và **Mật khẩu** do Sở TT&TT hoặc Quản trị đơn vị cấp.
3. Bấm nút **Đăng nhập**. Sau khi đăng nhập thành công, hệ thống tự động chuyển hướng đến màn hình **Dashboard & Báo cáo Tổng quan**.

### 1.2. Thanh Menu Điều hướng
- **📊 Dashboard & Báo cáo**: Trung tâm chỉ huy, theo dõi chỉ số độ phủ và tình hình sử dụng phần mềm.
- **💻 Danh mục Phần mềm**: Tra cứu danh mục phần mềm đã được tỉnh nghiệm thu và đưa vào sử dụng chung.
- **💡 Đề xuất Bổ sung DM**: Nơi cán bộ đơn vị gửi đề xuất bổ sung các phần mềm mới đã hoàn thành vào danh mục.
- **🚀 Hồ sơ Triển khai**: Quản lý chi tiết việc cài đặt, cấu hình, phiên bản và trạng thái vận hành của từng phần mềm tại cơ quan.
- **📊 Nhập / Xuất Excel**: Tiện ích tải mẫu chuẩn, kiểm tra lỗi và nhập dữ liệu hàng loạt theo cơ chế All-or-Nothing.
- **📜 Hợp đồng & License**: Theo dõi các hợp đồng mua sắm, bảo trì và phân bổ hạn mức bản quyền (ghế/unlimited).
- **🏢 Cơ quan & Đơn vị**: Cây tổ chức và thông tin các sở ban ngành, UBND huyện, thị xã, thành phố.
- **🔍 Nhật ký Kiểm toán**: (Dành cho Auditor / Admin) Tra cứu lịch sử thay đổi dữ liệu toàn hệ thống.

---

## Chương 2: Dành cho Cán bộ Đơn vị (UnitEditor)

### 2.1. Khởi tạo Hồ sơ Triển khai Mới
1. Vào menu **Hồ sơ Triển khai** → bấm nút **+ Thêm Hồ sơ Triển khai**.
2. Chọn **Phần mềm** từ danh mục dùng chung của tỉnh.
3. Chọn **Môi trường** (`Production`, `Staging`, `Development` hoặc `DR`).
4. Nhập **Định danh phiên bản cài đặt (Instance Key)** (ví dụ: `ubnd-baothang-main`).
5. Chọn **Phiên bản phát hành (Release)** và điền **Ngày tiếp nhận**, **Ngày đưa vào sử dụng**.
6. Chọn **Cán bộ phụ trách** và bấm **Lưu bản nháp**.

### 2.2. Đính kèm Tài liệu & Gửi Phê duyệt
1. Tại trang chi tiết hồ sơ, chọn tab **Hồ sơ Tài liệu**.
2. Kéo thả tệp tin (biên bản nghiệm thu, quyết định đưa vào sử dụng) dưới định dạng PDF hoặc DOCX (tối đa 20MB).
3. Hệ thống sẽ tự động quét an toàn. Khi tệp đạt trạng thái **Sạch (Clean)**, bấm nút **Gửi phê duyệt (Submit)** ở góc trên bên phải màn hình.

### 2.3. Nhập Dữ liệu Triển khai Hàng loạt bằng Excel
1. Vào menu **Nhập / Xuất Excel** → chọn tab **Nhập dữ liệu từ Excel**.
2. Bấm nút **Tải mẫu Excel chuẩn (.xlsx)**. Mẫu đã tích hợp sẵn danh mục mã phần mềm và mã đơn vị của tỉnh.
3. Điền thông tin vào sheet `Deployments` theo đúng hướng dẫn (không sử dụng công thức tính toán).
4. Kéo thả file Excel vào khu vực tải lên. Hệ thống sẽ quét và kiểm tra lỗi từng dòng.
5. Nếu có lỗi: Xem chi tiết dòng/cột bị lỗi trên bảng, sửa lại file và tải lên lại.
6. Khi bảng hiển thị **0 lỗi (Hợp lệ)**: Bấm nút **Xác nhận Commit (All-or-Nothing)**. Toàn bộ hồ sơ sẽ được tạo dưới dạng bản nháp an toàn.

---

## Chương 3: Dành cho Lãnh đạo Phê duyệt (UnitApprover)

### 3.1. Tiếp nhận và Xem xét Hồ sơ Chờ duyệt
1. Nhấp vào biểu tượng **🔔 Thông báo** ở góc trên thanh tiêu đề để xem các hồ sơ mới được gửi duyệt.
2. Hoặc vào menu **Hồ sơ Triển khai** → lọc trạng thái **Chờ duyệt (Submitted)**.
3. Nhấp vào hồ sơ để kiểm tra thông tin cấu hình máy chủ, tài liệu đính kèm và hạn mức bản quyền.

### 3.2. Phê duyệt hoặc Từ chối Hồ sơ
- **Phê duyệt (Approve)**: Bấm nút **Phê duyệt Hồ sơ** → Nhập ý kiến nhận xét (nếu có) → Bấm Xác nhận. Hồ sơ lập tức chuyển thành bản chính thức (**Approved**) và được cập nhật vào số liệu báo cáo Dashboard của tỉnh.
- **Từ chối (Reject)**: Bấm nút **Từ chối Phê duyệt** → Bắt buộc nhập **Lý do từ chối** cụ thể → Bấm Xác nhận. Hồ sơ chuyển sang trạng thái **Rejected** để cán bộ khởi tạo có thể bấm **Mở lại (Reopen)** và chỉnh sửa theo yêu cầu.

---

## Chương 4: Dành cho Cán bộ Quản lý Hợp đồng & Bản quyền (Coordinator)

### 4.1. Tạo Hợp đồng và Thiết lập Hạn mức License
1. Vào menu **Hợp đồng & License** → bấm **+ Thêm Hợp đồng Mới**.
2. Nhập số hợp đồng, đối tác nhà cung cấp (VNPT, Viettel, FPT, MISA...), tổng giá trị hợp đồng, thời hạn hiệu lực và thời hạn bảo trì.
3. Thêm các **Hạng mục phần mềm** trong hợp đồng và thiết lập **Gói bản quyền (License Entitlement)** dạng `Seat` (số lượng ghế) hoặc `Unlimited` (không giới hạn).

### 4.2. Phân bổ Hạn mức License cho Hồ sơ Triển khai
1. Tại chi tiết hợp đồng, vào tab **Phân bổ Bản quyền**.
2. Bấm nút **+ Cấp phát License cho Hồ sơ Triển khai**.
3. Chọn hồ sơ triển khai cần cấp và nhập số lượng ghế muốn phân bổ.
4. Hệ thống áp dụng cơ chế khóa bi quan chống vượt quota: Nếu số ghế yêu cầu vượt quá số lượng còn lại, hệ thống sẽ tự động cảnh báo và từ chối.

---

## Chương 5: Dành cho Kiểm toán viên (Auditor)

### 5.1. Tra cứu Nhật ký Kiểm toán (Audit Logs)
1. Vào menu **Nhật ký Kiểm toán**.
2. Sử dụng bộ lọc đa tiêu chí: Lọc theo Thực thể (`Deployment`, `Contract`, `User`...), Hành động (`Create`, `Update`, `Submit`, `Approve`, `Reject`), Cán bộ thực hiện hoặc Khoảng thời gian.
3. Bấm **Xem chi tiết thay đổi**: Cửa sổ so sánh Before / After diff sẽ hiển thị chi tiết các trường thông tin thay đổi (màu xanh cho nội dung mới, màu đỏ cho nội dung cũ). Các thông tin nhạy cảm (mật khẩu, secret, token) đã được hệ thống tự động lọc sạch nhằm bảo đảm an toàn thông tin.
