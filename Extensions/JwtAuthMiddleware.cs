using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.Services;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Extensions
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
            // 1. Lấy AccessToken từ Cookie hoặc Header Authorization
            var accessToken = context.Request.Cookies["AccessToken"];
            if (string.IsNullOrEmpty(accessToken))
            {
                var authHeader = context.Request.Headers["Authorization"].ToString();
                if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    accessToken = authHeader["Bearer ".Length..].Trim();
                }
            }

            // 2. Xác minh AccessToken
            var payload = jwtService.ValidateAccessToken(accessToken);

            // 3. Nếu AccessToken hết hạn -> Dùng bảng RefreshToken trong Database để làm mới
            if (payload == null)
            {
                var refreshToken = context.Request.Cookies["RefreshToken"];
                if (!string.IsNullOrWhiteSpace(refreshToken))
                {
                    var tokenRecord = await db.RefreshToken
                        .Include(r => r.TaiKhoan)
                        .FirstOrDefaultAsync(r => r.Token == refreshToken && !r.IsRevoked);

                    if (tokenRecord != null && tokenRecord.ExpiryDate > DateTime.Now && tokenRecord.TaiKhoan != null && tokenRecord.TaiKhoan.TrangThai)
                    {
                        var account = tokenRecord.TaiKhoan;
                        var avatar = account.AnhDaiDien;
                        if (string.IsNullOrEmpty(avatar))
                        {
                            var student = await db.SinhVien.AsNoTracking()
                                .FirstOrDefaultAsync(s => s.MaTaiKhoan == account.MaTaiKhoan);
                            avatar = student?.AnhDaiDien;
                        }

                        // Thu hồi token cũ (Cơ chế Token Rotation)
                        tokenRecord.IsRevoked = true;

                        // Tạo cặp token mới
                        var newAccessToken = jwtService.GenerateAccessToken(account, avatar);
                        var newRefreshToken = jwtService.GenerateRefreshToken();

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
                        payload = jwtService.ValidateAccessToken(newAccessToken);
                    }
                    else
                    {
                        ClearTokenCookies(context);
                        context.Session.Clear();
                    }
                }
                else
                {
                    context.Session.Clear();
                }
            }

            // 4. Gắn thông tin người dùng đã xác thực vào Session và Request Context
            if (payload != null)
            {
                var currentAccountId = context.Session.GetInt32("MaTaiKhoan");
                context.Items["JwtUser"] = payload;
                context.Session.SetInt32("MaTaiKhoan", payload.AccountId);
                context.Session.SetString("VaiTro", payload.Role);

                if (currentAccountId != payload.AccountId || string.IsNullOrEmpty(context.Session.GetString("HoTen")))
                {
                    context.Session.SetString("HoTen", payload.FullName);
                }
                if ((currentAccountId != payload.AccountId || string.IsNullOrEmpty(context.Session.GetString("AnhDaiDien")))
                    && !string.IsNullOrEmpty(payload.Avatar))
                {
                    context.Session.SetString("AnhDaiDien", payload.Avatar);
                }
            }

            await _next(context);
        }

        // Lưu AccessToken & RefreshToken vào HttpOnly Cookie
        public static void SaveTokenCookies(HttpContext context, string accessToken, string refreshToken)
        {
            context.Response.Cookies.Append("AccessToken", accessToken, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddMinutes(JwtService.AccessTokenExpireMinutes)
            });

            context.Response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(JwtService.RefreshTokenExpireDays)
            });
        }

        // Xóa Cookie chứa token khi đăng xuất hoặc token không hợp lệ
        public static void ClearTokenCookies(HttpContext context)
        {
            context.Response.Cookies.Delete("AccessToken");
            context.Response.Cookies.Delete("RefreshToken");
        }
    }
}
