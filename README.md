# Hệ thống Quản lý Học bổng & Hồ sơ Xét học bổng UNETI

> **Đề tài 22** — Lớp **TI17A3HN** · Trường Đại học Kinh tế - Kỹ thuật Công nghiệp (UNETI)

Ứng dụng web quản lý quy trình học bổng trực tuyến cho sinh viên và hội đồng xét duyệt: từ công bố chương trình học bổng, tiếp nhận hồ sơ & minh chứng, xét duyệt sơ bộ, sắp xếp lịch phỏng vấn, công bố kết quả cho đến thống kê & xuất báo cáo Excel.

---

## 1. Công nghệ & Kiến trúc sử dụng

| Thành phần | Công nghệ / Giải pháp |
| :--- | :--- |
| **Backend Framework** | ASP.NET Core MVC (`.NET 10.0`) |
| **ORM & Database** | Entity Framework Core `10.0` + SQL Server (LocalDB) |
| **Kiến trúc** | Repository Pattern + Unit of Work + Business Services |
| **Xác thực & Phân quyền** | **Pure JWT** (AccessToken 15 phút + RefreshToken Rotation 7 ngày lưu `HttpOnly Cookie`) & `RoleAuthMiddleware` |
| **Chống Spam / Brute-force** | **Redis Rate Limiting** (Fixed Window `INCR` + `EXPIRE` 60s, tự động Fallback sang `IMemoryCache`) |
| **Hàng đợi xử lý ngầm** | **Redis Queue** (`LPUSH`/`RPOP`) + `BackgroundService` (`RedisQueueWorker`, tự thử lại 3 lần, Fallback `ConcurrentQueue`) |
| **Thông báo Realtime** | **SignalR (WebSocket)** (`NotificationHub`) + Popup Toast & Dropdown chuông |
| **Lưu trữ tệp Cloud** | **Supabase Storage** (đồng bộ nền qua hàng đợi cho minh chứng & ảnh đại diện) |
| **Gửi Email thông báo** | Gmail **SMTP** / **Brevo REST API** (kèm mẫu HTML thương hiệu UNETI) |
| **Kiểm thử tự động** | **xUnit** + **EF Core InMemory** (29 unit/integration tests) |

---

## 2. Chức năng chính

### Cổng Sinh viên
- Xem danh sách & chi tiết các chương trình học bổng đang mở.
- Cập nhật hồ sơ cá nhân & ảnh đại diện (tự đồng bộ lên Supabase Cloud).
- Nộp hồ sơ xin học bổng trực tuyến kèm tệp minh chứng (kiểm tra tự động GPA, hạn nộp, hồ sơ trùng).
- Theo dõi trạng thái hồ sơ, lịch phỏng vấn, kết quả xét duyệt và nhận thông báo Realtime + Email.

### Cổng Quản trị (Admin & Cán bộ học bổng)
- **Dashboard tổng quan**: Thống kê nhanh chỉ số hồ sơ, lịch phỏng vấn và biểu đồ phân bố trạng thái.
- **Quản lý Đơn vị tài trợ & Chương trình học bổng**: Thêm/sửa/đóng/mở và xóa mềm học bổng.
- **Xét duyệt sơ bộ hồ sơ**: Kiểm tra minh chứng, duyệt đạt / không đạt kèm nhận xét.
- **Quản lý Lịch phỏng vấn**: Tự động kiểm tra chống trùng lịch sinh viên & hội đồng phỏng vấn; khóa hủy khi đã có kết quả.
- **Quản lý Kết quả học bổng**: Chấm điểm, xét cấp học bổng, tự động kiểm soát & đóng học bổng khi đủ số suất.
- **Báo cáo & Thống kê**: Thống kê 10 chỉ tiêu bằng LINQ, vẽ biểu đồ trực quan và xuất báo cáo Excel (`.xls`).
- **Quản lý Tài khoản & Sinh viên**: Phân quyền (`Admin`, `Cán bộ học bổng`, `Sinh viên`), cập nhật avatar, khóa/mở tài khoản.

