// Họ và tên: Phan Sỹ Tú
// Mã sinh viên: 23103100145
// Nội dung thực hiện: Module 4 - Xử lý nghiệp vụ tiếp nhận hồ sơ,
// xét duyệt sơ bộ, quản lý trạng thái và kiểm soát số suất (kết hợp Repository & UnitOfWork).
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Constants;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.Repositories.Abstractions;
using QuanLyHocBong_UNETI2_TI17A3HN.Services.Interface;
using QuanLyHocBong_UNETI2_TI17A3HN.ViewModels;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Services.Implementations
{
    public class HoSoHocBongService : IHoSoHocBongService
    {
        private readonly IUnitOfWork _unitOfWork;

        public HoSoHocBongService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        // 1. DANH SÁCH HỒ SƠ + TÌM KIẾM + LỌC + SẮP XẾP + PHÂN TRANG
        public async Task<HoSoFilterViewModel> LayDanhSachAsync(HoSoFilterViewModel filter)
        {
            return await _unitOfWork.HoSoHocBongs.GetDanhSachAsync(filter);
        }

        // 2. CHI TIẾT HỒ SƠ
        public async Task<HoSoChiTietViewModel?> LayChiTietAsync(int maHoSo)
        {
            var hoSo = await _unitOfWork.HoSoHocBongs.GetByIdWithDetailsAsync(maHoSo);
            if (hoSo == null) return null;

            int soDaCap = await _unitOfWork.HoSoHocBongs.CountApprovedByHocBongAsync(hoSo.MaHocBong);
            var taiLieus = await _unitOfWork.HoSoHocBongs.GetTaiLieuItemsAsync(maHoSo);

            return new HoSoChiTietViewModel
            {
                // Hồ sơ
                MaHoSoHocBong = hoSo.MaHoSoHocBong,
                NgayNop = hoSo.NgayNop,
                ThuXinHocBong = hoSo.ThuXinHocBong,
                GhiChu = hoSo.GhiChu,
                TrangThai = hoSo.TrangThai,
                NgayXuLy = hoSo.NgayXuLy,
                NhanXetXetDuyet = hoSo.NhanXetXetDuyet,
                FileMinhChung = hoSo.FileMinhChung,
                TaiLieus = taiLieus,

                // Sinh viên
                MaSinhVien = hoSo.SinhVien.MaSinhVien,
                HoTenSinhVien = hoSo.SinhVien.HoTen,
                NgaySinh = hoSo.SinhVien.NgaySinh,
                GioiTinh = hoSo.SinhVien.GioiTinh,
                SoDienThoai = hoSo.SinhVien.SoDienThoai,
                EmailSinhVien = hoSo.SinhVien.Email,
                DiaChi = hoSo.SinhVien.DiaChi,
                BacDaoTaoSinhVien = hoSo.SinhVien.BacDaoTao,
                Nganh = hoSo.SinhVien.Nganh,
                DiemTrungBinhTichLuy = hoSo.SinhVien.DiemTrungBinhTichLuy,
                ThanhTichHocTap = hoSo.SinhVien.ThanhTichHocTap,

                // Học bổng
                MaHocBong = hoSo.HocBong.MaHocBong,
                TenHocBong = hoSo.HocBong.TenHocBong,
                SoSuat = hoSo.HocBong.SoSuat,
                BacDaoTaoHocBong = hoSo.HocBong.BacDaoTao,
                DiemTrungBinhToiThieu = hoSo.HocBong.DiemTrungBinhToiThieu,
                NgayBatDauNhanHoSo = hoSo.HocBong.NgayBatDauNhanHoSo,
                HanNopHoSo = hoSo.HocBong.HanNopHoSo,
                DieuKienXetHocBong = hoSo.HocBong.DieuKienXetHocBong,

                // Đơn vị tài trợ
                TenDonViTaiTro = hoSo.HocBong.DonViTaiTro.TenDonViTaiTro,

                // Số suất
                SoDaCap = soDaCap
            };
        }

        // 3. FORM XÉT DUYỆT SƠ BỘ (GET)
        public async Task<XetDuyetSoBoViewModel?> LayFormXetDuyetAsync(int maHoSo)
        {
            var hoSo = await _unitOfWork.HoSoHocBongs.GetByIdWithDetailsAsync(maHoSo);
            if (hoSo == null) return null;

            var taiLieus = await _unitOfWork.HoSoHocBongs.GetTaiLieuItemsAsync(maHoSo);

            return new XetDuyetSoBoViewModel
            {
                MaHoSoHocBong = hoSo.MaHoSoHocBong,
                TenSinhVien = hoSo.SinhVien.HoTen,
                TenHocBong = hoSo.HocBong.TenHocBong,
                TenDonViTaiTro = hoSo.HocBong.DonViTaiTro.TenDonViTaiTro,
                DiemTrungBinh = hoSo.SinhVien.DiemTrungBinhTichLuy,
                DiemYeuCau = hoSo.HocBong.DiemTrungBinhToiThieu,
                BacDaoTaoSinhVien = hoSo.SinhVien.BacDaoTao,
                BacDaoTaoHocBong = hoSo.HocBong.BacDaoTao,
                NgayNop = hoSo.NgayNop,
                ThuXinHocBong = hoSo.ThuXinHocBong,
                ThanhTichHocTap = hoSo.SinhVien.ThanhTichHocTap,
                FileMinhChung = hoSo.FileMinhChung,
                TaiLieus = taiLieus
            };
        }

        // 4. XỬ LÝ XÉT DUYỆT SƠ BỘ (POST)
        public async Task<(bool ThanhCong, string? Loi)> XetDuyetSoBoAsync(XetDuyetSoBoViewModel model)
        {
            var hoSo = await _unitOfWork.HoSoHocBongs.GetByIdWithDetailsAsync(model.MaHoSoHocBong);

            // Hồ sơ tồn tại?
            if (hoSo == null)
                return (false, "Hồ sơ không tồn tại.");

            // Hồ sơ chưa bị hủy?
            if (hoSo.TrangThai == TrangThaiHoSo.DaHuy)
                return (false, "Hồ sơ đã bị hủy, không thể xử lý.");

            // Chỉ hồ sơ "Chờ duyệt" mới được xét duyệt sơ bộ
            if (hoSo.TrangThai != TrangThaiHoSo.ChoDuyet)
                return (false, "Chỉ hồ sơ ở trạng thái 'Chờ duyệt' mới được xét duyệt sơ bộ.");

            // Học bổng liên quan tồn tại?
            if (hoSo.HocBong == null)
                return (false, "Học bổng liên quan không tồn tại.");

            // Sinh viên còn hoạt động / đang học?
            if (hoSo.SinhVien == null ||
                (!string.Equals(hoSo.SinhVien.TrangThai, "Đang học", StringComparison.OrdinalIgnoreCase) &&
                 !string.Equals(hoSo.SinhVien.TrangThai, "Hoạt động", StringComparison.OrdinalIgnoreCase)))
                return (false, "Sinh viên không còn hoạt động.");

            // Nhận xét bắt buộc
            if (string.IsNullOrWhiteSpace(model.NhanXetXetDuyet))
                return (false, "Vui lòng nhập nhận xét xét duyệt.");

            // Xác định trạng thái đích
            string trangThaiMoi = model.KetQua switch
            {
                "Dat" => TrangThaiHoSo.DatXetDuyetSoBo,
                "KhongDat" => TrangThaiHoSo.KhongDatXetDuyetSoBo,
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(trangThaiMoi))
                return (false, "Kết quả xét duyệt không hợp lệ.");

            // Kiểm tra luồng chuyển trạng thái hợp lệ
            if (!TrangThaiHoSo.CoTheChuyen(hoSo.TrangThai, trangThaiMoi))
                return (false, $"Không thể chuyển từ '{hoSo.TrangThai}' sang '{trangThaiMoi}'.");

            // Cập nhật
            hoSo.TrangThai = trangThaiMoi;
            hoSo.NgayXuLy = DateTime.Now;
            hoSo.NhanXetXetDuyet = model.NhanXetXetDuyet.Trim();

            _unitOfWork.HoSoHocBongs.Update(hoSo);
            await _unitOfWork.SaveChangesAsync();

            return (true, null);
        }

        // 5. ĐẾM SỐ HỒ SƠ ĐÃ ĐƯỢC CẤP HỌC BỔNG
        public Task<int> DemSoDaCapAsync(int maHocBong)
        {
            return _unitOfWork.HoSoHocBongs.CountApprovedByHocBongAsync(maHocBong);
        }
        public Task<List<DonViTaiTro>> LayDanhSachDonViTaiTroAsync()
        {
            return _unitOfWork.DonViTaiTros.Query(asNoTracking: true)
                .Where(d => d.TrangThai == true)
                .OrderBy(d => d.TenDonViTaiTro)
                .ToListAsync();
        }
    }
}
