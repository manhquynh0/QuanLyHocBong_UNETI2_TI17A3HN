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
        var accessToken = jwtService.TaoAccessToken(account, avatar);
        var refreshToken = jwtService.TaoRefreshToken();

        // 2. Lưu RefreshToken và thời hạn vào Database (bảng TaiKhoan)
        account.RefreshToken = refreshToken;
        account.RefreshTokenExpiryTime = DateTime.Now.AddDays(JwtService.ThoiGianRefreshTokenNgay);
        await db.SaveChangesAsync();

        // 3. Lưu AccessToken & RefreshToken vào HttpOnly Cookie
        JwtAuthMiddleware.LuuTokenVaoCookie(HttpContext, accessToken, refreshToken);

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

    // Endpoint làm mới AccessToken từ RefreshToken lưu trong Database
    [HttpPost]
    public async Task<IActionResult> LamMoiToken(string? refreshToken = null)
    {
        var tokenCanKiemTra = refreshToken ?? Request.Cookies["RefreshToken"];
        if (string.IsNullOrWhiteSpace(tokenCanKiemTra))
        {
            return Unauthorized(new { message = "Không tìm thấy Refresh Token." });
        }

        var account = await db.TaiKhoan.FirstOrDefaultAsync(t =>
            t.RefreshToken == tokenCanKiemTra &&
            t.TrangThai == true);

        if (account == null || account.RefreshTokenExpiryTime <= DateTime.Now)
        {
            JwtAuthMiddleware.XoaTokenCookie(HttpContext);
            return Unauthorized(new { message = "Refresh Token không hợp lệ hoặc đã hết hạn." });
        }

        var newAccessToken = jwtService.TaoAccessToken(account);
        var newRefreshToken = jwtService.TaoRefreshToken();

        account.RefreshToken = newRefreshToken;
        account.RefreshTokenExpiryTime = DateTime.Now.AddDays(JwtService.ThoiGianRefreshTokenNgay);
        await db.SaveChangesAsync();

        JwtAuthMiddleware.LuuTokenVaoCookie(HttpContext, newAccessToken, newRefreshToken);

        return Ok(new
        {
            accessToken = newAccessToken,
            refreshToken = newRefreshToken,
            expiresInMinutes = JwtService.ThoiGianAccessTokenPhut
        });
    }

    [HttpGet, HttpPost]
    public async Task<IActionResult> DangXuat()
    {
        // Xóa RefreshToken trong Database khi đăng xuất
        var refreshToken = Request.Cookies["RefreshToken"];
        var maTaiKhoan = HttpContext.Session.GetInt32("MaTaiKhoan");

        TaiKhoan? account = null;
        if (!string.IsNullOrEmpty(refreshToken))
        {
            account = await db.TaiKhoan.FirstOrDefaultAsync(t => t.RefreshToken == refreshToken);
        }
        else if (maTaiKhoan.HasValue)
        {
            account = await db.TaiKhoan.FindAsync(maTaiKhoan.Value);
        }

        if (account != null)
        {
            account.RefreshToken = null;
            account.RefreshTokenExpiryTime = null;
            await db.SaveChangesAsync();
        }

        JwtAuthMiddleware.XoaTokenCookie(HttpContext);
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(DangNhap));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
