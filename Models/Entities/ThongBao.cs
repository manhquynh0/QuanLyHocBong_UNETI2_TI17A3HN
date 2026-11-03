using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Models
{
    public class ThongBao
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaThongBao { get; set; }

        [Required]
        [ForeignKey("TaiKhoan")]
        public int MaTaiKhoan { get; set; }

        [Required]
        [StringLength(255)]
        [Column(TypeName = "nvarchar(255)")]
        public string TieuDe { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        [Column(TypeName = "nvarchar(1000)")]
        public string NoiDung { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        [Column(TypeName = "nvarchar(50)")]
        public string LoaiThongBao { get; set; } = "HeThong";

        [StringLength(500)]
        [Column(TypeName = "nvarchar(500)")]
        public string? LienKet { get; set; }

        [ForeignKey("HoSoHocBong")]
        public int? MaHoSoHocBong { get; set; }

        [Required]
        public bool DaDoc { get; set; } = false;

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime NgayTao { get; set; } = DateTime.Now;

        public virtual TaiKhoan? TaiKhoan { get; set; }

        public virtual HoSoHocBong? HoSoHocBong { get; set; }
    }
}

