// Họ và tên: Phan Sỹ Tú
// Mã sinh viên: 23103100145
// Nội dung thực hiện: Module 4 - Interface service xử lý nghiệp vụ hồ sơ học bổng.

using QuanLyHocBong_UNETI2_TI17A3HN.ViewModels;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Services.Interface
{
    public interface IHoSoHocBongService
    {
        /// <summary>
        /// Lấy danh sách hồ sơ có tìm kiếm, lọc, sắp xếp, phân trang.
        /// </summary>
        Task<HoSoFilterViewModel> LayDanhSachAsync(HoSoFilterViewModel filter);

        /// <summary>
        /// Lấy chi tiết hồ sơ theo mã.
        /// </summary>
        Task<HoSoChiTietViewModel?> LayChiTietAsync(int maHoSo);

        /// <summary>
        /// Lấy dữ liệu form xét duyệt sơ bộ.
        /// </summary>
        Task<XetDuyetSoBoViewModel?> LayFormXetDuyetAsync(int maHoSo);

        /// <summary>
        /// Xử lý xét duyệt sơ bộ. Trả về (thành công, thông báo lỗi).
        /// </summary>
        Task<(bool ThanhCong, string? Loi)> XetDuyetSoBoAsync(XetDuyetSoBoViewModel model);

        /// <summary>
        /// Đếm số hồ sơ đã được cấp học bổng theo học bổng.
        /// </summary>
        Task<int> DemSoDaCapAsync(int maHocBong);
    }
}