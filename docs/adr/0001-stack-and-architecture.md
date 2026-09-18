# ADR 0001: Lựa chọn Công nghệ và Kiến trúc Hệ thống

## Trạng thái
Đã chấp thuận (Accepted) — Ngày: 18/09/2026

## Bối cảnh
Hệ thống quản lý phần mềm chuyển đổi số tại tỉnh Lào Cai cần một nền tảng tập trung để theo dõi danh mục phần mềm, hồ sơ triển khai theo đơn vị, quản lý hợp đồng, hạn mức bản quyền (license), bảo trì và báo cáo thống kê. Hệ thống cần đảm bảo phân quyền chặt chẽ theo đơn vị (Scope-based Access Control), kiểm soát phiên an toàn, hỗ trợ kiểm toán dữ liệu và vận hành tin cậy.

## Quyết định Kiến trúc

1. **Mô hình kiến trúc**:
   - Sử dụng **Modular Monolith** trên một cơ sở dữ liệu PostgreSQL duy nhất.
   - Phân tầng rõ ràng: `Domain` (Core), `Application` (Use cases & DTOs), `Infrastructure` (EF Core & Adapters), `Api` (REST HTTP Endpoints), `Worker` (Background job processor).
   - Chưa sử dụng Microservices, Kubernetes hoặc Redis ở giai đoạn MVP để tối ưu nguồn lực và đơn giản hóa vận hành.

2. **Stack công nghệ Backend**:
   - .NET 10 LTS (`net10.0`), C# 14.
   - EF Core 10 + Npgsql Entity Framework Core Provider 10.
   - ASP.NET Core Identity kết hợp Cookie HttpOnly + Token CSRF (`XSRF-TOKEN`).
   - ClosedXML cho xử lý Excel phía server.

3. **Stack công nghệ Frontend**:
   - Angular 22 standalone components, strict TypeScript mode.
   - Angular Material + CDK 22 (SCSS Theme, responsive, tiếng Việt).
   - Signals cho state management giao diện + RxJS cho HTTP streams.
   - Apache ECharts (lazy loaded) cho biểu đồ thống kê.

4. **Cơ sở dữ liệu & Lưu trữ**:
   - PostgreSQL 18.
   - Quy ước: Khóa chính UUIDv7 / UUID, snake_case cho bảng/cột, timestamptz UTC cho toàn bộ thời gian.
   - Kho tệp riêng biệt qua `IFileStorage` có cơ chế cách ly quét virus trước khi công bố.

## Hệ quả
- Khóa chặt phiên bản công nghệ qua `global.json`, `Directory.Packages.props` và `package-lock.json`.
- Hệ thống dễ bảo trì, dễ kiểm thử tự động (Unit test, Integration test với TestContainers, E2E Playwright).
