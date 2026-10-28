using System.ComponentModel.DataAnnotations;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Models;

public class DangNhapViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
    public string TenDangNhap { get; set; } = "";
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [DataType(DataType.Password)]
    public string MatKhau { get; set; } = "";
}
