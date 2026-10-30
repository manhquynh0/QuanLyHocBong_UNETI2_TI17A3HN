using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.Repositories.Interfaces;
using QuanLyHocBong_UNETI2_TI17A3HN.Services.Interface;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Services.Implementations;

public class TaiKhoanService : ITaiKhoanService
{
    public readonly ITaiKhoanRepository _taiKhoanRepo;
    public async Task<SinhVien?> GetSinhVienByTaiKhoanAsync(int maTaiKhoan)
    {
        return await _taiKhoanRepo.GetSinhVienByTaiKhoanAsync(maTaiKhoan);
    }

    public async Task<HocBong?> GetHocBongByIdAsync(int maHocBong)
    {
        return await _taiKhoanRepo.GetHocBongByIdAsync(maHocBong);
    }
}