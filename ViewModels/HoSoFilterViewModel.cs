// Họ và tên: Phan Sỹ Tú
// Mã sinh viên: 23103100145
// Nội dung thực hiện: Module 4 - ViewModel cho danh sách hồ sơ,
// bao gồm tìm kiếm, lọc, sắp xếp và phân trang.

using System.ComponentModel.DataAnnotations;

namespace QuanLyHocBong_UNETI2_TI17A3HN.ViewModels
{
    public class HoSoFilterViewModel
    {
        // Điều kiện tìm kiếm / lọc
        [Display(Name = "Từ khóa")]
        public string? TuKhoa { get; set; }

        [Display(Name = "Đơn vị tài trợ")]
        public int? MaDonViTaiTro { get; set; }

        [Display(Name = "Trạng thái hồ sơ")]
        public string? TrangThai { get; set; }

        [Display(Name = "Từ ngày nộp")]
        [DataType(DataType.Date)]
        public DateTime? TuNgay { get; set; }

        [Display(Name = "Đến ngày nộp")]
        [DataType(DataType.Date)]
        public DateTime? DenNgay { get; set; }

        // Sắp xếp
        [Display(Name = "Sắp xếp theo")]
        public string? SapXep { get; set; }

        // phân trang
        public int Trang { get; set; } = 1;
        public int KichThuocTrang { get; set; } = 10;
        public int TongSoTrang { get; set; }
        public int TongSoBanGhi { get; set; }

        // Dữ liệu trả về
        public List<HoSoItemViewModel> DanhSach { get; set; } = new();

        // Helper cho View
        public bool CoTrangTruoc => Trang > 1;
        public bool CoTrangSau => Trang < TongSoTrang;
    }
}