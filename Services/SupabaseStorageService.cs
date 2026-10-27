using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Services
{
    public class SupabaseStorageService : ISupabaseStorageService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<SupabaseStorageService> _logger;

        public SupabaseStorageService(
            IConfiguration configuration,
            HttpClient httpClient,
            ILogger<SupabaseStorageService> logger)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logger = logger;
        }

        private (string? projectId, string? secretKey, string bucket) GetCredentials()
        {
            var projectId = _configuration["PROJECT_ID"]?.Trim();
            var secretKey = (_configuration["SUPBER_BASE_API_KEY_SECRET"] ?? _configuration["SUPBER_BASE_API_KEY"])?.Trim();
            var bucket = _configuration["SUPABASE_BUCKET"]?.Trim() ?? "QLHB";

            if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(secretKey))
            {
                var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
                if (File.Exists(envPath))
                {
                    foreach (var line in File.ReadAllLines(envPath))
                    {
                        var trimmed = line.Trim();
                        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;
                        var parts = trimmed.Split('=', 2);
                        if (parts.Length == 2)
                        {
                            var key = parts[0].Trim();
                            var val = parts[1].Trim();
                            _configuration[key] = val;
                            if (key == "PROJECT_ID") projectId = val;
                            if (key == "SUPBER_BASE_API_KEY_SECRET") secretKey = val;
                            if (key == "SUPBER_BASE_API_KEY" && string.IsNullOrEmpty(secretKey)) secretKey = val;
                            if (key == "SUPABASE_BUCKET") bucket = val;
                        }
                    }
                }
            }

            return (projectId, secretKey, bucket);
        }

        public async Task<string?> UploadFileAsync(IFormFile file, string folder = "minhchung")
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            var (projectId, secretKey, bucket) = GetCredentials();
            if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(secretKey))
            {
                _logger.LogError("Thiếu cấu hình Cloud Storage (PROJECT_ID hoặc SUPBER_BASE_API_KEY_SECRET) trong .env");
                return null;
            }

            try
            {
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                var rawName = Path.GetFileNameWithoutExtension(file.FileName);
                var safeName = Regex.Replace(rawName, @"[^a-zA-Z0-9_\-]", "_");
                if (safeName.Length > 40)
                {
                    safeName = safeName.Substring(0, 40);
                }

                var fileName = $"{Guid.NewGuid():N}_{safeName}{ext}";
                var storagePath = string.IsNullOrWhiteSpace(folder) ? fileName : $"{folder.Trim('/')}/{fileName}";

                var uploadEndpoint = $"https://{projectId}.supabase.co/storage/v1/object/{bucket}/{storagePath}";

                using var request = new HttpRequestMessage(HttpMethod.Post, uploadEndpoint);
                request.Headers.Add("apikey", secretKey);
                request.Headers.Add("Authorization", $"Bearer {secretKey}");

                using var stream = file.OpenReadStream();
                using var content = new StreamContent(stream);

                var contentType = !string.IsNullOrWhiteSpace(file.ContentType)
                    ? file.ContentType
                    : GetContentTypeByExtension(ext);

                content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                request.Content = content;

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Lỗi khi tải file lên Cloud Storage: {StatusCode} - {Error}", response.StatusCode, errorBody);
                    return null;
                }

                var publicUrl = $"https://{projectId}.supabase.co/storage/v1/object/public/{bucket}/{storagePath}";
                _logger.LogInformation("Upload file/ảnh thành công lên Cloud Storage: {Url}", publicUrl);
                return publicUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ngoại lệ khi upload file lên Cloud Storage: {FileName}", file.FileName);
                return null;
            }
        }

        public async Task<bool> DeleteFileAsync(string fileUrlOrPath)
        {
            if (string.IsNullOrWhiteSpace(fileUrlOrPath))
            {
                return false;
            }

            var (projectId, secretKey, bucket) = GetCredentials();
            if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(secretKey))
            {
                return false;
            }

            try
            {
                string storagePath = fileUrlOrPath;
                var marker = $"/storage/v1/object/public/{bucket}/";
                if (storagePath.Contains(marker))
                {
                    storagePath = storagePath.Substring(storagePath.IndexOf(marker) + marker.Length);
                }
                storagePath = storagePath.TrimStart('/');

                var deleteEndpoint = $"https://{projectId}.supabase.co/storage/v1/object/{bucket}/{storagePath}";
                using var request = new HttpRequestMessage(HttpMethod.Delete, deleteEndpoint);
                request.Headers.Add("apikey", secretKey);
                request.Headers.Add("Authorization", $"Bearer {secretKey}");

                var response = await _httpClient.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ngoại lệ khi xóa file Cloud Storage: {Path}", fileUrlOrPath);
                return false;
            }
        }

        private static string GetContentTypeByExtension(string ext)
        {
            return ext switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };
        }
    }
}

