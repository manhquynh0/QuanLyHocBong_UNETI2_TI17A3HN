// Họ và tên: Nguyễn Phi Hùng
// Mã sinh viên: 23103100148
// Nội dung thực hiện: Module 3 - danh sách và chi tiết học bổng phía sinh viên
// (tìm kiếm, lọc, sắp xếp, phân trang bằng LINQ trên truy vấn EF Core).
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Helpers;
using QuanLyHocBong_UNETI2_TI17A3HN.Services;
using QuanLyHocBong_UNETI2_TI17A3HN.ViewModels;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Controllers
{
    public class HocBongSinhVienController : SinhVienBaseController
    {
        private const int PageSize = 6;

        public HocBongSinhVienController(QuanLyHocBong_UNETI2_TI17A3HNContext context) : base(context) { }

        public async Task<IActionResult> Index(string? keyword, int? maDonViTaiTro, string? bacDaoTao,
            string? hanNop, bool phuHop = false, string? sort = "han_asc", int page = 1)
        {
            var sv = await LaySinhVienHienTaiAsync();
            var maSv = sv?.MaSinhVien ?? 0;
            var diemCuaToi = sv?.DiemTrungBinhTichLuy ?? 0m;
            var homNay = DateTime.Today;
            var dangXuLy = TrangThaiHoSo.DangXuLy;

            // 1. Truy vấn gốc: chỉ học bổng đang nhận hồ sơ
            var query = _context.HocBong.AsNoTracking()
                .Where(h => h.TrangThai == TrangThaiHocBong.DangNhanHoSo);

            // 2. Tìm kiếm theo tên học bổng / tên đơn vị tài trợ
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var k = keyword.Trim();
                query = query.Where(h => h.TenHocBong.Contains(k) || h.DonViTaiTro.TenDonViTaiTro.Contains(k));
            }

            // 3. Lọc (các điều kiện kết hợp được với nhau)
            if (maDonViTaiTro.HasValue)
                query = query.Where(h => h.MaDonViTaiTro == maDonViTaiTro.Value);
            if (!string.IsNullOrWhiteSpace(bacDaoTao))
                query = query.Where(h => h.BacDaoTao == bacDaoTao);
            if (hanNop == "conhan")
                query = query.Where(h => h.NgayBatDauNhanHoSo <= homNay && h.HanNopHoSo >= homNay);
            else if (hanNop == "hethan")
                query = query.Where(h => h.HanNopHoSo < homNay);
            if (phuHop)
                query = query.Where(h => h.DiemTrungBinhToiThieu <= diemCuaToi);

            // 4. Sắp xếp (thêm MaHocBong để thứ tự ổn định khi phân trang)
            query = sort switch
            {
                "ten_asc" => query.OrderBy(h => h.TenHocBong).ThenBy(h => h.MaHocBong),
                "ten_desc" => query.OrderByDescending(h => h.TenHocBong).ThenBy(h => h.MaHocBong),
                "han_desc" => query.OrderByDescending(h => h.HanNopHoSo).ThenBy(h => h.MaHocBong),
                "suat_asc" => query.OrderBy(h => h.SoSuat).ThenBy(h => h.MaHocBong),
                "suat_desc" => query.OrderByDescending(h => h.SoSuat).ThenBy(h => h.MaHocBong),
                _ => query.OrderBy(h => h.HanNopHoSo).ThenBy(h => h.MaHocBong)   // han_asc
            };
            sort = sort is "ten_asc" or "ten_desc" or "han_desc" or "suat_asc" or "suat_desc" ? sort : "han_asc";

            // 5. Phân trang trên truy vấn (Skip/Take), xử lý trang đầu/cuối
            var total = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
            page = Math.Clamp(page, 1, totalPages);

            var items = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .Select(h => new HocBongItemViewModel
                {
                    MaHocBong = h.MaHocBong,
                    TenHocBong = h.TenHocBong,
                    TenDonViTaiTro = h.DonViTaiTro.TenDonViTaiTro,
                    BacDaoTao = h.BacDaoTao,
                    SoSuat = h.SoSuat,
                    DiemTrungBinhToiThieu = h.DiemTrungBinhToiThieu,
                    NgayBatDauNhanHoSo = h.NgayBatDauNhanHoSo,
                    HanNopHoSo = h.HanNopHoSo,
                    MoTaHocBong = h.MoTaHocBong,
                    TrangThai = h.TrangThai,
                    DaNop = h.HoSoHocBongs.Any(hs => hs.MaSinhVien == maSv && dangXuLy.Contains(hs.TrangThai))
                })
                .ToListAsync();

            foreach (var item in items)
                item.DuDiem = diemCuaToi >= item.DiemTrungBinhToiThieu;

            var model = new HocBongSinhVienListViewModel
            {
                Items = items,
                Keyword = keyword,
                MaDonViTaiTro = maDonViTaiTro,
                BacDaoTao = bacDaoTao,
                HanNop = hanNop,
                PhuHop = phuHop,
                Sort = sort ?? "han_asc",
                Page = page,
                PageSize = PageSize,
                TotalItems = total,
                DiemCuaToi = diemCuaToi,
                CoHoSoCaNhan = sv != null,
                DonViTaiTros = await _context.DonViTaiTro.AsNoTracking()
                    .Where(d => d.TrangThai).OrderBy(d => d.TenDonViTaiTro).ToListAsync(),
                BacDaoTaos = await _context.HocBong.AsNoTracking()
                    .Select(h => h.BacDaoTao).Distinct().OrderBy(b => b).ToListAsync()
            };
            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var hb = await _context.HocBong.AsNoTracking()
                .Include(h => h.DonViTaiTro)
                .FirstOrDefaultAsync(h => h.MaHocBong == id && h.TrangThai != "Chưa mở");
            if (hb == null) return NotFound();

            var sv = await LaySinhVienHienTaiAsync();
            var model = new HocBongChiTietViewModel
            {
                HocBong = hb,
                LyDoKhongNop = await NopHoSoRules.KiemTraAsync(_context, sv, hb, DateTime.Now)
            };
            return View(model);
        }
    }
}
