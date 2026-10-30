using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Services
{
    public class JwtPayload
    {
        public int AccountId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public long Exp { get; set; }
    }

    public class JwtService
    {
        private readonly string _secretKey;

        public const int AccessTokenExpireMinutes = 15;
        public const int RefreshTokenExpireDays = 7;

        public JwtService(IConfiguration configuration)
        {
            _secretKey = configuration["JWT_SECRET_KEY"]
                ?? "KhoaBiMat_QuanLyHocBong_UNETI2_TI17A3HN_2026_SecretKey!@#";
        }

        // Tạo AccessToken (thời hạn 15 phút)
        public string GenerateAccessToken(TaiKhoan account, string? avatar = null)
        {
            var header = new { alg = "HS256", typ = "JWT" };
            var payload = new JwtPayload
            {
                AccountId = account.MaTaiKhoan,
                Username = account.TenDangNhap,
                FullName = account.HoTen,
                Role = account.VaiTro,
                Avatar = avatar ?? account.AnhDaiDien,
                Exp = DateTimeOffset.UtcNow.AddMinutes(AccessTokenExpireMinutes).ToUnixTimeSeconds()
            };

            string headerBase64 = Base64UrlEncode(JsonSerializer.Serialize(header));
            string payloadBase64 = Base64UrlEncode(JsonSerializer.Serialize(payload));
            string dataToSign = $"{headerBase64}.{payloadBase64}";
            string signature = CreateHmacSignature(dataToSign);

            return $"{dataToSign}.{signature}";
        }

        // Tạo chuỗi RefreshToken ngẫu nhiên
        public string GenerateRefreshToken()
        {
            var randomBytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(randomBytes);
        }

        // Kiểm tra và giải mã AccessToken
        public JwtPayload? ValidateAccessToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            var parts = token.Split('.');
            if (parts.Length != 3)
            {
                return null;
            }

            string dataToSign = $"{parts[0]}.{parts[1]}";
            string providedSignature = parts[2];
            string validSignature = CreateHmacSignature(dataToSign);

            if (!string.Equals(providedSignature, validSignature, StringComparison.Ordinal))
            {
                return null;
            }

            try
            {
                string payloadJson = Base64UrlDecode(parts[1]);
                var payload = JsonSerializer.Deserialize<JwtPayload>(payloadJson);
                if (payload == null)
                {
                    return null;
                }

                long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (payload.Exp <= currentTime)
                {
                    return null;
                }

                return payload;
            }
            catch
            {
                return null;
            }
        }

        private string CreateHmacSignature(string input)
        {
            var keyBytes = Encoding.UTF8.GetBytes(_secretKey);
            var inputBytes = Encoding.UTF8.GetBytes(input);
            using var hmac = new HMACSHA256(keyBytes);
            var hashBytes = hmac.ComputeHash(inputBytes);
            return Convert.ToBase64String(hashBytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static string Base64UrlEncode(string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static string Base64UrlDecode(string base64Url)
        {
            string padded = base64Url.Replace('-', '+').Replace('_', '/');
            switch (padded.Length % 4)
            {
                case 2: padded += "=="; break;
                case 3: padded += "="; break;
            }
            var bytes = Convert.FromBase64String(padded);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
