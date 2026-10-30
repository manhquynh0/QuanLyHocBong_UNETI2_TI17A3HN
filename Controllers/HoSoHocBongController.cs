// Họ và tên: Phan Sỹ Tú
// Mã sinh viên: 23103100145
// Nội dung thực hiện: Module 4 - Controller xử lý tiếp nhận hồ sơ,
// xét duyệt sơ bộ, quản lý trạng thái và kiểm soát số suất.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using QuanLyHocBong_UNETI2_TI17A3HN.Attributes;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.Services;
using QuanLyHocBong_UNETI2_TI17A3HN.Services.Interface;
using QuanLyHocBong_UNETI2_TI17A3HN.ViewModels;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Controllers
{
    [RoleAuthorize("Admin", "Cán bộ học bổng")]
    public class HoSoHocBongController : Controller
    {
        private readonly IHoSoHocBongService _hoSoService;
        private readonly ITaiKhoanService _taiKhoanService;
        private readonly ISupabaseStorageService _storageService;

        public HoSoHocBongController(
            IHoSoHocBongService hoSoService,
            ISupabaseStorageService storageService,
            ITaiKhoanService taiKhoanService)
        {
            _hoSoService = hoSoService;
            _storageService = storageService;
            _taiKhoanService = taiKhoanService;
        }

        // 1. DANH SÁCH HỒ SƠ + TÌM KIẾM + LỌC + SẮP XẾP + PHÂN TRANG
        public async Task<IActionResult> Index(HoSoFilterViewModel filter)
        {
            var response = await _hoSoService.LayDanhSachAsync(filter);
            var pagedData = response.Data;

            ViewBag.DanhSach = pagedData!.Items;
            ViewBag.TongSoBanGhi = pagedData.TotalItems;
            ViewBag.Trang = pagedData.PageNumber;
            ViewBag.TongSoTrang = pagedData.TotalPages;

            var dsDonVi = await _hoSoService.LayDanhSachDonViTaiTroAsync();
            ViewBag.DonViTaiTro = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                dsDonVi, "MaDonViTaiTro", "TenDonViTaiTro", filter.MaDonViTaiTro);

            return View(filter);
        }

        // 2. CHI TIẾT HỒ SƠ
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XetDuyetSoBo(XetDuyetSoBoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await LoadThongTinHienThiAsync(model);
                return View(model);
            }

            var response = await _hoSoService.XetDuyetSoBoAsync(model);
            if (!response.Success)
            {
                ModelState.AddModelError("", response.Message);
                await LoadThongTinHienThiAsync(model);
                return View(model);
            }

            TempData["Success"] = response.Message;
            return RedirectToAction(nameof(Index));
        }

        // 5. KIỂM TRA SỐ SUẤT ĐÃ CẤP (JSON - dùng cho AJAX)
        [HttpGet]
        public async Task<IActionResult> KiemTraSoSuat(int maHocBong)
        {
            var soDaCap = await _hoSoService.DemSoDaCapAsync(maHocBong);
            var soSuat = await _hoSoService.LaySoSuatHocBongAsync(maHocBong);
            return Json(new
            {
                soSuat = soSuat,
                soDaCap = soDaCap,
                conLai = Math.Max(0, soSuat - soDaCap),
                daDatSuat = soDaCap >= soSuat
            });
        }

        // 6. HỒ SƠ CỦA TÔI (SINH VIÊN)
        [RoleAuthorize]
        public async Task<IActionResult> HoSoCuaToi()
        {
            var maTaiKhoan = HttpContext.Session.GetInt32("MaTaiKhoan");
            if (!maTaiKhoan.HasValue) return RedirectToAction("DangNhap", "TaiKhoan");

            // Lấy thông tin sinh viên từ Service (đã được xử lý trong Service)
            var sv = await _taiKhoanService.GetSinhVienByTaiKhoanAsync(maTaiKhoan.Value);
            if (sv == null) return View(new List<HoSoHocBong>());

            var danhSach = await _hoSoService.LayHoSoCuaSinhVienAsync(sv.MaSinhVien);
            return View(danhSach);
        }

        // 7. NỘP HỒ SƠ (SINH VIÊN - GET)
        [RoleAuthorize]
        [HttpGet]
        public async Task<IActionResult> NopHoSo(int maHocBong)
        {
            var maTaiKhoan = HttpContext.Session.GetInt32("MaTaiKhoan");
            if (!maTaiKhoan.HasValue) return RedirectToAction("DangNhap", "TaiKhoan");

            var hocBong = await _taiKhoanService.GetHocBongByIdAsync(maHocBong);
            if (hocBong == null) return NotFound();

            var sv = await _taiKhoanService.GetSinhVienByTaiKhoanAsync(maTaiKhoan.Value);
            if (sv == null)
            {
                TempData["ErrorMessage"] = "Vui lòng cập nhật hồ sơ sinh viên trước khi nộp hồ sơ.";
                return RedirectToAction("ThongTinCaNhan", "SinhVien");
            }

            ViewBag.HocBong = hocBong;
            ViewBag.SinhVien = sv;
            return View(new HoSoHocBong { MaHocBong = maHocBong, MaSinhVien = sv.MaSinhVien });
        }

        // 8. NỘP HỒ SƠ (SINH VIÊN - POST)
        [RoleAuthorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NopHoSo(int MaHocBong, string ThuXinHocBong, string? GhiChu, IFormFile? FileMinhChungUpload)
        {
            var maTaiKhoan = HttpContext.Session.GetInt32("MaTaiKhoan");
            if (!maTaiKhoan.HasValue) return RedirectToAction("DangNhap", "TaiKhoan");

            var hocBong = await _taiKhoanService.GetHocBongByIdAsync(MaHocBong);
            var sv = await _taiKhoanService.GetSinhVienByTaiKhoanAsync(maTaiKhoan.Value);
            if (hocBong == null || sv == null) return NotFound();

            string? fileUrl = null;
            if (FileMinhChungUpload != null && FileMinhChungUpload.Length > 0)
            {
                fileUrl = await _storageService.UploadFileAsync(FileMinhChungUpload, "minhchung");
            }

            var response = await _hoSoService.NopHoSoAsync(sv.MaSinhVien, MaHocBong, ThuXinHocBong, GhiChu ?? "", fileUrl, hocBong, sv);

            if (!response.Success)
            {
                ViewBag.HocBong = hocBong;
                ViewBag.SinhVien = sv;
                TempData["ErrorMessage"] = response.Message;
                return View(new HoSoHocBong { MaHocBong = MaHocBong, ThuXinHocBong = ThuXinHocBong ?? "", GhiChu = GhiChu ?? "" });
            }

            TempData["SuccessMessage"] = response.Message;
            return RedirectToAction(nameof(HoSoCuaToi));
        }

        // 9. HỦY HỒ SƠ (SINH VIÊN - POST)
        [RoleAuthorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> HuyHoSo(int id)
        {
            var maTaiKhoan = HttpContext.Session.GetInt32("MaTaiKhoan");
            if (!maTaiKhoan.HasValue) return RedirectToAction("DangNhap", "TaiKhoan");

            var sv = await _taiKhoanService.GetSinhVienByTaiKhoanAsync(maTaiKhoan.Value);
            if (sv == null) return NotFound();

            var response = await _hoSoService.HuyHoSoAsync(id, sv.MaSinhVien);

            if (response.Success) TempData["SuccessMessage"] = response.Message;
            else TempData["ErrorMessage"] = response.Message;

            return RedirectToAction(nameof(HoSoCuaToi));
        }

        private async Task LoadThongTinHienThiAsync(XetDuyetSoBoViewModel model)
        {
            var hoSo = await _hoSoService.LayFormXetDuyetAsync(model.MaHoSoHocBong);
            if (hoSo == null) return;

            model.TenSinhVien = hoSo.TenSinhVien;
            model.TenHocBong = hoSo.TenHocBong;
            model.TenDonViTaiTro = hoSo.TenDonViTaiTro;
            model.DiemTrungBinh = hoSo.DiemTrungBinh;
            model.DiemYeuCau = hoSo.DiemYeuCau;
            model.BacDaoTaoSinhVien = hoSo.BacDaoTaoSinhVien;
            model.BacDaoTaoHocBong = hoSo.BacDaoTaoHocBong;
            model.NgayNop = hoSo.NgayNop;
            model.ThuXinHocBong = hoSo.ThuXinHocBong;
            model.ThanhTichHocTap = hoSo.ThanhTichHocTap;
            model.FileMinhChung = hoSo.FileMinhChung;
        }
    }
}