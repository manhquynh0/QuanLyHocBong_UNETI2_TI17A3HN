using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

