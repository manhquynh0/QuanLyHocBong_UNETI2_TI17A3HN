// Họ và tên: Nguyễn Phi Hùng
// Mã sinh viên: 23103100148
// Nội dung thực hiện: Module 3 - xem và cập nhật hồ sơ cá nhân của sinh viên đang đăng nhập.
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Services;
using QuanLyHocBong_UNETI2_TI17A3HN.ViewModels;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Controllers
{
    public class HoSoCaNhanController : SinhVienBaseController
    {
        public HoSoCaNhanController(QuanLyHocBong_UNETI2_TI17A3HNContext context) : base(context) { }

        // GET: /SinhVien  -> hồ sơ cá nhân (chỉ của chính mình, lấy theo Session, không nhận mã từ URL)
        public async Task<IActionResult> Index()
        {
            var sv = await LaySinhVienHienTaiAsync();
            if (sv == null) return View("ChuaCoHoSo");

            ViewBag.ThongTinThieu = NopHoSoRules.ThongTinCaNhanConThieu(sv);
            ViewBag.SoHoSo = await _context.HoSoHocBong.CountAsync(h => h.MaSinhVien == sv.MaSinhVien);
            return View(sv);
        }

        // GET: /SinhVien/Edit
        public async Task<IActionResult> Edit()
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
                ThanhTichHocTap = sv.ThanhTichHocTap
            };
            return View(model);
        }

        // POST: /SinhVien/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(HoSoCaNhanViewModel model)
        {
            // Luôn lấy sinh viên từ Session -> không thể sửa hồ sơ của người khác.
            var sv = await LaySinhVienHienTaiAsync(theoDoi: true);
            if (sv == null) return View("ChuaCoHoSo");

            if (model.NgaySinh.HasValue)
            {
                if (model.NgaySinh.Value.Date > DateTime.Today)
                    ModelState.AddModelError(nameof(model.NgaySinh), "Ngày sinh không được ở tương lai");
                else if (model.NgaySinh.Value.Date < DateTime.Today.AddYears(-100))
                    ModelState.AddModelError(nameof(model.NgaySinh), "Ngày sinh không hợp lệ");
            }

            if (!ModelState.IsValid) return View(model);

            sv.HoTen = model.HoTen.Trim();
            sv.NgaySinh = model.NgaySinh!.Value.Date;
            sv.GioiTinh = model.GioiTinh;
            sv.SoDienThoai = model.SoDienThoai.Trim();
            sv.Email = model.Email.Trim();
            sv.DiaChi = model.DiaChi?.Trim() ?? string.Empty;           // cột DB là NOT NULL
            sv.BacDaoTao = model.BacDaoTao;
            sv.Nganh = model.Nganh.Trim();
            sv.DiemTrungBinhTichLuy = model.DiemTrungBinhTichLuy!.Value;
            sv.ThanhTichHocTap = model.ThanhTichHocTap?.Trim() ?? string.Empty; // cột DB là NOT NULL
            // MaSinhVien, MaTaiKhoan, TrangThai không bao giờ lấy từ form.

            await _context.SaveChangesAsync();

            TempData["ThongBao"] = "Đã cập nhật hồ sơ cá nhân.";
            return RedirectToAction(nameof(Index));
        }
    }
}
