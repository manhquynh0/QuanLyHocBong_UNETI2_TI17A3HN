// Họ và tên: Nguyễn Phi Hùng
// Mã sinh viên: 23103100148
// Nội dung thực hiện: Module 3 - ViewModel form nộp hồ sơ xin học bổng.
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using ModelHocBong = QuanLyHocBong_UNETI2_TI17A3HN.Models.HocBong;

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

        [Display(Name = "Loại tài liệu")]
        public string LoaiTaiLieu { get; set; } = "Minh chứng";

        // Tệp đính kèm (PDF/JPG/PNG). Định dạng, dung lượng, nội dung được kiểm tra ở TaiLieuHoSoStorage.
        public List<IFormFile>? TaiLieus { get; set; }

        // ---- Chỉ để hiển thị, không nhận từ form ----
        [BindNever] public ModelHocBong? HocBong { get; set; }
        [BindNever] public SinhVien? SinhVien { get; set; }
        [BindNever] public List<string> LyDoKhongHopLe { get; set; } = new();
        public bool DuDieuKien => !LyDoKhongHopLe.Any();
    }
}
