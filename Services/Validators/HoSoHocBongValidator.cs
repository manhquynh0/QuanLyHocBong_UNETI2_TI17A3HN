using QuanLyHocBong_UNETI2_TI17A3HN.Constants;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Services.Validators
{
    public class HoSoHocBongValidator
    {
        public (bool IsValid, string? ErrorMessage) ValidateXetDuyetSoBo(HoSoHocBong hoSo)
        {
            if (hoSo == null) return (false, "Hồ sơ không tồn tại.");
            if (hoSo.TrangThai == TrangThaiHoSo.DaHuy) return (false, "Hồ sơ đã bị hủy.");
            if (hoSo.TrangThai != TrangThaiHoSo.ChoDuyet) return (false, "Chỉ hồ sơ 'Chờ duyệt' mới được xét duyệt.");
            if (hoSo.SinhVien == null || hoSo.SinhVien.TrangThai != "Hoạt động") return (false, "Sinh viên không còn hoạt động.");
            return (true, null);
        }

        // Kiểm tra trước khi Nộp hồ sơ
        public (bool IsValid, string? ErrorMessage) ValidateNopHoSo(HocBong hocBong, SinhVien sinhVien)
        {
            if (hocBong.TrangThai != "Đang nhận hồ sơ" || hocBong.HanNopHoSo < DateTime.Today)
                return (false, "Học bổng đã đóng hoặc hết hạn nhận hồ sơ.");

            if (sinhVien.DiemTrungBinhTichLuy < hocBong.DiemTrungBinhToiThieu)
                return (false, $"Điểm GPA chưa đạt mức tối thiểu ({hocBong.DiemTrungBinhToiThieu}).");

            return (true, null);
        }

        // Kiểm tra chuyển trạng thái có đúng luồng không
        public (bool IsValid, string? ErrorMessage) ValidateTransition(string currentStatus, string targetStatus)
        {
            if (!TrangThaiHoSo.CoTheChuyen(currentStatus, targetStatus))
                return (false, $"Không thể chuyển từ '{currentStatus}' sang '{targetStatus}'.");

            return (true, null);
        }
        // kiem tra Huy ho so
        public (bool IsValid, string? ErrorMessage) ValidateHuyHoSo(HoSoHocBong hoSo, int maSinhVien)
        {
            if (hoSo == null || hoSo.MaSinhVien != maSinhVien)
                return (false, "Không tìm thấy hồ sơ hoặc bạn không có quyền hủy.");

            if (hoSo.TrangThai != TrangThaiHoSo.ChoDuyet)
                return (false, "Chỉ có thể hủy hồ sơ khi đang ở trạng thái 'Chờ duyệt'.");
            return (true, null);
        }

    }
}