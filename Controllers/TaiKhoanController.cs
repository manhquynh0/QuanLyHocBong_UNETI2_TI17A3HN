using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Extensions;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.Services;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Controllers;

public class TaiKhoanController(
    QuanLyHocBong_UNETI2_TI17A3HNContext db,
    JwtService jwtService) : Controller
{
    [HttpGet]
    public IActionResult DangNhap(string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        return View(new DangNhapViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DangNhap(DangNhapViewModel model, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var username = model.TenDangNhap.Trim();
        var account = await db.TaiKhoan.SingleOrDefaultAsync(x => x.TenDangNhap == username || x.Email == username);
        if (account is null)
        {
            ModelState.AddModelError("", "Tên đăng nhập không tồn tại.");
            return View(model);
        }
        if (!account.TrangThai)
        {
            ModelState.AddModelError("", "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên.");
            return View(model);
        }

        var hasher = new PasswordHasher<TaiKhoan>();
        var sha256Hex = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(model.MatKhau)));
        var verification = account.MatKhau.StartsWith("AQAAAA", StringComparison.Ordinal)
            ? hasher.VerifyHashedPassword(account, account.MatKhau, model.MatKhau)
            : ((account.MatKhau == model.MatKhau || string.Equals(account.MatKhau, sha256Hex, StringComparison.OrdinalIgnoreCase))
                ? PasswordVerificationResult.Success
                : PasswordVerificationResult.Failed);

        if (verification == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError("", "Mật khẩu không chính xác.");
            return View(model);
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded || !account.MatKhau.StartsWith("AQAAAA", StringComparison.Ordinal))
        {
            account.MatKhau = hasher.HashPassword(account, model.MatKhau);
        }

        var role = account.VaiTro ?? "Sinh viên";
        if (role.StartsWith("Sinh", StringComparison.OrdinalIgnoreCase)) role = "Sinh viên";
        else if (role.StartsWith("C", StringComparison.OrdinalIgnoreCase)) role = "Cán bộ học bổng";
        else if (role.StartsWith("Admin", StringComparison.OrdinalIgnoreCase)) role = "Admin";
        account.VaiTro = role;

        var avatar = account.AnhDaiDien;
        if (string.IsNullOrEmpty(avatar))
        {
            var sv = await db.SinhVien.AsNoTracking().FirstOrDefaultAsync(s => s.MaTaiKhoan == account.MaTaiKhoan);
            if (!string.IsNullOrEmpty(sv?.AnhDaiDien))
            {
                avatar = sv.AnhDaiDien;
            }
        }

        // 1. Tạo JWT AccessToken (15 phút) và RefreshToken (7 ngày)
        var accessToken = jwtService.GenerateAccessToken(account, avatar);
        var refreshToken = jwtService.GenerateRefreshToken();

        // 2. Lưu bản ghi RefreshToken vào bảng riêng RefreshToken trong Database
        var tokenEntity = new RefreshToken
        {
            MaTaiKhoan = account.MaTaiKhoan,
            Token = refreshToken,
            ExpiryDate = DateTime.Now.AddDays(JwtService.RefreshTokenExpireDays),
            CreatedAt = DateTime.Now,
            IsRevoked = false
        };
        db.RefreshToken.Add(tokenEntity);
        await db.SaveChangesAsync();

        // 3. Lưu AccessToken & RefreshToken vào HttpOnly Cookie
        JwtAuthMiddleware.SaveTokenCookies(HttpContext, accessToken, refreshToken);

        // Đồng bộ ngữ cảnh hiện tại cho giao diện
        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32("MaTaiKhoan", account.MaTaiKhoan);
        HttpContext.Session.SetString("HoTen", account.HoTen);
        HttpContext.Session.SetString("VaiTro", role);
        if (!string.IsNullOrEmpty(avatar))
        {
            HttpContext.Session.SetString("AnhDaiDien", avatar);
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction("Index", "Home");
    }

    // Endpoint làm mới AccessToken từ bảng RefreshToken trong Database
    [HttpPost]
    public async Task<IActionResult> LamMoiToken(string? refreshToken = null)
    {
        var tokenToCheck = refreshToken ?? Request.Cookies["RefreshToken"];
        if (string.IsNullOrWhiteSpace(tokenToCheck))
        {
            return Unauthorized(new { message = "Không tìm thấy Refresh Token." });
        }

        var tokenRecord = await db.RefreshToken
            .Include(r => r.TaiKhoan)
            .FirstOrDefaultAsync(r => r.Token == tokenToCheck && !r.IsRevoked);

        if (tokenRecord == null || tokenRecord.ExpiryDate <= DateTime.Now || tokenRecord.TaiKhoan == null || !tokenRecord.TaiKhoan.TrangThai)
        {
            JwtAuthMiddleware.ClearTokenCookies(HttpContext);
            return Unauthorized(new { message = "Refresh Token không hợp lệ hoặc đã hết hạn." });
        }

        // Thu hồi token cũ (Cơ chế Token Rotation)
        tokenRecord.IsRevoked = true;

        var newAccessToken = jwtService.GenerateAccessToken(tokenRecord.TaiKhoan);
        var newRefreshToken = jwtService.GenerateRefreshToken();

        // Tạo bản ghi token mới trong bảng RefreshToken
        db.RefreshToken.Add(new RefreshToken
        {
            MaTaiKhoan = tokenRecord.MaTaiKhoan,
            Token = newRefreshToken,
            ExpiryDate = DateTime.Now.AddDays(JwtService.RefreshTokenExpireDays),
            CreatedAt = DateTime.Now,
            IsRevoked = false
        });
        await db.SaveChangesAsync();

        JwtAuthMiddleware.SaveTokenCookies(HttpContext, newAccessToken, newRefreshToken);

        return Ok(new
        {
            accessToken = newAccessToken,
            refreshToken = newRefreshToken,
            expiresInMinutes = JwtService.AccessTokenExpireMinutes
        });
    }

    [HttpGet, HttpPost]
    public async Task<IActionResult> DangXuat()
    {
        // Thu hồi RefreshToken trong bảng RefreshToken khi đăng xuất
        var refreshToken = Request.Cookies["RefreshToken"];
        if (!string.IsNullOrEmpty(refreshToken))
        {
            var tokenRecord = await db.RefreshToken.FirstOrDefaultAsync(r => r.Token == refreshToken);
            if (tokenRecord != null)
            {
                tokenRecord.IsRevoked = true;
                await db.SaveChangesAsync();
            }
        }

        JwtAuthMiddleware.ClearTokenCookies(HttpContext);
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(DangNhap));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
