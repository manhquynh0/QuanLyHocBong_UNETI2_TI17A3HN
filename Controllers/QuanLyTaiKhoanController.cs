using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Attributes;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Middleware;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.Models.Jobs;
using QuanLyHocBong_UNETI2_TI17A3HN.Services;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Controllers;

[RoleAuthorize("Admin")]
public class QuanLyTaiKhoanController(
    QuanLyHocBong_UNETI2_TI17A3HNContext db,
    IWebHostEnvironment env,
    IBackgroundQueueService queueService,
    JwtService jwtService) : Controller
{
    public async Task<IActionResult> Index() => View(await db.TaiKhoan.AsNoTracking().OrderBy(x => x.MaTaiKhoan).ToListAsync());

    public IActionResult Create() => View("Form", new TaiKhoanFormViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TaiKhoanFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.MatKhau)) ModelState.AddModelError(nameof(model.MatKhau), "Mật khẩu là bắt buộc");
        ValidateRole(model.VaiTro);
        ValidateAvatar(model.AnhDaiDienUpload);

        var username = model.TenDangNhap?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(username) && await db.TaiKhoan.AnyAsync(x => x.TenDangNhap == username))
            ModelState.AddModelError(nameof(model.TenDangNhap), "Tên đăng nhập đã tồn tại");

        if (!ModelState.IsValid) return View("Form", model);

        var (localPath, avatarUrl) = await SaveAvatarLocallyAsync(model.AnhDaiDienUpload);
        var account = new TaiKhoan
        {
            TenDangNhap = username,
            HoTen = model.HoTen.Trim(),
            Email = model.Email.Trim(),
            VaiTro = model.VaiTro,
            TrangThai = model.TrangThai,
            AnhDaiDien = avatarUrl
        };
        account.MatKhau = new PasswordHasher<TaiKhoan>().HashPassword(account, model.MatKhau!);

        db.Add(account);
        await db.SaveChangesAsync();

        await EnqueueAvatarUploadAsync(localPath, model.AnhDaiDienUpload?.FileName, account.MaTaiKhoan);
        TempData["Success"] = "Tạo tài khoản mới thành công.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var a = await db.TaiKhoan.FindAsync(id);
        if (a is null) return NotFound();

        return View("Form", new TaiKhoanFormViewModel
        {
            MaTaiKhoan = a.MaTaiKhoan,
            TenDangNhap = a.TenDangNhap,
            HoTen = a.HoTen,
            Email = a.Email,
            VaiTro = a.VaiTro,
            TrangThai = a.TrangThai,
            AnhDaiDien = a.AnhDaiDien
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, TaiKhoanFormViewModel model)
    {
        if (id != model.MaTaiKhoan) return BadRequest();
        ValidateRole(model.VaiTro);
        ValidateAvatar(model.AnhDaiDienUpload);

        var username = model.TenDangNhap?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(username) && await db.TaiKhoan.AnyAsync(x => x.TenDangNhap == username && x.MaTaiKhoan != id))
            ModelState.AddModelError(nameof(model.TenDangNhap), "Tên đăng nhập đã tồn tại");

        var account = await db.TaiKhoan.FindAsync(id);
        if (account is null) return NotFound();

        model.AnhDaiDien ??= account.AnhDaiDien;
        if (!ModelState.IsValid) return View("Form", model);

        var (localPath, avatarUrl) = await SaveAvatarLocallyAsync(model.AnhDaiDienUpload);
        account.TenDangNhap = username;
        account.HoTen = model.HoTen.Trim();
        account.Email = model.Email.Trim();
        account.VaiTro = model.VaiTro;
        account.TrangThai = model.TrangThai;

        if (!string.IsNullOrEmpty(avatarUrl))
        {
            account.AnhDaiDien = avatarUrl;
            var sv = await db.SinhVien.FirstOrDefaultAsync(s => s.MaTaiKhoan == account.MaTaiKhoan);
            if (sv != null) sv.AnhDaiDien = avatarUrl;
        }

        if (!string.IsNullOrWhiteSpace(model.MatKhau))
            account.MatKhau = new PasswordHasher<TaiKhoan>().HashPassword(account, model.MatKhau);

        await db.SaveChangesAsync();
        await EnqueueAvatarUploadAsync(localPath, model.AnhDaiDienUpload?.FileName, account.MaTaiKhoan);

        if ((HttpContext.Items["MaTaiKhoan"] as int?) == account.MaTaiKhoan)
        {
            var newAccessToken = jwtService.GenerateAccessToken(account, account.AnhDaiDien);
            JwtAuthMiddleware.UpdateAccessTokenCookie(HttpContext, newAccessToken);
        }

        TempData["Success"] = "Cập nhật tài khoản thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if ((HttpContext.Items["MaTaiKhoan"] as int?) == id)
        {
            TempData["Error"] = "Không thể xóa tài khoản đang đăng nhập.";
            return RedirectToAction(nameof(Index));
        }
        var a = await db.TaiKhoan.FindAsync(id);
        if (a is null) return NotFound();
        if (await db.SinhVien.AnyAsync(s => s.MaTaiKhoan == id))
        {
            TempData["Error"] = "Không thể xóa tài khoản đã liên kết hồ sơ sinh viên.";
            return RedirectToAction(nameof(Index));
        }
        db.TaiKhoan.Remove(a);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private void ValidateRole(string role)
    {
        if (!new[] { "Admin", "Cán bộ học bổng", "Sinh viên" }.Contains(role))
            ModelState.AddModelError(nameof(TaiKhoanFormViewModel.VaiTro), "Vai trò không hợp lệ");
    }

    private void ValidateAvatar(IFormFile? file)
    {
        if (file == null || file.Length == 0) return;
        var error = TaiLieuHoSoStorage.KiemTraAnhDaiDien(file);
        if (error != null) ModelState.AddModelError(nameof(TaiKhoanFormViewModel.AnhDaiDienUpload), error);
    }

    private async Task<(string? localPath, string? relativeUrl)> SaveAvatarLocallyAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0) return (null, null);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var webRoot = string.IsNullOrEmpty(env.WebRootPath) ? Path.Combine(env.ContentRootPath, "wwwroot") : env.WebRootPath;
        var uploadFolder = Path.Combine(webRoot, "uploads", "avatars");
        Directory.CreateDirectory(uploadFolder);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadFolder, fileName);
        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return (filePath, $"/uploads/avatars/{fileName}");
    }

    private async Task EnqueueAvatarUploadAsync(string? localPath, string? originalFileName, int accountId)
    {
        if (string.IsNullOrEmpty(localPath) || string.IsNullOrEmpty(originalFileName)) return;

        await queueService.EnqueueUploadAsync(new UploadFileJob
        {
            LocalFilePath = localPath,
            OriginalFileName = originalFileName,
            Folder = "avatars",
            EntityType = "TaiKhoanAvatar",
            EntityId = accountId
        });
    }
}
