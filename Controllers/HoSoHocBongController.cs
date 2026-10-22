// Họ và tên: Phan Sỹ Tú
// Mã sinh viên: 23103100145
// Nội dung thực hiện: Module 4 - Controller xử lý tiếp nhận hồ sơ,
// xét duyệt sơ bộ, quản lý trạng thái và kiểm soát số suất.

using Microsoft.AspNetCore.Mvc;
using QuanLyHocBong_UNETI2_TI17A3HN.Constants;
using QuanLyHocBong_UNETI2_TI17A3HN.Services.Interface;
using QuanLyHocBong_UNETI2_TI17A3HN.ViewModels;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Controllers
{
    public class HoSoHocBongController : Controller
    {
        private readonly IHoSoHocBongService _hoSoService;

        public HoSoHocBongController(IHoSoHocBongService hoSoService)
        {
            _hoSoService = hoSoService;
        }

        // 1. DANH SÁCH HỒ SƠ + TÌM KIẾM + LỌC + SẮP XẾP + PHÂN TRANG
        // GET: /HoSoHocBong
        public async Task<IActionResult> Index(HoSoFilterViewModel filter)
        {
            var model = await _hoSoService.LayDanhSachAsync(filter);

            // Load dropdown Đơn vị tài trợ 
            var dsDonVi = await _hoSoService.LayDanhSachDonViTaiTroAsync();
            ViewBag.DonViTaiTro = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                dsDonVi, "MaDonViTaiTro", "TenDonViTaiTro", filter.MaDonViTaiTro);

            return View(model);
        }

        // 2. CHI TIẾT HỒ SƠ
        // GET: /HoSoHocBong/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var model = await _hoSoService.LayChiTietAsync(id);

            if (model == null)
            {
                TempData["Error"] = "Không tìm thấy hồ sơ.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // 3. FORM XÉT DUYỆT SƠ BỘ (GET)
        // GET: /HoSoHocBong/XetDuyetSoBo/5
        public async Task<IActionResult> XetDuyetSoBo(int id)
        {
            var model = await _hoSoService.LayFormXetDuyetAsync(id);

            if (model == null)
            {
                TempData["Error"] = "Không tìm thấy hồ sơ.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // 4. XỬ LÝ XÉT DUYỆT SƠ BỘ (POST)
        // POST: /HoSoHocBong/XetDuyetSoBo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XetDuyetSoBo(XetDuyetSoBoViewModel model)
        {
            // Kiểm tra validation từ Data Annotation
            if (!ModelState.IsValid)
            {
                await LoadThongTinHienThiAsync(model);
                return View(model);
            }

            // Gọi service xử lý nghiệp vụ
            var (thanhCong, loi) = await _hoSoService.XetDuyetSoBoAsync(model);

            if (!thanhCong)
            {
                ModelState.AddModelError("", loi ?? "Có lỗi xảy ra khi xử lý.");
                await LoadThongTinHienThiAsync(model);
                return View(model);
            }

            TempData["Success"] = "Xét duyệt sơ bộ thành công.";
            return RedirectToAction(nameof(Index));
        }

        // 5. KIỂM TRA SỐ SUẤT ĐÃ CẤP (JSON - dùng cho AJAX)
        // GET: /HoSoHocBong/KiemTraSoSuat/5
        [HttpGet]
        public async Task<IActionResult> KiemTraSoSuat(int maHocBong)
        {
            var soDaCap = await _hoSoService.DemSoDaCapAsync(maHocBong);
            return Json(new { MaHocBong = maHocBong, SoDaCap = soDaCap });
        }

        // Helper: Load lại thông tin readonly khi form bị lỗi
        // (Vì khi submit form, các field readonly không được gửi lên server)
        private async Task LoadThongTinHienThiAsync(XetDuyetSoBoViewModel model)
        {
            var formData = await _hoSoService.LayFormXetDuyetAsync(model.MaHoSoHocBong);
            if (formData == null) return;

            model.TenSinhVien = formData.TenSinhVien;
            model.TenHocBong = formData.TenHocBong;
            model.TenDonViTaiTro = formData.TenDonViTaiTro;
            model.DiemTrungBinh = formData.DiemTrungBinh;
            model.DiemYeuCau = formData.DiemYeuCau;
            model.BacDaoTaoSinhVien = formData.BacDaoTaoSinhVien;
            model.BacDaoTaoHocBong = formData.BacDaoTaoHocBong;
            model.NgayNop = formData.NgayNop;
            model.ThuXinHocBong = formData.ThuXinHocBong;
            model.ThanhTichHocTap = formData.ThanhTichHocTap;
        }
    }
}