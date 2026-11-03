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
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;

        public HoSoHocBongController(
            IHoSoHocBongService hoSoService,
            QuanLyHocBong_UNETI2_TI17A3HNContext context,
            ISupabaseStorageService storageService,
            INotificationService notificationService,
            IEmailService emailService)
        {
            _hoSoService = hoSoService;
            _context = context;
            _storageService = storageService;
            _notificationService = notificationService;
            _emailService = emailService;
        }

        // 1. DANH SÁCH HỒ SƠ + TÌM KIẾM + LỌC + SẮP XẾP + PHÂN TRANG
        // GET: /HoSoHocBong
        [RoleAuthorize("Admin", "Cán bộ học bổng")]
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
        [RoleAuthorize("Admin", "Cán bộ học bổng")]
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
        [RoleAuthorize("Admin", "Cán bộ học bổng")]
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
        [RoleAuthorize("Admin", "Cán bộ học bổng")]
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

            // Gửi thông báo Realtime (SignalR) cho sinh viên về kết quả xét duyệt sơ bộ
            if (hoSo?.SinhVien != null)
            {
                try
                {
                    var tieuDe = model.KetQua == "Đạt xét duyệt sơ bộ"
                        ? "Hồ sơ đạt xét duyệt sơ bộ"
                        : "Kết quả xét duyệt sơ bộ hồ sơ";
                    var noiDung = $"Hồ sơ học bổng \"{hoSo.HocBong?.TenHocBong}\" của bạn: {model.KetQua}. {(string.IsNullOrWhiteSpace(model.NhanXetXetDuyet) ? "" : $"Nhận xét: {model.NhanXetXetDuyet}")}";

                    await _notificationService.GuiThongBaoAsync(
                        maTaiKhoan: hoSo.SinhVien.MaTaiKhoan,
                        tieuDe: tieuDe,
                        noiDung: noiDung,
                        loaiThongBao: "HoSo",
                        lienKet: $"/HoSoCuaToi/Details/{model.MaHoSoHocBong}",
                        maHoSoHocBong: model.MaHoSoHocBong
                    );
                }
                catch
                {
                    // Lỗi thông báo không làm gián đoạn nghiệp vụ chính
                }

                // Gửi email kết quả xét duyệt sơ bộ cho sinh viên
                try
                {
                    var studentEmail = !string.IsNullOrWhiteSpace(hoSo.SinhVien.Email)
                        ? hoSo.SinhVien.Email
                        : hoSo.SinhVien.TaiKhoan?.Email;

                    if (!string.IsNullOrWhiteSpace(studentEmail))
                    {
                        bool isDat = model.KetQua == "Đạt xét duyệt sơ bộ" || model.KetQua == "Dat";
                        var bannerColor = isDat ? "#059669" : "#dc2626";
                        var bannerTitle = isDat ? "HỒ SƠ ĐÃ ĐẠT XÉT DUYỆT SƠ BỘ" : "KẾT QUẢ XÉT DUYỆT SƠ BỘ HỒ SƠ";
                        var subject = isDat
                            ? $"[UNETI] Chúc mừng: Hồ sơ #{model.MaHoSoHocBong} đã ĐẠT xét duyệt sơ bộ - {hoSo.HocBong?.TenHocBong}"
                            : $"[UNETI] Thông báo kết quả xét duyệt sơ bộ hồ sơ #{model.MaHoSoHocBong} - {hoSo.HocBong?.TenHocBong}";

                        var message = $@"
                            <div style='background: {bannerColor}; color: #ffffff; padding: 18px 20px; border-radius: 8px; margin-bottom: 20px; text-align: center;'>
                                <h2 style='margin: 0; font-size: 18px; font-weight: 700;'>{bannerTitle}</h2>
                            </div>
                            <p>Kính gửi sinh viên <strong>{hoSo.SinhVien.HoTen}</strong>,</p>
                            <p>Hội đồng xét duyệt học bổng UNETI xin thông báo kết quả thẩm định sơ bộ cho hồ sơ <strong>#{model.MaHoSoHocBong}</strong> ({hoSo.HocBong?.TenHocBong}):</p>
                            <ul>
                                <li><strong>Kết quả sơ bộ:</strong> <strong style='color: {bannerColor};'>{model.KetQua}</strong></li>
                                <li><strong>Thời gian xử lý:</strong> {DateTime.Now:HH:mm dd/MM/yyyy}</li>
                                <li><strong>Nhận xét từ Hội đồng:</strong> {(string.IsNullOrWhiteSpace(model.NhanXetXetDuyet) ? "Không có" : model.NhanXetXetDuyet)}</li>
                            </ul>
                            {(isDat ? "<p>Hồ sơ của bạn đã đáp ứng yêu cầu sơ tuyển và được chuyển sang bước tiếp theo để sắp xếp lịch phỏng vấn. Bạn vui lòng theo dõi thông báo trên hệ thống và email để nắm thông tin lịch phỏng vấn.</p>" : "<p>Rất tiếc hồ sơ chưa đáp ứng tiêu chí đợt này. Chúc bạn tiếp tục nỗ lực và gặt hái thành công ở các cơ hội học bổng tiếp theo.</p>")}
                            <p>Trân trọng,<br/><strong>Hội đồng Xét duyệt Học bổng UNETI</strong></p>";

                        await _emailService.SendEmailAsync(studentEmail, subject, message);
                    }
                }
                catch
                {
                    // Lỗi email không làm gián đoạn nghiệp vụ chính
                }
            }

            TempData["Success"] = "Xét duyệt sơ bộ thành công.";
            return RedirectToAction(nameof(Index));
        }

        // 5. KIỂM TRA SỐ SUẤT ĐÃ CẤP (JSON - dùng cho AJAX)
        // GET: /HoSoHocBong/KiemTraSoSuat/5
        [HttpGet]
        [RoleAuthorize("Admin", "Cán bộ học bổng")]
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
            var maTaiKhoan = (HttpContext.Items["MaTaiKhoan"] as int?);
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
            var maTaiKhoan = (HttpContext.Items["MaTaiKhoan"] as int?);
            if (!maTaiKhoan.HasValue) return RedirectToAction("DangNhap", "TaiKhoan");

            var hocBong = await _context.HocBong
                .Include(h => h.DonViTaiTro)
                .FirstOrDefaultAsync(h => h.MaHocBong == maHocBong);
            if (hocBong == null) return NotFound();

            var sv = await _context.SinhVien.FirstOrDefaultAsync(s => s.MaTaiKhoan == maTaiKhoan.Value);
            if (sv == null)
            {
                TempData["ErrorMessage"] = "Vui lòng cập nhật hồ sơ sinh viên trước khi nộp hồ sơ.";
                return RedirectToAction("Index", "HoSoCaNhan");
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
            var maTaiKhoan = (HttpContext.Items["MaTaiKhoan"] as int?);
            if (!maTaiKhoan.HasValue) return RedirectToAction("DangNhap", "TaiKhoan");

            var hocBong = await _context.HocBong
                .Include(h => h.DonViTaiTro)
                .FirstOrDefaultAsync(h => h.MaHocBong == MaHocBong);
            if (hocBong == null) return NotFound();

            var sv = await _context.SinhVien.FirstOrDefaultAsync(s => s.MaTaiKhoan == maTaiKhoan.Value);
            if (sv == null) return RedirectToAction("Index", "HoSoCaNhan");

            ViewBag.HocBong = hocBong;
            ViewBag.SinhVien = sv;

            if (hocBong.TrangThai != "Đang nhận hồ sơ" || hocBong.HanNopHoSo < DateTime.Today || hocBong.NgayBatDauNhanHoSo > DateTime.Today)
            {
                TempData["ErrorMessage"] = "Học bổng này chưa mở, tạm dừng, đã đóng hoặc hết hạn nhận hồ sơ.";
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
                var dsLoiTep = await TaiLieuHoSoStorage.KiemTraAsync(new[] { FileMinhChungUpload }, 0);
                if (dsLoiTep.Count > 0)
                {
                    TempData["ErrorMessage"] = string.Join(" ", dsLoiTep);
                    return View(new HoSoHocBong { MaHocBong = MaHocBong, ThuXinHocBong = ThuXinHocBong ?? "", GhiChu = GhiChu ?? "" });
                }

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
            var maTaiKhoan = (HttpContext.Items["MaTaiKhoan"] as int?);
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
            model.FileMinhChung = hoSo.FileMinhChung;
            model.TaiLieus = await TaiLieuHoSoStorage.DanhSachAsync(_context, model.MaHoSoHocBong);
        }
    }
}
