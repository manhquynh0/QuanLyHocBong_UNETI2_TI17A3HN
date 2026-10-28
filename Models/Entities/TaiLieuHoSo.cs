// Họ và tên: Nguyễn Phi Hùng
// Mã sinh viên: 23103100148
// Nội dung thực hiện: Module 3 - Entity tệp đính kèm hồ sơ (bảng điểm, minh chứng...). Tệp lưu trên đĩa, bảng này chỉ lưu thông tin.
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Models
{
    public class TaiLieuHoSo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MaTaiLieu { get; set; }

        [Required]
        public int MaHoSoHocBong { get; set; }

        [Required]
        [StringLength(50)]
        [Column(TypeName = "nvarchar(50)")]
        public string LoaiTaiLieu { get; set; } = string.Empty;          // Bảng điểm / Minh chứng / Khác

        [Required]
        [StringLength(255)]
        [Column(TypeName = "nvarchar(255)")]
        public string TenFileGoc { get; set; } = string.Empty;           // tên sinh viên đặt, chỉ để hiển thị/tải về

        [Required]
        [StringLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string TenFileLuu { get; set; } = string.Empty;           // tên do hệ thống tạo (GUID + đuôi), dùng để tìm tệp trên đĩa

        [Required]
        [StringLength(100)]
        [Column(TypeName = "varchar(100)")]
        public string LoaiNoiDung { get; set; } = string.Empty;          // MIME do hệ thống xác định từ đuôi tệp đã kiểm tra

        public long DungLuong { get; set; }                              // byte

        [DataType(DataType.DateTime)]
        public DateTime NgayTai { get; set; }

        public int? MaTaiKhoanTai { get; set; }                          // tài khoản đã tải lên

        [ForeignKey(nameof(MaHoSoHocBong))]
        public virtual HoSoHocBong HoSoHocBong { get; set; } = default!;
    }
}
