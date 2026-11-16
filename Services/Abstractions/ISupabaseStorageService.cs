using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Services
{
    public interface ISupabaseStorageService
    {
        /// <summary>
        /// Tải file hoặc hình ảnh từ IFormFile lên Cloud Storage.
        /// </summary>
        Task<string?> UploadFileAsync(IFormFile file, string folder = "minhchung");

        /// <summary>
        /// Tải file từ đường dẫn lưu tạm trên ổ đĩa máy chủ lên Cloud Storage (dùng cho Background Worker).
        /// </summary>
        Task<string?> UploadLocalFileAsync(string localFilePath, string originalFileName, string folder = "minhchung");

        /// <summary>
        /// Xóa file khỏi Cloud Storage theo URL hoặc đường dẫn file.
        /// </summary>
        Task<bool> DeleteFileAsync(string fileUrlOrPath);
    }
}
