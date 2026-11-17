using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Constants;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.Repositories.Abstractions;
using QuanLyHocBong_UNETI2_TI17A3HN.Services;
using QuanLyHocBong_UNETI2_TI17A3HN.ViewModels;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Repositories.Implementations
{
    public class HoSoHocBongRepository(QuanLyHocBong_UNETI2_TI17A3HNContext context)
        : GenericRepository<HoSoHocBong>(context), IHoSoHocBongRepository
    {
        public IQueryable<HoSoHocBong> QueryWithDetails(bool asNoTracking = true)
        {
            var query = asNoTracking ? _context.HoSoHocBong.AsNoTracking() : _context.HoSoHocBong.AsQueryable();
            return query
                .Include(h => h.SinhVien)
                .Include(h => h.HocBong)
                    .ThenInclude(hb => hb!.DonViTaiTro);
        }
        public async Task<HoSoFilterViewModel> GetDanhSachAsync(
            HoSoFilterViewModel filter)
        {
            var query = QueryWithDetails();

            // Tìm kiếm theo tên SV hoặc tên học bổng
            if (!string.IsNullOrWhiteSpace(filter.TuKhoa))
            {
                var tuKhoa = filter.TuKhoa.Trim();
                query = query.Where(h =>
                    h.SinhVien.HoTen.Contains(tuKhoa) ||
                    h.HocBong.TenHocBong.Contains(tuKhoa));
            }
            // Lọc theo đơn vị tài trợ
            if (filter.MaDonViTaiTro.HasValue)
                query = query.Where(h => h.HocBong.MaDonViTaiTro == filter.MaDonViTaiTro.Value);

            // Lọc theo trạng thái
            if (!string.IsNullOrWhiteSpace(filter.TrangThai))
                query = query.Where(h => h.TrangThai == filter.TrangThai);

            // Lọc theo ngày nộp
            if (filter.TuNgay.HasValue)
                query = query.Where(h => h.NgayNop >= filter.TuNgay.Value);
            if (filter.DenNgay.HasValue)
            {
                // Bao gồm cả ngày kết thúc (đến 23:59:59)
                var denNgay = filter.DenNgay.Value.Date.AddDays(1).AddSeconds(-1);
                query = query.Where(h => h.NgayNop <= denNgay);
            }
            // Sắp xếp
            query = filter.SapXep switch
            {
                "ngay_nop_asc" => query.OrderBy(h => h.NgayNop),
                "ngay_nop_desc" => query.OrderByDescending(h => h.NgayNop),
                "ten_sv_asc" => query.OrderBy(h => h.SinhVien.HoTen),
                "ten_sv_desc" => query.OrderByDescending(h => h.SinhVien.HoTen),
                _ => query.OrderByDescending(h => h.NgayNop)
            };
            // Phân trang
            int pageSize = filter.KichThuocTrang > 0 ? filter.KichThuocTrang : 10;
            int page = filter.Trang > 0 ? filter.Trang : 1;
            int total = await query.CountAsync();

            filter.TongSoBanGhi = total;
            filter.TongSoTrang = (int)Math.Ceiling((double)total / pageSize);
            if (filter.TongSoTrang == 0) filter.TongSoTrang = 1;
            filter.KichThuocTrang = pageSize;

            var taiLieuQuery = _context.TaiLieuHoSo.AsNoTracking();

            filter.DanhSach = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(h => new HoSoItemViewModel
                {
                    MaHoSoHocBong = h.MaHoSoHocBong,
                    TenSinhVien = h.SinhVien.HoTen,
                    TenHocBong = h.HocBong.TenHocBong,
                    TenDonViTaiTro = h.HocBong.DonViTaiTro.TenDonViTaiTro,
                    NgayNop = h.NgayNop,
                    TrangThai = h.TrangThai,
                    DiemTrungBinh = h.SinhVien.DiemTrungBinhTichLuy,
                    DiemYeuCau = h.HocBong.DiemTrungBinhToiThieu,
                    SoTaiLieu = taiLieuQuery.Count(t => t.MaHoSoHocBong == h.MaHoSoHocBong),
                    FileMinhChung = h.FileMinhChung
                })
                .ToListAsync();

            return filter;
        }

        public Task<HoSoHocBong?> GetByIdWithDetailsAsync(int maHoSo) =>
            _context.HoSoHocBong
                .Include(h => h.SinhVien)
                .Include(h => h.HocBong)
                    .ThenInclude(hb => hb!.DonViTaiTro)
                .FirstOrDefaultAsync(h => h.MaHoSoHocBong == maHoSo);

        public Task<int> CountByHocBongAsync(int maHocBong) =>
            _context.HoSoHocBong.CountAsync(h => h.MaHocBong == maHocBong);

        public Task<int> CountApprovedByHocBongAsync(int maHocBong) =>
            _context.HoSoHocBong.CountAsync(h =>
                h.MaHocBong == maHocBong && h.TrangThai == TrangThaiHoSo.DuocCapHocBong);

        public Task<List<TaiLieuItem>> GetTaiLieuItemsAsync(int maHoSo) =>
            TaiLieuHoSoStorage.DanhSachAsync(_context, maHoSo);
    }
}

