// Họ và tên: Nguyễn Phi Hùng
// Mã sinh viên: 23103100148
// Nội dung thực hiện: Module 3 - xem và cập nhật hồ sơ cá nhân của sinh viên đang đăng nhập.
using System.Text.RegularExpressions;
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
        // Chữ cái (kể cả tiếng Việt, dấu rời), khoảng trắng, dấu chấm, nháy đơn, gạch nối.
        private static readonly Regex HoTenRegex = new(@"^[\p{L}][\p{L}\p{M} .'\-]*$", RegexOptions.Compiled);
        private const int TuoiToiThieu = 15;

        public HoSoCaNhanController(QuanLyHocBong_UNETI2_TI17A3HNContext context) : base(context) { }

        // GET: /HoSoCaNhan  -> hồ sơ cá nhân.
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

        // GET: /HoSoCaNhan/Edit[?returnUrl=...]
        public async Task<IActionResult> Edit(string? returnUrl = null)
        {
            var sv = await LaySinhVienHienTaiAsync();
            if (sv == null) return View("ChuaCoHoSo");

            var model = new HoSoCaNhanViewModel
            {
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
        public async Task<IActionResult> Edit(HoSoCaNhanViewModel model)
        {
            // Đối tượng được cập nhật luôn là sinh viên của tài khoản đang đăng nhập.
            // Form không có (và server không đọc) MaSinhVien/MaTaiKhoan/TrangThai -> không thể sửa hồ sơ người khác.
            var sv = await LaySinhVienHienTaiAsync(theoDoi: true);
            if (sv == null) return View("ChuaCoHoSo");

            if (!Url.IsLocalUrl(model.ReturnUrl)) model.ReturnUrl = null;   // chống open redirect

            // ---- Kiểm tra bổ sung ngoài Data Annotation ----
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

            // Cột DB là decimal(4,2): chỉ nhận tối đa 2 chữ số thập phân thay vì âm thầm làm tròn.
            if (model.DiemTrungBinhTichLuy.HasValue &&
                decimal.Round(model.DiemTrungBinhTichLuy.Value, 2) != model.DiemTrungBinhTichLuy.Value)
                ModelState.AddModelError(nameof(model.DiemTrungBinhTichLuy), "Điểm trung bình chỉ nhập tối đa 2 chữ số thập phân (ví dụ 3.25)");

            if (!ModelState.IsValid)
            {
                NapThongTinHoSo(sv);
                return View(model);
            }

            // ---- Ghi dữ liệu ----
            var soDienThoai = model.SoDienThoai.StartsWith("+84") ? "0" + model.SoDienThoai[3..] : model.SoDienThoai;

            sv.HoTen = model.HoTen;
            sv.NgaySinh = model.NgaySinh!.Value.Date;
            sv.GioiTinh = model.GioiTinh;
            sv.SoDienThoai = soDienThoai;                                        // luôn lưu dạng 0xxxxxxxxx
            sv.Email = model.Email;
            sv.DiaChi = model.DiaChi?.Trim() ?? string.Empty;                    // cột DB là NOT NULL
            sv.BacDaoTao = model.BacDaoTao;
            sv.Nganh = model.Nganh.Trim();
            sv.DiemTrungBinhTichLuy = model.DiemTrungBinhTichLuy!.Value;
            sv.ThanhTichHocTap = model.ThanhTichHocTap?.Trim() ?? string.Empty;  // cột DB là NOT NULL

            try
            {
                await _context.SaveChangesAsync();
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

        private void NapThongTinHoSo(SinhVien sv)
        {
            ViewBag.MaSinhVien = sv.MaSinhVien;
            ViewBag.TrangThaiHocTap = sv.TrangThai;
            ViewBag.HoTenHoSo = sv.HoTen;
        }
    }
}
