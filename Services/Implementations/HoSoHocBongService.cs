// Họ và tên: Phan Sỹ Tú
// Mã sinh viên: 23103100145
// Nội dung thực hiện: Module 4 - Xử lý nghiệp vụ tiếp nhận hồ sơ,
// xét duyệt sơ bộ, quản lý trạng thái và kiểm soát số suất.
using QuanLyHocBong_UNETI2_TI17A3HN.Common;
using QuanLyHocBong_UNETI2_TI17A3HN.Constants;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.Repositories.Interfaces;
using QuanLyHocBong_UNETI2_TI17A3HN.Services.Interface;
using QuanLyHocBong_UNETI2_TI17A3HN.Services.Validators;
using QuanLyHocBong_UNETI2_TI17A3HN.ViewModels;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Services.Implementations
{
    public class HoSoHocBongService : IHoSoHocBongService
    {
        private readonly IHoSoHocBongRepository _hoSoRepo;
        private readonly HoSoHocBongValidator _validator;

        // Đã bỏ _context vì mọi query đều đã được đẩy về Repository
        public HoSoHocBongService(
            IHoSoHocBongRepository hoSoRepo,
            HoSoHocBongValidator validator) // Sửa lỗi chính tả HocSoVHocBongValidator
        {
            _hoSoRepo = hoSoRepo;
            _validator = validator;
        }

        // 1. DANH SÁCH HỒ SƠ + TÌM KIẾM + LỌC + SẮP XẾP + PHÂN TRANG
        public async Task<ApiResponse<PagedResult<HoSoItemViewModel>>> LayDanhSachAsync(HoSoFilterViewModel filter)
        {
            var query = _hoSoRepo.GetQueryableWithDetails();

            // Tìm kiếm theo tên SV hoặc tên học bổng
            if (!string.IsNullOrWhiteSpace(filter.TuKhoa))
            {
                var tuKhoa = filter.TuKhoa.Trim();
                query = query.Where(h =>
                    h.SinhVien!.HoTen.Contains(tuKhoa) ||
                    h.HocBong!.TenHocBong.Contains(tuKhoa));
            }

            // Lọc theo đơn vị tài trợ
            if (filter.MaDonViTaiTro.HasValue)
                query = query.Where(h => h.HocBong!.MaDonViTaiTro == filter.MaDonViTaiTro.Value);

            // Lọc theo trạng thái
            if (!string.IsNullOrWhiteSpace(filter.TrangThai))
                query = query.Where(h => h.TrangThai == filter.TrangThai);

            // Lọc theo ngày nộp
            if (filter.TuNgay.HasValue)
                query = query.Where(h => h.NgayNop >= filter.TuNgay.Value);

            if (filter.DenNgay.HasValue)
            {
                var denNgay = filter.DenNgay.Value.Date.AddDays(1).AddSeconds(-1);
                query = query.Where(h => h.NgayNop <= denNgay);
            }

            // Sắp xếp
            query = filter.SapXep switch
            {
                "ngay_nop_asc" => query.OrderBy(h => h.NgayNop),
                "ngay_nop_desc" => query.OrderByDescending(h => h.NgayNop),
                "ten_sv_asc" => query.OrderBy(h => h.SinhVien!.HoTen),
                "ten_sv_desc" => query.OrderByDescending(h => h.SinhVien!.HoTen),
                _ => query.OrderByDescending(h => h.NgayNop)
            };

            // Projection sang ViewModel
            var projectedQuery = query.Select(h => new HoSoItemViewModel
            {
                MaHoSoHocBong = h.MaHoSoHocBong,
                TenSinhVien = h.SinhVien!.HoTen,
                TenHocBong = h.HocBong!.TenHocBong,
                TenDonViTaiTro = h.HocBong.DonViTaiTro!.TenDonViTaiTro,
                NgayNop = h.NgayNop,
                TrangThai = h.TrangThai,
                DiemTrungBinh = h.SinhVien.DiemTrungBinhTichLuy,
                DiemYeuCau = h.HocBong.DiemTrungBinhToiThieu
            });

            // Gọi PagedResult để phân trang
            var pagedResult = await PagedResult<HoSoItemViewModel>.CreateAsync(
                projectedQuery,
                filter.Trang,
                filter.KichThuocTrang);

            return ApiResponse<PagedResult<HoSoItemViewModel>>.Ok(pagedResult);
        }

        // 2. CHI TIẾT HỒ SƠ
        public async Task<HoSoChiTietViewModel?> LayChiTietAsync(int maHoSo)
        {
            var hoSo = await _hoSoRepo.GetByIdWithDetailsAsync(maHoSo);
            if (hoSo == null) return null;

            int soDaCap = await _hoSoRepo.CountDaCapByHocBongAsync(hoSo.MaHocBong);

            return new HoSoChiTietViewModel
            {
                MaHoSoHocBong = hoSo.MaHoSoHocBong,
                NgayNop = hoSo.NgayNop,
                ThuXinHocBong = hoSo.ThuXinHocBong,
                GhiChu = hoSo.GhiChu,
                TrangThai = hoSo.TrangThai,
                NgayXuLy = hoSo.NgayXuLy,
                NhanXetXetDuyet = hoSo.NhanXetXetDuyet,
                FileMinhChung = hoSo.FileMinhChung,

                MaSinhVien = hoSo.SinhVien!.MaSinhVien,
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

                MaHocBong = hoSo.HocBong!.MaHocBong,
                TenHocBong = hoSo.HocBong.TenHocBong,
                SoSuat = hoSo.HocBong.SoSuat,
                BacDaoTaoHocBong = hoSo.HocBong.BacDaoTao,
                DiemTrungBinhToiThieu = hoSo.HocBong.DiemTrungBinhToiThieu,
                NgayBatDauNhanHoSo = hoSo.HocBong.NgayBatDauNhanHoSo,
                HanNopHoSo = hoSo.HocBong.HanNopHoSo,
                DieuKienXetHocBong = hoSo.HocBong.DieuKienXetHocBong,

                TenDonViTaiTro = hoSo.HocBong.DonViTaiTro!.TenDonViTaiTro,
                SoDaCap = soDaCap
            };
        }
        public async Task<XetDuyetSoBoViewModel?> LayFormXetDuyetAsync(int maHoSo)
        {
            var hoSo = await _hoSoRepo.GetByIdWithDetailsAsync(maHoSo);
            if (hoSo == null) return null;

            return new XetDuyetSoBoViewModel
            {
                MaHoSoHocBong = hoSo.MaHoSoHocBong,
                MaHocBong = hoSo.MaHocBong,
                TenSinhVien = hoSo.SinhVien!.HoTen,
                TenHocBong = hoSo.HocBong!.TenHocBong,
                TenDonViTaiTro = hoSo.HocBong.DonViTaiTro!.TenDonViTaiTro,
                DiemTrungBinh = hoSo.SinhVien.DiemTrungBinhTichLuy,
                DiemYeuCau = hoSo.HocBong.DiemTrungBinhToiThieu,
                BacDaoTaoSinhVien = hoSo.SinhVien.BacDaoTao,
                BacDaoTaoHocBong = hoSo.HocBong.BacDaoTao,
                NgayNop = hoSo.NgayNop,
                ThuXinHocBong = hoSo.ThuXinHocBong,
                ThanhTichHocTap = hoSo.SinhVien.ThanhTichHocTap,
                FileMinhChung = hoSo.FileMinhChung
            };
        }

        // 4. XỬ LÝ XÉT DUYỆT SƠ BỘ
        public async Task<ApiResponse<bool>> XetDuyetSoBoAsync(XetDuyetSoBoViewModel model)
        {
            var hoSo = await _hoSoRepo.GetByIdWithDetailsAsync(model.MaHoSoHocBong);
            if (hoSo == null) return ApiResponse<bool>.Fail("Hồ sơ không tồn tại.");

            // 1. Dùng Validator kiểm tra điều kiện
            var result = _validator.ValidateXetDuyetSoBo(hoSo);
            if (!result.IsValid) return ApiResponse<bool>.Fail(result.ErrorMessage!);

            // 2. Xác định trạng thái mới
            string trangThaiMoi = model.KetQua switch
            {
                "Dat" => TrangThaiHoSo.DatXetDuyetSoBo,
                _ => TrangThaiHoSo.KhongDatXetDuyetSoBo
            };

            // 3. Kiểm tra luồng chuyển trạng thái
            var transResult = _validator.ValidateTransition(hoSo.TrangThai, trangThaiMoi);
            if (!transResult.IsValid) return ApiResponse<bool>.Fail(transResult.ErrorMessage!);

            // 4. Cập nhật
            hoSo.TrangThai = trangThaiMoi;
            hoSo.NgayXuLy = DateTime.Now;
            hoSo.NhanXetXetDuyet = model.NhanXetXetDuyet.Trim();

            await _hoSoRepo.UpdateAsync(hoSo);
            return ApiResponse<bool>.Ok(true, "Xét duyệt thành công.");
        }

        // 5. HÀM HỖ TRỢ CHO SINH VIÊN
        public async Task<ApiResponse<bool>> NopHoSoAsync(int maSinhVien, int maHocBong, string thuXin, string ghiChu, string? fileUrl, HocBong hocBong, SinhVien sinhVien)
        {
            var result = _validator.ValidateNopHoSo(hocBong, sinhVien);
            if (!result.IsValid) return ApiResponse<bool>.Fail(result.ErrorMessage!);

            // Kiểm tra nộp trùng (Dùng Repo)
            if (await _hoSoRepo.CheckDaNopHoSoAsync(maSinhVien, maHocBong))
                return ApiResponse<bool>.Fail("Bạn đã nộp hồ sơ cho chương trình học bổng này rồi.");

            var hoSo = new HoSoHocBong
            {
                MaSinhVien = maSinhVien,
                MaHocBong = maHocBong,
                NgayNop = DateTime.Now,
                ThuXinHocBong = thuXin.Trim(),
                GhiChu = (string.IsNullOrEmpty(ghiChu) ? "Đã nộp đầy đủ hồ sơ" : ghiChu).Trim(),
                TrangThai = TrangThaiHoSo.ChoDuyet,
                FileMinhChung = fileUrl
            };

            await _hoSoRepo.AddAsync(hoSo);
            return ApiResponse<bool>.Ok(true, "Nộp hồ sơ thành công.");
        }

        public async Task<ApiResponse<bool>> HuyHoSoAsync(int maHoSo, int maSinhVien)
        {
            var hoSo = await _hoSoRepo.GetByIdAsync(maHoSo);
            if (hoSo == null) return ApiResponse<bool>.Fail("Hồ sơ không tồn tại.");

            var result = _validator.ValidateHuyHoSo(hoSo, maSinhVien);
            if (!result.IsValid) return ApiResponse<bool>.Fail(result.ErrorMessage!);

            hoSo.TrangThai = TrangThaiHoSo.DaHuy;
            hoSo.NgayXuLy = DateTime.Now;

            await _hoSoRepo.UpdateAsync(hoSo);
            return ApiResponse<bool>.Ok(true, "Hủy hồ sơ thành công."); // Sửa lại thông báo
        }

        public async Task<int> DemSoDaCapAsync(int maHocBong) => await _hoSoRepo.CountDaCapByHocBongAsync(maHocBong);
        public async Task<List<DonViTaiTro>> LayDanhSachDonViTaiTroAsync() => await _hoSoRepo.GetDanhSachDonViVaitro();
        public async Task<int> LaySoSuatHocBongAsync(int maHocBong) => await _hoSoRepo.GetSoSuatHocBongAsync(maHocBong);
        public async Task<List<HoSoHocBong>> LayHoSoCuaSinhVienAsync(int maSinhVien) => await _hoSoRepo.GetBySinhVienIdAsync(maSinhVien);


    }
}