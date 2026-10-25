using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Infrastructure;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Controllers;

[SessionRole("Admin", "Cán bộ học bổng")]
public class DonViTaiTroController(QuanLyHocBong_UNETI2_TI17A3HNContext db) : Controller
{
    public async Task<IActionResult> Index() => View(await db.DonViTaiTro.Include(x => x.HocBongs).AsNoTracking().OrderBy(x => x.TenDonViTaiTro).ToListAsync());
    public async Task<IActionResult> Details(int id) { var x = await db.DonViTaiTro.Include(d => d.HocBongs).AsNoTracking().FirstOrDefaultAsync(d => d.MaDonViTaiTro == id); return x is null ? NotFound() : View(x); }
    public IActionResult Create() => View("Form", new DonViTaiTro { TrangThai = true });
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DonViTaiTro model)
    {
        await CheckDuplicate(model);
        if (!ModelState.IsValid) return View("Form", model);
        model.TenDonViTaiTro = model.TenDonViTaiTro.Trim(); db.Add(model); await db.SaveChangesAsync(); return RedirectToAction(nameof(Index));
    }
    public async Task<IActionResult> Edit(int id) { var x = await db.DonViTaiTro.FindAsync(id); return x is null ? NotFound() : View("Form", x); }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DonViTaiTro model)
    {
        if (id != model.MaDonViTaiTro) return BadRequest(); await CheckDuplicate(model, id);
        if (!ModelState.IsValid) return View("Form", model);
        model.TenDonViTaiTro = model.TenDonViTaiTro.Trim(); db.Update(model); await db.SaveChangesAsync(); return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var x = await db.DonViTaiTro.FindAsync(id); if (x is null) return NotFound();
        if (await db.HocBong.AnyAsync(h => h.MaDonViTaiTro == id)) { TempData["Error"] = "Không thể xóa đơn vị đang được học bổng sử dụng. Hãy cập nhật trạng thái thay thế."; return RedirectToAction(nameof(Index)); }
        db.Remove(x); await db.SaveChangesAsync(); return RedirectToAction(nameof(Index));
    }
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var x = await db.DonViTaiTro.FindAsync(id); if (x is null) return NotFound(); x.TrangThai = !x.TrangThai; await db.SaveChangesAsync(); return RedirectToAction(nameof(Index));
    }
    private async Task CheckDuplicate(DonViTaiTro model, int? exceptId = null)
    {
        if (!string.IsNullOrWhiteSpace(model.TenDonViTaiTro) && await db.DonViTaiTro.AnyAsync(x => x.TenDonViTaiTro == model.TenDonViTaiTro.Trim() && x.MaDonViTaiTro != exceptId))
            ModelState.AddModelError(nameof(model.TenDonViTaiTro), "Tên đơn vị tài trợ đã tồn tại");
    }
}
