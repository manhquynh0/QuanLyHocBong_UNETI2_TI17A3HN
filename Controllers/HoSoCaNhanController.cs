// Họ và tên: Nguyễn Phi Hùng
// Mã sinh viên: 23103100148
// Nội dung thực hiện: Module 3 - xem và cập nhật hồ sơ cá nhân của sinh viên đang đăng nhập (bao gồm ảnh đại diện).
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.Services;
using QuanLyHocBong_UNETI2_TI17A3HN.ViewModels;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Controllers
{
    public class HoSoCaNhanController : SinhVienBaseController
    {
        private static readonly Regex HoTenRegex = new(@"^[\p{L}][\p{L}\p{M} .'\-]*$", RegexOptions.Compiled);
        private const int TuoiToiThieu = 15;
        private readonly ISupabaseStorageService _storageService;
        private readonly IWebHostEnvironment _env;

        public HoSoCaNhanController(
            QuanLyHocBong_UNETI2_TI17A3HNContext context,
            ISupabaseStorageService storageService,
            IWebHostEnvironment env) : base(context)
        {
            _storageService = storageService;
            _env = env;
        }

        // GET: /HoSoCaNhan
        public async Task<IActionResult> Index()
        {
            var sv = await LaySinhVienHienTaiAsync();
            if (sv == null) return View("ChuaCoHoSo");

            ViewBag.ThongTinThieu = NopHoSoRules.ThongTinCaNhanConThieu(sv);
            ViewBag.SoHoSo = await _context.HoSoHocBong.CountAsync(h => h.MaSinhVien == sv.MaSinhVien);
            NapThongTinHoSo(sv);
            return View(sv);
        }

        // GET: /HoSoCaNhan/Edit
        public async Task<IActionResult> Edit(string? returnUrl = null)
        {
            var sv = await LaySinhVienHienTaiAsync();
            if (sv == null) return View("ChuaCoHoSo");

            var model = new HoSoCaNhanViewModel
            {
                AnhDaiDien = sv.AnhDaiDien,
                HoTen = sv.HoTen,
                NgaySinh = sv.NgaySinh,
                GioiTinh = sv.GioiTinh,
                SoDienThoai = sv.SoDienThoai,
                Email = sv.Email,
                DiaChi = sv.DiaChi,
                BacDaoTao = sv.BacDaoTao,
                Nganh = sv.Nganh,
                DiemTrungBinhTichLuy = sv.DiemTrungBinhTichLuy,
                ThanhTichHocTap = sv.ThanhTichHocTap,
                ReturnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : null
            };
            NapThongTinHoSo(sv);
            return View(model);
        }

        // POST: /HoSoCaNhan/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(HoSoCaNhanViewModel model, IFormFile? AnhDaiDienUpload)
        {
            var sv = await LaySinhVienHienTaiAsync(theoDoi: true);
            if (sv == null) return View("ChuaCoHoSo");

            if (!Url.IsLocalUrl(model.ReturnUrl)) model.ReturnUrl = null;

            if (!string.IsNullOrEmpty(model.HoTen) && !HoTenRegex.IsMatch(model.HoTen))
                ModelState.AddModelError(nameof(model.HoTen), "Họ tên chỉ gồm chữ cái, khoảng trắng, dấu chấm, dấu nháy đơn hoặc gạch nối");

            if (model.NgaySinh.HasValue)
            {
                var ngaySinh = model.NgaySinh.Value.Date;
                if (ngaySinh > DateTime.Today)
                    ModelState.AddModelError(nameof(model.NgaySinh), "Ngày sinh không được ở tương lai");
                else if (ngaySinh < DateTime.Today.AddYears(-100))
                    ModelState.AddModelError(nameof(model.NgaySinh), "Ngày sinh không hợp lệ");
                else if (ngaySinh > DateTime.Today.AddYears(-TuoiToiThieu))
                    ModelState.AddModelError(nameof(model.NgaySinh), $"Sinh viên phải từ {TuoiToiThieu} tuổi trở lên");
            }

            if (model.DiemTrungBinhTichLuy.HasValue &&
                decimal.Round(model.DiemTrungBinhTichLuy.Value, 2) != model.DiemTrungBinhTichLuy.Value)
                ModelState.AddModelError(nameof(model.DiemTrungBinhTichLuy), "Điểm trung bình chỉ nhập tối đa 2 chữ số thập phân (ví dụ 3.25)");

            var fileUpload = model.FileAnhDaiDien ?? AnhDaiDienUpload;
            if (fileUpload != null && fileUpload.Length > 0)
            {
                var loiAnh = TaiLieuHoSoStorage.KiemTraAnhDaiDien(fileUpload);
                if (loiAnh != null)
                {
                    ModelState.AddModelError(nameof(model.FileAnhDaiDien), loiAnh);
                }
            }

            if (!ModelState.IsValid)
            {
                NapThongTinHoSo(sv);
                return View(model);
            }

            if (fileUpload != null && fileUpload.Length > 0)
            {
                var avatarUrl = await _storageService.UploadFileAsync(fileUpload, "avatars");
                if (string.IsNullOrEmpty(avatarUrl))
                {
                    var ext = Path.GetExtension(fileUpload.FileName).ToLowerInvariant();
                    var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "avatars");
                    Directory.CreateDirectory(uploadFolder);
                    var fileName = $"{Guid.NewGuid():N}{ext}";
                    var filePath = Path.Combine(uploadFolder, fileName);
                    await using var stream = new FileStream(filePath, FileMode.Create);
                    await fileUpload.CopyToAsync(stream);
                    avatarUrl = $"/uploads/avatars/{fileName}";
                }

                sv.AnhDaiDien = avatarUrl;
            }

            var soDienThoai = model.SoDienThoai.StartsWith("+84") ? "0" + model.SoDienThoai[3..] : model.SoDienThoai;

            sv.HoTen = model.HoTen;
            sv.NgaySinh = model.NgaySinh!.Value.Date;
            sv.GioiTinh = model.GioiTinh;
            sv.SoDienThoai = soDienThoai;
            sv.Email = model.Email;
            sv.DiaChi = model.DiaChi?.Trim() ?? string.Empty;
            sv.BacDaoTao = model.BacDaoTao;
            sv.Nganh = model.Nganh.Trim();
            sv.DiemTrungBinhTichLuy = model.DiemTrungBinhTichLuy!.Value;
            sv.ThanhTichHocTap = model.ThanhTichHocTap?.Trim() ?? string.Empty;

            var tk = await _context.TaiKhoan.FindAsync(sv.MaTaiKhoan);
            if (tk != null)
            {
                if (!string.IsNullOrEmpty(sv.AnhDaiDien))
                    tk.AnhDaiDien = sv.AnhDaiDien;
                tk.HoTen = sv.HoTen;
                tk.Email = sv.Email;
            }

            try
            {
                await _context.SaveChangesAsync();
                if (!string.IsNullOrEmpty(sv.AnhDaiDien))
                {
                    HttpContext.Session.SetString("AnhDaiDien", sv.AnhDaiDien);
                }
                HttpContext.Session.SetString("HoTen", sv.HoTen);
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Không thể lưu thay đổi. Vui lòng kiểm tra lại dữ liệu và thử lại.");
                NapThongTinHoSo(sv);
                return View(model);
            }

            TempData["ThongBao"] = "Đã cập nhật hồ sơ cá nhân.";
            if (model.ReturnUrl != null) return LocalRedirect(model.ReturnUrl);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatAnhDaiDien(IFormFile? AnhDaiDienUpload)
        {
            var sv = await LaySinhVienHienTaiAsync(theoDoi: true);
            if (sv == null) return View("ChuaCoHoSo");

            if (AnhDaiDienUpload != null && AnhDaiDienUpload.Length > 0)
            {
                var loiAnh = TaiLieuHoSoStorage.KiemTraAnhDaiDien(AnhDaiDienUpload);
                if (loiAnh != null)
                {
                    TempData["Loi"] = loiAnh;
                    return RedirectToAction(nameof(Index));
                }

                var avatarUrl = await _storageService.UploadFileAsync(AnhDaiDienUpload, "avatars");
                if (string.IsNullOrEmpty(avatarUrl))
                {
                    var ext = Path.GetExtension(AnhDaiDienUpload.FileName).ToLowerInvariant();
                    var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "avatars");
                    Directory.CreateDirectory(uploadFolder);
                    var fileName = $"{Guid.NewGuid():N}{ext}";
                    var filePath = Path.Combine(uploadFolder, fileName);
                    await using var stream = new FileStream(filePath, FileMode.Create);
                    await AnhDaiDienUpload.CopyToAsync(stream);
                    avatarUrl = $"/uploads/avatars/{fileName}";
                }

                sv.AnhDaiDien = avatarUrl;
                var tk = await _context.TaiKhoan.FindAsync(sv.MaTaiKhoan);
                if (tk != null) tk.AnhDaiDien = avatarUrl;
                await _context.SaveChangesAsync();
                HttpContext.Session.SetString("AnhDaiDien", avatarUrl);
                TempData["ThongBao"] = "Đã cập nhật ảnh đại diện thành công.";
            }

            return RedirectToAction(nameof(Index));
        }

        private void NapThongTinHoSo(SinhVien sv)
        {
            ViewBag.MaSinhVien = sv.MaSinhVien;
            ViewBag.TrangThaiHocTap = sv.TrangThai;
            ViewBag.HoTenHoSo = sv.HoTen;
            ViewBag.AnhDaiDien = !string.IsNullOrEmpty(sv.AnhDaiDien)
                ? sv.AnhDaiDien
                : HttpContext.Session.GetString("AnhDaiDien");
        }
    }
}
