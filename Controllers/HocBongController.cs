using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Controllers
{
    public class HocBongController : Controller
    {
        private readonly QuanLyHocBong_UNETI2_TI17A3HNContext _context;

        public HocBongController(QuanLyHocBong_UNETI2_TI17A3HNContext context)
        {
            _context = context;
        }

        // GET: HocBong
        public async Task<IActionResult> Index()
        {
            var quanLyHocBong_UNETI2_TI17A3HNContext = _context.HocBong.Include(h => h.DonViTaiTro);
            return View(await quanLyHocBong_UNETI2_TI17A3HNContext.ToListAsync());
        }

        // GET: HocBong/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hocBong = await _context.HocBong
                .Include(h => h.DonViTaiTro)
                .FirstOrDefaultAsync(m => m.MaHocBong == id);
            if (hocBong == null)
            {
                return NotFound();
            }

            ViewData["SoHoSo"] = await _context.HoSoHocBong
                .CountAsync(h => h.MaHocBong == hocBong.MaHocBong);

            return View(hocBong);
        }

        // GET: HocBong/Create
        public IActionResult Create()
        {
            ViewData["MaDonViTaiTro"] = new SelectList(_context.DonViTaiTro, "MaDonViTaiTro", "TenDonViTaiTro");
            return View();
        }

        // POST: HocBong/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaHocBong,TenHocBong,MaDonViTaiTro,SoSuat,BacDaoTao,DiemTrungBinhToiThieu,NgayBatDauNhanHoSo,HanNopHoSo,MoTaHocBong,DieuKienXetHocBong,TrangThai")] HocBong hocBong)
        {
            if (ModelState.IsValid)
            {
                _context.Add(hocBong);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaDonViTaiTro"] = new SelectList(_context.DonViTaiTro, "MaDonViTaiTro", "TenDonViTaiTro", hocBong.MaDonViTaiTro);
            return View(hocBong);
        }

        // GET: HocBong/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hocBong = await _context.HocBong.FindAsync(id);
            if (hocBong == null)
            {
                return NotFound();
            }
            ViewData["MaDonViTaiTro"] = new SelectList(_context.DonViTaiTro, "MaDonViTaiTro", "TenDonViTaiTro", hocBong.MaDonViTaiTro);
            return View(hocBong);
        }

        // POST: HocBong/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaHocBong,TenHocBong,MaDonViTaiTro,SoSuat,BacDaoTao,DiemTrungBinhToiThieu,NgayBatDauNhanHoSo,HanNopHoSo,MoTaHocBong,DieuKienXetHocBong,TrangThai")] HocBong hocBong)
        {
            if (id != hocBong.MaHocBong)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(hocBong);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!HocBongExists(hocBong.MaHocBong))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaDonViTaiTro"] = new SelectList(_context.DonViTaiTro, "MaDonViTaiTro", "TenDonViTaiTro", hocBong.MaDonViTaiTro);
            return View(hocBong);
        }

        // GET: HocBong/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var hocBong = await _context.HocBong
                .Include(h => h.DonViTaiTro)
                .FirstOrDefaultAsync(m => m.MaHocBong == id);
            if (hocBong == null)
            {
                return NotFound();
            }

            return View(hocBong);
        }

        // POST: HocBong/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var hocBong = await _context.HocBong.FindAsync(id);
            if (hocBong != null)
            {
                _context.HocBong.Remove(hocBong);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool HocBongExists(int id)
        {
            return _context.HocBong.Any(e => e.MaHocBong == id);
        }
    }
}
