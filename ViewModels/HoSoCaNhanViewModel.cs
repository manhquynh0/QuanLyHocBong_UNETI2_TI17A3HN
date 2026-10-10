// Họ và tên: Nguyễn Phi Hùng
// Mã sinh viên: 23103100148
// Nội dung thực hiện: Module 3 - ViewModel form cập nhật hồ sơ cá nhân (không có MaSinhVien/MaTaiKhoan/TrangThai để tránh over-posting).
using System.ComponentModel.DataAnnotations;

namespace QuanLyHocBong_UNETI2_TI17A3HN.ViewModels
{
    public class HoSoCaNhanViewModel
    {
        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string HoTen { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngày sinh là bắt buộc")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "Ngày sinh")]
        public DateTime? NgaySinh { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giới tính")]
        [RegularExpression("^(Nam|Nữ|Khác)$", ErrorMessage = "Giới tính không hợp lệ")]
        [Display(Name = "Giới tính")]
        public string GioiTinh { get; set; } = string.Empty;

        [Required(ErrorMessage = "Số điện thoại là bắt buộc")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [RegularExpression(@"^(0\d{9}|\+84\d{9})$", ErrorMessage = "Số điện thoại phải gồm 10 chữ số (bắt đầu bằng 0) hoặc dạng +84xxxxxxxxx")]
        [Display(Name = "Số điện thoại")]
        public string SoDienThoai { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
        [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Địa chỉ tối đa 500 ký tự")]
        [Display(Name = "Địa chỉ")]
        public string? DiaChi { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn bậc đào tạo")]
        [StringLength(100)]
        [Display(Name = "Bậc đào tạo")]
        public string BacDaoTao { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ngành học là bắt buộc")]
        [StringLength(100, ErrorMessage = "Ngành học tối đa 100 ký tự")]
        [Display(Name = "Ngành học")]
        public string Nganh { get; set; } = string.Empty;

        [Required(ErrorMessage = "Điểm trung bình tích lũy là bắt buộc")]
        [Range(0.0, 4.0, ErrorMessage = "Điểm trung bình tích lũy phải từ 0 đến 4")]
        [Display(Name = "Điểm trung bình tích lũy (thang 4)")]
        public decimal? DiemTrungBinhTichLuy { get; set; }

        [StringLength(1000, ErrorMessage = "Thành tích học tập tối đa 1000 ký tự")]
        [Display(Name = "Thành tích học tập")]
        public string? ThanhTichHocTap { get; set; }
    }
}
