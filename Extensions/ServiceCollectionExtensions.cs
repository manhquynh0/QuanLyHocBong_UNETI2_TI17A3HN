using System;
using System.IO;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Repositories.Abstractions;
using QuanLyHocBong_UNETI2_TI17A3HN.Repositories.Implementations;
using QuanLyHocBong_UNETI2_TI17A3HN.Services;
using QuanLyHocBong_UNETI2_TI17A3HN.Services.Business;
using QuanLyHocBong_UNETI2_TI17A3HN.Services.Implementations;
using QuanLyHocBong_UNETI2_TI17A3HN.Services.Interface;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Extensions
{
    /// <summary>
    /// Các phương thức mở rộng (Extension Methods) đăng ký Dependency Injection kết hợp Repository & Service Pattern.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static WebApplicationBuilder AddDotEnvConfiguration(this WebApplicationBuilder builder)
        {
            var envFilePath = Path.Combine(builder.Environment.ContentRootPath, ".env");
            if (File.Exists(envFilePath))
            {
                foreach (var line in File.ReadAllLines(envFilePath))
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
                    var parts = trimmed.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var val = parts[1].Trim();
                        Environment.SetEnvironmentVariable(key, val);
                        builder.Configuration[key] = val;
                    }
                }
            }
            return builder;
        }

        public static IServiceCollection AddPersistenceLayer(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<QuanLyHocBong_UNETI2_TI17A3HNContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("QuanLyHocBong_UNETI2_TI17A3HNContext")
                    ?? throw new InvalidOperationException("Connection string 'QuanLyHocBong_UNETI2_TI17A3HNContext' not found."))
                       .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

            // Đăng ký Repository Pattern & Unit of Work Pattern
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IHocBongRepository, HocBongRepository>();
            services.AddScoped<IHoSoHocBongRepository, HoSoHocBongRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            // Đăng ký dịch vụ JWT (AccessToken + RefreshToken)
            services.AddSingleton<JwtService>();

            // Module 3 - Học bổng (Service gọi qua Repository & UnitOfWork)
            services.AddScoped<IHocBongService, HocBongService>();

            // Module 4 - Hồ sơ học bổng (Service gọi qua Repository & UnitOfWork)
            services.AddScoped<IHoSoHocBongService, HoSoHocBongService>();

            // Module Email (dùng bởi KetQuaHocBong & LichPhongVan)
            services.AddScoped<IEmailService, EmailService>();

            // Module Thông báo Realtime (SignalR WebSocket)
            services.AddSignalR();
            services.AddScoped<INotificationService, NotificationService>();

            // HttpClient & Cloud Storage Service
            services.AddHttpClient();
            services.AddScoped<ISupabaseStorageService, SupabaseStorageService>();

            return services;
        }

        /// <summary>
        /// Cấu hình Rate Limiting bảo vệ các điểm nóng (Đăng nhập, Nộp hồ sơ, Upload tệp) chống spam và dò mật khẩu.
        /// Sử dụng thư viện có sẵn của .NET, hoàn toàn trong bộ nhớ (In-Memory), không cần Redis.
        /// </summary>
        public static IServiceCollection AddAppRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Xử lý phản hồi khi người dùng gửi quá số lượt cho phép
                options.OnRejected = async (context, cancellationToken) =>
                {
                    context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                    var isAjax = string.Equals(context.HttpContext.Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
                                 || context.HttpContext.Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);

                    if (isAjax)
                    {
                        context.HttpContext.Response.ContentType = "application/json; charset=utf-8";
                        await context.HttpContext.Response.WriteAsync(
                            "{\"success\": false, \"message\": \"Bạn đang thao tác quá nhanh. Vui lòng thử lại sau 1 phút!\"}",
                            cancellationToken);
                    }
                    else
                    {
                        context.HttpContext.Response.ContentType = "text/html; charset=utf-8";
                        await context.HttpContext.Response.WriteAsync(@"
<!DOCTYPE html>
<html lang='vi'>
<head>
    <meta charset='utf-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <title>429 - Thao tác quá nhanh</title>
    <link rel='stylesheet' href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css' />
    <link rel='stylesheet' href='https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css' />
</head>
<body class='bg-light d-flex align-items-center justify-content-center' style='min-height: 100vh; font-family: Segoe UI, sans-serif;'>
    <div class='card shadow-sm border-0 p-4 text-center' style='max-width: 480px; border-radius: 12px;'>
        <div class='mb-3 text-warning'>
            <i class='bi bi-hourglass-split' style='font-size: 54px;'></i>
        </div>
        <h4 class='fw-bold text-dark'>Thao tác quá nhanh!</h4>
        <p class='text-muted small mt-2'>
            Hệ thống phát hiện có quá nhiều yêu cầu gửi liên tục từ bạn trong thời gian ngắn để bảo vệ an toàn máy chủ.
        </p>
        <div class='alert alert-warning py-2 small'>
            <i class='bi bi-shield-lock me-1'></i>Vui lòng chờ khoảng <strong>1 phút</strong> rồi thử lại.
        </div>
        <div class='mt-3'>
            <a href='javascript:history.back()' class='btn btn-primary px-4'>
                <i class='bi bi-arrow-left me-1'></i>Quay lại trang trước
            </a>
        </div>
    </div>
</body>
</html>", cancellationToken);
                    }
                };

                // 1. Điểm nóng Đăng nhập: Tối đa 5 lần / 1 phút theo IP (Chống Brute Force dò mật khẩu)
                options.AddPolicy("login_limit", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));

                // 2. Điểm nóng Nộp hồ sơ: Tối đa 3 lần / 1 phút (Chống click đúp và spam nộp hồ sơ liên tiếp)
                options.AddPolicy("submit_limit", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 3,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));

                // 3. Điểm nóng Tải lên tệp minh chứng: Tối đa 5 lần / 1 phút (Chống spam upload làm đầy server)
                options.AddPolicy("upload_limit", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown_ip",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));
            });

            return services;
        }
    }
}
