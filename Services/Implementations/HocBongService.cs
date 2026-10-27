using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Constants;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.Services.Interface;
using QuanLyHocBong_UNETI2_TI17A3HN.ViewModels.HocBong;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Services.Implementations
{
    public class HocBongService : IHocBongService
    {
        private const int KichThuocTrang = 10;
        private readonly QuanLyHocBong_UNETI2_TI17A3HNContext _context;

        public HocBongService(QuanLyHocBong_UNETI2_TI17A3HNContext context)
        {
            _context = context;
        }

        public async Task<HocBongIndexViewModel> LayDanhSachAsync(HocBongIndexViewModel filters)
        {
            var query = _context.HocBong
                .AsNoTracking()
                .Where(h => !h.DaXoa)
                .Include(h => h.DonViTaiTro)
                .AsQueryable();

            var tuKhoa = filters.TuKhoa?.Trim();
            if (!string.IsNullOrEmpty(tuKhoa))
            {
                query = query.Where(h =>
                    h.TenHocBong.Contains(tuKhoa)
                    || h.DonViTaiTro.TenDonViTaiTro.Contains(tuKhoa));
                filters.TuKhoa = tuKhoa;
            }

            if (filters.MaDonViTaiTro.HasValue)
            {
                query = query.Where(h => h.MaDonViTaiTro == filters.MaDonViTaiTro.Value);
            }

            if (!string.IsNullOrWhiteSpace(filters.BacDaoTao))
            {
                query = query.Where(h => h.BacDaoTao == filters.BacDaoTao);
            }

            var today = DateTime.Today;
            filters.TrangThai = filters.TrangThai is TrangThaiHocBong.SapMo
                or TrangThaiHocBong.DangNhanHoSo
                or TrangThaiHocBong.DaDong
                    ? filters.TrangThai
                    : null;

            if (filters.TrangThai == TrangThaiHocBong.SapMo)
            {
                query = query.Where(h => h.NgayBatDauNhanHoSo > today);
            }
            else if (filters.TrangThai == TrangThaiHocBong.DangNhanHoSo)
            {
                query = query.Where(h => h.NgayBatDauNhanHoSo <= today && h.HanNopHoSo >= today);
            }
            else if (filters.TrangThai == TrangThaiHocBong.DaDong)
            {
                query = query.Where(h => h.HanNopHoSo < today);
            }

            if (filters.DiemToiThieu is >= 0 and <= 4)
            {
                query = query.Where(h => h.DiemTrungBinhToiThieu >= filters.DiemToiThieu.Value);
            }

            if (filters.HanNop == "con-han")
            {
                query = query.Where(h => h.HanNopHoSo >= today);
            }
            else if (filters.HanNop == "het-han")
            {
                query = query.Where(h => h.HanNopHoSo < today);
            }

            filters.SapXep = filters.SapXep switch
            {
                "ten-az" or "ten-za" or "han-tang" or "han-giam" or "suat-tang" or "suat-giam"
                    => filters.SapXep,
                _ => "ma-tang"
            };

            query = filters.SapXep switch
            {
                "ten-az" => query.OrderBy(h => h.TenHocBong).ThenBy(h => h.MaHocBong),
                "ten-za" => query.OrderByDescending(h => h.TenHocBong).ThenBy(h => h.MaHocBong),
                "han-tang" => query.OrderBy(h => h.HanNopHoSo).ThenBy(h => h.MaHocBong),
                "han-giam" => query.OrderByDescending(h => h.HanNopHoSo).ThenBy(h => h.MaHocBong),
                "suat-tang" => query.OrderBy(h => h.SoSuat).ThenBy(h => h.MaHocBong),
                "suat-giam" => query.OrderByDescending(h => h.SoSuat).ThenBy(h => h.MaHocBong),
                _ => query.OrderBy(h => h.MaHocBong)
            };

            filters.SoKetQua = await query.CountAsync();
            filters.KichThuocTrang = KichThuocTrang;
            filters.TongSoTrang = Math.Max(1, (int)Math.Ceiling(filters.SoKetQua / (double)KichThuocTrang));
            filters.Trang = Math.Clamp(filters.Trang, 1, filters.TongSoTrang);
            filters.HocBongs = await query
                .Skip((filters.Trang - 1) * KichThuocTrang)
                .Take(KichThuocTrang)
                .ToListAsync();
            foreach (var hocBong in filters.HocBongs)
            {
                hocBong.TrangThai = TrangThaiHocBong.TinhTrang(
                    hocBong.NgayBatDauNhanHoSo,
                    hocBong.HanNopHoSo,
                    today);
            }

            await PopulateFilterOptionsAsync(filters);
            return filters;
        }

        public Task<List<DonViTaiTro>> LayDanhSachDonViTaiTroAsync()
        {
            return _context.DonViTaiTro
                .AsNoTracking()
                .OrderBy(d => d.TenDonViTaiTro)
                .ToListAsync();
        }

        public async Task<HocBong?> LayTheoMaAsync(int maHocBong)
        {
            var hocBong = await _context.HocBong
                .Include(h => h.DonViTaiTro)
                .FirstOrDefaultAsync(h => h.MaHocBong == maHocBong && !h.DaXoa);
            if (hocBong is not null)
            {
                hocBong.TrangThai = TrangThaiHocBong.TinhTrang(
                    hocBong.NgayBatDauNhanHoSo,
                    hocBong.HanNopHoSo);
            }

            return hocBong;
        }

        public Task<int> DemSoHoSoAsync(int maHocBong)
        {
            return _context.HoSoHocBong.CountAsync(h => h.MaHocBong == maHocBong);
        }

        public async Task<(bool ThanhCong, string? Loi)> TaoAsync(HocBong hocBong)
        {
            var loi = await KiemTraDuLieuAsync(hocBong);
            if (loi is not null)
            {
                return (false, loi);
            }

            hocBong.TrangThai = TrangThaiHocBong.TinhTrang(
                hocBong.NgayBatDauNhanHoSo,
                hocBong.HanNopHoSo);
            _context.HocBong.Add(hocBong);
            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool ThanhCong, string? Loi)> CapNhatAsync(int maHocBong, HocBong hocBong)
        {
            var hocBongHienTai = await _context.HocBong
                .FirstOrDefaultAsync(h => h.MaHocBong == maHocBong && !h.DaXoa);
            if (hocBongHienTai is null)
            {
                return (false, "Không tìm thấy học bổng cần cập nhật.");
            }

            var loi = await KiemTraDuLieuAsync(hocBong);
            if (loi is not null)
            {
                return (false, loi);
            }

            var coHoSoDangKy = await _context.HoSoHocBong
                .AnyAsync(h => h.MaHocBong == maHocBong);
            if (coHoSoDangKy
                && (hocBong.NgayBatDauNhanHoSo.Date != hocBongHienTai.NgayBatDauNhanHoSo.Date
                    || hocBong.HanNopHoSo.Date != hocBongHienTai.HanNopHoSo.Date))
            {
                return (false, "Không thể thay đổi thời gian nhận hồ sơ vì học bổng đã có hồ sơ đăng ký.");
            }

            var soHoSoDuocCap = await _context.HoSoHocBong
                .CountAsync(h => h.MaHocBong == maHocBong
                    && h.TrangThai == TrangThaiHoSo.DuocCapHocBong);
            if (hocBong.SoSuat < soHoSoDuocCap)
            {
                return (false, $"Số suất không thể nhỏ hơn {soHoSoDuocCap}, là số hồ sơ đã được cấp học bổng.");
            }

            hocBongHienTai.TenHocBong = hocBong.TenHocBong;
            hocBongHienTai.MaDonViTaiTro = hocBong.MaDonViTaiTro;
            hocBongHienTai.SoSuat = hocBong.SoSuat;
            hocBongHienTai.BacDaoTao = hocBong.BacDaoTao;
            hocBongHienTai.DiemTrungBinhToiThieu = hocBong.DiemTrungBinhToiThieu;
            hocBongHienTai.NgayBatDauNhanHoSo = hocBong.NgayBatDauNhanHoSo;
            hocBongHienTai.HanNopHoSo = hocBong.HanNopHoSo;
            hocBongHienTai.MoTaHocBong = hocBong.MoTaHocBong;
            hocBongHienTai.DieuKienXetHocBong = hocBong.DieuKienXetHocBong;
            hocBongHienTai.TrangThai = TrangThaiHocBong.TinhTrang(
                hocBong.NgayBatDauNhanHoSo,
                hocBong.HanNopHoSo);

            await _context.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool ThanhCong, string? Loi)> XoaAsync(int maHocBong)
        {
            var hocBong = await _context.HocBong
                .FirstOrDefaultAsync(h => h.MaHocBong == maHocBong && !h.DaXoa);
            if (hocBong is null)
            {
                return (false, "Không tìm thấy học bổng cần xóa.");
            }

            hocBong.DaXoa = true;
            await _context.SaveChangesAsync();
            return (true, null);
        }

        private async Task<string?> KiemTraDuLieuAsync(HocBong hocBong)
        {
            if (hocBong.HanNopHoSo.Date < hocBong.NgayBatDauNhanHoSo.Date)
            {
                return "Hạn nộp hồ sơ không thể trước ngày bắt đầu nhận hồ sơ.";
            }

            if (!await _context.DonViTaiTro.AnyAsync(d => d.MaDonViTaiTro == hocBong.MaDonViTaiTro))
            {
                return "Đơn vị tài trợ đã chọn không tồn tại.";
            }

            return null;
        }

        private async Task PopulateFilterOptionsAsync(HocBongIndexViewModel model)
        {
            model.DonViTaiTroOptions = await _context.DonViTaiTro
                .AsNoTracking()
                .OrderBy(d => d.TenDonViTaiTro)
                .Select(d => new SelectListItem
                {
                    Value = d.MaDonViTaiTro.ToString(),
                    Text = d.TenDonViTaiTro
                })
                .ToListAsync();
            model.BacDaoTaoOptions = await _context.HocBong
                .AsNoTracking()
                .Where(h => !h.DaXoa)
                .Select(h => h.BacDaoTao)
                .Distinct()
                .OrderBy(value => value)
                .Select(value => new SelectListItem { Value = value, Text = value })
                .ToListAsync();
            model.TrangThaiOptions = new[]
                {
                    TrangThaiHocBong.SapMo,
                    TrangThaiHocBong.DangNhanHoSo,
                    TrangThaiHocBong.DaDong
                }
                .Select(value => new SelectListItem { Value = value, Text = value })
                .ToList();
        }
    }
}
