// Họ và tên: Nguyễn Phi Hùng
// Mã sinh viên: 23103100148
// Nội dung thực hiện: Module 3 - ViewModel hiển thị danh sách tệp đính kèm hồ sơ (cho sinh viên và cán bộ).
using System.Globalization;

namespace QuanLyHocBong_UNETI2_TI17A3HN.ViewModels
{
    public class TaiLieuItem
    {
        public int MaTaiLieu { get; set; }
        public string LoaiTaiLieu { get; set; } = string.Empty;
        public string TenFileGoc { get; set; } = string.Empty;
        public string LoaiNoiDung { get; set; } = string.Empty;
        public long DungLuong { get; set; }
        public DateTime NgayTai { get; set; }

        public bool LaPdf => LoaiNoiDung == "application/pdf";
        public string DungLuongHienThi => DinhDang(DungLuong);

        public static string DinhDang(long bytes) =>
            bytes >= 1024 * 1024
                ? (bytes / 1048576.0).ToString("0.0", CultureInfo.InvariantCulture) + " MB"
                : Math.Max(1, (long)Math.Round(bytes / 1024.0)).ToString(CultureInfo.InvariantCulture) + " KB";
    }

    public class TaiLieuHoSoListViewModel
    {
        public int MaHoSoHocBong { get; set; }
        public List<TaiLieuItem> Items { get; set; } = new();
        public bool CoTheThem { get; set; }     // sinh viên được thêm tệp (hồ sơ còn Chờ duyệt)
        public bool CoTheXoa { get; set; }      // sinh viên được xóa tệp (hồ sơ còn Chờ duyệt)
    }

    public class TaiLieuHoSoNhanVienViewModel
    {
        public int MaHoSoHocBong { get; set; }
        public int MaSinhVien { get; set; }
        public string HoTenSinhVien { get; set; } = string.Empty;
        public string TenHocBong { get; set; } = string.Empty;
        public string TrangThai { get; set; } = string.Empty;
        public DateTime NgayNop { get; set; }
        public TaiLieuHoSoListViewModel TaiLieu { get; set; } = new();
    }
}
