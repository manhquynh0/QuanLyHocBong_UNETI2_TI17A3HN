using System.ComponentModel.DataAnnotations;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Models;

public class TaiKhoanFormViewModel
{
    public int MaTaiKhoan { get; set; }
    [Required(ErrorMessage = "Tên đăng nhập là bắt buộc"), StringLength(50)]
    public string TenDangNhap { get; set; } = "";
    [DataType(DataType.Password)]
    public string? MatKhau { get; set; }
    [Required(ErrorMessage = "Họ tên là bắt buộc"), StringLength(100)]
    public string HoTen { get; set; } = "";
    [Required(ErrorMessage = "Email là bắt buộc"), EmailAddress(ErrorMessage = "Email không đúng định dạng"), StringLength(100)]
    public string Email { get; set; } = "";
    [Required]
    public string VaiTro { get; set; } = "Sinh viên";
    public bool TrangThai { get; set; } = true;
}
