using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Middleware
{
    /// <summary>
    /// Middleware giới hạn tần suất gửi yêu cầu (Rate Limiting) đơn giản theo khung thời gian 60 giây.
    /// - Ưu tiên đếm bằng Redis (INCR + EXPIRE).
    /// - Tự động dùng RAM (IMemoryCache) nếu Redis chưa bật hoặc mất kết nối.
    /// </summary>
    public class RedisRateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConnectionMultiplexer? _redis;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<RedisRateLimitMiddleware> _logger;

        public RedisRateLimitMiddleware(
            RequestDelegate next,
            IMemoryCache memoryCache,
            ILogger<RedisRateLimitMiddleware> logger,
            IConnectionMultiplexer? redis = null)
        {
            _next = next;
            _memoryCache = memoryCache;
            _logger = logger;
            _redis = redis;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // 1. Kiểm tra xem Action hiện tại có gắn [EnableRateLimiting] không
            var rateLimitAttr = context.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>();
            if (rateLimitAttr == null)
            {
                await _next(context);
                return;
            }

            // 2. Xác định số lần tối đa được phép trong 60 giây
            var policy = rateLimitAttr.PolicyName ?? "";
            int maxRequests = policy switch
            {
                "login_limit" => 5,   // Tối đa 5 lần / 60 giây
                "submit_limit" => 3,  // Tối đa 3 lần / 60 giây
                "upload_limit" => 5,  // Tối đa 5 lần / 60 giây
                _ => 10
            };

            // 3. Tạo key định danh theo chính sách và IP người dùng
            var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                     ?? context.Connection.RemoteIpAddress?.ToString()
                     ?? "unknown_ip";
            var key = $"rl:{policy}:{ip}";

            // 4. Kiểm tra số lượt gọi hiện tại
            bool isBlocked = await IsRateLimitExceededAsync(key, maxRequests);

            if (isBlocked)
            {
                await WriteTooManyRequestsResponseAsync(context);
                return;
            }

            await _next(context);
        }

        private async Task<bool> IsRateLimitExceededAsync(string key, int maxRequests)
        {
            // Ưu tiên đếm trên Redis
            if (_redis != null && _redis.IsConnected)
            {
                try
                {
                    var db = _redis.GetDatabase();
                    long count = await db.StringIncrementAsync(key);

                    // Lần đầu tiên gọi thì đặt thời gian tự xóa key sau 60 giây
                    if (count == 1)
                    {
                        await db.KeyExpireAsync(key, TimeSpan.FromSeconds(60));
                    }

                    return count > maxRequests;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi Redis Rate Limit, chuyển sang đếm trên RAM.");
                }
            }

            // Dự phòng trên RAM khi không có Redis
            int memoryCount = _memoryCache.Get<int>(key) + 1;
            _memoryCache.Set(key, memoryCount, TimeSpan.FromSeconds(60));
            return memoryCount > maxRequests;
        }

        private static async Task WriteTooManyRequestsResponseAsync(HttpContext context)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;

            var isAjax = string.Equals(context.Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
                         || context.Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);

            if (isAjax)
            {
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync("{\"success\": false, \"message\": \"Bạn đang thao tác quá nhanh. Vui lòng thử lại sau 1 phút!\"}");
                return;
            }

            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(@"
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
    <div class='card shadow-sm border-0 p-4 text-center' style='max-width: 460px; border-radius: 12px;'>
        <div class='mb-3 text-warning'><i class='bi bi-hourglass-split' style='font-size: 52px;'></i></div>
        <h4 class='fw-bold text-dark'>Thao tác quá nhanh!</h4>
        <p class='text-muted small mt-2'>Bạn đã gửi quá nhiều yêu cầu liên tiếp trong thời gian ngắn. Vui lòng chờ 1 phút rồi thử lại.</p>
        <div class='mt-3'>
            <a href='javascript:history.back()' class='btn btn-primary px-4'><i class='bi bi-arrow-left me-1'></i>Quay lại</a>
        </div>
    </div>
</body>
</html>");
        }
    }
}
