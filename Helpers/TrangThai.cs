// Họ và tên: Nguyễn Phi Hùng
// Mã sinh viên: 23103100148
// Nội dung thực hiện: Module 3 - hằng số trạng thái dùng chung cho phía sinh viên.
namespace QuanLyHocBong_UNETI2_TI17A3HN.Helpers
{
    public static class VaiTro
    {
        public const string SinhVien = "Sinh viên";
    }

    public static class TrangThaiSinhVien
    {
        // Giá trị seed hiện tại là "Đang học"; đổi tại đây nếu nhóm thống nhất giá trị khác.
        public const string DangHoc = "Đang học";
    }

    public static class TrangThaiHocBong
    {
        public const string DangNhanHoSo = "Đang nhận hồ sơ";
    }

    public static class TrangThaiLich
    {
        public const string DaHuy = "Đã hủy";
    }

    public static class TrangThaiHoSo
    {
        public const string ChoDuyet = "Chờ duyệt";
        public const string DatSoBo = "Đạt xét duyệt sơ bộ";
        public const string KhongDatSoBo = "Không đạt xét duyệt sơ bộ";
        public const string ChoPhongVan = "Chờ phỏng vấn";
        public const string DaPhongVan = "Đã phỏng vấn";
        public const string DuocCap = "Được cấp học bổng";
        public const string KhongDuocCap = "Không được cấp học bổng";
        public const string DaHuy = "Đã hủy";

        // Các trạng thái "đang xử lý": không cho nộp thêm hồ sơ cùng học bổng.
        public static readonly string[] DangXuLy = { ChoDuyet, DatSoBo, ChoPhongVan, DaPhongVan };
    }
}
