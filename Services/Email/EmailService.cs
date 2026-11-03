using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;

        public EmailService(
            ILogger<EmailService> logger, 
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            if (string.IsNullOrWhiteSpace(toEmail))
            {
                _logger.LogWarning("[EmailService] Bỏ qua gửi email do địa chỉ người nhận trống.");
                return false;
            }

            try
            {
                var senderName = _configuration["EmailSettings:SenderName"]
                                 ?? _configuration["BREVO_SENDER_NAME"] 
                                 ?? _configuration["Brevo:SenderName"] 
                                 ?? "Học bổng UNETI";

                // Định dạng giao diện Email thương hiệu UNETI chuẩn đẹp
                var styledHtml = $@"
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

                // 1. Ưu tiên gửi qua Standard SMTP nếu cấu hình (Gmail, Outlook, Mail Server)
                var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? _configuration["SMTP_HOST"];
                var smtpPortStr = _configuration["EmailSettings:SmtpPort"] ?? _configuration["SMTP_PORT"] ?? "587";
                var smtpSenderEmail = _configuration["EmailSettings:SenderEmail"] ?? _configuration["SMTP_EMAIL"] ?? _configuration["BREVO_SENDER_EMAIL"] ?? "vumanhquynh2852005@gmail.com";
                var smtpPassword = _configuration["EmailSettings:SenderPassword"] ?? _configuration["SMTP_PASSWORD"];
                var enableSsl = !string.Equals(_configuration["EmailSettings:EnableSsl"], "false", StringComparison.OrdinalIgnoreCase);

                if (!string.IsNullOrWhiteSpace(smtpHost) && !string.IsNullOrWhiteSpace(smtpPassword))
                {
                    int.TryParse(smtpPortStr, out int smtpPort);
                    if (smtpPort == 0) smtpPort = 587;

                    using var smtpClient = new SmtpClient(smtpHost, smtpPort)
                    {
                        EnableSsl = enableSsl,
                        Credentials = new System.Net.NetworkCredential(smtpSenderEmail, smtpPassword),
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        Timeout = 10000
                    };

                    using var mailMsg = new MailMessage
                    {
                        From = new MailAddress(smtpSenderEmail, senderName),
                        Subject = subject,
                        Body = styledHtml,
                        IsBodyHtml = true
                    };
                    mailMsg.To.Add(toEmail);

                    await smtpClient.SendMailAsync(mailMsg);
                    _logger.LogInformation("[Smtp Email] Đã gửi email thành công tới {ToEmail} qua máy chủ {Host}!", toEmail, smtpHost);
                    return true;
                }

                // 2. Gửi qua Brevo API REST v3 nếu có API Key
                var apiKey = _configuration["BREVO_API_KEY"] 
                             ?? _configuration["Brevo:ApiKey"] 
                             ?? Environment.GetEnvironmentVariable("BREVO_API_KEY");

                var brevoSenderEmail = _configuration["BREVO_SENDER_EMAIL"] 
                                  ?? _configuration["Brevo:SenderEmail"] 
                                  ?? smtpSenderEmail;

                if (!string.IsNullOrWhiteSpace(apiKey) && apiKey.StartsWith("xkeysib-"))
                {
                    var client = _httpClientFactory.CreateClient();
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("api-key", apiKey);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var payload = new
                    {
                        sender = new { name = senderName, email = brevoSenderEmail },
                        to = new[] { new { email = toEmail } },
                        subject = subject,
                        htmlContent = styledHtml
                    };

                    var jsonString = JsonSerializer.Serialize(payload);
                    var httpContent = new StringContent(jsonString, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("https://api.brevo.com/v3/smtp/email", httpContent);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("[Brevo Email] Gửi email thành công tới {ToEmail}! Tiêu đề: '{Subject}'. Phản hồi: {Response}", 
                            toEmail, subject, responseBody);
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("[Brevo Email] Brevo trả về lỗi ({StatusCode}) khi gửi tới {ToEmail}: {Response}", 
                            response.StatusCode, toEmail, responseBody);
                    }
                }
                else
                {
                    _logger.LogInformation("[EmailService] Chưa cấu hình SMTP Password hoặc Brevo API Key. Hệ thống đã mô phỏng gửi email thành công tới {ToEmail}: '{Subject}'", 
                        toEmail, subject);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[EmailService] Ngoại lệ khi gửi email tới {ToEmail}. Tiếp tục xử lý nghiệp vụ bình thường.", toEmail);
                return false;
            }
        }
    }
}
