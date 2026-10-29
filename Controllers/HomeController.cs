// Họ và tên: Vũ Mạnh Quỳnh
// Nội dung thực hiện: Module 5 - Controller tổng quan hệ thống, thống kê 9 số liệu Dashboard bằng LINQ.

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.Models.ViewModels;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Controllers
{
    public class HomeController : Controller
    {
        private readonly QuanLyHocBong_UNETI2_TI17A3HNContext _context;

        public HomeController(QuanLyHocBong_UNETI2_TI17A3HNContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var maTaiKhoanSession = HttpContext.Session.GetInt32("MaTaiKhoan");
            if (!maTaiKhoanSession.HasValue)
            {
                return RedirectToAction("DangNhap", "TaiKhoan");
            }

            var role = HttpContext.Session.GetString("VaiTro") ?? "Sinh viên";
            ViewBag.UserRole = role;
            ViewBag.UserName = HttpContext.Session.GetString("HoTen") ?? "Người dùng";

            bool isStudent = role.Equals("Sinh viên", StringComparison.OrdinalIgnoreCase)
                || role.StartsWith("Sinh", StringComparison.OrdinalIgnoreCase);

            // Nếu là Sinh viên, chuyển đến Dashboard sinh viên với thông tin cá nhân và hồ sơ riêng
            if (isStudent)
            {
                var maTaiKhoan = maTaiKhoanSession;

                var sinhVien = await _context.SinhVien
                    .Include(s => s.TaiKhoan)
                    .FirstOrDefaultAsync(s => s.MaTaiKhoan == maTaiKhoan.Value);

                if (sinhVien == null)
                {
                    var taiKhoan = await _context.TaiKhoan.FindAsync(maTaiKhoan.Value);
                    sinhVien = new SinhVien
                    {
                        MaTaiKhoan = maTaiKhoan.Value,
                        HoTen = taiKhoan?.HoTen ?? "Sinh viên",
                        Email = taiKhoan?.Email ?? "",
                        NgaySinh = new DateTime(2004, 1, 1),
                        GioiTinh = "Nam",
                        SoDienThoai = "0980000000",
                        DiaChi = "Hà Nội",
                        BacDaoTao = "Đại học",
                        Nganh = "Công nghệ thông tin",
                        DiemTrungBinhTichLuy = 3.2m,
                        ThanhTichHocTap = "Chưa cập nhật",
                        TrangThai = "Đang học"
                    };
                    _context.SinhVien.Add(sinhVien);
                    await _context.SaveChangesAsync();
                    HttpContext.Session.SetInt32("MaSinhVien", sinhVien.MaSinhVien);
                }

                var tongHoSoDaNop = await _context.HoSoHocBong.CountAsync(hs => hs.MaSinhVien == sinhVien.MaSinhVien);
                var hoSoDangXetDuyet = await _context.HoSoHocBong.CountAsync(hs =>
                    hs.MaSinhVien == sinhVien.MaSinhVien &&
                    (hs.TrangThai == "Chờ duyệt" || hs.TrangThai == "Đang xét duyệt" || hs.TrangThai == "Đạt xét duyệt sơ bộ" || hs.TrangThai == "Chờ phỏng vấn"));
                var hocBongDuocCap = await _context.HoSoHocBong.CountAsync(hs => hs.MaSinhVien == sinhVien.MaSinhVien && hs.TrangThai == "Được cấp học bổng");
                var svLichPVSapToi = await _context.LichPhongVan.CountAsync(l =>
                    l.HoSoHocBong != null &&
                    l.HoSoHocBong.MaSinhVien == sinhVien.MaSinhVien &&
                    l.TrangThai == "Đã lên lịch" &&
                    l.ThoiGianBatDau >= DateTime.Now);

                var danhSachHoSo = await _context.HoSoHocBong
                    .Include(hs => hs.HocBong)
                        .ThenInclude(h => h.DonViTaiTro)
                    .Include(hs => hs.KetQuaHocBong)
                    .Include(hs => hs.LichPhongVans)
                    .Where(hs => hs.MaSinhVien == sinhVien.MaSinhVien)
                    .OrderByDescending(hs => hs.NgayNop)
                    .Take(5)
                    .ToListAsync();

                var lichPhongVanList = await _context.LichPhongVan
                    .Include(l => l.HoSoHocBong)
                        .ThenInclude(hs => hs!.HocBong)
                    .Where(l => l.HoSoHocBong != null && l.HoSoHocBong.MaSinhVien == sinhVien.MaSinhVien && l.TrangThai != "Đã hủy")
                    .OrderBy(l => l.ThoiGianBatDau)
                    .Take(3)
                    .ToListAsync();

                var today = DateTime.Today;
                var hocBongDangMo = await _context.HocBong
                    .Include(h => h.DonViTaiTro)
                    .Where(h => !h.DaXoa && h.TrangThai == "Đang nhận hồ sơ" && h.HanNopHoSo >= today && h.NgayBatDauNhanHoSo <= today)
                    .OrderBy(h => h.HanNopHoSo)
                    .Take(4)
                    .ToListAsync();

                var svModel = new StudentDashboardViewModel
                {
                    SinhVien = sinhVien,
                    TongHoSoDaNop = tongHoSoDaNop,
                    HoSoDangXetDuyet = hoSoDangXetDuyet,
                    HocBongDuocCap = hocBongDuocCap,
                    LichPhongVanSapToi = svLichPVSapToi,
                    DanhSachHoSo = danhSachHoSo,
                    LichPhongVanList = lichPhongVanList,
                    HocBongDangMo = hocBongDangMo
                };

                return View("StudentDashboard", svModel);
            }

            // 1. Tính toán 9 chỉ số tổng hợp theo Mục 9.5 doc.docx bằng LINQ
            var tongDonViTaiTro = await _context.DonViTaiTro.CountAsync();
            var tongHocBong = await _context.HocBong.CountAsync(h => !h.DaXoa);
            var hocBongDangNhan = await _context.HocBong.CountAsync(h => !h.DaXoa && h.TrangThai == "Đang nhận hồ sơ");
            var tongSinhVien = await _context.SinhVien.CountAsync();
            var tongHoSo = await _context.HoSoHocBong.CountAsync();
            var hoSoChoDuyet = await _context.HoSoHocBong.CountAsync(hs => hs.TrangThai == "Chờ duyệt");
            var hoSoDatSoBo = await _context.HoSoHocBong.CountAsync(hs => hs.TrangThai == "Đạt xét duyệt sơ bộ");
            var lichPVSapToi = await _context.LichPhongVan.CountAsync(l => l.TrangThai == "Đã lên lịch");
            var hoSoDuocCap = await _context.HoSoHocBong.CountAsync(hs => hs.TrangThai == "Được cấp học bổng");

            // 2. Thống kê tỷ lệ trạng thái hồ sơ bằng LINQ
            var trangThaiGroup = await _context.HoSoHocBong
                .GroupBy(hs => hs.TrangThai)
                .Select(g => new
                {
                    TrangThai = g.Key,
                    SoLuong = g.Count()
                })
                .ToListAsync();

            var colorMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Chờ duyệt", "#3b82f6" },
                { "Đang xét duyệt", "#3b82f6" },
                { "Đạt xét duyệt sơ bộ", "#10b981" },
                { "Không đạt xét duyệt sơ bộ", "#ef4444" },
                { "Chờ phỏng vấn", "#8b5cf6" },
                { "Đã phỏng vấn", "#06b6d4" },
                { "Được cấp học bổng", "#059669" },
                { "Không được cấp học bổng", "#dc2626" },
                { "Đã hủy", "#6b7280" }
            };

            var phanBoTrangThai = new List<StatusDistributionStat>();
            if (tongHoSo > 0)
            {
                foreach (var item in trangThaiGroup)
                {
                    phanBoTrangThai.Add(new StatusDistributionStat
                    {
                        TenTrangThai = item.TrangThai,
                        SoLuong = item.SoLuong,
                        TiLePhanTram = Math.Round((double)item.SoLuong / tongHoSo * 100, 1),
                        MauSac = colorMap.TryGetValue(item.TrangThai, out var c) ? c : "#6366f1"
                    });
                }
            }

            // 3. Thống kê hồ sơ nộp theo tháng từ Database bằng LINQ (Mục 9.5 & 22.8)
            var ngayNopList = await _context.HoSoHocBong
                .Select(hs => hs.NgayNop)
                .ToListAsync();

            var hoSoTheoThang = ngayNopList
                .GroupBy(d => new { d.Year, d.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .TakeLast(6)
                .Select(g => new MonthlyApplicationStat
                {
                    Thang = $"T{g.Key.Month:D2}/{g.Key.Year}",
                    SoLuong = g.Count()
                })
                .ToList();

            // 4. Lấy danh sách hồ sơ gần đây bằng LINQ
            var hoSoGanDay = await _context.HoSoHocBong
                .Include(hs => hs.SinhVien)
                .Include(hs => hs.HocBong)
                .OrderByDescending(hs => hs.NgayNop)
                .Take(5)
                .Select(hs => new HoSoGanDayItem
                {
                    MaHoSo = hs.MaHoSoHocBong,
                    TenSinhVien = hs.SinhVien != null ? hs.SinhVien.HoTen : "Chưa xác định",
                    TenHocBong = hs.HocBong != null ? hs.HocBong.TenHocBong : "Chưa xác định",
                    NgayNop = hs.NgayNop,
                    TrangThai = hs.TrangThai
                })
                .ToListAsync();

            var viewModel = new DashboardViewModel
            {
                TongDonViTaiTro = tongDonViTaiTro,
                TongHocBong = tongHocBong,
                HocBongDangNhanHoSo = hocBongDangNhan,
                TongSinhVien = tongSinhVien,
                TongHoSo = tongHoSo,
                HoSoChoDuyet = hoSoChoDuyet,
                HoSoDatSoBo = hoSoDatSoBo,
                LichPhongVanSapToi = lichPVSapToi,
                HoSoDuocCapHocBong = hoSoDuocCap,
                PhanBoTrangThai = phanBoTrangThai,
                HoSoTheoThang = hoSoTheoThang,
                HoSoGanDay = hoSoGanDay
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
