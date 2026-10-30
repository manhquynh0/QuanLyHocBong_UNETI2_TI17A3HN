using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.ViewModels;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Repositories.Interfaces
{
    public interface IHoSoHocBongRepository
    {
        Task<HoSoHocBong?> GetByIdAsync(int id);
        Task<HoSoHocBong?> GetByIdWithDetailsAsync(int id);
        Task<List<HoSoHocBong>> GetBySinhVienIdAsync(int maSinhVien);
        Task<bool> CheckDaNopHoSoAsync(int maSinhVien, int maHocBong);
        Task<int> CountDaCapByHocBongAsync(int maHocBong);

        Task AddAsync(HoSoHocBong hoSo);
        Task UpdateAsync(HoSoHocBong hoSo);

        IQueryable<HoSoHocBong> GetQueryableWithDetails();

        Task<List<DonViTaiTro>> GetDanhSachDonViVaitro();
        Task<int> GetSoSuatHocBongAsync(int maHocBong);

    }
}