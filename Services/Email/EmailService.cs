using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuanLyHocBong_UNETI2_TI17A3HN.Models.Jobs;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IBackgroundQueueService _queueService;

        public EmailService(
            ILogger<EmailService> logger,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            IBackgroundQueueService queueService)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _queueService = queueService;
        }

        /// <summary>
        /// Đẩy công việc gửi Email vào Hàng đợi Redis để phản hồi ngay lập tức.
        /// </summary>
        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("[EmailService] Bỏ qua gửi email do địa chỉ người nhận trống.");
                return false;
            }

            await _queueService.EnqueueEmailAsync(new EmailJob
            {
                ToEmail = toEmail.Trim(),
                Subject = subject,
                HtmlMessage = htmlMessage
            });
            return true;
        }

        /// <summary>
        /// Gửi Email trực tiếp qua SMTP hoặc Brevo API (được RedisQueueWorker gọi ở nền).
        /// </summary>
        public async Task<bool> SendEmailDirectlyAsync(string toEmail, string subject, string htmlMessage)
        {
            if (string.IsNullOrWhiteSpace(toEmail)) return false;

            try
            {
                var senderName = _configuration["EmailSettings:SenderName"]
                                 ?? _configuration["BREVO_SENDER_NAME"]
                                 ?? _configuration["Brevo:SenderName"]
                                 ?? "Học bổng UNETI";

                var styledHtml = WrapEmailTemplate(htmlMessage);

                // 1. Ưu tiên gửi qua SMTP nếu có cấu hình mật khẩu
                if (await TrySendViaSmtpAsync(toEmail, subject, styledHtml, senderName))
                    return true;

                // 2. Dự phòng gửi qua Brevo REST API nếu có API Key
                var brevoResult = await TrySendViaBrevoAsync(toEmail, subject, styledHtml, senderName);
                if (brevoResult.HasValue)
                    return brevoResult.Value;

                // 3. Chưa cấu hình kênh gửi -> mô phỏng thành công để không chặn luồng test/dev
                _logger.LogInformation("[EmailService] Chưa cấu hình SMTP/Brevo. Mô phỏng gửi email tới {ToEmail}: '{Subject}'", toEmail, subject);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EmailService] Lỗi khi gửi email tới {ToEmail}.", toEmail);
                return false;
            }
        }

        private async Task<bool> TrySendViaSmtpAsync(string toEmail, string subject, string styledHtml, string senderName)
        {
            var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? _configuration["SMTP_HOST"];
            var smtpPassword = _configuration["EmailSettings:SenderPassword"] ?? _configuration["SMTP_PASSWORD"];
            if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpPassword))
                return false;

            var smtpPortStr = _configuration["EmailSettings:SmtpPort"] ?? _configuration["SMTP_PORT"] ?? "587";
            int.TryParse(smtpPortStr, out int smtpPort);
            if (smtpPort == 0) smtpPort = 587;

            var senderEmail = _configuration["EmailSettings:SenderEmail"]
                              ?? _configuration["SMTP_EMAIL"]
                              ?? _configuration["BREVO_SENDER_EMAIL"]
                              ?? "vumanhquynh2852005@gmail.com";
            var enableSsl = !string.Equals(_configuration["EmailSettings:EnableSsl"], "false", StringComparison.OrdinalIgnoreCase);

            using var smtpClient = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(senderEmail, smtpPassword),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 10000
            };

            using var mailMsg = new MailMessage
            {
                From = new MailAddress(senderEmail, senderName),
                Subject = subject,
                Body = styledHtml,
                IsBodyHtml = true
            };
            mailMsg.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMsg);
            _logger.LogInformation("[Smtp Email] Đã gửi thành công tới {ToEmail} qua {Host}.", toEmail, smtpHost);
            return true;
        }

        private async Task<bool?> TrySendViaBrevoAsync(string toEmail, string subject, string styledHtml, string senderName)
        {
            var apiKey = _configuration["BREVO_API_KEY"]
                         ?? _configuration["Brevo:ApiKey"]
                         ?? Environment.GetEnvironmentVariable("BREVO_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey) || !apiKey.StartsWith("xkeysib-"))
                return null;

            var senderEmail = _configuration["BREVO_SENDER_EMAIL"]
                              ?? _configuration["Brevo:SenderEmail"]
                              ?? _configuration["EmailSettings:SenderEmail"]
                              ?? "vumanhquynh2852005@gmail.com";

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("api-key", apiKey);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var payload = new
            {
                sender = new { name = senderName, email = senderEmail },
                to = new[] { new { email = toEmail } },
                subject,
                htmlContent = styledHtml
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.brevo.com/v3/smtp/email", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[Brevo Email] Đã gửi thành công tới {ToEmail}. Phản hồi: {Response}", toEmail, responseBody);
                return true;
            }

            _logger.LogWarning("[Brevo Email] Gửi thất bại ({StatusCode}) tới {ToEmail}: {Response}", response.StatusCode, toEmail, responseBody);
            return false;
        }

        private static string WrapEmailTemplate(string htmlMessage) => $@"
            <div style=""font-family: 'Segoe UI', Arial, sans-serif; max-width: 620px; margin: 0 auto; background: #ffffff; border-radius: 10px; overflow: hidden; border: 1px solid #e2e8f0; box-shadow: 0 4px 12px rgba(0,0,0,0.05);"">
                <div style=""background: linear-gradient(135deg, #0e1d35 0%, #1e3a8a 60%, #2563eb 100%); color: #ffffff; padding: 26px 30px; text-align: center;"">
                    <h2 style=""margin: 0; font-size: 20px; font-weight: 700; letter-spacing: 0.5px;"">TRƯỜNG ĐẠI HỌC KINH TẾ - KỸ THUẬT CÔNG NGHIỆP</h2>
                    <p style=""margin: 6px 0 0 0; font-size: 14px; opacity: 0.9;"">Cổng thông tin & Quản lý Xét duyệt Học bổng UNETI</p>
                </div>
                <div style=""padding: 30px; color: #1e293b; line-height: 1.65; font-size: 14.5px;"">
                    {htmlMessage}
                </div>
                <div style=""background: #f8fafc; padding: 18px 30px; text-align: center; font-size: 12px; color: #64748b; border-top: 1px solid #e2e8f0;"">
                    <p style=""margin: 0 0 4px 0; font-weight: 600;"">Hệ thống Thông tin Quản lý Học bổng Trực tuyến UNETI</p>
                    <p style=""margin: 0;"">Cơ sở Hà Nội: 456 Minh Khai, Hai Bà Trưng | 218 Lĩnh Nam, Hoàng Mai, Hà Nội</p>
                    <p style=""margin: 4px 0 0 0; font-style: italic; color: #94a3b8;"">Email này được gửi tự động từ hệ thống xét học bổng UNETI, vui lòng không phản hồi trực tiếp email này.</p>
                </div>
            </div>";
    }
}
