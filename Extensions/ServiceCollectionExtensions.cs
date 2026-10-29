using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Repositories.Abstractions;
using QuanLyHocBong_UNETI2_TI17A3HN.Repositories.Implementations;
using QuanLyHocBong_UNETI2_TI17A3HN.Services;
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

            // Giữ bộ nhớ đệm ngữ cảnh để đồng bộ hiển thị trên Razor Views
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Module 3 - Học bổng (Service gọi qua Repository & UnitOfWork)
            services.AddScoped<IHocBongService, HocBongService>();

            // Module 4 - Hồ sơ học bổng (Service gọi qua Repository & UnitOfWork)
            services.AddScoped<IHoSoHocBongService, HoSoHocBongService>();

            // Module Email (dùng bởi KetQuaHocBong & LichPhongVan)
            services.AddScoped<IEmailService, EmailService>();

            // HttpClient & Cloud Storage Service
            services.AddHttpClient();
            services.AddScoped<ISupabaseStorageService, SupabaseStorageService>();

            return services;
        }
    }
}
