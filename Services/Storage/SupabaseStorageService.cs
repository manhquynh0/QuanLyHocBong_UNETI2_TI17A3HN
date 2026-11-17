using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Services
{
    public class SupabaseStorageService : ISupabaseStorageService
    {
        private readonly string? _projectId;
        private readonly string? _secretKey;
        private readonly string _bucket;
        private readonly HttpClient _httpClient;
        private readonly ILogger<SupabaseStorageService> _logger;

        public SupabaseStorageService(
            IConfiguration configuration,
            HttpClient httpClient,
            ILogger<SupabaseStorageService> logger)
        {
            _projectId = configuration["PROJECT_ID"]?.Trim();
            _secretKey = (configuration["SUPBER_BASE_API_KEY_SECRET"] ?? configuration["SUPBER_BASE_API_KEY"])?.Trim();
            _bucket = configuration["SUPABASE_BUCKET"]?.Trim() ?? "QLHB";
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<string?> UploadFileAsync(IFormFile file, string folder = "minhchung")
        {
            if (file == null || file.Length == 0 || file.Length > 10 * 1024 * 1024)
                return null;

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            string[] allowedExts = { ".pdf", ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExts.Contains(ext))
                return null;

            await using var stream = file.OpenReadStream();
            return await UploadStreamAsync(stream, ext, folder);
        }

        public async Task<string?> UploadLocalFileAsync(string localFilePath, string originalFileName, string folder = "minhchung")
        {
            if (string.IsNullOrWhiteSpace(localFilePath) || !File.Exists(localFilePath))
                return null;

            var ext = Path.GetExtension(string.IsNullOrWhiteSpace(originalFileName) ? localFilePath : originalFileName).ToLowerInvariant();
            await using var stream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return await UploadStreamAsync(stream, ext, folder);
        }

        public async Task<bool> DeleteFileAsync(string fileUrlOrPath)
        {
            if (string.IsNullOrWhiteSpace(fileUrlOrPath) || string.IsNullOrEmpty(_projectId) || string.IsNullOrEmpty(_secretKey))
                return false;

            try
            {
                var marker = $"/storage/v1/object/public/{_bucket}/";
                var storagePath = fileUrlOrPath.Contains(marker)
                    ? fileUrlOrPath[(fileUrlOrPath.IndexOf(marker) + marker.Length)..]
                    : fileUrlOrPath;
                storagePath = storagePath.TrimStart('/');

                var url = $"https://{_projectId}.supabase.co/storage/v1/object/{_bucket}/{storagePath}";
                using var request = new HttpRequestMessage(HttpMethod.Delete, url);
                request.Headers.Add("apikey", _secretKey);
                request.Headers.Add("Authorization", $"Bearer {_secretKey}");

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async Task<string?> UploadStreamAsync(Stream stream, string ext, string folder)
        {
            if (string.IsNullOrEmpty(_projectId) || string.IsNullOrEmpty(_secretKey))
            {
                _logger.LogError("Thiếu cấu hình PROJECT_ID hoặc SUPBER_BASE_API_KEY trong .env");
                return null;
            }

            try
            {
                var fileName = $"{Guid.NewGuid():N}{ext}";
                var storagePath = string.IsNullOrWhiteSpace(folder) ? fileName : $"{folder.Trim('/')}/{fileName}";
                var url = $"https://{_projectId}.supabase.co/storage/v1/object/{_bucket}/{storagePath}";

                using var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Add("apikey", _secretKey);
                request.Headers.Add("Authorization", $"Bearer {_secretKey}");

                using var content = new StreamContent(stream);
                content.Headers.ContentType = new MediaTypeHeaderValue(TaiLieuHoSoRules.LayMimeType(ext));
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode) return null;

                return $"https://{_projectId}.supabase.co/storage/v1/object/public/{_bucket}/{storagePath}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi upload file lên Supabase");
                return null;
            }
        }
    }
}
