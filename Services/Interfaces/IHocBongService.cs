using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.ViewModels.HocBong;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Services.Interface
{
    public interface IHocBongService
    {
        Task<HocBongIndexViewModel> LayDanhSachAsync(HocBongIndexViewModel filters);
        Task<List<DonViTaiTro>> LayDanhSachDonViTaiTroAsync();
        Task<HocBong?> LayTheoMaAsync(int maHocBong);
        Task<int> DemSoHoSoAsync(int maHocBong);
        Task<(bool ThanhCong, string? Loi)> TaoAsync(HocBong hocBong);
        Task<(bool ThanhCong, string? Loi)> CapNhatAsync(int maHocBong, HocBong hocBong);
        Task<(bool ThanhCong, string? Loi)> XoaAsync(int maHocBong);
    }
}
