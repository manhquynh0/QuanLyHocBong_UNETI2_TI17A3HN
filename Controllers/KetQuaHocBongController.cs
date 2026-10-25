// ============================================================================
// ĐỀ TÀI 22: XÂY DỰNG HỆ THỐNG QUẢN LÝ HỌC BỔNG VÀ HỒ SƠ XÉT HỌC BỔNG
// NHÓM 2 - LỚP TI17A3HN - UNETI
// MODULE 5: KẾT QUẢ HỌC BỔNG, NHẬP ĐIỂM, KIỂM SOÁT SỐ SUẤT & CÔNG BỐ
// ============================================================================

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Attributes;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Controllers
{
    public class KetQuaHocBongController : Controller
    {
        private readonly QuanLyHocBong_UNETI2_TI17A3HNContext _context;
        private readonly Services.IEmailService _emailService;

        public KetQuaHocBongController(
            QuanLyHocBong_UNETI2_TI17A3HNContext context,
            Services.IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ====================================================================
        // GET: KetQuaHocBong/Index
        // ====================================================================
        [RoleAuthorize]
        public async Task<IActionResult> Index(string? searchString, int? hocBongId, string? ketQuaFilter, int page = 1)
        {
            var userRole = HttpContext.Session.GetString("VaiTro") ?? "";
            bool isStudent = userRole.Equals("Sinh viên", StringComparison.OrdinalIgnoreCase)
                || userRole.StartsWith("Sinh", StringComparison.OrdinalIgnoreCase);

            var query = _context.KetQuaHocBong
                .Include(k => k.HoSoHocBong)
                    .ThenInclude(hs => hs.SinhVien)
                .Include(k => k.HoSoHocBong)
                    .ThenInclude(hs => hs.HocBong)
                .AsQueryable();

            // Nếu là sinh viên, chỉ xem kết quả của chính mình
            if (isStudent)
            {
                var maTaiKhoan = HttpContext.Session.GetInt32("MaTaiKhoan")!.Value;
                var sv = await _context.SinhVien.FirstOrDefaultAsync(s => s.MaTaiKhoan == maTaiKhoan);
                if (sv != null)
                {
                    query = query.Where(k => k.HoSoHocBong.MaSinhVien == sv.MaSinhVien);
                }
                else
                {
                    query = query.Where(k => false);
                }
            }

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var kw = searchString.Trim();
                query = query.Where(k => k.HoSoHocBong.SinhVien!.HoTen.Contains(kw) ||
                                         k.HoSoHocBong.HocBong!.TenHocBong.Contains(kw));
            }

            if (hocBongId.HasValue && hocBongId.Value > 0)
            {
                query = query.Where(k => k.HoSoHocBong.MaHocBong == hocBongId.Value);
            }

            if (!string.IsNullOrWhiteSpace(ketQuaFilter))
            {
                query = query.Where(k => k.KetQua == ketQuaFilter);
            }

            int pageSize = 10;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var items = await query
                .OrderByDescending(k => k.NgayCapNhat)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentHocBong = hocBongId;
            ViewBag.CurrentKetQua = ketQuaFilter;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.IsStudent = isStudent;

            ViewBag.HocBongList = await _context.HocBong.ToListAsync();

            return View(items);
        }

        // ====================================================================
        // GET: KetQuaHocBong/NhapKetQua (Module 5: Nhập kết quả xét học bổng)
        // ====================================================================
        [RoleAuthorize("Admin", "Cán bộ học bổng")]
        public async Task<IActionResult> NhapKetQua(int maHoSo)
        {
            var hoSo = await _context.HoSoHocBong
                .Include(hs => hs.SinhVien)
                .Include(hs => hs.HocBong)
                .Include(hs => hs.LichPhongVans)
                .Include(hs => hs.KetQuaHocBong)
                .FirstOrDefaultAsync(hs => hs.MaHoSoHocBong == maHoSo);

            if (hoSo == null) return NotFound();

            if (hoSo.TrangThai == "Chờ duyệt" && hoSo.KetQuaHocBong == null)
            {
                TempData["ErrorMessage"] = "Hồ sơ đang ở trạng thái 'Chờ duyệt'. Vui lòng xét duyệt sơ bộ hồ sơ trước khi công bố kết quả xét học bổng.";
                return RedirectToAction("XetDuyetSoBo", "HoSoHocBong", new { id = maHoSo });
            }

            // Kiểm soát số suất học bổng bằng LINQ
            if (hoSo.HocBong != null)
            {
                int soDaCap = await _context.HoSoHocBong.CountAsync(h => h.MaHocBong == hoSo.MaHocBong && h.TrangThai == "Được cấp học bổng");
                ViewBag.SoDaCap = soDaCap;
                ViewBag.TongSoSuat = hoSo.HocBong.SoSuat;
                ViewBag.CanhBaoHetSuat = soDaCap >= hoSo.HocBong.SoSuat;
            }

            ViewBag.HoSo = hoSo;

            var ketQua = hoSo.KetQuaHocBong ?? new KetQuaHocBong
            {
                MaHoSoHocBong = maHoSo,
                KetQua = "Được cấp học bổng",
                DiemDanhGia = 85.0m,
                NhanXet = "Sinh viên xuất sắc, thái độ phỏng vấn tốt, mục tiêu học tập rõ ràng."
            };

            return View(ketQua);
        }

        // ====================================================================
        // POST: KetQuaHocBong/NhapKetQua
        // ====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "Cán bộ học bổng")]
        public async Task<IActionResult> NhapKetQua(int MaHoSoHocBong, string KetQua, decimal? DiemDanhGia, string? NhanXet)
        {
            var hoSo = await _context.HoSoHocBong
                .Include(hs => hs.SinhVien)
                    .ThenInclude(sv => sv.TaiKhoan)
                .Include(hs => hs.HocBong)
                    .ThenInclude(hb => hb.DonViTaiTro)
                .FirstOrDefaultAsync(hs => hs.MaHoSoHocBong == MaHoSoHocBong);

            if (hoSo == null) return NotFound();

            if (DiemDanhGia.HasValue && (DiemDanhGia.Value < 0 || DiemDanhGia.Value > 100))
            {
                ModelState.AddModelError("DiemDanhGia", "Điểm đánh giá phải nằm trong khoảng từ 0 đến 100.");
            }

            if (KetQua != "Được cấp học bổng" && KetQua != "Không được cấp học bổng")
            {
                ModelState.AddModelError("KetQua", "Kết quả phải là 'Được cấp học bổng' hoặc 'Không được cấp học bổng'.");
            }

            if (ModelState.IsValid)
            {
                var safeNhanXet = NhanXet ?? string.Empty;
                var existingKq = await _context.KetQuaHocBong.FirstOrDefaultAsync(k => k.MaHoSoHocBong == MaHoSoHocBong);
                if (existingKq == null)
                {
                    existingKq = new KetQuaHocBong
                    {
                        MaHoSoHocBong = MaHoSoHocBong,
                        KetQua = KetQua,
                        DiemDanhGia = DiemDanhGia,
                        NhanXet = safeNhanXet,
                        NgayCapNhat = DateTime.Now
                    };
                    _context.KetQuaHocBong.Add(existingKq);
                }
                else
                {
                    existingKq.KetQua = KetQua;
                    existingKq.DiemDanhGia = DiemDanhGia;
                    existingKq.NhanXet = safeNhanXet;
                    existingKq.NgayCapNhat = DateTime.Now;
                }

                // Cập nhật trạng thái của hồ sơ tương ứng theo State Machine (Mục 9.4)
                hoSo.TrangThai = KetQua;

                await _context.SaveChangesAsync();

                var studentEmail = !string.IsNullOrWhiteSpace(hoSo.SinhVien?.Email)
                    ? hoSo.SinhVien.Email
                    : hoSo.SinhVien?.TaiKhoan?.Email;

                if (!string.IsNullOrWhiteSpace(studentEmail))
                {
                    bool isDuocCap = KetQua == "Được cấp học bổng";
                    string subject = isDuocCap
                        ? $"[UNETI] CHÚC MỪNG TRÚNG TUYỂN: Bạn đã ĐƯỢC CẤP học bổng #{hoSo.MaHoSoHocBong} - {hoSo.HocBong?.TenHocBong}"
                        : $"[UNETI] THÔNG BÁO KẾT QUẢ: Hồ sơ học bổng #{hoSo.MaHoSoHocBong} - KHÔNG ĐƯỢC CẤP HỌC BỔNG";

                    string bannerColor = isDuocCap ? "#059669" : "#dc2626";
                    string bannerTitle = isDuocCap ? "🏆 CHÚC MỪNG BẠN ĐÃ TRÚNG TUYỂN HỌC BỔNG!" : "THÔNG BÁO KẾT QUẢ XÉT DUYỆT HỌC BỔNG";

                    string message = $@"
                        <div style='background: {bannerColor}; color: #ffffff; padding: 18px 20px; border-radius: 8px; margin-bottom: 20px; text-align: center;'>
                            <h2 style='margin: 0; font-size: 18px; font-weight: 700;'>{bannerTitle}</h2>
                        </div>
                        <p>Kính gửi sinh viên <strong>{hoSo.SinhVien?.HoTen}</strong>,</p>
                        <p>Hội đồng xét duyệt học bổng UNETI xin thông báo kết quả chính thức cho hồ sơ <strong>#{hoSo.MaHoSoHocBong}</strong> ({hoSo.HocBong?.TenHocBong}):</p>
                        <ul>
                            <li><strong>Kết quả:</strong> {KetQua}</li>
                            <li><strong>Điểm đánh giá:</strong> {(DiemDanhGia.HasValue ? DiemDanhGia.Value.ToString("0.0") + " / 100" : "N/A")}</li>
                            <li><strong>Nhận xét:</strong> {safeNhanXet}</li>
                        </ul>
                        <p>Trân trọng,<br/><strong>Hội đồng Xét duyệt Học bổng UNETI</strong></p>";

                    await _emailService.SendEmailAsync(studentEmail, subject, message);
                }

                TempData["SuccessMessage"] = $"Đã công bố kết quả: '{KetQua}' cho hồ sơ #{MaHoSoHocBong}!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.HoSo = hoSo;
            return View(new KetQuaHocBong { MaHoSoHocBong = MaHoSoHocBong, KetQua = KetQua, DiemDanhGia = DiemDanhGia, NhanXet = NhanXet ?? string.Empty });
        }
    }
}

