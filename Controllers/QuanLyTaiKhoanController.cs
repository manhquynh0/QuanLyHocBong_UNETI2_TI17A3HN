using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Infrastructure;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Controllers;

[SessionRole("Admin")]
public class QuanLyTaiKhoanController(QuanLyHocBong_UNETI2_TI17A3HNContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.TaiKhoan.AsNoTracking().OrderBy(x => x.MaTaiKhoan).ToListAsync());
    public IActionResult Create() => View("Form", new TaiKhoanFormViewModel());
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaiKhoanFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.MatKhau)) ModelState.AddModelError(nameof(model.MatKhau), "Mật khẩu là bắt buộc");
        ValidateRole(model.VaiTro);
        if (await db.TaiKhoan.AnyAsync(x => x.TenDangNhap == model.TenDangNhap.Trim())) ModelState.AddModelError(nameof(model.TenDangNhap), "Tên đăng nhập đã tồn tại");
        if (!ModelState.IsValid) return View("Form", model);
        var account = new TaiKhoan { TenDangNhap=model.TenDangNhap.Trim(), HoTen=model.HoTen.Trim(), Email=model.Email.Trim(), VaiTro=model.VaiTro, TrangThai=model.TrangThai };
        account.MatKhau = new PasswordHasher<TaiKhoan>().HashPassword(account, model.MatKhau!);
        db.Add(account); await db.SaveChangesAsync(); return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Edit(int id)
    {
        var a = await db.TaiKhoan.FindAsync(id); if (a is null) return NotFound();
        return View("Form", new TaiKhoanFormViewModel { MaTaiKhoan=a.MaTaiKhoan, TenDangNhap=a.TenDangNhap, HoTen=a.HoTen, Email=a.Email, VaiTro=a.VaiTro, TrangThai=a.TrangThai });
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TaiKhoanFormViewModel model)
    {
        if (id != model.MaTaiKhoan) return BadRequest(); ValidateRole(model.VaiTro);
        if (await db.TaiKhoan.AnyAsync(x => x.TenDangNhap == model.TenDangNhap.Trim() && x.MaTaiKhoan != id)) ModelState.AddModelError(nameof(model.TenDangNhap), "Tên đăng nhập đã tồn tại");
        var account = await db.TaiKhoan.FindAsync(id); if (account is null) return NotFound();
        if (!ModelState.IsValid) return View("Form", model);
        account.TenDangNhap=model.TenDangNhap.Trim(); account.HoTen=model.HoTen.Trim(); account.Email=model.Email.Trim(); account.VaiTro=model.VaiTro; account.TrangThai=model.TrangThai;
        if (!string.IsNullOrWhiteSpace(model.MatKhau)) account.MatKhau = new PasswordHasher<TaiKhoan>().HashPassword(account, model.MatKhau);
        await db.SaveChangesAsync(); return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (HttpContext.Session.GetInt32("MaTaiKhoan") == id) { TempData["Error"] = "Không thể xóa tài khoản đang đăng nhập."; return RedirectToAction(nameof(Index)); }
        var a = await db.TaiKhoan.FindAsync(id); if (a is null) return NotFound();
        if (await db.SinhVien.AnyAsync(s => s.MaTaiKhoan == id)) { TempData["Error"] = "Không thể xóa tài khoản đã liên kết hồ sơ sinh viên."; return RedirectToAction(nameof(Index)); }
        db.TaiKhoan.Remove(a); await db.SaveChangesAsync(); return RedirectToAction(nameof(Index));
    }
    private void ValidateRole(string role) { if (!new[] { "Admin", "Cán bộ học bổng", "Sinh viên" }.Contains(role)) ModelState.AddModelError(nameof(TaiKhoanFormViewModel.VaiTro), "Vai trò không hợp lệ"); }
}
