using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Controllers;

public class TaiKhoanController(QuanLyHocBong_UNETI2_TI17A3HNContext db) : Controller
{
    [HttpGet]
    public IActionResult DangNhap(string? returnUrl = null) { ViewBag.ReturnUrl = returnUrl; return View(new DangNhapViewModel()); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DangNhap(DangNhapViewModel model, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        if (!ModelState.IsValid) return View(model);
        var username = model.TenDangNhap.Trim();
        var account = await db.TaiKhoan.SingleOrDefaultAsync(x => x.TenDangNhap == username || x.Email == username);
        if (account is null) { ModelState.AddModelError("", "Tên đăng nhập không tồn tại."); return View(model); }
        if (!account.TrangThai) { ModelState.AddModelError("", "Tài khoản đã bị khóa. Vui lòng liên hệ quản trị viên."); return View(model); }
        var hasher = new PasswordHasher<TaiKhoan>();
        var verification = account.MatKhau.StartsWith("AQAAAA", StringComparison.Ordinal)
            ? hasher.VerifyHashedPassword(account, account.MatKhau, model.MatKhau)
            : (account.MatKhau == model.MatKhau ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed);
        if (verification == PasswordVerificationResult.Failed) { ModelState.AddModelError("", "Mật khẩu không chính xác."); return View(model); }
        if (verification == PasswordVerificationResult.SuccessRehashNeeded || !account.MatKhau.StartsWith("AQAAAA", StringComparison.Ordinal))
        { account.MatKhau = hasher.HashPassword(account, model.MatKhau); await db.SaveChangesAsync(); }

        var role = account.VaiTro ?? "Sinh viên";
        if (role.StartsWith("Sinh", StringComparison.OrdinalIgnoreCase)) role = "Sinh viên";
        else if (role.StartsWith("C", StringComparison.OrdinalIgnoreCase)) role = "Cán bộ học bổng";
        else if (role.StartsWith("Admin", StringComparison.OrdinalIgnoreCase)) role = "Admin";

        HttpContext.Session.Clear();
        HttpContext.Session.SetInt32("MaTaiKhoan", account.MaTaiKhoan);
        HttpContext.Session.SetString("HoTen", account.HoTen);
        HttpContext.Session.SetString("VaiTro", role);
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult DangXuat() { HttpContext.Session.Clear(); return RedirectToAction(nameof(DangNhap)); }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
