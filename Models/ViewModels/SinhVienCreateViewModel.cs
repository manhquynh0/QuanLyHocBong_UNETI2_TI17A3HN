using System;
using System.ComponentModel.DataAnnotations;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Models.ViewModels
{
    public class SinhVienCreateViewModel
    {
        [Required(ErrorMessage = "Tên đăng nhập là bắt buộc")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập phải từ 3 đến 50 ký tự")]
        [Display(Name = "Tên đăng nhập (Mã SV / Username)")]
        public string TenDangNhap { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu khởi tạo là bắt buộc")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu khởi tạo")]
        public string MatKhau { get; set; } = "123456";

        [Required(ErrorMessage = "Họ và tên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string HoTen { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày sinh là bắt buộc")]
        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime NgaySinh { get; set; } = new DateTime(2004, 1, 1);

        [Required(ErrorMessage = "Giới tính là bắt buộc")]
        [StringLength(10)]
        [Display(Name = "Giới tính")]
        public string GioiTinh { get; set; } = "Nam";

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [Phone(ErrorMessage = "Số điện thoại không đúng định dạng")]
        [StringLength(20)]
        [Display(Name = "Số điện thoại")]
        public string SoDienThoai { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [StringLength(100)]
        [Display(Name = "Email liên hệ")]
        public string Email { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Địa chỉ cư trú")]
        public string? DiaChi { get; set; }

        [Required(ErrorMessage = "Bậc đào tạo là bắt buộc")]
        [StringLength(100)]
        [Display(Name = "Bậc đào tạo")]
        public string BacDaoTao { get; set; } = "Đại học";

        [Required(ErrorMessage = "Chuyên ngành học là bắt buộc")]
        [StringLength(100)]
        [Display(Name = "Chuyên ngành")]
        public string Nganh { get; set; } = "Công nghệ thông tin";

        [Required(ErrorMessage = "Điểm GPA tích lũy là bắt buộc")]
        [Range(0.0, 4.0, ErrorMessage = "Điểm GPA tích lũy phải từ 0.0 đến 4.0")]
        [Display(Name = "Điểm GPA tích lũy (hệ 4)")]
        public decimal DiemTrungBinhTichLuy { get; set; } = 3.20m;

        [StringLength(1000)]
        [Display(Name = "Thành tích học tập & Hoạt động phong trào")]
        public string? ThanhTichHocTap { get; set; }

        [Required(ErrorMessage = "Trạng thái sinh viên là bắt buộc")]
        [StringLength(50)]
        [Display(Name = "Trạng thái sinh viên")]
        public string TrangThai { get; set; } = "Đang học";

        [Display(Name = "Kích hoạt tài khoản ngay")]
        public bool KichHoatTaiKhoan { get; set; } = true;
    }
}

