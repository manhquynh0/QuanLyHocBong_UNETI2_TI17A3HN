using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using QuanLyHocBong_UNETI2_TI17A3HN.Models.Jobs;
using StackExchange.Redis;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Services
{
    /// <summary>
    /// Hàng đợi công việc nền trên Redis (đẩy việc vào bằng LPUSH, lấy việc ra bằng RPOP).
    /// Tự động dùng hàng đợi trong RAM (ConcurrentQueue) nếu Redis chưa bật hoặc mất kết nối.
    /// </summary>
    public class RedisQueueService : IBackgroundQueueService
    {
        private const string EmailQueueKey = "queue:email";
        private const string UploadQueueKey = "queue:upload";

        private readonly IConnectionMultiplexer? _redis;
        private readonly ILogger<RedisQueueService> _logger;

        // Hàng đợi dự phòng trong RAM
        private readonly ConcurrentQueue<EmailJob> _emailFallback = new();
        private readonly ConcurrentQueue<UploadFileJob> _uploadFallback = new();

        public RedisQueueService(
            ILogger<RedisQueueService> logger,
            IConnectionMultiplexer? redis = null)
        {
            _logger = logger;
            _redis = redis;
        }

        public Task EnqueueEmailAsync(EmailJob job)  => PushJobAsync(EmailQueueKey, job, _emailFallback);

        public Task EnqueueUploadAsync(UploadFileJob job)  => PushJobAsync(UploadQueueKey, job, _uploadFallback);

        public Task<EmailJob?> DequeueEmailAsync()  => PopJobAsync(EmailQueueKey, _emailFallback);

        public Task<UploadFileJob?> DequeueUploadAsync() => PopJobAsync(UploadQueueKey, _uploadFallback);

        private async Task PushJobAsync<T>(string queueKey, T job, ConcurrentQueue<T> fallbackQueue)
        {
            if (_redis != null && _redis.IsConnected)
            {
                try
                {
                    var db = _redis.GetDatabase();
                    var json = JsonSerializer.Serialize(job);
                    await db.ListLeftPushAsync(queueKey, json);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi đẩy job vào Redis ({QueueKey}), chuyển sang RAM.", queueKey);
                }
            }

            fallbackQueue.Enqueue(job);
        }

        private async Task<T?> PopJobAsync<T>(string queueKey, ConcurrentQueue<T> fallbackQueue) where T : class
        {
            if (_redis != null && _redis.IsConnected)
            {
                try
                {
                    var db = _redis.GetDatabase();
                    var value = await db.ListRightPopAsync(queueKey);
                    if (value.HasValue)
                    {
                        return JsonSerializer.Deserialize<T>((string)value!);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi lấy job từ Redis ({QueueKey}).", queueKey);
                }
            }

            return fallbackQueue.TryDequeue(out var job) ? job : null;
        }
    }
}
