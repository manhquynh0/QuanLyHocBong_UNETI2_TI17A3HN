// ============================================================================
// ĐỀ TÀI 22: XÂY DỰNG HỆ THỐNG QUẢN LÝ HỌC BỔNG VÀ HỒ SƠ XÉT HỌC BỔNG
// NHÓM 2 - LỚP TI17A3HN - UNETI
// VIEWMODEL: DASHBOARD DÀNH CHO SINH VIÊN
// ============================================================================

using System.Collections.Generic;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Models.ViewModels
{
    public class StudentDashboardViewModel
    {
        public SinhVien SinhVien { get; set; } = null!;
        public int TongHoSoDaNop { get; set; }
        public int HoSoDangXetDuyet { get; set; }
        public int HocBongDuocCap { get; set; }
        public int LichPhongVanSapToi { get; set; }

        public List<HoSoHocBong> DanhSachHoSo { get; set; } = new();
        public List<LichPhongVan> LichPhongVanList { get; set; } = new();
        public List<HocBong> HocBongDangMo { get; set; } = new();
    }
}

