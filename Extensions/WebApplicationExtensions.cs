using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Extensions
{
    /// <summary>
    /// Các phương thức mở rộng cấu hình HTTP Pipeline, Database Migration và Endpoint Routing.
    /// </summary>
    public static class WebApplicationExtensions
    {
        public static WebApplication UseDatabaseMigration(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            try
            {
                var context = services.GetRequiredService<QuanLyHocBong_UNETI2_TI17A3HNContext>();
                DbInitializer.Seed(context);
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseMigration");
                logger.LogError(ex, "Lỗi xảy ra trong quá trình tự động cập nhật Database Migration và Seed Data.");
            }
            return app;
        }

        public static WebApplication MapApplicationEndpoints(this WebApplication app)
        {
            app.MapStaticAssets();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=TaiKhoan}/{action=DangNhap}/{id?}")
                .WithStaticAssets();

            return app;
        }
    }
}
