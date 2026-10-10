// Họ và tên: Nguyễn Phi Hùng
// Mã sinh viên: 23103100148
// Nội dung thực hiện: Module 3 - ViewModel form nộp hồ sơ xin học bổng.
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;

namespace QuanLyHocBong_UNETI2_TI17A3HN.ViewModels
{
    public class NopHoSoViewModel
    {
        public int MaHocBong { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập thư xin học bổng")]
        [StringLength(2000, MinimumLength = 30, ErrorMessage = "Thư xin học bổng phải từ {2} đến {1} ký tự")]
        [Display(Name = "Thư xin học bổng")]
        public string ThuXinHocBong { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Ghi chú tối đa 1000 ký tự")]
        [Display(Name = "Ghi chú")]
        public string? GhiChu { get; set; }

        // ---- Chỉ để hiển thị, không nhận từ form ----
        [BindNever] public HocBong? HocBong { get; set; }
        [BindNever] public SinhVien? SinhVien { get; set; }
        [BindNever] public List<string> LyDoKhongHopLe { get; set; } = new();
        public bool DuDieuKien => !LyDoKhongHopLe.Any();
    }
}
