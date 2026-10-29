// Họ và tên: Nguyễn Phi Hùng
// Mã sinh viên: 23103100148
// Nội dung thực hiện: Module 3 - quy tắc, kiểm tra (định dạng, dung lượng) và lưu trữ tệp đính kèm hồ sơ.
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.ViewModels;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Services
{
    public static class TaiLieuHoSoRules
    {
        public const long DungLuongToiDa = 10L * 1024 * 1024;       // Tối đa 10 MB mỗi tệp tài liệu
        public const long DungLuongAnhToiDa = 5L * 1024 * 1024;     // Tối đa 5 MB cho ảnh đại diện
        public const int SoTepToiDa = 5;                             // Tối đa 5 tệp cho một hồ sơ
        public const long GioiHanRequest = 60L * 1024 * 1024;       // Giới hạn cả request
        public const string LoaiMacDinh = "Minh chứng";
        public const string DuoiChapNhan = ".pdf,.jpg,.jpeg,.png";

        public static readonly bool BatBuocTaiLieu = false;

        public static readonly string[] LoaiTaiLieuHopLe = { "Bảng điểm", "Minh chứng", "Khác" };

        // Danh sách các đuôi tệp tài liệu cho phép
        public static readonly string[] DuoiTaiLieuHopLe = { ".pdf", ".jpg", ".jpeg", ".png" };

        // Danh sách các đuôi ảnh đại diện cho phép
        public static readonly string[] DuoiAnhHopLe = { ".jpg", ".jpeg", ".png", ".webp" };

        public static string LayMimeType(string duoi)
        {
            return duoi.ToLower() switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }

        public static string ChuanHoaLoai(string? loai) =>
            LoaiTaiLieuHopLe.FirstOrDefault(l => string.Equals(l, loai?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? LoaiMacDinh;
    }

    public sealed record TepDaLuu(string TenFileGoc, string TenFileLuu, string LoaiNoiDung, long DungLuong);

    public static class TaiLieuHoSoStorage
    {
        public static string ThuMuc(string contentRoot) => Path.Combine(contentRoot, "App_Data", "TaiLieuHoSo");

        // Lọc ra các tệp thực sự được người dùng chọn
        public static List<IFormFile> LayTepDuocChon(IEnumerable<IFormFile>? files) =>
            files?.Where(f => f != null && f.Length > 0).ToList() ?? new List<IFormFile>();

        // Lấy tên file an toàn (bỏ đường dẫn thư mục nếu trình duyệt gửi kèm)
        public static string LamSachTenFile(string? ten)
        {
            var tenFile = Path.GetFileName(ten ?? "tailieu");
            return string.IsNullOrWhiteSpace(tenFile) ? "tailieu" : tenFile.Trim();
        }

        // 1. Kiểm tra danh sách tệp tài liệu đính kèm hồ sơ
        public static Task<List<string>> KiemTraAsync(IReadOnlyList<IFormFile> files, int soTepDaCo)
        {
            var danhSachLoi = new List<string>();

            // Kiểm tra số lượng tệp (tối đa 5 tệp)
            if (files.Count + soTepDaCo > TaiLieuHoSoRules.SoTepToiDa)
            {
                danhSachLoi.Add($"Mỗi hồ sơ chỉ được đính kèm tối đa {TaiLieuHoSoRules.SoTepToiDa} tệp.");
            }

            foreach (var file in files)
            {
                var tenFile = LamSachTenFile(file.FileName);
                var duoiFile = Path.GetExtension(tenFile).ToLower();

                // Kiểm tra tệp rỗng
                if (file.Length == 0)
                {
                    danhSachLoi.Add($"Tệp '{tenFile}' bị rỗng (0 KB).");
                    continue;
                }

                // Kiểm tra dung lượng (tối đa 10 MB)
                if (file.Length > TaiLieuHoSoRules.DungLuongToiDa)
                {
                    danhSachLoi.Add($"Tệp '{tenFile}' vượt quá dung lượng tối đa 10 MB.");
                    continue;
                }

                // Kiểm tra định dạng đuôi tệp (.pdf, .jpg, .jpeg, .png)
                if (!TaiLieuHoSoRules.DuoiTaiLieuHopLe.Contains(duoiFile))
                {
                    danhSachLoi.Add($"Tệp '{tenFile}' không đúng định dạng (chỉ nhận PDF, JPG, PNG).");
                }
            }

            return Task.FromResult(danhSachLoi);
        }

        // 2. Kiểm tra tệp ảnh đại diện (trả về chuỗi lỗi nếu sai, trả về null nếu hợp lệ)
        public static string? KiemTraAnhDaiDien(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return "Vui lòng chọn tệp ảnh hợp lệ.";

            var duoiFile = Path.GetExtension(file.FileName).ToLower();

            if (!TaiLieuHoSoRules.DuoiAnhHopLe.Contains(duoiFile))
                return "Ảnh đại diện chỉ chấp nhận định dạng .jpg, .jpeg, .png hoặc .webp.";

            if (file.Length > TaiLieuHoSoRules.DungLuongAnhToiDa)
                return "Dung lượng ảnh đại diện không được vượt quá 5 MB.";

            return null; // Hợp lệ
        }

        // Lưu tệp vào thư mục App_Data/TaiLieuHoSo
        public static async Task<List<TepDaLuu>> LuuAsync(string contentRoot, IEnumerable<IFormFile> files)
        {
            var thuMuc = ThuMuc(contentRoot);
            Directory.CreateDirectory(thuMuc);

            var ketQua = new List<TepDaLuu>();

            foreach (var file in files)
            {
                var tenGoc = LamSachTenFile(file.FileName);
                var duoi = Path.GetExtension(tenGoc).ToLower();
                var tenLuu = Guid.NewGuid().ToString("N") + duoi;
                var duongDan = Path.Combine(thuMuc, tenLuu);

                await using (var stream = new FileStream(duongDan, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                ketQua.Add(new TepDaLuu(tenGoc, tenLuu, TaiLieuHoSoRules.LayMimeType(duoi), file.Length));
            }

            return ketQua;
        }

        public static List<TaiLieuHoSo> TaoBanGhi(int maHoSo, IEnumerable<TepDaLuu> ds, string? loai, int? maTaiKhoan)
        {
            var loaiChuan = TaiLieuHoSoRules.ChuanHoaLoai(loai);
            var bayGio = DateTime.Now;
            return ds.Select(t => new TaiLieuHoSo
            {
                MaHoSoHocBong = maHoSo,
                LoaiTaiLieu = loaiChuan,
                TenFileGoc = t.TenFileGoc,
                TenFileLuu = t.TenFileLuu,
                LoaiNoiDung = t.LoaiNoiDung,
                DungLuong = t.DungLuong,
                NgayTai = bayGio,
                MaTaiKhoanTai = maTaiKhoan
            }).ToList();
        }

        public static string? DuongDan(string contentRoot, string tenFileLuu)
        {
            if (string.IsNullOrWhiteSpace(tenFileLuu)) return null;
            var tenAnToan = Path.GetFileName(tenFileLuu);
            return Path.Combine(ThuMuc(contentRoot), tenAnToan);
        }

        public static void Xoa(string contentRoot, IEnumerable<TepDaLuu> ds)
        {
            foreach (var t in ds) XoaFile(contentRoot, t.TenFileLuu);
        }

        public static void XoaFile(string contentRoot, string tenFileLuu)
        {
            var duongDan = DuongDan(contentRoot, tenFileLuu);
            if (duongDan != null && File.Exists(duongDan))
            {
                File.Delete(duongDan);
            }
        }

        public static Task<List<TaiLieuItem>> DanhSachAsync(QuanLyHocBong_UNETI2_TI17A3HNContext context, int maHoSo) =>
            context.TaiLieuHoSo.AsNoTracking()
                .Where(t => t.MaHoSoHocBong == maHoSo)
                .OrderBy(t => t.NgayTai).ThenBy(t => t.MaTaiLieu)
                .Select(t => new TaiLieuItem
                {
                    MaTaiLieu = t.MaTaiLieu,
                    LoaiTaiLieu = t.LoaiTaiLieu,
                    TenFileGoc = t.TenFileGoc,
                    LoaiNoiDung = t.LoaiNoiDung,
                    DungLuong = t.DungLuong,
                    NgayTai = t.NgayTai
                })
                .ToListAsync();
    }
}
