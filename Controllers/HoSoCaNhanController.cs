// Họ và tên: Nguyễn Phi Hùng
// Mã sinh viên: 23103100148
// Nội dung thực hiện: Module 3 - xem và cập nhật hồ sơ cá nhân của sinh viên đang đăng nhập (bao gồm ảnh đại diện).
using System.Text.RegularExpressions;
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

        public HoSoCaNhanController(
            QuanLyHocBong_UNETI2_TI17A3HNContext context,
            ISupabaseStorageService storageService) : base(context)
        {
            _storageService = storageService;
        }

        // GET: /HoSoCaNhan
        private readonly IWebHostEnvironment _env;

        public HoSoCaNhanController(QuanLyHocBong_UNETI2_TI17A3HNContext context, IWebHostEnvironment env) : base(context)
        {
            _env = env;
        }

        // GET: /HoSoCaNhan -> hồ sơ cá nhân.
        // Không nhận mã sinh viên từ URL: luôn lấy theo tài khoản trong Session, nên đổi tham số trên URL vô tác dụng.
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
            // Đối tượng được cập nhật luôn là sinh viên của tài khoản đang đăng nhập.
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

            // Kiểm tra ảnh đại diện nếu có chọn tệp
            if (AnhDaiDienUpload != null && AnhDaiDienUpload.Length > 0)
            {
                var loiAnh = TaiLieuHoSoStorage.KiemTraAnhDaiDien(AnhDaiDienUpload);
                if (loiAnh != null)
                {
                    ModelState.AddModelError(string.Empty, loiAnh);
            // ---- Kiểm tra & Lưu file ảnh đại diện nếu có tải lên ----
            string? newAvatarPath = null;
            if (model.FileAnhDaiDien != null && model.FileAnhDaiDien.Length > 0)
            {
                var duoiChoPhep = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = Path.GetExtension(model.FileAnhDaiDien.FileName).ToLowerInvariant();
                if (!duoiChoPhep.Contains(ext))
                {
                    ModelState.AddModelError(nameof(model.FileAnhDaiDien), "Ảnh đại diện chỉ chấp nhận định dạng JPG, JPEG, PNG hoặc WEBP");
                }
                else if (model.FileAnhDaiDien.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError(nameof(model.FileAnhDaiDien), "Dung lượng ảnh đại diện tối đa là 5MB");
                }
                else
                {
                    try
                    {
                        var uploadFolder = Path.Combine(_env.WebRootPath, "uploads", "avatars");
                        if (!Directory.Exists(uploadFolder))
                            Directory.CreateDirectory(uploadFolder);

                        var fileName = $"{Guid.NewGuid():N}{ext}";
                        var filePath = Path.Combine(uploadFolder, fileName);
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await model.FileAnhDaiDien.CopyToAsync(stream);
                        }
                        newAvatarPath = $"/uploads/avatars/{fileName}";
                    }
                    catch
                    {
                        ModelState.AddModelError(nameof(model.FileAnhDaiDien), "Không thể lưu tệp ảnh đại diện. Vui lòng thử lại.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                NapThongTinHoSo(sv);
                return View(model);
            }

            if (AnhDaiDienUpload != null && AnhDaiDienUpload.Length > 0)
            {
                var avatarUrl = await _storageService.UploadFileAsync(AnhDaiDienUpload, "avatars");
                if (!string.IsNullOrEmpty(avatarUrl))
                {
                    sv.AnhDaiDien = avatarUrl;
                    var tk = await _context.TaiKhoan.FindAsync(sv.MaTaiKhoan);
                    if (tk != null)
                    {
                        tk.AnhDaiDien = avatarUrl;
                    }
                    HttpContext.Session.SetString("AnhDaiDien", avatarUrl);
                }
            }

            var soDienThoai = model.SoDienThoai.StartsWith("+84") ? "0" + model.SoDienThoai[3..] : model.SoDienThoai;

            if (!string.IsNullOrEmpty(newAvatarPath))
            {
                sv.AnhDaiDien = newAvatarPath;
            }

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

            var taiKhoan = await _context.TaiKhoan.FindAsync(sv.MaTaiKhoan);
            if (taiKhoan != null)
            {
                taiKhoan.HoTen = sv.HoTen;
                taiKhoan.Email = sv.Email;
            }

            try
            {
                // Đồng bộ sang bảng TaiKhoan
                var tk = await _context.TaiKhoan.FindAsync(sv.MaTaiKhoan);
                if (tk != null)
                {
                    if (!string.IsNullOrEmpty(sv.AnhDaiDien))
                        tk.AnhDaiDien = sv.AnhDaiDien;
                    tk.HoTen = sv.HoTen;
                    tk.Email = sv.Email;
                }

                await _context.SaveChangesAsync();

                // Cập nhật Session để hiển thị ngay trên thanh Topbar
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

            TempData["ThongBao"] = "Đã cập nhật hồ sơ cá nhân và ảnh đại diện.";
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
                if (!string.IsNullOrEmpty(avatarUrl))
                {
                    sv.AnhDaiDien = avatarUrl;
                    var tk = await _context.TaiKhoan.FindAsync(sv.MaTaiKhoan);
                    if (tk != null) tk.AnhDaiDien = avatarUrl;
                    await _context.SaveChangesAsync();
                    HttpContext.Session.SetString("AnhDaiDien", avatarUrl);
                    TempData["ThongBao"] = "Đã cập nhật ảnh đại diện thành công.";
                }
                else
                {
                    TempData["Loi"] = "Không thể tải lên ảnh đại diện lúc này. Vui lòng thử lại sau.";
                }
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
            ViewBag.AnhDaiDien = sv.AnhDaiDien;
        }
    }
}
