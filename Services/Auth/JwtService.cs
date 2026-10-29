using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Services
{
    public class JwtPayloadData
    {
        public int MaTaiKhoan { get; set; }
        public string TenDangNhap { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
        public string VaiTro { get; set; } = string.Empty;
        public string? AnhDaiDien { get; set; }
        public long exp { get; set; }
    }

    public class JwtService
    {
        private readonly string _secretKey;

        public const int ThoiGianAccessTokenPhut = 15;
        public const int ThoiGianRefreshTokenNgay = 7;

        public JwtService(IConfiguration configuration)
        {
            _secretKey = configuration["JWT_SECRET_KEY"]
                ?? "KhoaBiMat_QuanLyHocBong_UNETI2_TI17A3HN_2026_SecretKey!@#";
        }

        public string TaoAccessToken(TaiKhoan taiKhoan, string? anhDaiDien = null)
        {
            var header = new { alg = "HS256", typ = "JWT" };
            var payload = new JwtPayloadData
            {
                MaTaiKhoan = taiKhoan.MaTaiKhoan,
                TenDangNhap = taiKhoan.TenDangNhap,
                HoTen = taiKhoan.HoTen,
                VaiTro = taiKhoan.VaiTro,
                AnhDaiDien = anhDaiDien ?? taiKhoan.AnhDaiDien,
                exp = DateTimeOffset.UtcNow.AddMinutes(ThoiGianAccessTokenPhut).ToUnixTimeSeconds()
            };

            string headerBase64 = Base64UrlEncode(JsonSerializer.Serialize(header));
            string payloadBase64 = Base64UrlEncode(JsonSerializer.Serialize(payload));
            string duLieuCanKy = $"{headerBase64}.{payloadBase64}";
            string chuKy = TaoChuKyHmacSha256(duLieuCanKy);

            return $"{duLieuCanKy}.{chuKy}";
        }

        public string TaoRefreshToken()
        {
            var randomBytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(randomBytes);
        }

        public JwtPayloadData? XacMinhAccessToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;

            var parts = token.Split('.');
            if (parts.Length != 3)
                return null;

            string duLieuCanKy = $"{parts[0]}.{parts[1]}";
            string chuKyGuiLen = parts[2];
            string chuKyHopLe = TaoChuKyHmacSha256(duLieuCanKy);

            if (!string.Equals(chuKyGuiLen, chuKyHopLe, StringComparison.Ordinal))
                return null;

            try
            {
                string payloadJson = Base64UrlDecode(parts[1]);
                var payload = JsonSerializer.Deserialize<JwtPayloadData>(payloadJson);
                if (payload == null)
                    return null;

                long bayGio = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (payload.exp <= bayGio)
                    return null;

                return payload;
            }
            catch
            {
                return null;
            }
        }

        private string TaoChuKyHmacSha256(string input)
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