---

## 3. Cấu trúc thư mục

```text
QuanLyHocBong_UNETI2_TI17A3HN/
├── Attributes/            # Nhãn phân quyền [RoleAuthorize]
├── Constants/             # Hằng số trạng thái hồ sơ, học bổng, lịch phỏng vấn
├── Controllers/           # Các MVC & API Controllers
├── Data/                  # DbContext & DbInitializer (tự động migrate + seed dữ liệu mẫu)
├── Extensions/            # Cấu hình DI, Persistence, Redis & Pipeline
├── Helpers/               # Các lớp hỗ trợ & quy tắc trạng thái
├── Hubs/                  # SignalR NotificationHub
├── Middleware/            # JwtAuthMiddleware, RoleAuthMiddleware, RedisRateLimitMiddleware
├── Migrations/            # EF Core Migrations
├── Models/                # Entities, ViewModels & QueueJobs
├── Repositories/          # GenericRepository, HocBongRepository, HoSoHocBongRepository, UnitOfWork
├── Services/              # Auth (JWT), Business, Email, Queue, Storage
├── Views/                 # Giao diện Razor (.cshtml)
├── wwwroot/               # Static assets (CSS, JS, uploads)
└── tests/
    └── QuanLyHocBong_UNETI2_TI17A3HN.Tests/   # Bộ kiểm thử xUnit
```

---

## 4. Hướng dẫn cài đặt & chạy dự án

### Yêu cầu môi trường
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server / SQL Server Express LocalDB
- *(Tùy chọn)* Redis Server / Redis Cloud (nếu chưa cấu hình Redis, hệ thống tự động chạy ở chế độ In-Memory Fallback)

### Bước 1: Cấu hình biến môi trường (`.env`)
Tạo file `.env` ở thư mục gốc dự án (tham khảo các biến sau):
```env
JWT_SECRET_KEY=your_access_token_secret_key_at_least_32_chars
JWT_REFRESH_SECRET_KEY=your_refresh_token_secret_key_at_least_32_chars

# Tùy chọn: Redis (bỏ trống sẽ dùng In-Memory Fallback)
REDIS_URL=localhost:6379,abortConnect=false

# Tùy chọn: Supabase Storage
PROJECT_ID=your_supabase_project_id
SUPBER_BASE_API_KEY_SECRET=your_supabase_service_key
SUPABASE_BUCKET=QLHB

# Tùy chọn: Email SMTP hoặc Brevo
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_EMAIL=your_email@gmail.com
SMTP_PASSWORD=your_app_password
```

### Bước 2: Chạy ứng dụng
```bash
dotnet restore
dotnet run
```
Khi khởi chạy lần đầu, `DbInitializer.Seed()` sẽ tự động:
1. Chạy EF Core Migrations để tạo bảng trong LocalDB (`QuanLyHocBong`).
2. Khởi tạo dữ liệu mẫu: 5+ đơn vị tài trợ, 15+ học bổng, 30 sinh viên và các hồ sơ/lịch phỏng vấn mẫu.

Truy cập trình duyệt tại: `http://localhost:5142` (hoặc `https://localhost:7160`).

### Bước 3: Chạy kiểm thử tự động
```bash
dotnet test tests/QuanLyHocBong_UNETI2_TI17A3HN.Tests/QuanLyHocBong_UNETI2_TI17A3HN.Tests.csproj
```

---

## 5. Tài khoản đăng nhập mẫu

| Vai trò | Tên đăng nhập | Mật khẩu |
| :--- | :--- | :--- |
| **Admin** | `admin` | `123456` |
| **Cán bộ học bổng** | `canbo` / `canbo1` | `123456` |
| **Sinh viên** | `sv001` .. `sv030` | `123456` |
