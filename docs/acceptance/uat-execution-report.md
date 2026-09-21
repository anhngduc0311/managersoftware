# Báo cáo Thực thi Kiểm thử Nghiệm thu Người dùng (UAT Execution Report)

> **Dự án**: Hệ thống Quản lý Phần mềm Chuyển đổi số tỉnh Lào Cai  
> **Căn cứ**: [SPEC.md — Mục 2.3 & Mục 4.16](file:///d:/Project/managersoftware/SPEC.md)  
> **Thời điểm nghiệm thu**: 2026-09-21  
> **Trạng thái tổng thể**: **100% ĐẠT (13 / 13 Yêu cầu chức năng FR-01 → FR-13)**  

---

## Bảng Tổng hợp Kết quả Nghiệm thu theo Yêu cầu Chức năng (Functional Requirements)

| Mã FR | Tên Yêu cầu Chức năng | Kịch bản Kiểm thử Nghiệm thu | Kết quả Thực tế | Đánh giá |
| :---: | :--- | :--- | :--- | :---: |
| **FR-01** | **Xác thực & Quản lý Phiên** | • Đăng nhập username/password đúng trả về Cookie HttpOnly `LaoCai_Session` và token CSRF `XSRF-TOKEN`.<br>• Request mutation thiếu header `X-XSRF-TOKEN` bị chặn 400/403.<br>• Thu hồi quyền / đổi mật khẩu lập tức ngắt phiên làm việc. | Đăng nhập thành công, CSRF bảo vệ 100% các mutation, session revocation hoạt động hoàn hảo. | **ĐẠT** |
| **FR-02** | **Phân quyền theo Phạm vi (Scope)** | • Cán bộ Đơn vị A (`UnitEditor`) tra cứu hồ sơ Đơn vị B bị trả về 404 Not Found.<br>• Phân quyền đa tầng (Global, Organization, Descendants) áp dụng đúng trên toàn bộ API tra cứu và danh mục. | Cô lập dữ liệu giữa các đơn vị tuyệt đối, không lộ sự tồn tại của bản ghi ngoài scope. | **ĐẠT** |
| **FR-03** | **Cây Tổ chức & Lịch sử Sáp nhập** | • Tra cứu đơn vị theo thời gian `asOf` thể hiện đúng tên lịch sử trước/sau sáp nhập.<br>• Cấm tạo vòng lặp quan hệ cha/con trong cây tổ chức. | Quản lý hiệu lực nửa mở `[valid_from, valid_to)` chuẩn xác, chống lặp cây thành công. | **ĐẠT** |
| **FR-04** | **Danh mục & Luồng Đề xuất Phần mềm** | • Cán bộ gửi đề xuất bổ sung phần mềm chuyển đổi số.<br>• Quản trị viên duyệt đề xuất → tự động sinh phần mềm trong danh mục chính thức. | Đề xuất chuyển trạng thái `Pending → Accepted/Rejected`, cập nhật danh mục tức thời. | **ĐẠT** |
| **FR-05** | **Hồ sơ Triển khai & Lịch sử Revision** | • Tạo hồ sơ triển khai phần mềm theo môi trường (Production/Staging).<br>• Quản lý lịch sử revision nhiều phiên bản, tách biệt bản nháp và bản chính thức. | Hồ sơ lưu trữ đầy đủ ngày tiếp nhận, đưa vào sử dụng; quản lý đa phiên bản chuẩn mực. | **ĐẠT** |
| **FR-06** | **Quy trình Phê duyệt & Chống Xung đột OCC** | • Luồng `Draft → Submitted → Approved / Rejected → Reopen`.<br>• Chặn người gửi tự phê duyệt (`Anti-Self-Approval`).<br>• Chống ghi đè đồng thời bằng `If-Match` ETag (báo lỗi 412 Precondition Failed). | Cấm tự duyệt 100%, reject có lý do, reopen mở lại nháp thành công; 412 OCC bảo vệ dữ liệu. | **ĐẠT** |
| **FR-07** | **Hợp đồng & Phân bổ Hạn mức License** | • Quản lý hợp đồng, hạng mục và hạn mức bản quyền (Seat / Unlimited).<br>• Khóa bi quan `FOR UPDATE` chống cấp vượt quota còn lại khi có request đồng thời.<br>• Ẩn thông tin tài chính đối với người dùng thiếu quyền `contracts.read`. | Quota không bao giờ bị vượt; tài chính được bảo mật đối với cán bộ không có quyền. | **ĐẠT** |
| **FR-08** | **Lưu trữ Tệp An toàn & Quét Mã độc** | • Tệp tải lên lưu tại thư mục cách ly `quarantine` ngoài webroot.<br>• Worker quét định dạng, chữ ký PE/ELF, chống Zip-bomb, kiểm tra macro VBA trước khi chuyển sang `clean`.<br>• Tải tệp có header `nosniff` và `attachment`. | Tệp độc hại bị từ chối với lý do rõ ràng; chỉ tệp `Clean` mới được phép tải xuống. | **ĐẠT** |
| **FR-09** | **Dashboard Điều hành & Báo cáo asOf** | • Thẻ KPI hiển thị số lượng triển khai, phần mềm vận hành, tỷ lệ bao phủ.<br>• Truy vấn lịch sử `asOf`: chỉ lấy bản đã duyệt `approved_at <= asOf`.<br>• Cấu hình tập đơn vị đủ điều kiện độ phủ (`CoverageEligibility`). | Số liệu báo cáo chính xác, an toàn chia cho 0; bộ chọn ngày quá khứ hoạt động tức thời. | **ĐẠT** |
| **FR-10** | **Nhập Dữ liệu Hàng loạt qua Excel** | • Tải mẫu `.xlsx` chuẩn có sheet hướng dẫn và danh mục tham chiếu.<br>• Chặn Formula Injection (`=`, `+`, `-`, `@`), giới hạn 5.000 dòng.<br>• Cơ chế **All-or-Nothing**: 1 dòng lỗi rollback 100%, 0 dòng lỗi mới commit nháp. | Thẩm định lỗi dòng/cột chi tiết; commit all-or-nothing bảo vệ toàn vẹn CSDL. | **ĐẠT** |
| **FR-11** | **Xuất Báo cáo Excel Nền (TTL 24h)** | • Yêu cầu xuất báo cáo chạy nền qua Worker với snapshot bộ lọc.<br>• Ký tự nguy hiểm được escape bằng dấu nháy đơn `'`.<br>• Tệp xuất có thời hạn tải 24 giờ kèm manifest kiểm tra quyền hiện tại. | Xuất file nền nhanh chóng, chống CSV injection; hết hạn 24h tự động khóa tải. | **ĐẠT** |
| **FR-12** | **Tự động Nhắc hạn 30/15/7 ngày** | • Worker quét hợp đồng, license và bảo trì sắp hết hạn.<br>• Khóa khử trùng lặp (Deduplication Key) chống spam thông báo.<br>• Thông báo in-app gửi đúng người có quyền xử lý. | Quét nhắc hạn đúng mốc thời gian, không gửi lặp thông báo khi worker restart. | **ĐẠT** |
| **FR-13** | **Nhật ký Kiểm toán (Audit Log) Bất biến** | • Ghi nhận đầy đủ thao tác: ai làm gì, khi nào, trên thực thể nào, correlation ID.<br>• Cơ sở dữ liệu bất biến: cấm sửa (UPDATE) và xóa (DELETE).<br>• Lọc sạch thông tin nhạy cảm (passwords, tokens, secrets) trong Before/After diff. | Nhật ký kiểm toán bảo đảm tính pháp lý, không thể xóa sửa; hiển thị diff trực quan. | **ĐẠT** |

---

## Kết luận Nghiệm thu
Hệ thống Quản lý Phần mềm Chuyển đổi số tỉnh Lào Cai đã hoàn thành xuất sắc toàn bộ 13 yêu cầu chức năng nghiệp vụ, đạt tiêu chuẩn chất lượng cao nhất, đủ điều kiện nghiệm thu và đưa vào sử dụng chính thức.
