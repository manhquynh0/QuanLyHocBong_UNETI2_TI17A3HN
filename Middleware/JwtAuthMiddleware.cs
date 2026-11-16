using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.Services;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Middleware
{
    public class JwtAuthMiddleware
    {
        private readonly RequestDelegate _next;

        public JwtAuthMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            JwtService jwtService,
            QuanLyHocBong_UNETI2_TI17A3HNContext db)
        {
            // 1. Lấy và kiểm tra AccessToken
            var accessToken = GetAccessToken(context);
            var payload = jwtService.ValidateAccessToken(accessToken);

            // 2. Nếu AccessToken hết hạn -> Thử làm mới bằng RefreshToken trong DB
            if (payload == null)
            {
                payload = await TryRefreshTokenAsync(context, jwtService, db);
            }

            // 3. Gắn thông tin người dùng vào HttpContext nếu xác thực thành công
            if (payload != null)
            {
                AttachUserToContext(context, payload);
            }

            await _next(context);
        }

        private static string? GetAccessToken(HttpContext context)
        {
            var token = context.Request.Cookies["AccessToken"];
            if (!string.IsNullOrEmpty(token)) return token;

            var authHeader = context.Request.Headers["Authorization"].ToString();
            if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authHeader["Bearer ".Length..].Trim();
            }

            if (context.Request.Query.TryGetValue("access_token", out var queryToken) && !string.IsNullOrWhiteSpace(queryToken))
            {
                return queryToken.ToString();
            }

            return null;
        }

        private static async Task<JwtPayload?> TryRefreshTokenAsync(
            HttpContext context,
            JwtService jwtService,
            QuanLyHocBong_UNETI2_TI17A3HNContext db)
        {
            var refreshToken = context.Request.Cookies["RefreshToken"];
            if (string.IsNullOrWhiteSpace(refreshToken)) return null;

            if (jwtService.ValidateRefreshToken(refreshToken) == null)
            {
                ClearTokenCookies(context);
                return null;
            }

            var tokenRecord = await db.RefreshToken
                .Include(r => r.TaiKhoan)
                .FirstOrDefaultAsync(r => r.Token == refreshToken && !r.IsRevoked);

            if (tokenRecord == null || tokenRecord.ExpiryDate <= DateTime.Now || tokenRecord.TaiKhoan == null || !tokenRecord.TaiKhoan.TrangThai)
            {
                ClearTokenCookies(context);
                return null;
            }

            var account = tokenRecord.TaiKhoan;
            var avatar = account.AnhDaiDien;
            if (string.IsNullOrEmpty(avatar))
            {
                var student = await db.SinhVien.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.MaTaiKhoan == account.MaTaiKhoan);
                avatar = student?.AnhDaiDien;
            }

            // Thu hồi token cũ và cấp cặp token mới (Token Rotation)
            tokenRecord.IsRevoked = true;
            var newAccessToken = jwtService.GenerateAccessToken(account, avatar);
            var newRefreshToken = jwtService.GenerateRefreshToken(account);

            db.RefreshToken.Add(new RefreshToken
            {
                MaTaiKhoan = account.MaTaiKhoan,
                Token = newRefreshToken,
                ExpiryDate = DateTime.Now.AddDays(JwtService.RefreshTokenExpireDays),
                CreatedAt = DateTime.Now,
                IsRevoked = false
            });
            await db.SaveChangesAsync();

            SaveTokenCookies(context, newAccessToken, newRefreshToken);
            return jwtService.ValidateAccessToken(newAccessToken);
        }

        private static void AttachUserToContext(HttpContext context, JwtPayload payload)
        {
            context.Items["JwtUser"] = payload;
            context.Items["MaTaiKhoan"] = payload.AccountId;
            context.Items["HoTen"] = payload.FullName;
            context.Items["VaiTro"] = payload.Role;
            context.Items["AnhDaiDien"] = payload.Avatar;

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, payload.AccountId.ToString()),
                new(ClaimTypes.Name, payload.FullName ?? string.Empty),
                new(ClaimTypes.Role, payload.Role ?? string.Empty)
            };

            if (!string.IsNullOrEmpty(payload.Avatar))
            {
                claims.Add(new Claim("AnhDaiDien", payload.Avatar));
            }

            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "JwtAuth"));
        }

        private static CookieOptions BuildCookieOptions(HttpContext context, DateTimeOffset expires)
        {
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = context.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Expires = expires
            };
        }

        // Lưu AccessToken & RefreshToken vào HttpOnly Cookie
        public static void SaveTokenCookies(HttpContext context, string accessToken, string refreshToken)
        {
            context.Response.Cookies.Append("AccessToken", accessToken,
                BuildCookieOptions(context, DateTimeOffset.UtcNow.AddMinutes(JwtService.AccessTokenExpireMinutes)));

            context.Response.Cookies.Append("RefreshToken", refreshToken,
                BuildCookieOptions(context, DateTimeOffset.UtcNow.AddDays(JwtService.RefreshTokenExpireDays)));
        }

        // Cập nhật lại riêng AccessToken khi người dùng đổi Họ tên hoặc Ảnh đại diện
        public static void UpdateAccessTokenCookie(HttpContext context, string accessToken)
        {
            context.Response.Cookies.Append("AccessToken", accessToken,
                BuildCookieOptions(context, DateTimeOffset.UtcNow.AddMinutes(JwtService.AccessTokenExpireMinutes)));
        }

        // Xóa Cookie chứa token khi đăng xuất hoặc token không hợp lệ
        public static void ClearTokenCookies(HttpContext context)
        {
            context.Response.Cookies.Delete("AccessToken");
            context.Response.Cookies.Delete("RefreshToken");
        }
    }
}
