// Họ và tên: Phan Sỹ Tú
// Mã sinh viên: 23103100145
// Nội dung thực hiện: Module 4 - ViewModel cho một dòng hồ sơ trong danh sách.

namespace QuanLyHocBong_UNETI2_TI17A3HN.ViewModels
{
    public class HoSoItemViewModel
    {
        public int MaHoSoHocBong { get; set; }
        public string TenSinhVien { get; set; } = string.Empty;
        public string TenHocBong { get; set; } = string.Empty;
        public string TenDonViTaiTro { get; set; } = string.Empty;
        public DateTime NgayNop { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public decimal DiemTrungBinh { get; set; }
        public decimal DiemYeuCau { get; set; }

        // Cờ tiện dụng cho View
        public bool DuDiem => DiemTrungBinh >= DiemYeuCau;
        public bool CoTheXetDuyet =>
            TrangThai == Constants.TrangThaiHoSo.ChoDuyet;
    }
}