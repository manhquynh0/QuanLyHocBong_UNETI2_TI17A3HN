using QuanLyHocBong_UNETI2_TI17A3HN.Models;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Repositories.Interfaces
{
    public interface ITaiKhoanRepository
    {
        Task<SinhVien?> GetSinhVienByTaiKhoanAsync(int maTaiKhoan);


        //Xóa đi, vì đây đang áp dụng và có liên quan đến module của tôi nên viết chung vào đây.
        Task<HocBong?> GetHocBongByIdAsync(int maHocBong);
    }
}