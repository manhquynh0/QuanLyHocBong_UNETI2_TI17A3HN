// Họ và tên: Phan Sỹ Tú
// Mã sinh viên: 23103100145
// Nội dung thực hiện: Module 4 - ViewModel cho form xét duyệt sơ bộ hồ sơ.

using System;
using System.ComponentModel.DataAnnotations;

namespace QuanLyHocBong_UNETI2_TI17A3HN.ViewModels
{
    public class XetDuyetSoBoViewModel
    {
        [Required]
        public int MaHoSoHocBong { get; set; }

        // Thông tin hiển thị (readonly)
        public string TenSinhVien { get; set; } = string.Empty;
        public string TenHocBong { get; set; } = string.Empty;
        public string TenDonViTaiTro { get; set; } = string.Empty;
        public decimal DiemTrungBinh { get; set; }
        public decimal DiemYeuCau { get; set; }
        public string BacDaoTaoSinhVien { get; set; } = string.Empty;
        public string BacDaoTaoHocBong { get; set; } = string.Empty;
        public DateTime NgayNop { get; set; }
        public string? ThuXinHocBong { get; set; }
        public string? ThanhTichHocTap { get; set; }
        public string? FileMinhChung { get; set; }

        // Kết quả xét duyệt 
        [Required(ErrorMessage = "Vui lòng chọn kết quả xét duyệt")]
        [Display(Name = "Kết quả xét duyệt")]
        public string KetQua { get; set; } = string.Empty;
        // Giá trị: "Dat" hoặc "KhongDat"

        [Required(ErrorMessage = "Vui lòng nhập nhận xét xét duyệt")]
        [StringLength(1000, ErrorMessage = "Nhận xét không được vượt quá 1000 ký tự")]
        [Display(Name = "Nhận xét xét duyệt")]
        public string NhanXetXetDuyet { get; set; } = string.Empty;

        // Cờ cảnh báo cho View 
        public bool DuDiem => DiemTrungBinh >= DiemYeuCau;
        public bool DungBacDaoTao => BacDaoTaoSinhVien == BacDaoTaoHocBong;
    }
}
