// Họ và tên: Phan Sỹ Tú
// Mã sinh viên: 23103100145
// Nội dung thực hiện: Module 4 - Xử lý nghiệp vụ tiếp nhận hồ sơ,
// xét duyệt sơ bộ, quản lý trạng thái và kiểm soát số suất.

using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Constants;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.Services.Interface;
using QuanLyHocBong_UNETI2_TI17A3HN.ViewModels;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Services.Implementations
{
    public class HoSoHocBongService : IHoSoHocBongService
    {
        private readonly QuanLyHocBong_UNETI2_TI17A3HNContext _context;

        public HoSoHocBongService(QuanLyHocBong_UNETI2_TI17A3HNContext context)
        {
            _context = context;
        }

        // 1. DANH SÁCH HỒ SƠ + TÌM KIẾM + LỌC + SẮP XẾP + PHÂN TRANG
        public async Task<HoSoFilterViewModel> LayDanhSachAsync(HoSoFilterViewModel filter)
        {
            var query = _context.HoSoHocBong
                .Include(h => h.SinhVien)
                .Include(h => h.HocBong)
                    .ThenInclude(hb => hb.DonViTaiTro)
                .AsQueryable();

            // Tìm kiếm theo tên SV hoặc tên học bổng
            if (!string.IsNullOrWhiteSpace(filter.TuKhoa))
            {
                var tuKhoa = filter.TuKhoa.Trim();
                query = query.Where(h =>
                    h.SinhVien.HoTen.Contains(tuKhoa) ||
                    h.HocBong.TenHocBong.Contains(tuKhoa));
            }

            // Lọc theo đơn vị tài trợ
            if (filter.MaDonViTaiTro.HasValue)
                query = query.Where(h => h.HocBong.MaDonViTaiTro == filter.MaDonViTaiTro.Value);

            // Lọc theo trạng thái
            if (!string.IsNullOrWhiteSpace(filter.TrangThai))
                query = query.Where(h => h.TrangThai == filter.TrangThai);

            // Lọc theo ngày nộp
            if (filter.TuNgay.HasValue)
                query = query.Where(h => h.NgayNop >= filter.TuNgay.Value);

            if (filter.DenNgay.HasValue)
            {
                // Bao gồm cả ngày kết thúc (đến 23:59:59)
                var denNgay = filter.DenNgay.Value.Date.AddDays(1).AddSeconds(-1);
                query = query.Where(h => h.NgayNop <= denNgay);
            }

            // Sắp xếp
            query = filter.SapXep switch
            {
                "ngay_nop_asc" => query.OrderBy(h => h.NgayNop),
                "ngay_nop_desc" => query.OrderByDescending(h => h.NgayNop),
                "ten_sv_asc" => query.OrderBy(h => h.SinhVien.HoTen),
                "ten_sv_desc" => query.OrderByDescending(h => h.SinhVien.HoTen),
                _ => query.OrderByDescending(h => h.NgayNop)
            };

            // Phân trang
            int pageSize = filter.KichThuocTrang > 0 ? filter.KichThuocTrang : 10;
            int page = filter.Trang > 0 ? filter.Trang : 1;
            int total = await query.CountAsync();

            filter.TongSoBanGhi = total;
            filter.TongSoTrang = (int)Math.Ceiling((double)total / pageSize);
            if (filter.TongSoTrang == 0) filter.TongSoTrang = 1;
            filter.KichThuocTrang = pageSize;

            filter.DanhSach = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(h => new HoSoItemViewModel
                {
                    MaHoSoHocBong = h.MaHoSoHocBong,
                    TenSinhVien = h.SinhVien.HoTen,
                    TenHocBong = h.HocBong.TenHocBong,
                    TenDonViTaiTro = h.HocBong.DonViTaiTro.TenDonViTaiTro,
                    NgayNop = h.NgayNop,
                    TrangThai = h.TrangThai,
                    DiemTrungBinh = h.SinhVien.DiemTrungBinhTichLuy,
                    DiemYeuCau = h.HocBong.DiemTrungBinhToiThieu
                })
                .ToListAsync();

            return filter;
        }

        // 2. CHI TIẾT HỒ SƠ
        public async Task<HoSoChiTietViewModel?> LayChiTietAsync(int maHoSo)
        {
            var hoSo = await _context.HoSoHocBong
                .Include(h => h.SinhVien)
                .Include(h => h.HocBong)
                    .ThenInclude(hb => hb.DonViTaiTro)
                .FirstOrDefaultAsync(h => h.MaHoSoHocBong == maHoSo);

            if (hoSo == null) return null;

            // Đếm số hồ sơ đã được cấp học bổng của học bổng này
            int soDaCap = await _context.HoSoHocBong
                .CountAsync(h => h.MaHocBong == hoSo.MaHocBong
                              && h.TrangThai == TrangThaiHoSo.DuocCapHocBong);

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
            var hoSo = await _context.HoSoHocBong
                .Include(h => h.SinhVien)
                .Include(h => h.HocBong)
                    .ThenInclude(hb => hb.DonViTaiTro)
                .FirstOrDefaultAsync(h => h.MaHoSoHocBong == maHoSo);

            if (hoSo == null) return null;

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
                ThanhTichHocTap = hoSo.SinhVien.ThanhTichHocTap
            };
        }

        // 4. XỬ LÝ XÉT DUYỆT SƠ BỘ (POST)
        public async Task<(bool ThanhCong, string? Loi)> XetDuyetSoBoAsync(XetDuyetSoBoViewModel model)
        {
            var hoSo = await _context.HoSoHocBong
                .Include(h => h.SinhVien)
                .Include(h => h.HocBong)
                .FirstOrDefaultAsync(h => h.MaHoSoHocBong == model.MaHoSoHocBong);

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

            await _context.SaveChangesAsync();

            return (true, null);
        }

        // 5. ĐẾM SỐ HỒ SƠ ĐÃ ĐƯỢC CẤP HỌC BỔNG
        public async Task<int> DemSoDaCapAsync(int maHocBong)
        {
            return await _context.HoSoHocBong
                .CountAsync(h => h.MaHocBong == maHocBong
                              && h.TrangThai == TrangThaiHoSo.DuocCapHocBong);
        }

        public async Task<List<DonViTaiTro>> LayDanhSachDonViTaiTroAsync()
        {
            return await _context.DonViTaiTro
                .Where(d => d.TrangThai == true)
                .OrderBy(d => d.TenDonViTaiTro)
                .ToListAsync();
        }
    }
}
