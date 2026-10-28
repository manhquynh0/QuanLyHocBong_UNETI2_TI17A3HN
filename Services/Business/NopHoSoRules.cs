// Họ và tên: Nguyễn Phi Hùng
// Mã sinh viên: 23103100148
// Nội dung thực hiện: Module 3 - kiểm tra điều kiện nộp hồ sơ xin học bổng bằng EF Core/LINQ.
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Helpers;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Services
{
    public static class NopHoSoRules
    {
        /// <summary>Các thông tin cá nhân bắt buộc mà sinh viên chưa khai báo.</summary>
        public static List<string> ThongTinCaNhanConThieu(SinhVien sv)
        {
            var thieu = new List<string>();
            if (string.IsNullOrWhiteSpace(sv.HoTen)) thieu.Add("Họ và tên");
            if (sv.NgaySinh == default) thieu.Add("Ngày sinh");
            if (string.IsNullOrWhiteSpace(sv.GioiTinh)) thieu.Add("Giới tính");
            if (string.IsNullOrWhiteSpace(sv.SoDienThoai)) thieu.Add("Số điện thoại");
            if (string.IsNullOrWhiteSpace(sv.Email)) thieu.Add("Email");
            if (string.IsNullOrWhiteSpace(sv.BacDaoTao)) thieu.Add("Bậc đào tạo");
            if (string.IsNullOrWhiteSpace(sv.Nganh)) thieu.Add("Ngành học");
            return thieu;
        }

        /// <summary>
        /// Trả về danh sách lý do KHÔNG được nộp. Danh sách rỗng nghĩa là hợp lệ.
        /// </summary>
        public static async Task<List<string>> KiemTraAsync(
            QuanLyHocBong_UNETI2_TI17A3HNContext context, SinhVien? sv, HocBong? hb, DateTime now)
        {
            var loi = new List<string>();

            if (sv == null || sv.TrangThai != TrangThaiSinhVien.DangHoc)
                loi.Add("Sinh viên không tồn tại hoặc không còn ở trạng thái hoạt động.");

            if (hb == null)
            {
                loi.Add("Học bổng không tồn tại.");
                return loi;
            }

            if (hb.TrangThai != TrangThaiHocBong.DangNhanHoSo)
                loi.Add($"Học bổng đang ở trạng thái \"{hb.TrangThai}\", không nhận hồ sơ.");

            var homNay = now.Date;
            if (homNay < hb.NgayBatDauNhanHoSo.Date)
                loi.Add($"Chưa đến ngày nhận hồ sơ (bắt đầu từ {hb.NgayBatDauNhanHoSo:dd/MM/yyyy}).");
            if (homNay > hb.HanNopHoSo.Date)
                loi.Add($"Đã quá hạn nộp hồ sơ (hạn chót {hb.HanNopHoSo:dd/MM/yyyy}).");

            if (sv != null)
            {
                var thieu = ThongTinCaNhanConThieu(sv);
                if (thieu.Any())
                    loi.Add("Hồ sơ cá nhân chưa khai báo đủ: " + string.Join(", ", thieu) + ".");

                if (sv.DiemTrungBinhTichLuy < hb.DiemTrungBinhToiThieu)
                    loi.Add($"Điểm trung bình tích lũy của bạn ({sv.DiemTrungBinhTichLuy:0.00}) thấp hơn mức tối thiểu của học bổng ({hb.DiemTrungBinhToiThieu:0.00}).");

                var dangXuLy = TrangThaiHoSo.DangXuLy;
                var maSv = sv.MaSinhVien;
                var maHb = hb.MaHocBong;

                var daNop = await context.HoSoHocBong.AnyAsync(h =>
                    h.MaSinhVien == maSv && h.MaHocBong == maHb && dangXuLy.Contains(h.TrangThai));
                if (daNop)
                    loi.Add("Bạn đã có một hồ sơ đang xử lý cho học bổng này.");

                var daDuocCap = await context.HoSoHocBong.AnyAsync(h =>
                    h.MaSinhVien == maSv && h.MaHocBong == maHb && h.TrangThai == TrangThaiHoSo.DuocCap);
                if (daDuocCap)
                    loi.Add("Bạn đã được cấp học bổng này.");
            }

            return loi;
        }
    }
}
