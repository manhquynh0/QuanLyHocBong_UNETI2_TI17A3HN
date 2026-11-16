using System.Threading.Tasks;
using QuanLyHocBong_UNETI2_TI17A3HN.Models.Jobs;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Services
{
    public interface IBackgroundQueueService
    {
        Task EnqueueEmailAsync(EmailJob job);
        Task EnqueueUploadAsync(UploadFileJob job);
        Task<EmailJob?> DequeueEmailAsync();
        Task<UploadFileJob?> DequeueUploadAsync();
    }
}
