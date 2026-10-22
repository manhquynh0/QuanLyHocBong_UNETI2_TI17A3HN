// ============================================================================
// ĐỀ TÀI 22: XÂY DỰNG HỆ THỐNG QUẢN LÝ HỌC BỔNG VÀ HỒ SƠ XÉT HỌC BỔNG
// NHÓM 2 - LỚP TI17A3HN - UNETI
// MODULE PHỤ TRÁCH: Module 5 - Dashboard & Thống kê
// ============================================================================

using System;
using System.Collections.Generic;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Models.ViewModels
{
    public class DashboardViewModel
    {
        // 9 chỉ số bắt buộc theo mục 9.5 doc.docx
        public int TongDonViTaiTro { get; set; }
        public int TongHocBong { get; set; }
        public int HocBongDangNhanHoSo { get; set; }
        public int TongSinhVien { get; set; }
        public int TongHoSo { get; set; }
        public int HoSoChoDuyet { get; set; }
        public int HoSoDatSoBo { get; set; }
        public int LichPhongVanSapToi { get; set; }
        public int HoSoDuocCapHocBong { get; set; }

        // Dữ liệu cho biểu đồ cột: Hồ sơ tiếp nhận theo tháng
        public List<MonthlyApplicationStat> HoSoTheoThang { get; set; } = new();

        // Dữ liệu cho biểu đồ tròn: Tỷ lệ phân bố trạng thái hồ sơ
        public List<StatusDistributionStat> PhanBoTrangThai { get; set; } = new();

        // Danh sách hoạt động / hồ sơ gần đây
        public List<HoSoGanDayItem> HoSoGanDay { get; set; } = new();
    }

    public class MonthlyApplicationStat
    {
        public string Thang { get; set; } = string.Empty; // vd: T10/2025, T01/2026
        public int SoLuong { get; set; }
    }

    public class StatusDistributionStat
    {
        public string TenTrangThai { get; set; } = string.Empty;
        public int SoLuong { get; set; }
        public double TiLePhanTram { get; set; }
        public string MauSac { get; set; } = string.Empty;
    }

    public class HoSoGanDayItem
    {
        public int MaHoSo { get; set; }
        public string TenSinhVien { get; set; } = string.Empty;
        public string TenHocBong { get; set; } = string.Empty;
        public DateTime NgayNop { get; set; }
        public string TrangThai { get; set; } = string.Empty;
    }
}

