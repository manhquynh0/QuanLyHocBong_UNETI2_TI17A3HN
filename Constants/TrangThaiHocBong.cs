namespace QuanLyHocBong_UNETI2_TI17A3HN.Constants
{
    public static class TrangThaiHocBong
    {
        public const string SapMo = "Sắp mở";
        public const string DangNhanHoSo = "Đang nhận hồ sơ";
        public const string DaDong = "Đã đóng";

        public static string TinhTrang(DateTime ngayBatDau, DateTime hanNop, DateTime? ngayHienTai = null)
        {
            var homNay = (ngayHienTai ?? DateTime.Today).Date;
            if (homNay < ngayBatDau.Date)
            {
                return SapMo;
            }

            return homNay <= hanNop.Date ? DangNhanHoSo : DaDong;
        }
    }
}
