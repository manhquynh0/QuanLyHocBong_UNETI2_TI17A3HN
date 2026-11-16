using System.Threading.Tasks;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Services
{
    public interface IEmailService
    {
        /// <summary>
        /// Đẩy yêu cầu gửi email vào Hàng đợi Redis (BullMQ pattern) để phản hồi tức thì cho người dùng.
        /// </summary>
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlMessage);

        /// <summary>
        /// Thực thi gửi email trực tiếp qua SMTP / Brevo API (được gọi bởi Background Worker).
        /// </summary>
        Task<bool> SendEmailDirectlyAsync(string toEmail, string subject, string htmlMessage);
    }
}
