using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Constants;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.Repositories.Interfaces;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Repositories.Implementations
{
    public class HoSoHocBongRepository : IHoSoHocBongRepository
    {
        private readonly QuanLyHocBong_UNETI2_TI17A3HNContext _context;

        public HoSoHocBongRepository(QuanLyHocBong_UNETI2_TI17A3HNContext context)
        {
            _context = context;
        }

        public async Task AddAsync(HoSoHocBong hoSo)
        {
            await _context.HoSoHocBong.AddAsync(hoSo);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> CheckDaNopHoSoAsync(int maSinhVien, int maHocBong)
        {
            return await _context.HoSoHocBong.AnyAsync(
                n => n.MaSinhVien == maSinhVien &&
                n.MaHocBong == maHocBong &&
                n.TrangThai != TrangThaiHoSo.DaHuy
            );

        }

        public async Task<int> CountDaCapByHocBongAsync(int maHocBong)
        {
            return await _context.HoSoHocBong.CountAsync(h =>
               h.MaHocBong == maHocBong &&
               h.TrangThai == TrangThaiHoSo.DuocCapHocBong);
        }

        public async Task<HoSoHocBong?> GetByIdAsync(int id)
        {
            return await _context.HoSoHocBong.FindAsync(id);
        }

        public async Task<HoSoHocBong?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.HoSoHocBong
            .Include(x => x.SinhVien)
            .Include(x => x.HocBong)
                .ThenInclude(hb => hb!.DonViTaiTro)
            .FirstOrDefaultAsync(h => h.MaHoSoHocBong == id);
        }

        public async Task<List<HoSoHocBong>> GetBySinhVienIdAsync(int maSinhVien)
        {
            return await _context.HoSoHocBong
                .Include(h => h.HocBong)
                    .ThenInclude(hb => hb!.DonViTaiTro)
                .Include(h => h.LichPhongVans)
                .Include(h => h.KetQuaHocBong)
                .Where(h => h.MaSinhVien == maSinhVien)
                .OrderByDescending(h => h.NgayNop)
                .ToListAsync();
        }

        public async Task<List<DonViTaiTro>> GetDanhSachDonViVaitro()
        {
            return await _context.DonViTaiTro
               .Where(d => d.TrangThai == true)
               .OrderBy(d => d.TenDonViTaiTro)
               .ToListAsync();
        }


        public IQueryable<HoSoHocBong> GetQueryableWithDetails()
        {
            return _context.HoSoHocBong
                .Include(h => h.SinhVien)
                .Include(h => h.HocBong)
                    .ThenInclude(hb => hb!.DonViTaiTro)
                .AsQueryable();

        }

        public async Task UpdateAsync(HoSoHocBong hoSo)
        {
            _context.HoSoHocBong.Update(hoSo);
            await _context.SaveChangesAsync();
        }
        public async Task<int> GetSoSuatHocBongAsync(int maHB)
        {
            var hb = await _context.HocBong.FindAsync(maHB);
            return hb?.SoSuat ?? 0;
        }
    }
}