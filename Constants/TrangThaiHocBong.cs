namespace QuanLyHocBong_UNETI2_TI17A3HN.Constants
{
    public static class TrangThaiHocBong
    {
        public const string ChuaMo = "Chưa mở";
        public const string SapMo = "Sắp mở";
        public const string DangNhanHoSo = "Đang nhận hồ sơ";
        public const string TamDung = "Tạm dừng";
        public const string DaDong = "Đã đóng";

        public static string TinhTrang(DateTime ngayBatDau, DateTime hanNop, DateTime? ngayHienTai = null, string? trangThaiThuCong = null)
        {
            if (trangThaiThuCong is TamDung or DaDong or ChuaMo)
            {
                return trangThaiThuCong;
            }

            var homNay = (ngayHienTai ?? DateTime.Today).Date;
            if (homNay < ngayBatDau.Date)
            {
                return ChuaMo;
            }

            return homNay <= hanNop.Date ? DangNhanHoSo : DaDong;
        }
    }
}
