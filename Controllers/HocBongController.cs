using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.ViewModels.HocBong;

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
            var model = new HocBongIndexViewModel
            {
                HocBongs = await _context.HocBong
                .Include(h => h.DonViTaiTro)
                    .OrderBy(h => h.MaHocBong)
                    .ToListAsync()
            };

            model.SoKetQua = model.HocBongs.Count;
            await PopulateFilterOptionsAsync(model);
            return View(model);
        }

        // GET: HocBong/Search
        public async Task<IActionResult> Search([FromQuery] HocBongIndexViewModel filters)
        {
            var query = _context.HocBong
                .Include(h => h.DonViTaiTro)
                .AsQueryable();

            var tuKhoa = filters.TuKhoa?.Trim();
            if (!string.IsNullOrEmpty(tuKhoa))
            {
                query = query.Where(h =>
                    h.TenHocBong.Contains(tuKhoa)
                    || h.DonViTaiTro.TenDonViTaiTro.Contains(tuKhoa));
            }

            if (filters.MaDonViTaiTro.HasValue)
            {
                query = query.Where(h => h.MaDonViTaiTro == filters.MaDonViTaiTro.Value);
            }

            if (!string.IsNullOrWhiteSpace(filters.BacDaoTao))
            {
                query = query.Where(h => h.BacDaoTao == filters.BacDaoTao);
            }

            if (!string.IsNullOrWhiteSpace(filters.TrangThai))
            {
                query = query.Where(h => h.TrangThai == filters.TrangThai);
            }

            if (filters.DiemToiThieu.HasValue && ModelState.IsValid)
            {
                query = query.Where(h => h.DiemTrungBinhToiThieu >= filters.DiemToiThieu.Value);
            }

            var today = DateTime.Today;
            if (filters.HanNop == "con-han")
            {
                query = query.Where(h => h.HanNopHoSo >= today);
            }
            else if (filters.HanNop == "het-han")
            {
                query = query.Where(h => h.HanNopHoSo < today);
            }

            filters.SapXep = filters.SapXep switch
            {
                "ten-az" or "ten-za" or "han-tang" or "han-giam" or "suat-tang" or "suat-giam" => filters.SapXep,
                _ => "ma-tang"
            };

            query = filters.SapXep switch
            {
                "ten-az" => query.OrderBy(h => h.TenHocBong).ThenBy(h => h.MaHocBong),
                "ten-za" => query.OrderByDescending(h => h.TenHocBong).ThenBy(h => h.MaHocBong),
                "han-tang" => query.OrderBy(h => h.HanNopHoSo).ThenBy(h => h.MaHocBong),
                "han-giam" => query.OrderByDescending(h => h.HanNopHoSo).ThenBy(h => h.MaHocBong),
                "suat-tang" => query.OrderBy(h => h.SoSuat).ThenBy(h => h.MaHocBong),
                "suat-giam" => query.OrderByDescending(h => h.SoSuat).ThenBy(h => h.MaHocBong),
                _ => query.OrderBy(h => h.MaHocBong)
            };

            filters.HocBongs = await query.ToListAsync();
            filters.SoKetQua = filters.HocBongs.Count;
            await PopulateFilterOptionsAsync(filters);

            return View("Index", filters);
        }

        private async Task PopulateFilterOptionsAsync(HocBongIndexViewModel model)
        {
            model.DonViTaiTroOptions = await _context.DonViTaiTro
                .OrderBy(d => d.TenDonViTaiTro)
                .Select(d => new SelectListItem
                {
                    Value = d.MaDonViTaiTro.ToString(),
                    Text = d.TenDonViTaiTro
                })
                .ToListAsync();
            model.BacDaoTaoOptions = await _context.HocBong
                .Select(h => h.BacDaoTao)
                .Distinct()
                .OrderBy(value => value)
                .Select(value => new SelectListItem { Value = value, Text = value })
                .ToListAsync();
            model.TrangThaiOptions = await _context.HocBong
                .Select(h => h.TrangThai)
                .Distinct()
                .OrderBy(value => value)
                .Select(value => new SelectListItem { Value = value, Text = value })
                .ToListAsync();
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
