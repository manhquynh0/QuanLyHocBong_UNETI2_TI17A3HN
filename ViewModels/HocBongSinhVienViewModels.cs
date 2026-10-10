// Họ và tên: Nguyễn Phi Hùng
// Mã sinh viên: 23103100148
// Nội dung thực hiện: Module 3 - ViewModel danh sách và chi tiết học bổng phía sinh viên.
using QuanLyHocBong_UNETI2_TI17A3HN.Helpers;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;

namespace QuanLyHocBong_UNETI2_TI17A3HN.ViewModels
{
    public class HocBongItemViewModel
    {
        public int MaHocBong { get; set; }
        public string TenHocBong { get; set; } = string.Empty;
        public string TenDonViTaiTro { get; set; } = string.Empty;
        public string BacDaoTao { get; set; } = string.Empty;
        public int SoSuat { get; set; }
        public decimal DiemTrungBinhToiThieu { get; set; }
        public DateTime NgayBatDauNhanHoSo { get; set; }
        public DateTime HanNopHoSo { get; set; }
        public string? MoTaHocBong { get; set; }
        public string TrangThai { get; set; } = string.Empty;

        public bool DaNop { get; set; }      // đã có hồ sơ đang xử lý
        public bool DuDiem { get; set; }     // ĐTB của sinh viên >= mức tối thiểu

        public bool ChuaDenNgay => DateTime.Today < NgayBatDauNhanHoSo.Date;
        public bool HetHan => DateTime.Today > HanNopHoSo.Date;
        public bool DangMo => TrangThai == TrangThaiHocBong.DangNhanHoSo && !ChuaDenNgay && !HetHan;
        public int SoNgayConLai => (HanNopHoSo.Date - DateTime.Today).Days;
    }

    public class HocBongSinhVienListViewModel
    {
        public List<HocBongItemViewModel> Items { get; set; } = new();

        // Bộ lọc/tìm kiếm/sắp xếp hiện tại (để giữ lại khi chuyển trang)
        public string? Keyword { get; set; }
        public int? MaDonViTaiTro { get; set; }
        public string? BacDaoTao { get; set; }
        public string? HanNop { get; set; }          // "", "conhan", "hethan"
        public bool PhuHop { get; set; }              // chỉ hiện học bổng đủ điểm của tôi
        public string Sort { get; set; } = "han_asc";

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 6;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);

        public List<DonViTaiTro> DonViTaiTros { get; set; } = new();
        public List<string> BacDaoTaos { get; set; } = new();
        public decimal DiemCuaToi { get; set; }
        public bool CoHoSoCaNhan { get; set; }
    }

    public class HocBongChiTietViewModel
    {
        public HocBong HocBong { get; set; } = default!;
        public List<string> LyDoKhongNop { get; set; } = new();
        public bool DuocNop => !LyDoKhongNop.Any();
    }
}
