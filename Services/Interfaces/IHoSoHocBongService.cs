using QuanLyHocBong_UNETI2_TI17A3HN.Common;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.ViewModels;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Services.Interface
{
    public interface IHoSoHocBongService
    {
        Task<ApiResponse<PagedResult<HoSoItemViewModel>>> LayDanhSachAsync(HoSoFilterViewModel filter);
        Task<HoSoChiTietViewModel?> LayChiTietAsync(int maHoSo);
        Task<XetDuyetSoBoViewModel?> LayFormXetDuyetAsync(int maHoSo);
        Task<ApiResponse<bool>> XetDuyetSoBoAsync(XetDuyetSoBoViewModel model);
        Task<int> DemSoDaCapAsync(int maHocBong);
        Task<List<DonViTaiTro>> LayDanhSachDonViTaiTroAsync();
        Task<int> LaySoSuatHocBongAsync(int maHocBong);
        Task<List<HoSoHocBong>> LayHoSoCuaSinhVienAsync(int maSinhVien);
        Task<ApiResponse<bool>> NopHoSoAsync(int maSinhVien, int maHocBong, string thuXin, string ghiChu, string? fileUrl, HocBong hocBong, SinhVien sinhVien);
        Task<ApiResponse<bool>> HuyHoSoAsync(int maHoSo, int maSinhVien);
    }
}