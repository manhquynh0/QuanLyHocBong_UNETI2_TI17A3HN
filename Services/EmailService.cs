using System;
using System.Net.Http;
using System.Net.Http.Headers;
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
                // 1. Lấy thông tin cấu hình Brevo API (ưu tiên .env, environment variable, sau đó appsettings.json)
                var apiKey = _configuration["BREVO_API_KEY"] 
                             ?? _configuration["Brevo:ApiKey"] 
                             ?? Environment.GetEnvironmentVariable("BREVO_API_KEY");

                var senderEmail = _configuration["BREVO_SENDER_EMAIL"] 
                                  ?? _configuration["Brevo:SenderEmail"] 
                                  ?? Environment.GetEnvironmentVariable("BREVO_SENDER_EMAIL") 
                                  ?? "vumanhquynh2852005@gmail.com";

                var senderName = _configuration["BREVO_SENDER_NAME"] 
                                 ?? _configuration["Brevo:SenderName"] 
                                 ?? Environment.GetEnvironmentVariable("BREVO_SENDER_NAME") 
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
                        <p style=""margin: 4px 0 0 0; font-style: italic; color: #94a3b8;"">Email này được gửi tự động qua Brevo API, vui lòng không phản hồi trực tiếp email này.</p>
                    </div>
                </div>";

                // 2. Nếu có cấu hình Brevo API Key thì gửi trực tiếp qua Brevo REST API v3
                if (!string.IsNullOrWhiteSpace(apiKey) && apiKey.StartsWith("xkeysib-"))
                {
                    var client = _httpClientFactory.CreateClient();
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("api-key", apiKey);
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    object payload;
                    if (!string.IsNullOrWhiteSpace(senderEmail) && !senderEmail.Equals(toEmail, StringComparison.OrdinalIgnoreCase))
                    {
                        payload = new
                        {
                            sender = new { name = senderName, email = senderEmail },
                            to = new[] { new { email = toEmail } },
                            bcc = new[] { new { email = senderEmail } },
                            subject = subject,
                            htmlContent = styledHtml
                        };
                    }
                    else
                    {
                        payload = new
                        {
                            sender = new { name = senderName, email = senderEmail },
                            to = new[] { new { email = toEmail } },
                            subject = subject,
                            htmlContent = styledHtml
                        };
                    }

                    var jsonString = JsonSerializer.Serialize(payload);
                    var httpContent = new StringContent(jsonString, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("https://api.brevo.com/v3/smtp/email", httpContent);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("[Brevo Email] Gửi email thành công tới {ToEmail}! Tiêu đề: '{Subject}'. Phản hồi Brevo: {Response}", 
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
                    _logger.LogInformation("[EmailService] Chưa cấu hình Brevo API Key hoặc key không hợp lệ. Đã ghi nhận email gửi tới {ToEmail}: {Subject}", 
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
