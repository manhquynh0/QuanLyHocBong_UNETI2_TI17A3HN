// Họ và tên: Nguyễn Phi Hùng
// Mã sinh viên: 23103100148
// Nội dung thực hiện: Module 3 - ViewModel form cập nhật hồ sơ cá nhân.
// Không có MaSinhVien/MaTaiKhoan/TrangThai để sinh viên không thể gửi (over-posting) dữ liệu của người khác.
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace QuanLyHocBong_UNETI2_TI17A3HN.ViewModels
{
    public class HoSoCaNhanViewModel
    {
        public string? AnhDaiDien { get; set; }

        [Display(Name = "Ảnh đại diện")]
        public IFormFile? FileAnhDaiDien { get; set; }

        // Chuẩn hóa ngay khi binding (cắt khoảng trắng thừa) để validation chạy trên dữ liệu đã chuẩn hóa.
        private string _hoTen = string.Empty;
        private string _soDienThoai = string.Empty;
        private string _email = string.Empty;

        [Required(ErrorMessage = "Họ tên là bắt buộc")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
        [Display(Name = "Họ và tên")]
        public string HoTen
        {
            get => _hoTen;
            set => _hoTen = Regex.Replace((value ?? string.Empty).Trim(), @"\s+", " ");
        }

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
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ. Ví dụ: 0912345678")]
        [RegularExpression(@"^(0|\+84)[2-9]\d{8}$", ErrorMessage = "Số điện thoại không hợp lệ. Ví dụ: 0912345678 hoặc +84912345678 (nhập liền, không dấu cách)")]
        [Display(Name = "Số điện thoại")]
        public string SoDienThoai
        {
            get => _soDienThoai;
            set => _soDienThoai = (value ?? string.Empty).Trim();
        }

        // Cột Email trong DB là varchar -> chỉ nhận ký tự ASCII để không bị mất dấu khi lưu.
        [Required(ErrorMessage = "Email là bắt buộc")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng. Ví dụ: ten@uneti.edu.vn")]
        [RegularExpression(@"^[A-Za-z0-9._%+\-]+@[A-Za-z0-9\-]+(\.[A-Za-z0-9\-]+)*\.[A-Za-z]{2,}$", ErrorMessage = "Email không đúng định dạng. Ví dụ: ten@uneti.edu.vn (không dùng chữ có dấu)")]
        [StringLength(100, ErrorMessage = "Email tối đa 100 ký tự")]
        [Display(Name = "Email")]
        public string Email
        {
            get => _email;
            set => _email = (value ?? string.Empty).Trim().ToLowerInvariant();
        }

        [StringLength(500, ErrorMessage = "Địa chỉ tối đa 500 ký tự")]
        [Display(Name = "Địa chỉ")]
        public string? DiaChi { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn bậc đào tạo")]
        [RegularExpression("^(Cao đẳng|Đại học|Thạc sĩ)$", ErrorMessage = "Bậc đào tạo không hợp lệ")]
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

        // Địa chỉ nội bộ để quay lại sau khi lưu (ví dụ về form nộp hồ sơ). Server kiểm tra IsLocalUrl.
        public string? ReturnUrl { get; set; }
    }
}
