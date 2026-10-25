// ============================================================================
// ĐỀ TÀI 22: XÂY DỰNG HỆ THỐNG QUẢN LÝ HỌC BỔNG VÀ HỒ SƠ XÉT HỌC BỔNG
// NHÓM 2 - LỚP TI17A3HN - UNETI
// MODULE 5: VIEWMODEL THỐNG KÊ BÁO CÁO TOÀN DIỆN BẰNG LINQ
// ============================================================================

using System.Collections.Generic;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Models.ViewModels
{
    public class ThongKeViewModel
    {
        // 1. Số học bổng theo đơn vị tài trợ
        public List<ThongKeItem> HocBongTheoDonVi { get; set; } = new();

        // 2. Số hồ sơ theo từng học bổng
        public List<ThongKeItem> HoSoTheoHocBong { get; set; } = new();

        // 3. Số hồ sơ theo trạng thái
        public List<ThongKeItem> HoSoTheoTrangThai { get; set; } = new();

        // 4. Số hồ sơ đạt xét duyệt sơ bộ theo học bổng
        public List<ThongKeItem> HoSoDatSoBoTheoHocBong { get; set; } = new();

        // 5. Học bổng nhận nhiều hồ sơ nhất
        public string HocBongNhieuHoSoNhat { get; set; } = "Chưa có";
        public int SoHoSoNhieuNhat { get; set; }

        // 6. Số hồ sơ được cấp học bổng theo đơn vị tài trợ
        public List<ThongKeItem> HoSoCapTheoDonVi { get; set; } = new();

        // 7. Số hồ sơ nộp theo tháng
        public List<ThongKeItem> HoSoTheoThang { get; set; } = new();

        // 8. Số lịch phỏng vấn theo tháng
        public List<ThongKeItem> LichPVTheoThang { get; set; } = new();

        // 9. Tỷ lệ hồ sơ qua xét duyệt sơ bộ theo từng học bổng
        public List<TyLeItem> TyLeDatSoBoTheoHocBong { get; set; } = new();

        // 10. Tỷ lệ hồ sơ được cấp học bổng trên số hồ sơ đã phỏng vấn
        public double TyLeCapTrenPhongVan { get; set; }
        public int TongDaPhongVan { get; set; }
        public int TongDuocCap { get; set; }
    }

    public class ThongKeItem
    {
        public string Ten { get; set; } = string.Empty;
        public int SoLuong { get; set; }
    }

    public class TyLeItem
    {
        public string TenHocBong { get; set; } = string.Empty;
        public int TongHoSo { get; set; }
        public int SoDatSoBo { get; set; }
        public double TyLePhanTram { get; set; }
    }
}

