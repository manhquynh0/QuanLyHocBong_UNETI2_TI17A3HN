using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Models
{
    public class HoSoHocBong
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaHoSoHocBong { get; set; }

        [Required]
        [ForeignKey("SinhVien")]
        public int MaSinhVien { get; set; }

        [Required]
        [ForeignKey("HocBong")]
        public int MaHocBong { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime NgayNop { get; set; }

        [StringLength(4000, ErrorMessage = "Thư xin học bổng không được vượt quá 2000 ký tự")]
        [Column(TypeName = "nvarchar(2000)")]
        public string ThuXinHocBong { get; set; }

        [StringLength(1000)]
        [Column(TypeName = "nvarchar(1000)")]
        public string GhiChu { get; set; }

        [Required]
        [StringLength(50)]
        [Column(TypeName = "nvarchar(50)")]
        public string TrangThai { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? NgayXuLy { get; set; }

        [StringLength(1000)]
        [Column(TypeName = "nvarchar(1000)")]
        public string? NhanXetXetDuyet { get; set; }


        public virtual SinhVien SinhVien { get; set; }

        public virtual HocBong HocBong { get; set; }

        public virtual ICollection<LichPhongVan> LichPhongVans { get; set; }

        public virtual KetQuaHocBong KetQuaHocBong { get; set; }
    }
}
