using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Attributes;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.Models.ViewModels;
using QuanLyHocBong_UNETI2_TI17A3HN.Services;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Controllers
{
    public class SinhVienController : Controller
    {
        private readonly QuanLyHocBong_UNETI2_TI17A3HNContext _context;
        private readonly ISupabaseStorageService _storageService;

        public SinhVienController(QuanLyHocBong_UNETI2_TI17A3HNContext context, ISupabaseStorageService storageService)
        {
            _context = context;
            _storageService = storageService;
        }

        [RoleAuthorize("Admin", "Cán bộ học bổng")]
        public async Task<IActionResult> Index(string? searchString, string? nganhFilter, string? bacDaoTaoFilter, int page = 1)
        {
            int pageSize = 10;
            var query = _context.SinhVien.Include(s => s.TaiKhoan).AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var kw = searchString.Trim();
                query = query.Where(s => s.HoTen.Contains(kw) ||
                                         s.Email.Contains(kw) ||
                                         s.SoDienThoai.Contains(kw) ||
                                         (s.TaiKhoan != null && s.TaiKhoan.TenDangNhap.Contains(kw)));
            }

            if (!string.IsNullOrWhiteSpace(nganhFilter))
            {
                query = query.Where(s => s.Nganh == nganhFilter);
            }

            if (!string.IsNullOrWhiteSpace(bacDaoTaoFilter))
            {
                query = query.Where(s => s.BacDaoTao == bacDaoTaoFilter);
            }

            int totalItems = await query.CountAsync();
            var items = await query
                .OrderByDescending(s => s.MaSinhVien)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentNganh = nganhFilter;
            ViewBag.CurrentBac = bacDaoTaoFilter;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling((double)totalItems / pageSize));
            ViewBag.TotalItems = totalItems;

            return View(items);
        }

        [RoleAuthorize("Admin", "Cán bộ học bổng")]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new SinhVienCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "Cán bộ học bổng")]
        public async Task<IActionResult> Create(SinhVienCreateViewModel model, IFormFile? AnhDaiDienUpload)
        {
            var username = model.TenDangNhap?.Trim() ?? "";
            var email = model.Email?.Trim() ?? "";

            if (await _context.TaiKhoan.AnyAsync(t => t.TenDangNhap == username))
            {
                ModelState.AddModelError("TenDangNhap", "Tên đăng nhập này đã tồn tại trong hệ thống.");
            }

            if (await _context.SinhVien.AnyAsync(s => s.Email == email))
            {
                ModelState.AddModelError("Email", "Email này đã được sử dụng bởi một sinh viên khác.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string? avatarUrl = null;
            if (AnhDaiDienUpload != null && AnhDaiDienUpload.Length > 0)
            {
                avatarUrl = await _storageService.UploadFileAsync(AnhDaiDienUpload, "avatars");
            }

            var taiKhoan = new TaiKhoan
            {
                TenDangNhap = username,
                HoTen = model.HoTen.Trim(),
                Email = email,
                VaiTro = "Sinh viên",
                TrangThai = model.KichHoatTaiKhoan,
                AnhDaiDien = avatarUrl
            };
            taiKhoan.MatKhau = new PasswordHasher<TaiKhoan>().HashPassword(taiKhoan, model.MatKhau);

            _context.TaiKhoan.Add(taiKhoan);
            await _context.SaveChangesAsync();

            var sinhVien = new SinhVien
            {
                MaTaiKhoan = taiKhoan.MaTaiKhoan,
                HoTen = model.HoTen.Trim(),
                NgaySinh = model.NgaySinh,
                GioiTinh = model.GioiTinh,
                SoDienThoai = model.SoDienThoai.Trim(),
                Email = email,
                DiaChi = model.DiaChi?.Trim() ?? "Hà Nội",
                BacDaoTao = model.BacDaoTao,
                Nganh = model.Nganh,
                DiemTrungBinhTichLuy = model.DiemTrungBinhTichLuy,
                ThanhTichHocTap = model.ThanhTichHocTap?.Trim() ?? "Chưa cập nhật",
                TrangThai = model.TrangThai,
                AnhDaiDien = avatarUrl
            };

            _context.SinhVien.Add(sinhVien);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã tạo hồ sơ sinh viên '{sinhVien.HoTen}' (Tài khoản: {taiKhoan.TenDangNhap}) thành công!";
            return RedirectToAction(nameof(Index));
        }

        [RoleAuthorize("Admin", "Cán bộ học bổng")]
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var sinhVien = await _context.SinhVien
                .Include(s => s.TaiKhoan)
                .FirstOrDefaultAsync(s => s.MaSinhVien == id);

            if (sinhVien == null) return NotFound();

            return View(sinhVien);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "Cán bộ học bổng")]
        public async Task<IActionResult> Edit(int id, SinhVien model, bool? KichHoatTaiKhoan, string? MatKhauMoi, IFormFile? AnhDaiDienUpload)
        {
            if (id != model.MaSinhVien) return NotFound();

            var sinhVien = await _context.SinhVien
                .Include(s => s.TaiKhoan)
                .FirstOrDefaultAsync(s => s.MaSinhVien == id);

            if (sinhVien == null) return NotFound();

            ModelState.Remove("TaiKhoan");
            ModelState.Remove("HoSoHocBongs");

            if (ModelState.IsValid)
            {
                if (AnhDaiDienUpload != null && AnhDaiDienUpload.Length > 0)
                {
                    var avatarUrl = await _storageService.UploadFileAsync(AnhDaiDienUpload, "avatars");
                    if (!string.IsNullOrEmpty(avatarUrl))
                    {
                        sinhVien.AnhDaiDien = avatarUrl;
                        if (sinhVien.TaiKhoan != null)
                        {
                            sinhVien.TaiKhoan.AnhDaiDien = avatarUrl;
                        }
                    }
                }

                sinhVien.HoTen = model.HoTen.Trim();
                sinhVien.NgaySinh = model.NgaySinh;
                sinhVien.GioiTinh = model.GioiTinh;
                sinhVien.SoDienThoai = model.SoDienThoai.Trim();
                sinhVien.Email = model.Email.Trim();
                sinhVien.DiaChi = model.DiaChi?.Trim() ?? "Hà Nội";
                sinhVien.BacDaoTao = model.BacDaoTao;
                sinhVien.Nganh = model.Nganh;
                sinhVien.DiemTrungBinhTichLuy = model.DiemTrungBinhTichLuy;
                sinhVien.ThanhTichHocTap = model.ThanhTichHocTap?.Trim() ?? "";
                sinhVien.TrangThai = model.TrangThai;

                if (sinhVien.TaiKhoan != null)
                {
                    sinhVien.TaiKhoan.HoTen = sinhVien.HoTen;
                    sinhVien.TaiKhoan.Email = sinhVien.Email;
                    if (KichHoatTaiKhoan.HasValue)
                    {
                        sinhVien.TaiKhoan.TrangThai = KichHoatTaiKhoan.Value;
                    }
                    if (!string.IsNullOrWhiteSpace(MatKhauMoi))
                    {
                        sinhVien.TaiKhoan.MatKhau = new PasswordHasher<TaiKhoan>().HashPassword(sinhVien.TaiKhoan, MatKhauMoi.Trim());
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Cập nhật hồ sơ sinh viên #{sinhVien.MaSinhVien} ({sinhVien.HoTen}) thành công!";
                return RedirectToAction(nameof(Index));
            }

            model.TaiKhoan = sinhVien.TaiKhoan;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "Cán bộ học bổng")]
        public async Task<IActionResult> Delete(int id)
        {
            var sinhVien = await _context.SinhVien
                .Include(s => s.TaiKhoan)
                .Include(s => s.HoSoHocBongs)
                .FirstOrDefaultAsync(s => s.MaSinhVien == id);

            if (sinhVien == null) return NotFound();

            if (sinhVien.HoSoHocBongs != null && sinhVien.HoSoHocBongs.Any())
            {
                TempData["ErrorMessage"] = $"Không thể xóa sinh viên '{sinhVien.HoTen}' vì đã có hồ sơ xét duyệt học bổng.";
                return RedirectToAction(nameof(Index));
            }

            var taiKhoan = sinhVien.TaiKhoan;
            _context.SinhVien.Remove(sinhVien);
            if (taiKhoan != null)
            {
                _context.TaiKhoan.Remove(taiKhoan);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã xóa sinh viên '{sinhVien.HoTen}' thành công.";
            return RedirectToAction(nameof(Index));
        }

        [RoleAuthorize]
        public async Task<IActionResult> ThongTinCaNhan()
        {
            var maTaiKhoan = HttpContext.Session.GetInt32("MaTaiKhoan");
            if (!maTaiKhoan.HasValue) return RedirectToAction("DangNhap", "TaiKhoan");

            var sinhVien = await _context.SinhVien
                .Include(s => s.TaiKhoan)
                .FirstOrDefaultAsync(s => s.MaTaiKhoan == maTaiKhoan.Value);

            if (sinhVien == null)
            {
                var taiKhoan = await _context.TaiKhoan.FindAsync(maTaiKhoan.Value);
                sinhVien = new SinhVien
                {
                    MaTaiKhoan = maTaiKhoan.Value,
                    HoTen = taiKhoan?.HoTen ?? "Sinh viên",
                    Email = taiKhoan?.Email ?? "",
                    NgaySinh = new DateTime(2004, 1, 1),
                    GioiTinh = "Nam",
                    SoDienThoai = "0980000000",
                    DiaChi = "Hà Nội",
                    BacDaoTao = "Đại học",
                    Nganh = "Công nghệ thông tin",
                    DiemTrungBinhTichLuy = 3.2m,
                    ThanhTichHocTap = "Chưa cập nhật",
                    TrangThai = "Đang học"
                };
                _context.SinhVien.Add(sinhVien);
                await _context.SaveChangesAsync();
            }

            return View(sinhVien);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize]
        public async Task<IActionResult> CapNhatHoSo(SinhVien model, IFormFile? AnhDaiDienUpload)
        {
            var maTaiKhoan = HttpContext.Session.GetInt32("MaTaiKhoan");
            if (!maTaiKhoan.HasValue) return RedirectToAction("DangNhap", "TaiKhoan");

            var sinhVien = await _context.SinhVien.Include(s => s.TaiKhoan).FirstOrDefaultAsync(s => s.MaTaiKhoan == maTaiKhoan.Value);
            if (sinhVien == null || sinhVien.MaSinhVien != model.MaSinhVien)
            {
                return Forbid();
            }

            ModelState.Remove("TaiKhoan");
            ModelState.Remove("HoSoHocBongs");

            if (ModelState.IsValid)
            {
                if (AnhDaiDienUpload != null && AnhDaiDienUpload.Length > 0)
                {
                    var avatarUrl = await _storageService.UploadFileAsync(AnhDaiDienUpload, "avatars");
                    if (!string.IsNullOrEmpty(avatarUrl))
                    {
                        sinhVien.AnhDaiDien = avatarUrl;
                        if (sinhVien.TaiKhoan != null)
                        {
                            sinhVien.TaiKhoan.AnhDaiDien = avatarUrl;
                        }
                        HttpContext.Session.SetString("AnhDaiDien", avatarUrl);
                    }
                }

                sinhVien.HoTen = model.HoTen;
                sinhVien.NgaySinh = model.NgaySinh;
                sinhVien.GioiTinh = model.GioiTinh;
                sinhVien.SoDienThoai = model.SoDienThoai;
                sinhVien.DiaChi = model.DiaChi?.Trim() ?? "";
                sinhVien.BacDaoTao = model.BacDaoTao;
                sinhVien.Nganh = model.Nganh;
                sinhVien.DiemTrungBinhTichLuy = model.DiemTrungBinhTichLuy;
                sinhVien.ThanhTichHocTap = model.ThanhTichHocTap?.Trim() ?? "";

                await _context.SaveChangesAsync();
                HttpContext.Session.SetString("HoTen", sinhVien.HoTen);
                TempData["SuccessMessage"] = "Cập nhật hồ sơ cá nhân thành công!";
                return RedirectToAction(nameof(ThongTinCaNhan));
            }

            return View("ThongTinCaNhan", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize]
        public async Task<IActionResult> CapNhatAnhDaiDien(IFormFile? AnhDaiDienUpload)
        {
            var maTaiKhoan = HttpContext.Session.GetInt32("MaTaiKhoan");
            if (!maTaiKhoan.HasValue) return RedirectToAction("DangNhap", "TaiKhoan");

            if (AnhDaiDienUpload != null && AnhDaiDienUpload.Length > 0)
            {
                var avatarUrl = await _storageService.UploadFileAsync(AnhDaiDienUpload, "avatars");
                if (!string.IsNullOrEmpty(avatarUrl))
                {
                    var sv = await _context.SinhVien.FirstOrDefaultAsync(s => s.MaTaiKhoan == maTaiKhoan.Value);
                    if (sv != null) sv.AnhDaiDien = avatarUrl;
                    var tk = await _context.TaiKhoan.FindAsync(maTaiKhoan.Value);
                    if (tk != null) tk.AnhDaiDien = avatarUrl;
                    await _context.SaveChangesAsync();
                    HttpContext.Session.SetString("AnhDaiDien", avatarUrl);
                    TempData["SuccessMessage"] = "Cập nhật ảnh đại diện thành công!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể tải lên ảnh đại diện. Vui lòng thử lại.";
                }
            }
            return RedirectToAction(nameof(ThongTinCaNhan));
        }

        [RoleAuthorize("Admin", "Cán bộ học bổng")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var sinhVien = await _context.SinhVien
                .Include(s => s.TaiKhoan)
                .Include(s => s.HoSoHocBongs)
                    .ThenInclude(hs => hs.HocBong)
                .FirstOrDefaultAsync(m => m.MaSinhVien == id);

            if (sinhVien == null) return NotFound();

            return View(sinhVien);
        }
    }
}
