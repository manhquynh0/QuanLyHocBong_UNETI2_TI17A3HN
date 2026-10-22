using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Scholarship = QuanLyHocBong_UNETI2_TI17A3HN.Models.HocBong;

namespace QuanLyHocBong_UNETI2_TI17A3HN.ViewModels.HocBong
{
    public class HocBongIndexViewModel
    {
        public IReadOnlyList<Scholarship> HocBongs { get; set; } = Array.Empty<Scholarship>();

        public string? TuKhoa { get; set; }

        public int? MaDonViTaiTro { get; set; }

        public string? BacDaoTao { get; set; }

        public string? TrangThai { get; set; }

        [Range(typeof(decimal), "0", "4", ErrorMessage = "Điểm tối thiểu phải nằm trong khoảng 0 đến 4.")]
        public decimal? DiemToiThieu { get; set; }

        public string? HanNop { get; set; }

        public string SapXep { get; set; } = "ma-tang";

        public IReadOnlyList<SelectListItem> DonViTaiTroOptions { get; set; } = Array.Empty<SelectListItem>();

        public IReadOnlyList<SelectListItem> BacDaoTaoOptions { get; set; } = Array.Empty<SelectListItem>();

        public IReadOnlyList<SelectListItem> TrangThaiOptions { get; set; } = Array.Empty<SelectListItem>();

        public int SoKetQua { get; set; }

        public bool CoBoLoc =>
            !string.IsNullOrWhiteSpace(TuKhoa)
            || MaDonViTaiTro.HasValue
            || !string.IsNullOrWhiteSpace(BacDaoTao)
            || !string.IsNullOrWhiteSpace(TrangThai)
            || DiemToiThieu.HasValue
            || !string.IsNullOrWhiteSpace(HanNop);
    }
}
