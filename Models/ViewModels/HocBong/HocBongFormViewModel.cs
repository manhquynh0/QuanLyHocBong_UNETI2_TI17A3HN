using System.ComponentModel.DataAnnotations;

namespace QuanLyHocBong_UNETI2_TI17A3HN.ViewModels.HocBong
{
    public class HocBongFormViewModel
    {
        public int MaHocBong { get; set; }

        [Required(ErrorMessage = "Tên học bổng là bắt buộc")]
        [StringLength(200)]
        public string TenHocBong { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn đơn vị tài trợ")]
        public int MaDonViTaiTro { get; set; }

        [Required(ErrorMessage = "Số suất là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Số suất phải lớn hơn 0")]
        public int SoSuat { get; set; }

        [Required(ErrorMessage = "Bậc đào tạo là bắt buộc")]
        [StringLength(100)]
        public string BacDaoTao { get; set; } = string.Empty;

        [Range(typeof(decimal), "0", "4", ErrorMessage = "Điểm trung bình tích lũy tối thiểu phải từ 0 đến 4")]
        public decimal DiemTrungBinhToiThieu { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu nhận hồ sơ là bắt buộc")]
        [DataType(DataType.Date)]
        public DateTime? NgayBatDauNhanHoSo { get; set; }

        [Required(ErrorMessage = "Hạn nộp hồ sơ là bắt buộc")]
        [DataType(DataType.Date)]
        public DateTime? HanNopHoSo { get; set; }

        [StringLength(2000)]
        public string? MoTaHocBong { get; set; }

        [StringLength(2000)]
        public string? DieuKienXetHocBong { get; set; }

    }
}
