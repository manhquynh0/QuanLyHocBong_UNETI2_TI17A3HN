
namespace QuanLyHocBong_UNETI2_TI17A3HN.Constants
{
    public static class TrangThaiHoSo
    {
        public const string ChoDuyet = "Chờ duyệt";
        public const string DatXetDuyetSoBo = "Đạt xét duyệt sơ bộ";
        public const string KhongDatXetDuyetSoBo = "Không đạt xét duyệt sơ bộ";
        public const string ChoPhongVan = "Chờ phỏng vấn";
        public const string DaPhongVan = "Đã phỏng vấn";
        public const string DuocCapHocBong = "Được cấp học bổng";
        public const string KhongDuocCapHocBong = "Không được cấp học bổng";
        public const string DaHuy = "Đã hủy";

        // Danh sách tất cả trạng thái (dùng cho dropdown
        public static readonly string[] TatCa = new[]
        {
            ChoDuyet,
            DatXetDuyetSoBo,
            KhongDatXetDuyetSoBo,
            ChoPhongVan,
            DaPhongVan,
            DuocCapHocBong,
            KhongDuocCapHocBong,
            DaHuy
        };

        // Các trạng thái đang trong quá trình xử lý 
        // Dùng để kiểm tra "sinh viên không được có 2 hồ sơ đang xử lý cùng học bổng"
        public static readonly string[] DangXuLy = new[]
        {
            ChoDuyet,
            DatXetDuyetSoBo,
            ChoPhongVan,
            DaPhongVan
        };

        // Các trạng thái được phép hủy bởi sinh viên
        // (Chờ duyệt hoặc Đạt xét duyệt sơ bộ nhưng chưa có lịch phỏng vấn)
        public static readonly string[] CoTheHuy = new[]
        {
            ChoDuyet,
            DatXetDuyetSoBo
        };

        /// <summary>
        /// Kiểm tra một cặp chuyển trạng thái có hợp lệ không.
        /// </summary>
        public static bool CoTheChuyen(string tuTrangThai, string denTrangThai)
        {
            return (tuTrangThai, denTrangThai) switch
            {
                // Nhánh duyệt sơ bộ
                (ChoDuyet, DatXetDuyetSoBo) => true,
                (ChoDuyet, KhongDatXetDuyetSoBo) => true,

                // Nhánh phỏng vấn (Module 5 xử lý)
                (DatXetDuyetSoBo, ChoPhongVan) => true,
                (ChoPhongVan, DaPhongVan) => true,

                // Nhánh kết quả cuối (Module 5 xử lý)
                (DaPhongVan, DuocCapHocBong) => true,
                (DaPhongVan, KhongDuocCapHocBong) => true,

                // Nhánh hủy
                (ChoDuyet, DaHuy) => true,
                (DatXetDuyetSoBo, DaHuy) => true,

                _ => false
            };
        }

        /// <summary>
        /// Kiểm tra hồ sơ có đang trong quá trình xử lý không
        /// (dùng để chặn nộp trùng hồ sơ cùng học bổng).
        /// </summary>
        public static bool LaDangXuLy(string trangThai)
        {
            return DangXuLy.Contains(trangThai);
        }

        /// <summary>
        /// Kiểm tra hồ sơ có được phép hủy không.
        /// </summary>
        public static bool CoTheHuyHoSo(string trangThai)
        {
            return CoTheHuy.Contains(trangThai);
        }
    }
}