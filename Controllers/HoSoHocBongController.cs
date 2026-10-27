// Họ và tên: Phan Sỹ Tú
// Mã sinh viên: 23103100145
// Nội dung thực hiện: Module 4 - Controller xử lý tiếp nhận hồ sơ,
// xét duyệt sơ bộ, quản lý trạng thái và kiểm soát số suất.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Attributes;
using QuanLyHocBong_UNETI2_TI17A3HN.Constants;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.Services;
using QuanLyHocBong_UNETI2_TI17A3HN.Services.Interface;
using QuanLyHocBong_UNETI2_TI17A3HN.ViewModels;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Controllers
{
    public class HoSoHocBongController : Controller
    {
        private readonly IHoSoHocBongService _hoSoService;
        private readonly QuanLyHocBong_UNETI2_TI17A3HNContext _context;
        private readonly ISupabaseStorageService _storageService;

        public HoSoHocBongController(
            IHoSoHocBongService hoSoService,
            QuanLyHocBong_UNETI2_TI17A3HNContext context,
            ISupabaseStorageService storageService)
        {
            _hoSoService = hoSoService;
            _context = context;
            _storageService = storageService;
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
            var hoSo = await _context.HoSoHocBong
                .Include(h => h.SinhVien)
                .Include(h => h.HocBong)
                    .ThenInclude(hb => hb.DonViTaiTro)
                .Include(h => h.LichPhongVans)
                .Include(h => h.KetQuaHocBong)
                .FirstOrDefaultAsync(h => h.MaHoSoHocBong == id);

            if (hoSo == null)
            {
                TempData["Error"] = "Không tìm thấy hồ sơ.";
                return RedirectToAction(nameof(Index));
            }

            return View(hoSo);
        }

        // 3. FORM XÉT DUYỆT SƠ BỘ (GET)
        // GET: /HoSoHocBong/XetDuyetSoBo/5
        public async Task<IActionResult> XetDuyetSoBo(int id)
        {
            var hoSo = await _context.HoSoHocBong
                .Include(h => h.SinhVien)
                .Include(h => h.HocBong)
                    .ThenInclude(hb => hb.DonViTaiTro)
                .FirstOrDefaultAsync(h => h.MaHoSoHocBong == id);

            if (hoSo == null)
            {
                TempData["Error"] = "Không tìm thấy hồ sơ.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.HoSo = hoSo;

            var model = new XetDuyetSoBoViewModel
            {
                MaHoSoHocBong = hoSo.MaHoSoHocBong,
                TenSinhVien = hoSo.SinhVien?.HoTen ?? "",
                TenHocBong = hoSo.HocBong?.TenHocBong ?? "",
                TenDonViTaiTro = hoSo.HocBong?.DonViTaiTro?.TenDonViTaiTro ?? "",
                DiemTrungBinh = hoSo.SinhVien?.DiemTrungBinhTichLuy ?? 0,
                DiemYeuCau = hoSo.HocBong?.DiemTrungBinhToiThieu ?? 0,
                BacDaoTaoSinhVien = hoSo.SinhVien?.BacDaoTao ?? "",
                BacDaoTaoHocBong = hoSo.HocBong?.BacDaoTao ?? "",
                NgayNop = hoSo.NgayNop,
                ThuXinHocBong = hoSo.ThuXinHocBong,
                ThanhTichHocTap = hoSo.SinhVien?.ThanhTichHocTap ?? ""
            };

            return View(model);
        }

        // 4. XỬ LÝ XÉT DUYỆT SƠ BỘ (POST)
        // POST: /HoSoHocBong/XetDuyetSoBo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> XetDuyetSoBo(XetDuyetSoBoViewModel model)
        {
            var hoSo = await _context.HoSoHocBong
                .Include(h => h.SinhVien)
                .Include(h => h.HocBong)
                    .ThenInclude(hb => hb.DonViTaiTro)
                .FirstOrDefaultAsync(h => h.MaHoSoHocBong == model.MaHoSoHocBong);
            ViewBag.HoSo = hoSo;

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
        public async Task<IActionResult> NopHoSo(int MaHocBong, string ThuXinHocBong, string? GhiChu, IFormFile? FileMinhChungUpload)
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

            string? fileUrl = null;
            if (FileMinhChungUpload != null && FileMinhChungUpload.Length > 0)
            {
                fileUrl = await _storageService.UploadFileAsync(FileMinhChungUpload, "minhchung");
            }

            var hoSo = new HoSoHocBong
            {
                MaSinhVien = sv.MaSinhVien,
                MaHocBong = MaHocBong,
                NgayNop = DateTime.Now,
                ThuXinHocBong = (ThuXinHocBong ?? "").Trim(),
                GhiChu = (GhiChu ?? "Đã nộp đầy đủ hồ sơ").Trim(),
                TrangThai = "Chờ duyệt",
                FileMinhChung = fileUrl
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
        private async Task LoadThongTinHienThiAsync(XetDuyetSoBoViewModel model)
        {
            var hoSo = await _context.HoSoHocBong
                .Include(h => h.SinhVien)
                .Include(h => h.HocBong)
                    .ThenInclude(hb => hb.DonViTaiTro)
                .FirstOrDefaultAsync(h => h.MaHoSoHocBong == model.MaHoSoHocBong);
            if (hoSo == null) return;

            model.TenSinhVien = hoSo.SinhVien?.HoTen ?? "";
            model.TenHocBong = hoSo.HocBong?.TenHocBong ?? "";
            model.TenDonViTaiTro = hoSo.HocBong?.DonViTaiTro?.TenDonViTaiTro ?? "";
            model.DiemTrungBinh = hoSo.SinhVien?.DiemTrungBinhTichLuy ?? 0;
            model.DiemYeuCau = hoSo.HocBong?.DiemTrungBinhToiThieu ?? 0;
            model.BacDaoTaoSinhVien = hoSo.SinhVien?.BacDaoTao ?? "";
            model.BacDaoTaoHocBong = hoSo.HocBong?.BacDaoTao ?? "";
            model.NgayNop = hoSo.NgayNop;
            model.ThuXinHocBong = hoSo.ThuXinHocBong;
            model.ThanhTichHocTap = hoSo.SinhVien?.ThanhTichHocTap ?? "";
        }
    }
}
