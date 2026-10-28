// Họ và tên: Nguyễn Phi Hùng
// Mã sinh viên: 23103100148
// Nội dung thực hiện: Module 3 - quy tắc, kiểm tra (định dạng, dung lượng, nội dung thật của tệp) và lưu trữ tệp đính kèm hồ sơ.
// Tệp được lưu NGOÀI wwwroot (App_Data/TaiLieuHoSo) với tên GUID, chỉ truy cập được qua TaiLieuHoSoController (có kiểm tra quyền).
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.ViewModels;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Services
{
    public static class TaiLieuHoSoRules
    {
        public const long DungLuongToiDa = 10L * 1024 * 1024;       // 10 MB mỗi tệp
        public const int SoTepToiDa = 5;                             // tối đa 5 tệp cho một hồ sơ
        public const long GioiHanRequest = 60L * 1024 * 1024;       // giới hạn cả request (5 tệp x 10 MB + dự phòng)
        public const string LoaiMacDinh = "Minh chứng";
        public const string DuoiChapNhan = ".pdf,.jpg,.jpeg,.png";   // dùng cho thuộc tính accept của <input type="file">

        // Đặt true nếu đề/nhóm yêu cầu BẮT BUỘC phải có ít nhất một tệp khi nộp hồ sơ.
        public static readonly bool BatBuocTaiLieu = false;

        public static readonly string[] LoaiTaiLieuHopLe = { "Bảng điểm", "Minh chứng", "Khác" };

        // MIME do server quyết định theo đuôi đã được kiểm tra, KHÔNG tin ContentType do trình duyệt gửi.
        internal static readonly Dictionary<string, string> MimeTheoDuoi = new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png"
        };

        public static string ChuanHoaLoai(string? loai) =>
            LoaiTaiLieuHopLe.FirstOrDefault(l => string.Equals(l, loai?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? LoaiMacDinh;
    }

    /// <summary>Thông tin một tệp đã ghi ra đĩa (chưa chắc đã có bản ghi trong DB).</summary>
    public sealed record TepDaLuu(string TenFileGoc, string TenFileLuu, string LoaiNoiDung, long DungLuong);

    public static class TaiLieuHoSoStorage
    {
        // Ký tự cấm trong tên tệp: cố định để kết quả giống nhau trên mọi hệ điều hành (Windows, Linux).
        private static readonly char[] KyTuCam = Path.GetInvalidFileNameChars().Concat("<>:\"/\\|?*").Distinct().ToArray();

        public static string ThuMuc(string contentRoot) => Path.Combine(contentRoot, "App_Data", "TaiLieuHoSo");

        /// <summary>Bỏ các phần tử rỗng do trình duyệt gửi khi không chọn tệp nào.</summary>
        public static List<IFormFile> LayTepDuocChon(IEnumerable<IFormFile>? files) =>
            files?.Where(f => f != null && (f.Length > 0 || !string.IsNullOrWhiteSpace(f.FileName))).ToList() ?? new List<IFormFile>();

        /// <summary>Làm sạch tên tệp hiển thị: bỏ đường dẫn, ký tự cấm/điều khiển, giới hạn độ dài.</summary>
        public static string LamSachTenFile(string? ten)
        {
            var t = Path.GetFileName((ten ?? string.Empty).Replace('\\', '/'));
            t = string.Concat(t.Select(c => char.IsControl(c) || KyTuCam.Contains(c) ? '_' : c)).Trim();
            var duoi = Path.GetExtension(t);
            var phanTen = Path.GetFileNameWithoutExtension(t).Trim();
            if (phanTen.Length > 200) phanTen = phanTen[..200];
            if (string.IsNullOrWhiteSpace(phanTen)) phanTen = "tailieu";
            return phanTen + duoi;
        }

        /// <summary>Kiểm tra số lượng, dung lượng, đuôi tệp và chữ ký nội dung. Trả về danh sách lỗi (rỗng = hợp lệ).</summary>
        public static async Task<List<string>> KiemTraAsync(IReadOnlyList<IFormFile> files, int soTepDaCo)
        {
            var loi = new List<string>();
            if (files.Count + soTepDaCo > TaiLieuHoSoRules.SoTepToiDa)
                loi.Add($"Mỗi hồ sơ chỉ được đính kèm tối đa {TaiLieuHoSoRules.SoTepToiDa} tệp (đã có {soTepDaCo}, đang chọn thêm {files.Count}).");

            foreach (var f in files)
            {
                var ten = LamSachTenFile(f.FileName);
                var duoi = Path.GetExtension(ten).ToLowerInvariant();

                if (f.Length == 0)
                {
                    loi.Add($"Tệp \"{ten}\" rỗng (0 KB).");
                    continue;
                }
                if (f.Length > TaiLieuHoSoRules.DungLuongToiDa)
                {
                    loi.Add($"Tệp \"{ten}\" ({TaiLieuItem.DinhDang(f.Length)}) vượt quá {TaiLieuHoSoRules.DungLuongToiDa / (1024 * 1024)} MB.");
                    continue;
                }
                if (!TaiLieuHoSoRules.MimeTheoDuoi.ContainsKey(duoi))
                {
                    loi.Add($"Tệp \"{ten}\" không đúng định dạng. Chỉ chấp nhận PDF, JPG, PNG.");
                    continue;
                }
                if (!await KhopChuKyAsync(f, duoi))
                    loi.Add($"Nội dung tệp \"{ten}\" không phải là {duoi.TrimStart('.').ToUpperInvariant()} thật (có thể đã đổi đuôi tệp).");
            }
            return loi;
        }

        // Đọc vài byte đầu để chắc chắn tệp đúng loại (chống đổi đuôi .exe/.html thành .pdf/.jpg).
        private static async Task<bool> KhopChuKyAsync(IFormFile f, string duoi)
        {
            var buf = new byte[8];
            var n = 0;
            await using (var s = f.OpenReadStream())
            {
                while (n < buf.Length)
                {
                    var r = await s.ReadAsync(buf.AsMemory(n));
                    if (r == 0) break;
                    n += r;
                }
            }

            return duoi switch
            {
                ".pdf" => n >= 5 && buf[0] == 0x25 && buf[1] == 0x50 && buf[2] == 0x44 && buf[3] == 0x46 && buf[4] == 0x2D,   // %PDF-
                ".jpg" or ".jpeg" => n >= 3 && buf[0] == 0xFF && buf[1] == 0xD8 && buf[2] == 0xFF,
                ".png" => n >= 8 && buf[0] == 0x89 && buf[1] == 0x50 && buf[2] == 0x4E && buf[3] == 0x47
                                 && buf[4] == 0x0D && buf[5] == 0x0A && buf[6] == 0x1A && buf[7] == 0x0A,
                _ => false
            };
        }

        /// <summary>Ghi tệp ra đĩa với tên GUID. Nếu lỗi giữa chừng, xóa các tệp đã ghi rồi ném lại ngoại lệ.</summary>
        public static async Task<List<TepDaLuu>> LuuAsync(string contentRoot, IEnumerable<IFormFile> files)
        {
            var thuMuc = ThuMuc(contentRoot);
            Directory.CreateDirectory(thuMuc);

            var daLuu = new List<TepDaLuu>();
            var daTao = new List<string>();
            try
            {
                foreach (var f in files)
                {
                    var ten = LamSachTenFile(f.FileName);
                    var duoi = Path.GetExtension(ten).ToLowerInvariant();
                    var tenLuu = Guid.NewGuid().ToString("N") + duoi;
                    var duongDan = Path.Combine(thuMuc, tenLuu);

                    daTao.Add(duongDan);
                    long dungLuong;
                    await using (var fs = new FileStream(duongDan, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                    {
                        await f.CopyToAsync(fs);
                        dungLuong = fs.Length;
                    }
                    daLuu.Add(new TepDaLuu(ten, tenLuu, TaiLieuHoSoRules.MimeTheoDuoi[duoi], dungLuong));
                }
            }
            catch
            {
                foreach (var p in daTao) XoaDuongDan(p);
                throw;
            }
            return daLuu;
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

        /// <summary>Đường dẫn tuyệt đối của tệp; null nếu tên không hợp lệ hoặc nằm ngoài thư mục lưu trữ (chống path traversal).</summary>
        public static string? DuongDan(string contentRoot, string tenFileLuu)
        {
            if (string.IsNullOrWhiteSpace(tenFileLuu) || tenFileLuu != Path.GetFileName(tenFileLuu)) return null;
            if (tenFileLuu.IndexOfAny(new[] { '/', '\\', ':' }) >= 0 || tenFileLuu.Contains("..")) return null;
            var goc = Path.GetFullPath(ThuMuc(contentRoot));
            var day = Path.GetFullPath(Path.Combine(goc, tenFileLuu));
            return day.StartsWith(goc + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ? day : null;
        }

        public static void Xoa(string contentRoot, IEnumerable<TepDaLuu> ds)
        {
            foreach (var t in ds) XoaFile(contentRoot, t.TenFileLuu);
        }

        public static void XoaFile(string contentRoot, string tenFileLuu)
        {
            var p = DuongDan(contentRoot, tenFileLuu);
            if (p != null) XoaDuongDan(p);
        }

        private static void XoaDuongDan(string duongDan)
        {
            try { if (File.Exists(duongDan)) File.Delete(duongDan); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
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
