using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Services
{
    public interface ISupabaseStorageService
    {
        /// <summary>
        /// Tải file hoặc hình ảnh lên Cloud Storage.
        /// </summary>
        /// <param name="file">File/Ảnh do người dùng tải lên</param>
        /// <param name="folder">Thư mục con (ví dụ: minhchung, avatars)</param>
        /// <returns>Đường dẫn công khai (Public URL) của file trên CDN, hoặc null nếu lỗi</returns>
        Task<string?> UploadFileAsync(IFormFile file, string folder = "minhchung");

        /// <summary>
        /// Xóa file khỏi Cloud Storage theo URL hoặc đường dẫn file
        /// </summary>
        Task<bool> DeleteFileAsync(string fileUrlOrPath);
    }
}

