using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Models
{
    public class DonViTaiTro
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaDonViTaiTro { get; set; }

        [Required(ErrorMessage = "Tên đơn vị tài trợ là bắt buộc")]
        [StringLength(200)]
        [Column(TypeName = "nvarchar(200)")]
        public string TenDonViTaiTro { get; set; }

        [StringLength(2000)]
        [Column(TypeName = "nvarchar(2000)")]
        public string MoTa { get; set; } 

        [EmailAddress(ErrorMessage = "Email liên hệ không đúng định dạng")]
        [StringLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string EmailLienHe { get; set; }

        [Required]
        public bool TrangThai { get; set; }

        public virtual ICollection<HocBong> HocBongs { get; set; }
    }
}
