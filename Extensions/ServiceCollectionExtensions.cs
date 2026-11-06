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
using StackExchange.Redis;

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
        /// Cấu hình kết nối Redis và Rate Limiting (Fixed Window Counter: INCR + EXPIRE).
        /// - Đọc chuỗi kết nối từ REDIS_URL (.env) hoặc ConnectionStrings:Redis (appsettings.json).
        /// - Tự động Fallback sang IMemoryCache nếu Redis chưa được bật hoặc mất kết nối.
        /// </summary>
        public static IServiceCollection AddRedisRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            var rawConn = configuration["REDIS_URL"]
                          ?? configuration.GetConnectionString("Redis")
                          ?? "localhost:6379,abortConnect=false,connectTimeout=2000";

            try
            {
                var options = ParseRedisConfiguration(rawConn);
                var multiplexer = ConnectionMultiplexer.Connect(options);
                services.AddSingleton<IConnectionMultiplexer>(multiplexer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Redis Warning] Không thể kết nối Redis lúc khởi động: {ex.Message}. Sẽ dùng In-Memory Fallback.");
                var fallbackOptions = new ConfigurationOptions { AbortOnConnectFail = false };
                fallbackOptions.EndPoints.Add("localhost", 6379);
                services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(fallbackOptions));
            }

            // Bộ nhớ đệm dự phòng khi không có Redis
            services.AddMemoryCache();

            return services;
        }

        private static ConfigurationOptions ParseRedisConfiguration(string raw)
        {
            if (raw.StartsWith("redis://", StringComparison.OrdinalIgnoreCase) ||
                raw.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase))
            {
                var uri = new Uri(raw);
                var parts = uri.UserInfo.Split(':', 2);
                var password = parts.Length > 1 ? parts[1] : parts[0];
                var user = parts.Length > 1 ? parts[0] : null;

                var options = new ConfigurationOptions
                {
                    EndPoints = { { uri.Host, uri.Port > 0 ? uri.Port : 6379 } },
                    Password = string.IsNullOrEmpty(password) ? null : password,
                    User = string.IsNullOrEmpty(user) || user == "default" ? null : user,
                    Ssl = raw.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase),
                    AbortOnConnectFail = false,
                    ConnectTimeout = 3000,
                    SyncTimeout = 3000
                };
                return options;
            }

            var standard = ConfigurationOptions.Parse(raw);
            standard.AbortOnConnectFail = false;
            return standard;
        }
    }
}
