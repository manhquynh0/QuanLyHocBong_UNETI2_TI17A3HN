using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Models
{
    public class SinhVien
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaSinhVien { get; set; }

        [Required]
        [ForeignKey("TaiKhoan")]
        public int MaTaiKhoan { get; set; }

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string HoTen { get; set; }

        [Required(ErrorMessage = "Ngày sinh là bắt buộc")]
        [DataType(DataType.Date)]
        public DateTime NgaySinh { get; set; }

        [Required(ErrorMessage = "Giới tính là bắt buộc")]
        [StringLength(10)]
        [Column(TypeName = "nvarchar(10)")]
        public string GioiTinh { get; set; }

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(20)]
        [Column(TypeName = "varchar(20)")]
        public string SoDienThoai { get; set; }

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [StringLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string Email { get; set; }

        [StringLength(500)]
        [Column(TypeName = "nvarchar(500)")]
        public string DiaChi { get; set; }

        [Required(ErrorMessage = "Bậc đào tạo là bắt buộc")]
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string BacDaoTao { get; set; }

        [Required(ErrorMessage = "Ngành học là bắt buộc")]
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string Nganh { get; set; }

        [Required(ErrorMessage = "Điểm trung bình tích lũy là bắt buộc")]
        [Range(0, 4, ErrorMessage = "Điểm trung bình tích lũy phải từ 0 đến 4")]
        [Column(TypeName = "decimal(4,2)")]
        public decimal DiemTrungBinhTichLuy { get; set; }

        [StringLength(1000)]
        [Column(TypeName = "nvarchar(1000)")]
        public string ThanhTichHocTap { get; set; }

        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        [StringLength(50)]
        [Column(TypeName = "nvarchar(50)")]
        public string TrangThai { get; set; }

        public virtual TaiKhoan TaiKhoan { get; set; }

        public virtual ICollection<HoSoHocBong> HoSoHocBongs { get; set; }
    }
}
