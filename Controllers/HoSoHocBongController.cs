// Họ và tên: Phan Sỹ Tú
// Mã sinh viên: 23103100145
// Nội dung thực hiện: Module 4 - Controller xử lý tiếp nhận hồ sơ,
// xét duyệt sơ bộ, quản lý trạng thái và kiểm soát số suất.

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Attributes;
using QuanLyHocBong_UNETI2_TI17A3HN.Constants;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.Services.Interface;
using QuanLyHocBong_UNETI2_TI17A3HN.ViewModels;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Controllers
{
    public class HoSoHocBongController : Controller
    {
        private readonly IHoSoHocBongService _hoSoService;
        private readonly QuanLyHocBong_UNETI2_TI17A3HNContext _context;

        public HoSoHocBongController(
            IHoSoHocBongService hoSoService,
            QuanLyHocBong_UNETI2_TI17A3HNContext context)
        {
            _hoSoService = hoSoService;
            _context = context;
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

        // 6. HỒ SƠ CỦA TÔI (SINH VIÊN)
        // GET: /HoSoHocBong/HoSoCuaToi
        [RoleAuthorize]
        public async Task<IActionResult> HoSoCuaToi()
        {
            var maTaiKhoan = HttpContext.Session.GetInt32("MaTaiKhoan");
            if (!maTaiKhoan.HasValue) return RedirectToAction("DangNhap", "TaiKhoan");

            var sv = await _context.SinhVien.FirstOrDefaultAsync(s => s.MaTaiKhoan == maTaiKhoan.Value);
            if (sv == null)
            {
                return View(new List<HoSoHocBong>());
            }

            var danhSach = await _context.HoSoHocBong
                .Include(h => h.HocBong)
                    .ThenInclude(hb => hb.DonViTaiTro)
                .Include(h => h.LichPhongVans)
                .Include(h => h.KetQuaHocBong)
                .Where(h => h.MaSinhVien == sv.MaSinhVien)
                .OrderByDescending(h => h.NgayNop)
                .ToListAsync();

            return View(danhSach);
        }

        // 7. NỘP HỒ SƠ (SINH VIÊN - GET)
        // GET: /HoSoHocBong/NopHoSo?maHocBong=1
        [RoleAuthorize]
        [HttpGet]
        public async Task<IActionResult> NopHoSo(int maHocBong)
        {
            var maTaiKhoan = HttpContext.Session.GetInt32("MaTaiKhoan");
            if (!maTaiKhoan.HasValue) return RedirectToAction("DangNhap", "TaiKhoan");

            var hocBong = await _context.HocBong
                .Include(h => h.DonViTaiTro)
                .FirstOrDefaultAsync(h => h.MaHocBong == maHocBong);
            if (hocBong == null) return NotFound();

            var sv = await _context.SinhVien.FirstOrDefaultAsync(s => s.MaTaiKhoan == maTaiKhoan.Value);
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
        public async Task<IActionResult> NopHoSo(int MaHocBong, string ThuXinHocBong, string? GhiChu)
        {
            var maTaiKhoan = HttpContext.Session.GetInt32("MaTaiKhoan");
            if (!maTaiKhoan.HasValue) return RedirectToAction("DangNhap", "TaiKhoan");

            var hocBong = await _context.HocBong
                .Include(h => h.DonViTaiTro)
                .FirstOrDefaultAsync(h => h.MaHocBong == MaHocBong);
            if (hocBong == null) return NotFound();

            var sv = await _context.SinhVien.FirstOrDefaultAsync(s => s.MaTaiKhoan == maTaiKhoan.Value);
            if (sv == null) return RedirectToAction("ThongTinCaNhan", "SinhVien");

            ViewBag.HocBong = hocBong;
            ViewBag.SinhVien = sv;

            if (hocBong.TrangThai != "Đang nhận hồ sơ" || hocBong.HanNopHoSo < DateTime.Today)
            {
                TempData["ErrorMessage"] = "Học bổng này đã đóng hoặc hết hạn nhận hồ sơ.";
                return View(new HoSoHocBong { MaHocBong = MaHocBong, ThuXinHocBong = ThuXinHocBong ?? "", GhiChu = GhiChu ?? "" });
            }

            if (sv.DiemTrungBinhTichLuy < hocBong.DiemTrungBinhToiThieu)
            {
                TempData["ErrorMessage"] = $"Điểm GPA của bạn ({sv.DiemTrungBinhTichLuy}) chưa đạt mức tối thiểu ({hocBong.DiemTrungBinhToiThieu}).";
                return View(new HoSoHocBong { MaHocBong = MaHocBong, ThuXinHocBong = ThuXinHocBong ?? "", GhiChu = GhiChu ?? "" });
            }

            bool daNop = await _context.HoSoHocBong.AnyAsync(h =>
                h.MaSinhVien == sv.MaSinhVien &&
                h.MaHocBong == MaHocBong &&
                h.TrangThai != "Đã hủy");
            if (daNop)
            {
                TempData["ErrorMessage"] = "Bạn đã nộp hồ sơ cho chương trình học bổng này rồi.";
                return RedirectToAction(nameof(HoSoCuaToi));
            }

            var hoSo = new HoSoHocBong
            {
                MaSinhVien = sv.MaSinhVien,
                MaHocBong = MaHocBong,
                NgayNop = DateTime.Now,
                ThuXinHocBong = (ThuXinHocBong ?? "").Trim(),
                GhiChu = (GhiChu ?? "Đã nộp đầy đủ hồ sơ").Trim(),
                TrangThai = "Chờ duyệt"
            };

            _context.HoSoHocBong.Add(hoSo);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Nộp hồ sơ xét học bổng thành công!";
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

            var sv = await _context.SinhVien.FirstOrDefaultAsync(s => s.MaTaiKhoan == maTaiKhoan.Value);
            var hoSo = await _context.HoSoHocBong.FindAsync(id);

            if (hoSo == null || sv == null || hoSo.MaSinhVien != sv.MaSinhVien)
            {
                return NotFound();
            }

            if (hoSo.TrangThai == "Chờ duyệt")
            {
                hoSo.TrangThai = "Đã hủy";
                hoSo.NgayXuLy = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Đã hủy hồ sơ #{id} thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Chỉ có thể hủy hồ sơ khi đang ở trạng thái 'Chờ duyệt'.";
            }

            return RedirectToAction(nameof(HoSoCuaToi));
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
