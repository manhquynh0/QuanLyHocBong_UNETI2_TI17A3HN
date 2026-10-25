using System.Threading.Tasks;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Services
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string toEmail, string subject, string htmlMessage);
    }
}

