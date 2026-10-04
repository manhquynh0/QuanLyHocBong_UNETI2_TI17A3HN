using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Models
{
    public class KetQuaHocBong
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaKetQuaHocBong { get; set; }

        [Required(ErrorMessage = "Mã hồ sơ là bắt buộc")]
        [ForeignKey("HoSoHocBong")]
        public int MaHoSoHocBong { get; set; }

        [Required(ErrorMessage = "Kết quả là bắt buộc")]
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string KetQua { get; set; }

        [Range(0, 100, ErrorMessage = "Điểm đánh giá phải nằm trong khoảng từ 0 đến 100")]
        [Column(TypeName = "decimal(5,2)")]
        public decimal? DiemDanhGia { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime NgayCapNhat { get; set; }

        [StringLength(1000)]
        [Column(TypeName = "nvarchar(1000)")]
        public string NhanXet { get; set; }

        public virtual HoSoHocBong HoSoHocBong { get; set; }
    }
}
