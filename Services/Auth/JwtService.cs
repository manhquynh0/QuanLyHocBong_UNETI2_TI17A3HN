using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
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
        private readonly string _refreshSecretKey;

        public const int AccessTokenExpireMinutes = 15;
        public const int RefreshTokenExpireDays = 7;

        public JwtService(IConfiguration configuration)
        {
            _secretKey = configuration["JWT_SECRET_KEY"]
                ?? "KhoaBiMat_QuanLyHocBong_UNETI2_TI17A3HN_2026_SecretKey!@#";
            _refreshSecretKey = configuration["JWT_REFRESH_SECRET_KEY"]
                ?? "KhoaBiMat_RefreshToken_UNETI2_TI17A3HN_2026_RefreshKey!@#";
        }

        // 1. Tạo AccessToken (15 phút, chứa đầy đủ thông tin phiên làm việc)
        public string GenerateAccessToken(TaiKhoan account, string? avatar = null)
        {
            var claims = new List<Claim>
            {
                new("AccountId", account.MaTaiKhoan.ToString()),
                new("Username", account.TenDangNhap),
                new("FullName", account.HoTen ?? string.Empty),
                new(ClaimTypes.Role, account.VaiTro ?? string.Empty)
            };

            var avatarUrl = avatar ?? account.AnhDaiDien;
            if (!string.IsNullOrEmpty(avatarUrl))
            {
                claims.Add(new Claim("Avatar", avatarUrl));
            }

            return SignToken(claims, _secretKey, DateTime.UtcNow.AddMinutes(AccessTokenExpireMinutes));
        }

        // 2. Tạo RefreshToken (7 ngày, chỉ lưu AccountId + mã định danh jti cho gọn nhẹ)
        public string GenerateRefreshToken(TaiKhoan account)
        {
            var claims = new List<Claim>
            {
                new("AccountId", account.MaTaiKhoan.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            };

            return SignToken(claims, _refreshSecretKey, DateTime.UtcNow.AddDays(RefreshTokenExpireDays));
        }

        // 3. Kiểm tra AccessToken
        public JwtPayload? ValidateAccessToken(string? token)
            => ValidateToken(token, _secretKey);

        // 4. Kiểm tra RefreshToken
        public JwtPayload? ValidateRefreshToken(string? token)
            => ValidateToken(token, _refreshSecretKey);

        private static string SignToken(IEnumerable<Claim> claims, string secret, DateTime expires)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var token = new JwtSecurityToken(
                claims: claims,
                expires: expires,
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static JwtPayload? ValidateToken(string? token, string secret)
        {
            if (string.IsNullOrWhiteSpace(token)) return null;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var handler = new JwtSecurityTokenHandler();

            try
            {
                var principal = handler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out var validatedToken);

                var jwt = (JwtSecurityToken)validatedToken;
                int.TryParse(principal.FindFirst("AccountId")?.Value, out var id);

                return new JwtPayload
                {
                    AccountId = id,
                    Username = principal.FindFirst("Username")?.Value ?? string.Empty,
                    FullName = principal.FindFirst("FullName")?.Value ?? string.Empty,
                    Role = principal.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty,
                    Avatar = principal.FindFirst("Avatar")?.Value,
                    Exp = jwt.Payload.Expiration ?? 0
                };
            }
            catch
            {
                return null;
            }
        }
    }
}
