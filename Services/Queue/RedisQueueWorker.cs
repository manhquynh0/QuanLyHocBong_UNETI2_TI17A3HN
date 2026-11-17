using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Models.Jobs;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Services
{
    /// <summary>
    /// Tiến trình chạy ngầm (BackgroundService) tự động lấy Job từ hàng đợi
    /// để gửi Email và Upload File lên Supabase Cloud (thử lại tối đa 3 lần nếu lỗi).
    /// </summary>
    public class RedisQueueWorker : BackgroundService
    {
        private const int MaxRetryCount = 3;
        private readonly IBackgroundQueueService _queueService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RedisQueueWorker> _logger;

        public RedisQueueWorker(
            IBackgroundQueueService queueService,
            IServiceScopeFactory scopeFactory,
            ILogger<RedisQueueWorker> logger)
        {
            _queueService = queueService;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[RedisQueueWorker] Tiến trình xử lý hàng đợi ngầm (Email & Upload) đã khởi động.");

            while (!stoppingToken.IsCancellationRequested)
            {
                bool processedAny = false;

                try
                {
                    var emailJob = await _queueService.DequeueEmailAsync();
                    if (emailJob != null)
                    {
                        processedAny = true;
                        await ProcessEmailJobAsync(emailJob);
                    }

                    var uploadJob = await _queueService.DequeueUploadAsync();
                    if (uploadJob != null)
                    {
                        processedAny = true;
                        await ProcessUploadJobAsync(uploadJob, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[RedisQueueWorker] Lỗi trong vòng lặp xử lý hàng đợi.");
                }

                if (!processedAny)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }

            _logger.LogInformation("[RedisQueueWorker] Tiến trình xử lý hàng đợi ngầm đã dừng.");
        }

        private async Task ProcessEmailJobAsync(EmailJob job)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                bool success = await emailService.SendEmailDirectlyAsync(job.ToEmail, job.Subject, job.HtmlMessage);
                if (success)
                {
                    _logger.LogInformation("[RedisQueueWorker] Hoàn thành Job Email {JobId} gửi tới {To}.", job.JobId, job.ToEmail);
                    return;
                }

                await RetryJobAsync(
                    "Email", job.JobId, job.RetryCount,
                    r => job.RetryCount = r,
                    () => _queueService.EnqueueEmailAsync(job),
                    "Dịch vụ gửi email trả về không thành công.");
            }
            catch (Exception ex)
            {
                await RetryJobAsync(
                    "Email", job.JobId, job.RetryCount,
                    r => job.RetryCount = r,
                    () => _queueService.EnqueueEmailAsync(job),
                    ex.Message);
            }
        }

        private async Task ProcessUploadJobAsync(UploadFileJob job, CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var storageService = scope.ServiceProvider.GetRequiredService<ISupabaseStorageService>();
                var context = scope.ServiceProvider.GetRequiredService<QuanLyHocBong_UNETI2_TI17A3HNContext>();

                var cloudUrl = await storageService.UploadLocalFileAsync(job.LocalFilePath, job.OriginalFileName, job.Folder);
                if (string.IsNullOrEmpty(cloudUrl))
                {
                    await RetryJobAsync(
                        "Upload", job.JobId, job.RetryCount,
                        r => job.RetryCount = r,
                        () => _queueService.EnqueueUploadAsync(job),
                        "Upload lên Cloud trả về null.");
                    return;
                }

                await SaveCloudUrlToDbAsync(context, job, cloudUrl, stoppingToken);
                _logger.LogInformation("[RedisQueueWorker] Hoàn thành Job Upload {JobId}. Cloud URL: {Url}", job.JobId, cloudUrl);
            }
            catch (Exception ex)
            {
                await RetryJobAsync(
                    "Upload", job.JobId, job.RetryCount,
                    r => job.RetryCount = r,
                    () => _queueService.EnqueueUploadAsync(job),
                    ex.Message);
            }
        }

        private static async Task SaveCloudUrlToDbAsync(
            QuanLyHocBong_UNETI2_TI17A3HNContext context,
            UploadFileJob job,
            string cloudUrl,
            CancellationToken stoppingToken)
        {
            if (job.EntityType == "HoSoHocBong")
            {
                var hoSo = await context.HoSoHocBong.FindAsync(new object[] { job.EntityId }, stoppingToken);
                if (hoSo == null) return;

                hoSo.FileMinhChung = cloudUrl;
                await context.SaveChangesAsync(stoppingToken);
                return;
            }

            if (job.EntityType == "SinhVienAvatar")
            {
                var sv = await context.SinhVien.FindAsync(new object[] { job.EntityId }, stoppingToken);
                if (sv == null) return;

                sv.AnhDaiDien = cloudUrl;
                var tk = await context.TaiKhoan.FindAsync(new object[] { sv.MaTaiKhoan }, stoppingToken);
                if (tk != null) tk.AnhDaiDien = cloudUrl;

                await context.SaveChangesAsync(stoppingToken);
                return;
            }

            if (job.EntityType == "TaiKhoanAvatar")
            {
                var tk = await context.TaiKhoan.FindAsync(new object[] { job.EntityId }, stoppingToken);
                if (tk == null) return;

                tk.AnhDaiDien = cloudUrl;
                var sv = await context.SinhVien.FirstOrDefaultAsync(s => s.MaTaiKhoan == tk.MaTaiKhoan, stoppingToken);
                if (sv != null) sv.AnhDaiDien = cloudUrl;

                await context.SaveChangesAsync(stoppingToken);
            }
        }

        private async Task RetryJobAsync(
            string jobType,
            string jobId,
            int currentRetry,
            Action<int> setRetry,
            Func<Task> requeueAction,
            string reason)
        {
            if (currentRetry < MaxRetryCount)
            {
                int nextRetry = currentRetry + 1;
                setRetry(nextRetry);
                _logger.LogWarning("[RedisQueueWorker] Thử lại Job {Type} {JobId} (Lần {Retry}/{Max}): {Reason}",
                    jobType, jobId, nextRetry, MaxRetryCount, reason);
                await requeueAction();
            }
            else
            {
                _logger.LogError("[RedisQueueWorker] Job {Type} {JobId} thất bại sau {Max} lần thử: {Reason}",
                    jobType, jobId, MaxRetryCount, reason);
            }
        }
    }
}
