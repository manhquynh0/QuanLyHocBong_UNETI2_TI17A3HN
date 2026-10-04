using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Models
{
    public class HocBong
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaHocBong { get; set; }

        [Required(ErrorMessage = "Tên học bổng là bắt buộc")]
        [StringLength(200)]
        [Column(TypeName = "nvarchar(200)")]
        public string TenHocBong { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn Đơn vị tài trợ")]
        [ForeignKey("DonViTaiTro")]
        public int MaDonViTaiTro { get; set; }

        [Required(ErrorMessage = "Số suất là bắt buộc")]
        [Range(1, int.MaxValue, ErrorMessage = "Số suất phải lớn hơn 0")]
        public int SoSuat { get; set; }

        [Required(ErrorMessage = "Bậc đào tạo là bắt buộc")]
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string BacDaoTao { get; set; }

        [Required(ErrorMessage = "Điểm trung bình tối thiểu là bắt buộc")]
        [Range(0, 4, ErrorMessage = "Điểm trung bình tích lũy tối thiểu phải từ 0 đến 4")]
        [Column(TypeName = "decimal(4,2)")]
        public decimal DiemTrungBinhToiThieu { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu nhận hồ sơ là bắt buộc")]
        [DataType(DataType.Date)]
        public DateTime NgayBatDauNhanHoSo { get; set; }

        [Required(ErrorMessage = "Hạn nộp hồ sơ là bắt buộc")]
        [DataType(DataType.Date)]
        public DateTime HanNopHoSo { get; set; }

        [StringLength(2000)]
        [Column(TypeName = "nvarchar(2000)")]
        public string MoTaHocBong { get; set; }

        [StringLength(2000)]
        [Column(TypeName = "nvarchar(2000)")]
        public string DieuKienXetHocBong { get; set; }

        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        [StringLength(50)]
        [Column(TypeName = "nvarchar(50)")]
        public string TrangThai { get; set; }

        public virtual DonViTaiTro DonViTaiTro { get; set; }

        public virtual ICollection<HoSoHocBong> HoSoHocBongs { get; set; }

    }
}
