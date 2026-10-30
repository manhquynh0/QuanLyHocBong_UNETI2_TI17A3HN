using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.Repositories.Interfaces;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Repositories.Implementations
{
    public class TaiKhoanRepository : ITaiKhoanRepository
    {
        private readonly QuanLyHocBong_UNETI2_TI17A3HNContext _context;

        public TaiKhoanRepository(QuanLyHocBong_UNETI2_TI17A3HNContext context)
        {
            _context = context;
        }

        public async Task<HocBong?> GetHocBongByIdAsync(int maHocBong)
        {
            return await _context.HocBong
            .Include(h => h.DonViTaiTro)
            .FirstOrDefaultAsync(h => h.MaHocBong == maHocBong);
        }

        public async Task<SinhVien?> GetSinhVienByTaiKhoanAsync(int maTaiKhoan)
        {
            return await _context.SinhVien.FirstOrDefaultAsync(s => s.MaTaiKhoan == maTaiKhoan);
        }
    }
}