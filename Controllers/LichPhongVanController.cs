// ============================================================================
// ĐỀ TÀI 22: XÂY DỰNG HỆ THỐNG QUẢN LÝ HỌC BỔNG VÀ HỒ SƠ XÉT HỌC BỔNG
// NHÓM 2 - LỚP TI17A3HN - UNETI
// MODULE 5: QUẢN LÝ LỊCH PHỎNG VẤN & THUẬT TOÁN LINQ KIỂM TRA CHỐNG TRÙNG LỊCH
// ============================================================================

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Attributes;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Controllers
{
    public class LichPhongVanController : Controller
    {
        private readonly QuanLyHocBong_UNETI2_TI17A3HNContext _context;
        private readonly Services.IEmailService _emailService;

        public LichPhongVanController(
            QuanLyHocBong_UNETI2_TI17A3HNContext context,
            Services.IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ====================================================================
        // GET: LichPhongVan/Index
        // ====================================================================
        [RoleAuthorize]
        public async Task<IActionResult> Index(string? searchString, string? trangThaiFilter, int page = 1)
        {
            var userRole = HttpContext.Session.GetString("VaiTro") ?? "";
            bool isStudent = userRole.Equals("Sinh viên", StringComparison.OrdinalIgnoreCase)
                || userRole.StartsWith("Sinh", StringComparison.OrdinalIgnoreCase);

            var query = _context.LichPhongVan
                .Include(l => l.HoSoHocBong)
                    .ThenInclude(hs => hs.SinhVien)
                .Include(l => l.HoSoHocBong)
                    .ThenInclude(hs => hs.HocBong)
                .AsQueryable();

            // Nếu là sinh viên, chỉ xem lịch của chính mình
            if (isStudent)
            {
                var maTaiKhoan = HttpContext.Session.GetInt32("MaTaiKhoan")!.Value;
                var sv = await _context.SinhVien.FirstOrDefaultAsync(s => s.MaTaiKhoan == maTaiKhoan);
                if (sv != null)
                {
                    query = query.Where(l => l.HoSoHocBong.MaSinhVien == sv.MaSinhVien);
                }
                else
                {
                    query = query.Where(l => false);
                }
            }

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var kw = searchString.Trim();
                query = query.Where(l => l.HoSoHocBong.SinhVien!.HoTen.Contains(kw) ||
                                         l.HoSoHocBong.HocBong!.TenHocBong.Contains(kw) ||
                                         l.NguoiPhongVan.Contains(kw) ||
                                         (l.DiaDiemHoacLienKet != null && l.DiaDiemHoacLienKet.Contains(kw)));
            }

            if (!string.IsNullOrWhiteSpace(trangThaiFilter))
            {
                query = query.Where(l => l.TrangThai == trangThaiFilter);
            }

            int pageSize = 10;
            int totalItems = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            if (totalPages == 0) totalPages = 1;
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            var items = await query
                .OrderByDescending(l => l.ThoiGianBatDau)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentTrangThai = trangThaiFilter;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;
            ViewBag.IsStudent = isStudent;

            return View(items);
        }

        // ====================================================================
        // GET: LichPhongVan/Create (Mục 9.2: Lập lịch phỏng vấn)
        // ====================================================================
        [RoleAuthorize("Admin", "Cán bộ học bổng")]
        public async Task<IActionResult> Create(int? maHoSo)
        {
            var eligibleApplications = await _context.HoSoHocBong
                .Include(hs => hs.SinhVien)
                .Include(hs => hs.HocBong)
                .Where(hs => hs.TrangThai == "Đạt xét duyệt sơ bộ" || hs.TrangThai == "Chờ phỏng vấn")
                .OrderByDescending(hs => hs.NgayXuLy ?? hs.NgayNop)
                .ToListAsync();

            if (maHoSo.HasValue)
            {
                var targetApp = await _context.HoSoHocBong
                    .Include(hs => hs.SinhVien)
                    .Include(hs => hs.HocBong)
                    .FirstOrDefaultAsync(hs => hs.MaHoSoHocBong == maHoSo.Value);

                if (targetApp != null && !eligibleApplications.Any(hs => hs.MaHoSoHocBong == maHoSo.Value))
                {
                    eligibleApplications.Insert(0, targetApp);
                }
            }

            ViewBag.MaHoSoHocBong = new SelectList(
                eligibleApplications.Select(hs => new
                {
                    hs.MaHoSoHocBong,
                    DisplayText = $"#{hs.MaHoSoHocBong} - {hs.SinhVien?.HoTen} ({hs.HocBong?.TenHocBong}) [{hs.TrangThai}]"
                }), "MaHoSoHocBong", "DisplayText", maHoSo);

            ViewBag.HasEligibleApplications = eligibleApplications.Any();

            var defaultStart = DateTime.Today.AddDays(2).AddHours(9);
            return View(new LichPhongVan
            {
                MaHoSoHocBong = maHoSo ?? (eligibleApplications.FirstOrDefault()?.MaHoSoHocBong ?? 0),
                ThoiGianBatDau = defaultStart,
                ThoiGianKetThuc = defaultStart.AddHours(1),
                HinhThucPhongVan = "Trực tiếp",
                DiaDiemHoacLienKet = "Phòng Hội đồng A1 - UNETI",
                NguoiPhongVan = "Hội đồng xét duyệt Phòng CTSV",
                TrangThai = "Đã lên lịch"
            });
        }

        // ====================================================================
        // POST: LichPhongVan/Create (Thuật toán kiểm tra chống trùng lịch bằng LINQ)
        // ====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "Cán bộ học bổng")]
        public async Task<IActionResult> Create([Bind("MaHoSoHocBong,ThoiGianBatDau,ThoiGianKetThuc,HinhThucPhongVan,DiaDiemHoacLienKet,NguoiPhongVan,GhiChu,TrangThai")] LichPhongVan lich)
        {
            ModelState.Remove("HoSoHocBong");
            ModelState.Remove("GhiChu");

            var hoSo = await _context.HoSoHocBong
                .Include(hs => hs.SinhVien)
                .FirstOrDefaultAsync(hs => hs.MaHoSoHocBong == lich.MaHoSoHocBong);

            if (hoSo == null)
            {
                ModelState.AddModelError("MaHoSoHocBong", "Vui lòng chọn hồ sơ ứng tuyển.");
            }
            else
            {
                // Kiểm tra 1: Chỉ hồ sơ Đạt xét duyệt sơ bộ hoặc Chờ phỏng vấn mới được lập lịch phỏng vấn
                if (hoSo.TrangThai != "Đạt xét duyệt sơ bộ" && hoSo.TrangThai != "Chờ phỏng vấn")
                {
                    ModelState.AddModelError("MaHoSoHocBong", $"Hồ sơ #{hoSo.MaHoSoHocBong} hiện ở trạng thái '{hoSo.TrangThai}', chỉ hồ sơ 'Đạt xét duyệt sơ bộ' mới được phép lập lịch phỏng vấn.");
                }

                // Kiểm tra 2: Hồ sơ chưa có một lịch phỏng vấn đang hiệu lực khác
                bool hasActiveSchedule = await _context.LichPhongVan.AnyAsync(l =>
                    l.MaHoSoHocBong == hoSo.MaHoSoHocBong &&
                    l.TrangThai != "Đã hủy");
                if (hasActiveSchedule)
                {
                    ModelState.AddModelError("MaHoSoHocBong", "Hồ sơ này đã có một lịch phỏng vấn đang hiệu lực.");
                }

                // Kiểm tra 3: Thời gian phỏng vấn không được nằm trong quá khứ & kết thúc phải sau bắt đầu
                if (lich.ThoiGianBatDau < DateTime.Now)
                {
                    ModelState.AddModelError("ThoiGianBatDau", "Thời gian phỏng vấn không được nằm trong quá khứ tại thời điểm lập lịch.");
                }

                if (lich.ThoiGianKetThuc <= lich.ThoiGianBatDau)
                {
                    ModelState.AddModelError("ThoiGianKetThuc", "Thời gian kết thúc phỏng vấn phải sau thời gian bắt đầu.");
                }

                // Kiểm tra 4: THUẬT TOÁN LINQ KIỂM TRA TRÙNG LỊCH CHO SINH VIÊN
                // Khoảng thời gian [StartA, EndA] trùng với [StartB, EndB] khi: StartA < EndB && EndA > StartB
                bool isStudentConflict = await _context.LichPhongVan
                    .Include(l => l.HoSoHocBong)
                    .AnyAsync(l =>
                        l.HoSoHocBong.MaSinhVien == hoSo.MaSinhVien &&
                        l.TrangThai != "Đã hủy" &&
                        l.ThoiGianBatDau < lich.ThoiGianKetThuc &&
                        l.ThoiGianKetThuc > lich.ThoiGianBatDau);

                if (isStudentConflict)
                {
                    ModelState.AddModelError("ThoiGianBatDau", $"Sinh viên '{hoSo.SinhVien?.HoTen}' đã có một lịch phỏng vấn khác bị trùng khoảng thời gian này.");
                }

                // Kiểm tra 5: THUẬT TOÁN LINQ KIỂM TRA TRÙNG LỊCH CHO NGƯỜI PHỎNG VẤN
                if (!string.IsNullOrWhiteSpace(lich.NguoiPhongVan))
                {
                    bool isInterviewerConflict = await _context.LichPhongVan.AnyAsync(l =>
                        l.NguoiPhongVan == lich.NguoiPhongVan &&
                        l.TrangThai != "Đã hủy" &&
                        l.ThoiGianBatDau < lich.ThoiGianKetThuc &&
                        l.ThoiGianKetThuc > lich.ThoiGianBatDau);

                    if (isInterviewerConflict)
                    {
                        ModelState.AddModelError("NguoiPhongVan", $"Người/Hội đồng phỏng vấn '{lich.NguoiPhongVan}' đã có một lịch khác bị trùng khoảng thời gian này.");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                if (string.IsNullOrWhiteSpace(lich.TrangThai))
                {
                    lich.TrangThai = "Đã lên lịch";
                }

                lich.GhiChu = string.IsNullOrWhiteSpace(lich.GhiChu) ? string.Empty : lich.GhiChu.Trim();
                lich.DiaDiemHoacLienKet = string.IsNullOrWhiteSpace(lich.DiaDiemHoacLienKet) ? "Trực tiếp" : lich.DiaDiemHoacLienKet.Trim();
                lich.HinhThucPhongVan = string.IsNullOrWhiteSpace(lich.HinhThucPhongVan) ? "Trực tiếp" : lich.HinhThucPhongVan.Trim();
                lich.NguoiPhongVan = string.IsNullOrWhiteSpace(lich.NguoiPhongVan) ? "Hội đồng xét duyệt" : lich.NguoiPhongVan.Trim();

                _context.Add(lich);

                // Tự động chuyển trạng thái hồ sơ sang "Chờ phỏng vấn"
                if (hoSo != null)
                {
                    hoSo.TrangThai = "Chờ phỏng vấn";
                }

                await _context.SaveChangesAsync();

                if (!string.IsNullOrWhiteSpace(hoSo?.SinhVien?.Email))
                {
                    string subject = $"[UNETI] Giấy báo lịch phỏng vấn xét học bổng #{lich.MaHoSoHocBong}";
                    string message = $"<p>Kính gửi sinh viên <strong>{hoSo.SinhVien?.HoTen}</strong>,</p>" +
                                     $"<p>Bạn đã được xếp lịch phỏng vấn cho hồ sơ học bổng <strong>#{hoSo.MaHoSoHocBong}</strong> với thông tin chi tiết như sau:</p>" +
                                     $"<ul>" +
                                     $"<li><strong>Thời gian:</strong> {lich.ThoiGianBatDau:HH:mm dd/MM/yyyy} - {lich.ThoiGianKetThuc:HH:mm dd/MM/yyyy}</li>" +
                                     $"<li><strong>Hình thức:</strong> {lich.HinhThucPhongVan}</li>" +
                                     $"<li><strong>Địa điểm / Đường dẫn:</strong> {lich.DiaDiemHoacLienKet}</li>" +
                                     $"<li><strong>Hội đồng phỏng vấn:</strong> {lich.NguoiPhongVan}</li>" +
                                     $"<li><strong>Ghi chú:</strong> {(string.IsNullOrWhiteSpace(lich.GhiChu) ? "Không có" : lich.GhiChu)}</li>" +
                                     $"</ul>" +
                                     $"<p>Vui lòng có mặt đúng giờ và chuẩn bị đầy đủ hồ sơ/minh chứng theo yêu cầu.</p>" +
                                     $"<p>Trân trọng,<br/>Phòng Công tác sinh viên - Đại học Kinh tế - Kỹ thuật Công nghiệp (UNETI)</p>";
                    await _emailService.SendEmailAsync(hoSo.SinhVien.Email, subject, message);
                }

                TempData["SuccessMessage"] = $"Lập lịch phỏng vấn thành công cho hồ sơ #{lich.MaHoSoHocBong}!";
                return RedirectToAction(nameof(Index));
            }

            var eligibleApplications = await _context.HoSoHocBong
                .Include(hs => hs.SinhVien)
                .Include(hs => hs.HocBong)
                .Where(hs => hs.TrangThai == "Đạt xét duyệt sơ bộ" || hs.TrangThai == "Chờ phỏng vấn" || hs.MaHoSoHocBong == lich.MaHoSoHocBong)
                .ToListAsync();

            ViewBag.MaHoSoHocBong = new SelectList(
                eligibleApplications.Select(hs => new
                {
                    hs.MaHoSoHocBong,
                    DisplayText = $"#{hs.MaHoSoHocBong} - {hs.SinhVien?.HoTen} ({hs.HocBong?.TenHocBong}) [{hs.TrangThai}]"
                }), "MaHoSoHocBong", "DisplayText", lich.MaHoSoHocBong);

            ViewBag.HasEligibleApplications = eligibleApplications.Any();
            return View(lich);
        }

        // ====================================================================
        // POST: LichPhongVan/HoanThanh/5 (Mục 9.3: Cập nhật sau phỏng vấn)
        // ====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "Cán bộ học bổng")]
        public async Task<IActionResult> HoanThanh(int id)
        {
            var lich = await _context.LichPhongVan
                .Include(l => l.HoSoHocBong)
                .FirstOrDefaultAsync(l => l.MaLichPhongVan == id);

            if (lich == null) return NotFound();

            if (lich.TrangThai == "Đã hủy")
            {
                TempData["ErrorMessage"] = "Không thể hoàn thành một lịch phỏng vấn đã bị hủy.";
                return RedirectToAction(nameof(Index));
            }

            lich.TrangThai = "Đã hoàn thành";

            // Cập nhật trạng thái hồ sơ thành "Đã phỏng vấn" theo Mục 9.3 doc.docx
            if (lich.HoSoHocBong != null)
            {
                lich.HoSoHocBong.TrangThai = "Đã phỏng vấn";
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã xác nhận hoàn thành buổi phỏng vấn. Hồ sơ #{lich.MaHoSoHocBong} đã chuyển sang trạng thái 'Đã phỏng vấn' và sẵn sàng nhập kết quả!";
            return RedirectToAction(nameof(Index));
        }

        // ====================================================================
        // POST: LichPhongVan/HuyLich/5
        // ====================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RoleAuthorize("Admin", "Cán bộ học bổng")]
        public async Task<IActionResult> HuyLich(int id)
        {
            var lich = await _context.LichPhongVan
                .Include(l => l.HoSoHocBong)
                    .ThenInclude(hs => hs.SinhVien)
                .Include(l => l.HoSoHocBong)
                    .ThenInclude(hs => hs.HocBong)
                .FirstOrDefaultAsync(l => l.MaLichPhongVan == id);

            if (lich != null)
            {
                lich.TrangThai = "Đã hủy";
                if (lich.HoSoHocBong != null && lich.HoSoHocBong.TrangThai == "Chờ phỏng vấn")
                {
                    lich.HoSoHocBong.TrangThai = "Đạt xét duyệt sơ bộ";
                }
                await _context.SaveChangesAsync();

                var svEmail = lich.HoSoHocBong?.SinhVien?.Email;
                if (!string.IsNullOrWhiteSpace(svEmail))
                {
                    string subject = $"[UNETI] THÔNG BÁO: Hủy / Thay đổi lịch phỏng vấn học bổng #{lich.MaHoSoHocBong}";
                    string message = $@"
                        <div style='background: #e11d48; color: #ffffff; padding: 16px 20px; border-radius: 8px; margin-bottom: 20px; text-align: center;'>
                            <h2 style='margin: 0; font-size: 18px; font-weight: 700;'>THÔNG BÁO HỦY / THAY ĐỔI LỊCH PHỎNG VẤN</h2>
                        </div>
                        <p>Kính gửi sinh viên <strong>{lich.HoSoHocBong?.SinhVien?.HoTen}</strong>,</p>
                        <p>Hội đồng xét duyệt học bổng UNETI xin thông báo: Buổi phỏng vấn dự kiến vào lúc <strong>{lich.ThoiGianBatDau:HH:mm dd/MM/yyyy}</strong> cho chương trình <strong>{lich.HoSoHocBong?.HocBong?.TenHocBong}</strong> đã được hủy hoặc điều chỉnh kế hoạch.</p>
                        <p>Hồ sơ của bạn hiện được chuyển lại trạng thái <em>Đạt xét duyệt sơ bộ</em> để Hội đồng sắp xếp lịch phỏng vấn mới phù hợp hơn.</p>
                        <p>Trân trọng,<br/><strong>Phòng Công tác sinh viên - Trường ĐH Kinh tế - Kỹ thuật Công nghiệp (UNETI)</strong></p>";
                    await _emailService.SendEmailAsync(svEmail, subject, message);
                }

                TempData["SuccessMessage"] = "Đã hủy lịch phỏng vấn thành công.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

