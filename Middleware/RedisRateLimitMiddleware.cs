using System;
using System.Globalization;
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
    /// Middleware kiểm soát giới hạn tần suất gửi yêu cầu sử dụng thuật toán Token Bucket (Thùng chứa thẻ) trên Redis.
    /// - Xử lý mượt mà lưu lượng đột biến (Burst Traffic) và nạp lại thẻ liên tục theo thời gian.
    /// - Tự động Fallback sang IMemoryCache nếu Redis mất kết nối (đảm bảo hệ thống không bao giờ bị lỗi).
    /// </summary>
    public class RedisRateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConnectionMultiplexer? _redis;
        private readonly IMemoryCache _memoryCache;
        private readonly ILogger<RedisRateLimitMiddleware> _logger;

        // Lua Script nguyên tử (Atomic) thực thi thuật toán Token Bucket trực tiếp bên trong Redis
        private const string TokenBucketLuaScript = @"
            local key = KEYS[1]
            local capacity = tonumber(ARGV[1])
            local refill_rate = tonumber(ARGV[2])
            local now = tonumber(ARGV[3])

            local data = redis.call('HMGET', key, 'tokens', 'last_refill')
            local tokens = tonumber(data[1])
            local last_refill = tonumber(data[2])

            if not tokens or not last_refill then
                tokens = capacity
                last_refill = now
            else
                local delta = math.max(0, now - last_refill)
                tokens = math.min(capacity, tokens + (delta * refill_rate))
                last_refill = now
            end

            if tokens >= 1 then
                tokens = tokens - 1
                redis.call('HMSET', key, 'tokens', tokens, 'last_refill', last_refill)
                redis.call('EXPIRE', key, 180)
                return 1
            else
                redis.call('HMSET', key, 'tokens', tokens, 'last_refill', last_refill)
                redis.call('EXPIRE', key, 180)
                return 0
            end
        ";

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
            // 1. Kiểm tra xem Action hiện tại có gắn nhãn [EnableRateLimiting] không
            var endpoint = context.GetEndpoint();
            var rateLimitAttr = endpoint?.Metadata.GetMetadata<EnableRateLimitingAttribute>();

            // Nếu không gắn nhãn Rate Limiting, cho phép đi tiếp ngay
            if (rateLimitAttr == null)
            {
                await _next(context);
                return;
            }

            // 2. Cấu hình thông số Token Bucket (Capacity: Dung tích xô, RefillRate: Tốc độ nạp thẻ/giây)
            var policy = rateLimitAttr.PolicyName ?? "";
            var (capacity, refillRate) = policy switch
            {
                "login_limit" => (5.0, 5.0 / 60.0),   // Đăng nhập: Xô tối đa 5 thẻ, nạp 5 thẻ / 60s (1 thẻ mỗi 12s)
                "submit_limit" => (3.0, 3.0 / 60.0),  // Nộp hồ sơ: Xô tối đa 3 thẻ, nạp 3 thẻ / 60s (1 thẻ mỗi 20s)
                "upload_limit" => (5.0, 5.0 / 60.0),  // Upload tệp: Xô tối đa 5 thẻ, nạp 5 thẻ / 60s (1 thẻ mỗi 12s)
                _ => (10.0, 10.0 / 60.0)
            };

            // 3. Lấy IP của người gửi
            var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                     ?? context.Connection.RemoteIpAddress?.ToString()
                     ?? "unknown_ip";

            // Chuẩn hóa key Redis: ví dụ tb:login_limit:127.0.0.1
            var key = $"tb:{policy}:{ip}";
            var nowSeconds = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
            bool? allowed = null;

            // 4. KIỂM TRA BẰNG REDIS VỚI THUẬT TOÁN TOKEN BUCKET (ƯU TIÊN HÀNG ĐẦU)
            if (_redis != null && _redis.IsConnected)
            {
                try
                {
                    var db = _redis.GetDatabase();
                    var result = await db.ScriptEvaluateAsync(
                        TokenBucketLuaScript,
                        new RedisKey[] { key },
                        new RedisValue[]
                        {
                            capacity.ToString(CultureInfo.InvariantCulture),
                            refillRate.ToString(CultureInfo.InvariantCulture),
                            nowSeconds.ToString(CultureInfo.InvariantCulture)
                        });

                    allowed = ((int)result) == 1;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[TokenBucket] Lỗi truy vấn Redis, tự động chuyển sang In-Memory Fallback.");
                    allowed = null; // Để kích hoạt Fallback bên dưới
                }
            }

            // 5. CƠ CHẾ DỰ PHÒNG TOKEN BUCKET TRÊN RAM (KHI KHÔNG CÓ REDIS HOẶC REDIS MẤT KẾT NỐI)
            if (!allowed.HasValue)
            {
                allowed = CheckInMemoryTokenBucket(key, capacity, refillRate, nowSeconds);
            }

            // 6. NẾU HẾT TOKEN TRONG XÔ -> CHẶN YÊU CẦU (HTTP 429 TOO MANY REQUESTS)
            if (!allowed.Value)
            {
                await WriteRateLimitExceededResponseAsync(context);
                return;
            }

            // Còn Token hợp lệ -> Cho phép đi tiếp
            await _next(context);
        }

        /// <summary>
        /// Thuật toán Token Bucket dự phòng chạy trực tiếp trong RAM (In-Memory).
        /// </summary>
        private bool CheckInMemoryTokenBucket(string key, double capacity, double refillRate, double nowSeconds)
        {
            var bucket = _memoryCache.GetOrCreate(key, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(5);
                return new TokenBucketState
                {
                    Tokens = capacity,
                    LastRefillSeconds = nowSeconds
                };
            });

            if (bucket == null) return true;

            lock (bucket)
            {
                var delta = Math.Max(0, nowSeconds - bucket.LastRefillSeconds);
                bucket.Tokens = Math.Min(capacity, bucket.Tokens + (delta * refillRate));
                bucket.LastRefillSeconds = nowSeconds;

                if (bucket.Tokens >= 1.0)
                {
                    bucket.Tokens -= 1.0;
                    return true;
                }

                return false;
            }
        }

        private class TokenBucketState
        {
            public double Tokens { get; set; }
            public double LastRefillSeconds { get; set; }
        }

        /// <summary>
        /// Gửi thông báo lỗi 429 thân thiện (hỗ trợ cả JSON cho Ajax/Fetch và HTML đẹp cho trình duyệt).
        /// </summary>
        private static async Task WriteRateLimitExceededResponseAsync(HttpContext context)
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;

            var isAjax = string.Equals(context.Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase)
                         || context.Request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase);

            if (isAjax)
            {
                context.Response.ContentType = "application/json; charset=utf-8";
                await context.Response.WriteAsync("{\"success\": false, \"message\": \"Bạn đang thao tác quá nhanh. Vui lòng thử lại sau giây lát!\"}");
            }
            else
            {
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
    <div class='card shadow-sm border-0 p-4 text-center' style='max-width: 480px; border-radius: 12px;'>
        <div class='mb-3 text-warning'>
            <i class='bi bi-hourglass-split' style='font-size: 54px;'></i>
        </div>
        <h4 class='fw-bold text-dark'>Thao tác quá nhanh!</h4>
        <p class='text-muted small mt-2'>
            Hệ thống phát hiện có quá nhiều yêu cầu gửi liên tục từ bạn trong thời gian ngắn để bảo vệ an toàn máy chủ.
        </p>
        <div class='alert alert-warning py-2 small'>
            <i class='bi bi-shield-lock me-1'></i>Vui lòng chờ một chút rồi thử lại.
        </div>
        <div class='mt-3'>
            <a href='javascript:history.back()' class='btn btn-primary px-4'>
                <i class='bi bi-arrow-left me-1'></i>Quay lại trang trước
            </a>
        </div>
    </div>
</body>
</html>");
            }
        }
    }
}
