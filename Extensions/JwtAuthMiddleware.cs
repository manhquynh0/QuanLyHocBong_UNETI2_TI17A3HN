using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
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
            var accessToken = context.Request.Cookies["AccessToken"];
            if (string.IsNullOrEmpty(accessToken))
            {
                var authHeader = context.Request.Headers["Authorization"].ToString();
                if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    accessToken = authHeader["Bearer ".Length..].Trim();
                }
            }

            var payload = jwtService.XacMinhAccessToken(accessToken);

            if (payload == null)
            {
                var refreshToken = context.Request.Cookies["RefreshToken"];
                if (!string.IsNullOrWhiteSpace(refreshToken))
                {
                    var account = await db.TaiKhoan.FirstOrDefaultAsync(t =>
                        t.RefreshToken == refreshToken &&
                        t.TrangThai == true);

                    if (account != null && account.RefreshTokenExpiryTime > DateTime.Now)
                    {
                        var avatar = account.AnhDaiDien;
                        if (string.IsNullOrEmpty(avatar))
                        {
                            var sv = await db.SinhVien.AsNoTracking()
                                .FirstOrDefaultAsync(s => s.MaTaiKhoan == account.MaTaiKhoan);
                            avatar = sv?.AnhDaiDien;
                        }

                        var newAccessToken = jwtService.TaoAccessToken(account, avatar);
                        var newRefreshToken = jwtService.TaoRefreshToken();

                        account.RefreshToken = newRefreshToken;
                        account.RefreshTokenExpiryTime = DateTime.Now.AddDays(JwtService.ThoiGianRefreshTokenNgay);
                        await db.SaveChangesAsync();

                        LuuTokenVaoCookie(context, newAccessToken, newRefreshToken);
                        payload = jwtService.XacMinhAccessToken(newAccessToken);
                    }
                    else
                    {
                        XoaTokenCookie(context);
                        context.Session.Clear();
                    }
                }
                else
                {
                    context.Session.Clear();
                }
            }

            if (payload != null)
            {
                var currentAccountId = context.Session.GetInt32("MaTaiKhoan");
                context.Items["JwtUser"] = payload;
                context.Session.SetInt32("MaTaiKhoan", payload.MaTaiKhoan);
                context.Session.SetString("VaiTro", payload.VaiTro);

                if (currentAccountId != payload.MaTaiKhoan || string.IsNullOrEmpty(context.Session.GetString("HoTen")))
                {
                    context.Session.SetString("HoTen", payload.HoTen);
                }
                if ((currentAccountId != payload.MaTaiKhoan || string.IsNullOrEmpty(context.Session.GetString("AnhDaiDien")))
                    && !string.IsNullOrEmpty(payload.AnhDaiDien))
                {
                    context.Session.SetString("AnhDaiDien", payload.AnhDaiDien);
                }
            }

            await _next(context);
        }

        public static void LuuTokenVaoCookie(HttpContext context, string accessToken, string refreshToken)
        {
            context.Response.Cookies.Append("AccessToken", accessToken, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddMinutes(JwtService.ThoiGianAccessTokenPhut)
            });

            context.Response.Cookies.Append("RefreshToken", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(JwtService.ThoiGianRefreshTokenNgay)
            });
        }

        public static void XoaTokenCookie(HttpContext context)
        {
            context.Response.Cookies.Delete("AccessToken");
            context.Response.Cookies.Delete("RefreshToken");
        }
    }
}