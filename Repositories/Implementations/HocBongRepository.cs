using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.Repositories.Abstractions;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Repositories.Implementations
{
    public class HocBongRepository(QuanLyHocBong_UNETI2_TI17A3HNContext context)
        : GenericRepository<HocBong>(context), IHocBongRepository
    {
        public IQueryable<HocBong> QueryActiveWithSponsor(bool asNoTracking = true)
        {
            var query = asNoTracking ? _context.HocBong.AsNoTracking() : _context.HocBong.AsQueryable();
            return query
                .Where(h => !h.DaXoa)
                .Include(h => h.DonViTaiTro);
        }

        public Task<HocBong?> GetByIdWithSponsorAsync(int maHocBong) =>
            _context.HocBong
                .Include(h => h.DonViTaiTro)
                .FirstOrDefaultAsync(h => h.MaHocBong == maHocBong && !h.DaXoa);

        public Task<HocBong?> GetActiveByIdAsync(int maHocBong) =>
            _context.HocBong
                .FirstOrDefaultAsync(h => h.MaHocBong == maHocBong && !h.DaXoa);

        public Task<List<string>> GetDistinctBacDaoTaoAsync() =>
            _context.HocBong
                .AsNoTracking()
                .Where(h => !h.DaXoa)
                .Select(h => h.BacDaoTao)
                .Distinct()
                .OrderBy(value => value)
                .ToListAsync();
    }
}

