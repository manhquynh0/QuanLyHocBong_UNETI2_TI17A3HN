// ============================================================================
// ĐỀ TÀI 22: XÂY DỰNG HỆ THỐNG QUẢN LÝ HỌC BỔNG VÀ HỒ SƠ XÉT HỌC BỔNG
// NHÓM 2 - LỚP TI17A3HN - UNETI
// MODULE 5: BÁO CÁO THỐNG KÊ 10 CHỈ TIÊU BẰNG LINQ & XUẤT DỮ LIỆU EXCEL/CSV
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Attributes;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Models.ViewModels;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Controllers
{
    [RoleAuthorize("Admin", "Cán bộ học bổng")]
    public class ThongKeController : Controller
    {
        private readonly QuanLyHocBong_UNETI2_TI17A3HNContext _context;

        public ThongKeController(QuanLyHocBong_UNETI2_TI17A3HNContext context)
        {
            _context = context;
        }

        // ====================================================================
        // GET: ThongKe/Index (10 chỉ tiêu LINQ theo mục 9.6 doc.docx)
        // ====================================================================
        public async Task<IActionResult> Index()
        {
            var vm = new ThongKeViewModel();

            // 1. Số học bổng theo đơn vị tài trợ (LINQ GroupBy)
            vm.HocBongTheoDonVi = await _context.HocBong
                .GroupBy(h => h.DonViTaiTro!.TenDonViTaiTro)
                .Select(g => new ThongKeItem
                {
                    Ten = g.Key ?? "Chưa rõ",
                    SoLuong = g.Count()
                })
                .OrderByDescending(x => x.SoLuong)
                .ToListAsync();

            // 2. Số hồ sơ theo từng học bổng (LINQ GroupBy)
            vm.HoSoTheoHocBong = await _context.HoSoHocBong
                .GroupBy(hs => hs.HocBong!.TenHocBong)
                .Select(g => new ThongKeItem
                {
                    Ten = g.Key ?? "Chưa rõ",
                    SoLuong = g.Count()
                })
                .OrderByDescending(x => x.SoLuong)
                .Take(8)
                .ToListAsync();

            // 3. Số hồ sơ theo trạng thái (LINQ GroupBy)
            vm.HoSoTheoTrangThai = await _context.HoSoHocBong
                .GroupBy(hs => hs.TrangThai)
                .Select(g => new ThongKeItem
                {
                    Ten = g.Key,
                    SoLuong = g.Count()
                })
                .OrderByDescending(x => x.SoLuong)
                .ToListAsync();

            // 4. Số hồ sơ đạt xét duyệt sơ bộ theo học bổng (LINQ Where + GroupBy)
            vm.HoSoDatSoBoTheoHocBong = await _context.HoSoHocBong
                .Where(hs => hs.TrangThai == "Đạt xét duyệt sơ bộ" || hs.TrangThai == "Chờ phỏng vấn" || hs.TrangThai == "Đã phỏng vấn" || hs.TrangThai == "Được cấp học bổng")
                .GroupBy(hs => hs.HocBong!.TenHocBong)
                .Select(g => new ThongKeItem
                {
                    Ten = g.Key ?? "Chưa rõ",
                    SoLuong = g.Count()
                })
                .OrderByDescending(x => x.SoLuong)
                .Take(8)
                .ToListAsync();

            // 5. Học bổng nhận nhiều hồ sơ nhất (LINQ GroupBy + OrderByDescending + FirstOrDefault)
            var mostPopular = await _context.HoSoHocBong
                .GroupBy(hs => hs.HocBong!.TenHocBong)
                .Select(g => new
                {
                    TenHocBong = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .FirstOrDefaultAsync();

            if (mostPopular != null)
            {
                vm.HocBongNhieuHoSoNhat = mostPopular.TenHocBong ?? "Chưa rõ";
                vm.SoHoSoNhieuNhat = mostPopular.Count;
            }

            // 6. Số hồ sơ được cấp học bổng theo đơn vị tài trợ (LINQ Where + GroupBy)
            vm.HoSoCapTheoDonVi = await _context.HoSoHocBong
                .Where(hs => hs.TrangThai == "Được cấp học bổng")
                .GroupBy(hs => hs.HocBong!.DonViTaiTro!.TenDonViTaiTro)
                .Select(g => new ThongKeItem
                {
                    Ten = g.Key ?? "Chưa rõ",
                    SoLuong = g.Count()
                })
                .OrderByDescending(x => x.SoLuong)
                .ToListAsync();

            // 7. Số hồ sơ nộp theo tháng (LINQ GroupBy năm + tháng)
            var allApps = await _context.HoSoHocBong.Select(h => h.NgayNop).ToListAsync();
            vm.HoSoTheoThang = allApps
                .GroupBy(d => new { d.Year, d.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new ThongKeItem
                {
                    Ten = $"T{g.Key.Month:D2}/{g.Key.Year}",
                    SoLuong = g.Count()
                })
                .ToList();

            // 8. Số lịch phỏng vấn theo tháng
            var allSchedules = await _context.LichPhongVan.Select(l => l.ThoiGianBatDau).ToListAsync();
            vm.LichPVTheoThang = allSchedules
                .GroupBy(d => new { d.Year, d.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new ThongKeItem
                {
                    Ten = $"T{g.Key.Month:D2}/{g.Key.Year}",
                    SoLuong = g.Count()
                })
                .ToList();

            // 9. Tỷ lệ hồ sơ qua xét duyệt sơ bộ theo từng học bổng
            var allHocBongs = await _context.HocBong.Include(h => h.HoSoHocBongs).ToListAsync();
            vm.TyLeDatSoBoTheoHocBong = allHocBongs
                .Where(h => h.HoSoHocBongs.Any())
                .Select(h =>
                {
                    int total = h.HoSoHocBongs.Count;
                    int passed = h.HoSoHocBongs.Count(hs => hs.TrangThai != "Chờ duyệt" && hs.TrangThai != "Không đạt xét duyệt sơ bộ" && hs.TrangThai != "Đã hủy");
                    return new TyLeItem
                    {
                        TenHocBong = h.TenHocBong,
                        TongHoSo = total,
                        SoDatSoBo = passed,
                        TyLePhanTram = total > 0 ? Math.Round((double)passed / total * 100, 1) : 0
                    };
                })
                .OrderByDescending(x => x.TyLePhanTram)
                .Take(8)
                .ToList();

            // 10. Tỷ lệ hồ sơ được cấp học bổng trên số hồ sơ đã phỏng vấn
            vm.TongDaPhongVan = await _context.HoSoHocBong.CountAsync(hs => hs.TrangThai == "Đã phỏng vấn" || hs.TrangThai == "Được cấp học bổng" || hs.TrangThai == "Không được cấp học bổng");
            vm.TongDuocCap = await _context.HoSoHocBong.CountAsync(hs => hs.TrangThai == "Được cấp học bổng");
            vm.TyLeCapTrenPhongVan = vm.TongDaPhongVan > 0 ? Math.Round((double)vm.TongDuocCap / vm.TongDaPhongVan * 100, 1) : 0;

            return View(vm);
        }

        // ====================================================================
        // GET: ThongKe/ExportCsv (Chức năng nâng cao: Xuất báo cáo CSV / Excel)
        // ====================================================================
        public async Task<IActionResult> ExportCsv()
        {
            var apps = await _context.HoSoHocBong
                .Include(hs => hs.SinhVien)
                .Include(hs => hs.HocBong)
                    .ThenInclude(hb => hb.DonViTaiTro)
                .Include(hs => hs.KetQuaHocBong)
                .OrderByDescending(hs => hs.NgayNop)
                .ToListAsync();

            var sb = new StringBuilder();
            // Header
            sb.AppendLine("MaHoSo,SinhVien,Email,Nganh,GPA,HocBong,DonViTaiTro,NgayNop,TrangThai,DiemDanhGia,KetQua");

            foreach (var a in apps)
            {
                var row = $"\"{a.MaHoSoHocBong}\",\"{a.SinhVien?.HoTen}\",\"{a.SinhVien?.Email}\",\"{a.SinhVien?.Nganh}\",\"{a.SinhVien?.DiemTrungBinhTichLuy}\",\"{a.HocBong?.TenHocBong}\",\"{a.HocBong?.DonViTaiTro?.TenDonViTaiTro}\",\"{a.NgayNop:yyyy-MM-dd}\",\"{a.TrangThai}\",\"{a.KetQuaHocBong?.DiemDanhGia}\",\"{a.KetQuaHocBong?.KetQua}\"";
                sb.AppendLine(row);
            }

            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv", $"BaoCao_HocBong_UNETI_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }
    }
}

