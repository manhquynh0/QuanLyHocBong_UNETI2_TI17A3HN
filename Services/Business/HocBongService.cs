// Họ và tên: Nguyễn Văn Tình
// Mã sinh viên: 23103100142
// Nội dung thực hiện: Module 2 - Xử lý nghiệp vụ quản lý học bổng, tìm kiếm, lọc, sắp xếp và phân trang bằng LINQ.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Constants;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.Repositories.Abstractions;
using QuanLyHocBong_UNETI2_TI17A3HN.Services.Interface;
using QuanLyHocBong_UNETI2_TI17A3HN.ViewModels.HocBong;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Services.Implementations
{
    public class HocBongService : IHocBongService
    {
        private const int KichThuocTrang = 10;
        private readonly IUnitOfWork _unitOfWork;

        public HocBongService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<HocBongIndexViewModel> LayDanhSachAsync(HocBongIndexViewModel filters)
        {
            var query = _unitOfWork.HocBongs.QueryActiveWithSponsor(asNoTracking: true);

            var tuKhoa = filters.TuKhoa?.Trim();
            if (!string.IsNullOrEmpty(tuKhoa))
            {
                query = query.Where(h =>
                    h.TenHocBong.Contains(tuKhoa)
                    || h.DonViTaiTro.TenDonViTaiTro.Contains(tuKhoa));
                filters.TuKhoa = tuKhoa;
            }

            if (filters.MaDonViTaiTro.HasValue)
            {
                query = query.Where(h => h.MaDonViTaiTro == filters.MaDonViTaiTro.Value);
            }

            if (!string.IsNullOrWhiteSpace(filters.BacDaoTao))
            {
                query = query.Where(h => h.BacDaoTao == filters.BacDaoTao);
            }

            var today = DateTime.Today;
            filters.TrangThai = filters.TrangThai is TrangThaiHocBong.ChuaMo
                or TrangThaiHocBong.SapMo
                or TrangThaiHocBong.DangNhanHoSo
                or TrangThaiHocBong.TamDung
                or TrangThaiHocBong.DaDong
                    ? filters.TrangThai
                    : null;

            if (filters.TrangThai is TrangThaiHocBong.ChuaMo or TrangThaiHocBong.SapMo)
            {
                query = query.Where(h => h.TrangThai == TrangThaiHocBong.ChuaMo || h.TrangThai == TrangThaiHocBong.SapMo || h.NgayBatDauNhanHoSo > today);
            }
            else if (filters.TrangThai == TrangThaiHocBong.DangNhanHoSo)
            {
                query = query.Where(h => h.TrangThai == TrangThaiHocBong.DangNhanHoSo && h.NgayBatDauNhanHoSo <= today && h.HanNopHoSo >= today);
            }
            else if (filters.TrangThai == TrangThaiHocBong.TamDung)
            {
                query = query.Where(h => h.TrangThai == TrangThaiHocBong.TamDung);
            }
            else if (filters.TrangThai == TrangThaiHocBong.DaDong)
            {
                query = query.Where(h => h.TrangThai == TrangThaiHocBong.DaDong || h.HanNopHoSo < today);
            }

            if (filters.DiemToiThieu is >= 0 and <= 4)
            {
                query = query.Where(h => h.DiemTrungBinhToiThieu >= filters.DiemToiThieu.Value);
            }

            if (filters.HanNop == "con-han")
            {
                query = query.Where(h => h.HanNopHoSo >= today);
            }
            else if (filters.HanNop == "het-han")
            {
                query = query.Where(h => h.HanNopHoSo < today);
            }

            filters.SapXep = filters.SapXep switch
            {
                "ten-az" or "ten-za" or "han-tang" or "han-giam" or "suat-tang" or "suat-giam"
                    => filters.SapXep,
                _ => "ma-tang"
            };

            query = filters.SapXep switch
            {
                "ten-az" => query.OrderBy(h => h.TenHocBong).ThenBy(h => h.MaHocBong),
                "ten-za" => query.OrderByDescending(h => h.TenHocBong).ThenBy(h => h.MaHocBong),
                "han-tang" => query.OrderBy(h => h.HanNopHoSo).ThenBy(h => h.MaHocBong),
                "han-giam" => query.OrderByDescending(h => h.HanNopHoSo).ThenBy(h => h.MaHocBong),
                "suat-tang" => query.OrderBy(h => h.SoSuat).ThenBy(h => h.MaHocBong),
                "suat-giam" => query.OrderByDescending(h => h.SoSuat).ThenBy(h => h.MaHocBong),
                _ => query.OrderBy(h => h.MaHocBong)
            };

            filters.SoKetQua = await query.CountAsync();
            filters.KichThuocTrang = KichThuocTrang;
            filters.TongSoTrang = Math.Max(1, (int)Math.Ceiling(filters.SoKetQua / (double)KichThuocTrang));
            filters.Trang = Math.Clamp(filters.Trang, 1, filters.TongSoTrang);
            filters.HocBongs = await query
                .Skip((filters.Trang - 1) * KichThuocTrang)
                .Take(KichThuocTrang)
                .ToListAsync();
            foreach (var hocBong in filters.HocBongs)
            {
                hocBong.TrangThai = TrangThaiHocBong.TinhTrang(
                    hocBong.NgayBatDauNhanHoSo,
                    hocBong.HanNopHoSo,
                    today,
                    hocBong.TrangThai);
            }

            await PopulateFilterOptionsAsync(filters);
            return filters;
        }

        public Task<List<DonViTaiTro>> LayDanhSachDonViTaiTroAsync()
        {
            return _unitOfWork.DonViTaiTros.Query(asNoTracking: true)
                .OrderBy(d => d.TenDonViTaiTro)
                .ToListAsync();
        }

        public async Task<HocBong?> LayTheoMaAsync(int maHocBong)
        {
            var hocBong = await _unitOfWork.HocBongs.GetByIdWithSponsorAsync(maHocBong);
            if (hocBong is not null)
            {
                hocBong.TrangThai = TrangThaiHocBong.TinhTrang(
                    hocBong.NgayBatDauNhanHoSo,
                    hocBong.HanNopHoSo,
                    DateTime.Today,
                    hocBong.TrangThai);
            }

            return hocBong;
        }

        public Task<int> DemSoHoSoAsync(int maHocBong)
        {
            return _unitOfWork.HoSoHocBongs.CountByHocBongAsync(maHocBong);
        }

        public async Task<(bool ThanhCong, string? Loi)> TaoAsync(HocBong hocBong)
        {
            var loi = await KiemTraDuLieuAsync(hocBong);
            if (loi is not null)
            {
                return (false, loi);
            }

            hocBong.TrangThai = TrangThaiHocBong.TinhTrang(
                hocBong.NgayBatDauNhanHoSo,
                hocBong.HanNopHoSo,
                DateTime.Today,
                hocBong.TrangThai);
            await _unitOfWork.HocBongs.AddAsync(hocBong);
            await _unitOfWork.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool ThanhCong, string? Loi)> CapNhatAsync(int maHocBong, HocBong hocBong)
        {
            var hocBongHienTai = await _unitOfWork.HocBongs.GetActiveByIdAsync(maHocBong);
            if (hocBongHienTai is null)
            {
                return (false, "Không tìm thấy học bổng cần cập nhật.");
            }

            var loi = await KiemTraDuLieuAsync(hocBong);
            if (loi is not null)
            {
                return (false, loi);
            }

            var coHoSoDangKy = await _unitOfWork.HoSoHocBongs.AnyAsync(h => h.MaHocBong == maHocBong);
            if (coHoSoDangKy
                && (hocBong.NgayBatDauNhanHoSo.Date != hocBongHienTai.NgayBatDauNhanHoSo.Date
                    || hocBong.HanNopHoSo.Date != hocBongHienTai.HanNopHoSo.Date))
            {
                return (false, "Không thể thay đổi thời gian nhận hồ sơ vì học bổng đã có hồ sơ đăng ký.");
            }

            var soHoSoDuocCap = await _unitOfWork.HoSoHocBongs.CountApprovedByHocBongAsync(maHocBong);
            if (hocBong.SoSuat < soHoSoDuocCap)
            {
                return (false, $"Số suất không thể nhỏ hơn {soHoSoDuocCap}, là số hồ sơ đã được cấp học bổng.");
            }

            hocBongHienTai.TenHocBong = hocBong.TenHocBong;
            hocBongHienTai.MaDonViTaiTro = hocBong.MaDonViTaiTro;
            hocBongHienTai.SoSuat = hocBong.SoSuat;
            hocBongHienTai.BacDaoTao = hocBong.BacDaoTao;
            hocBongHienTai.DiemTrungBinhToiThieu = hocBong.DiemTrungBinhToiThieu;
            hocBongHienTai.NgayBatDauNhanHoSo = hocBong.NgayBatDauNhanHoSo;
            hocBongHienTai.HanNopHoSo = hocBong.HanNopHoSo;
            hocBongHienTai.MoTaHocBong = hocBong.MoTaHocBong;
            hocBongHienTai.DieuKienXetHocBong = hocBong.DieuKienXetHocBong;
            hocBongHienTai.TrangThai = TrangThaiHocBong.TinhTrang(
                hocBong.NgayBatDauNhanHoSo,
                hocBong.HanNopHoSo,
                DateTime.Today,
                hocBong.TrangThai);

            await _unitOfWork.SaveChangesAsync();
            return (true, null);
        }

        public async Task<(bool ThanhCong, string? Loi)> XoaAsync(int maHocBong)
        {
            var hocBong = await _unitOfWork.HocBongs.GetActiveByIdAsync(maHocBong);
            if (hocBong is null)
            {
                return (false, "Không tìm thấy học bổng cần xóa.");
            }

            hocBong.DaXoa = true;
            await _unitOfWork.SaveChangesAsync();
            return (true, null);
        }

        private async Task<string?> KiemTraDuLieuAsync(HocBong hocBong)
        {
            if (hocBong.HanNopHoSo.Date < hocBong.NgayBatDauNhanHoSo.Date)
            {
                return "Hạn nộp hồ sơ không thể trước ngày bắt đầu nhận hồ sơ.";
            }

            if (!await _unitOfWork.DonViTaiTros.AnyAsync(d => d.MaDonViTaiTro == hocBong.MaDonViTaiTro))
            {
                return "Đơn vị tài trợ đã chọn không tồn tại.";
            }

            // Mục 6.2: Không cho chuyển học bổng sang Đang nhận hồ sơ nếu dữ liệu bắt buộc chưa hợp lệ hoặc đã quá hạn
            if (hocBong.TrangThai == TrangThaiHocBong.DangNhanHoSo)
            {
                if (string.IsNullOrWhiteSpace(hocBong.TenHocBong)
                    || hocBong.SoSuat <= 0
                    || hocBong.DiemTrungBinhToiThieu is < 0 or > 4
                    || hocBong.HanNopHoSo.Date < DateTime.Today)
                {
                    return "Không thể chuyển học bổng sang trạng thái 'Đang nhận hồ sơ' khi hạn nộp đã qua hoặc dữ liệu bắt buộc chưa hợp lệ.";
                }
            }

            return null;
        }

        private async Task PopulateFilterOptionsAsync(HocBongIndexViewModel model)
        {
            model.DonViTaiTroOptions = await _unitOfWork.DonViTaiTros.Query(asNoTracking: true)
                .OrderBy(d => d.TenDonViTaiTro)
                .Select(d => new SelectListItem
                {
                    Value = d.MaDonViTaiTro.ToString(),
                    Text = d.TenDonViTaiTro
                })
                .ToListAsync();

            var bacDaoTaos = await _unitOfWork.HocBongs.GetDistinctBacDaoTaoAsync();
            model.BacDaoTaoOptions = bacDaoTaos
                .Select(value => new SelectListItem { Value = value, Text = value })
                .ToList();

            model.TrangThaiOptions = new[]
                {
                    TrangThaiHocBong.ChuaMo,
                    TrangThaiHocBong.DangNhanHoSo,
                    TrangThaiHocBong.TamDung,
                    TrangThaiHocBong.DaDong
                }
                .Select(value => new SelectListItem { Value = value, Text = value })
                .ToList();
        }
    }
}
