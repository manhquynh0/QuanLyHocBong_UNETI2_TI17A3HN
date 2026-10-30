using QuanLyHocBong_UNETI2_TI17A3HN.Models;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Services.Interface;

public interface ITaiKhoanService
{
    Task<SinhVien?> GetSinhVienByTaiKhoanAsync(int maTaiKhoan);
    Task<HocBong?> GetHocBongByIdAsync(int maHocBong);
}
