using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Models
{
    public class LichPhongVan
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaLichPhongVan { get; set; }

        [Required]
        [ForeignKey("HoSoHocBong")]
        public int MaHoSoHocBong { get; set; }

        [Required(ErrorMessage = "Thời gian bắt đầu là bắt buộc")]
        [DataType(DataType.DateTime)]
        public DateTime ThoiGianBatDau { get; set; }

        [Required(ErrorMessage = "Thời gian kết thúc là bắt buộc")]
        [DataType(DataType.DateTime)]
        public DateTime ThoiGianKetThuc { get; set; }

        [StringLength(500)]
        [Column(TypeName = "nvarchar(500)")]
        public string DiaDiemHoacLienKet { get; set; }

        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string HinhThucPhongVan { get; set; }

        [Required(ErrorMessage = "Người phỏng vấn là bắt buộc")]
        [StringLength(100)]
        [Column(TypeName = "nvarchar(100)")]
        public string NguoiPhongVan { get; set; }

        [StringLength(1000)]
        [Column(TypeName = "nvarchar(1000)")]
        public string GhiChu { get; set; }


        [Required(ErrorMessage = "Trạng thái là bắt buộc")]
        [StringLength(50)]
        [Column(TypeName = "nvarchar(50)")]
        public string TrangThai { get; set; }

        public virtual HoSoHocBong HoSoHocBong { get; set; }
    }
}
