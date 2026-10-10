// Họ và tên: Nguyễn Phi Hùng
// Mã sinh viên: 23103100148
// Nội dung thực hiện: Module 3 - ViewModel hiển thị hồ sơ đã nộp, lịch phỏng vấn và kết quả của sinh viên.
using QuanLyHocBong_UNETI2_TI17A3HN.Helpers;

namespace QuanLyHocBong_UNETI2_TI17A3HN.ViewModels
{
    public class LichPhongVanItem
    {
        public DateTime ThoiGianBatDau { get; set; }
        public DateTime ThoiGianKetThuc { get; set; }
        public string? HinhThucPhongVan { get; set; }
        public string? DiaDiemHoacLienKet { get; set; }
        public string? NguoiPhongVan { get; set; }
        public string? GhiChu { get; set; }
        public string TrangThai { get; set; } = string.Empty;
    }

    public class KetQuaItem
    {
        public string KetQua { get; set; } = string.Empty;
        public decimal? DiemDanhGia { get; set; }
        public string? NhanXet { get; set; }
        public DateTime NgayCapNhat { get; set; }
    }

    public class HoSoDaNopViewModel
    {
        public int MaHoSoHocBong { get; set; }
        public int MaHocBong { get; set; }
        public string TenHocBong { get; set; } = string.Empty;
        public string TenDonViTaiTro { get; set; } = string.Empty;
        public DateTime NgayNop { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public DateTime? NgayXuLy { get; set; }
        public string? NhanXetXetDuyet { get; set; }
        public string? ThuXinHocBong { get; set; }
        public string? GhiChu { get; set; }

        public LichPhongVanItem? LichPhongVan { get; set; }   // lịch chưa hủy gần nhất
        public KetQuaItem? KetQua { get; set; }

        // Chỉ hiện kết quả khi hồ sơ đã ở trạng thái cuối (đã công bố)
        public bool DaCongBoKetQua => KetQua != null &&
            (TrangThai == TrangThaiHoSo.DuocCap || TrangThai == TrangThaiHoSo.KhongDuocCap);

        // Hủy được khi: Chờ duyệt, hoặc Đạt sơ bộ nhưng chưa có lịch phỏng vấn còn hiệu lực
        public bool CoTheHuy => TrangThai == TrangThaiHoSo.ChoDuyet ||
            (TrangThai == TrangThaiHoSo.DatSoBo && LichPhongVan == null);
    }
}
