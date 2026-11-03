using System.Collections.Generic;
using System.Threading.Tasks;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Services
{
    public interface INotificationService
    {
        /// <summary>
        /// Gửi thông báo tới 1 tài khoản cụ thể (lưu Database + push realtime qua SignalR)
        /// </summary>
        Task<ThongBao?> GuiThongBaoAsync(int maTaiKhoan, string tieuDe, string noiDung, string loaiThongBao, string? lienKet = null, int? maHoSoHocBong = null);

        /// <summary>
        /// Gửi thông báo tới toàn bộ Cán bộ học bổng và Quản trị viên (Admin)
        /// </summary>
        Task GuiThongBaoChoCanBoAsync(string tieuDe, string noiDung, string loaiThongBao, string? lienKet = null, int? maHoSoHocBong = null);

        /// <summary>
        /// Lấy danh sách thông báo mới nhất của người dùng
        /// </summary>
        Task<List<ThongBao>> LayDanhSachAsync(int maTaiKhoan, int top = 15);

        /// <summary>
        /// Đếm số lượng thông báo chưa đọc
        /// </summary>
        Task<int> DemChuaDocAsync(int maTaiKhoan);

        /// <summary>
        /// Đánh dấu 1 thông báo là đã đọc (bảo mật kiểm tra quyền sở hữu)
        /// </summary>
        Task<bool> DanhDauDaDocAsync(int maThongBao, int maTaiKhoan);

        /// <summary>
        /// Đánh dấu toàn bộ thông báo của người dùng là đã đọc
        /// </summary>
        Task<bool> DanhDauTatCaDaDocAsync(int maTaiKhoan);
    }
}

