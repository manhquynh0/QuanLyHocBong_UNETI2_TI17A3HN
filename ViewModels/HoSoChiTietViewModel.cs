// Họ và tên: Phan Sỹ Tú
// Mã sinh viên: 23103100145
// Nội dung thực hiện: Module 4 - ViewModel chi tiết hồ sơ,
// gộp thông tin sinh viên, học bổng, đơn vị tài trợ và so sánh điều kiện.

namespace QuanLyHocBong_UNETI2_TI17A3HN.ViewModels
{
    public class HoSoChiTietViewModel
    {
        // Thông tin hồ sơ
        public int MaHoSoHocBong { get; set; }
        public DateTime NgayNop { get; set; }
        public string? ThuXinHocBong { get; set; }
        public string? GhiChu { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public DateTime? NgayXuLy { get; set; }
        public string? NhanXetXetDuyet { get; set; }
        public string? FileMinhChung { get; set; }
        public List<TaiLieuItem> TaiLieus { get; set; } = new();

        // Thông tin sinh viên
        public int MaSinhVien { get; set; }
        public string HoTenSinhVien { get; set; } = string.Empty;
        public DateTime NgaySinh { get; set; }
        public string GioiTinh { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string EmailSinhVien { get; set; } = string.Empty;
        public string? DiaChi { get; set; }
        public string BacDaoTaoSinhVien { get; set; } = string.Empty;
        public string Nganh { get; set; } = string.Empty;
        public decimal DiemTrungBinhTichLuy { get; set; }
        public string? ThanhTichHocTap { get; set; }

        // Thông tin học bổng 
        public int MaHocBong { get; set; }
        public string TenHocBong { get; set; } = string.Empty;
        public int SoSuat { get; set; }
        public string BacDaoTaoHocBong { get; set; } = string.Empty;
        public decimal DiemTrungBinhToiThieu { get; set; }
        public DateTime NgayBatDauNhanHoSo { get; set; }
        public DateTime HanNopHoSo { get; set; }
        public string? DieuKienXetHocBong { get; set; }

        // Thông tin đơn vị tài trợ 
        public string TenDonViTaiTro { get; set; } = string.Empty;

        // So sánh điều kiện 
        public bool DuDiem => DiemTrungBinhTichLuy >= DiemTrungBinhToiThieu;
        public bool DungBacDaoTao => BacDaoTaoSinhVien == BacDaoTaoHocBong;

        // Số suất
        public int SoDaCap { get; set; }
        public int SoSuatConLai => Math.Max(0, SoSuat - SoDaCap);
        public bool DaDatSuat => SoDaCap >= SoSuat;

        // Quyền hành động (View dùng để ẩn/hiện nút) 
        public bool CoTheXetDuyet =>
            TrangThai == Constants.TrangThaiHoSo.ChoDuyet;
        public bool CoTheLapLichPhongVan =>
            TrangThai == Constants.TrangThaiHoSo.DatXetDuyetSoBo;
    }
}
