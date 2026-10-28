// Họ và tên: Nguyễn Phi Hùng
// Mã sinh viên: 23103100148
// Nội dung thực hiện: Module 3 - xem, tải, thêm, xóa tệp đính kèm hồ sơ; phân quyền theo vai trò (Sinh viên / Cán bộ / Admin).
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Helpers;
using QuanLyHocBong_UNETI2_TI17A3HN.Services;
using QuanLyHocBong_UNETI2_TI17A3HN.ViewModels;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Controllers
{
    /// <summary>
    /// Quyền truy cập tệp đính kèm:
    ///  - Sinh viên: xem/tải tệp của hồ sơ CỦA MÌNH; thêm/xóa tệp khi hồ sơ còn "Chờ duyệt". Hồ sơ người khác -> 404.
    ///  - Cán bộ học bổng, Admin: xem/tải tệp của MỌI hồ sơ (phục vụ xét duyệt), không thêm/xóa.
    ///  - Chưa đăng nhập: chuyển về trang đăng nhập. Vai trò khác: 403.
    /// Tệp nằm ngoài wwwroot nên không thể mở bằng đường dẫn trực tiếp.
    /// </summary>
    public class TaiLieuHoSoController : Controller
    {
        private readonly QuanLyHocBong_UNETI2_TI17A3HNContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly ISupabaseStorageService _storageService;

        public TaiLieuHoSoController(
            QuanLyHocBong_UNETI2_TI17A3HNContext context,
            IWebHostEnvironment env,
            ISupabaseStorageService storageService)
        {
            _context = context;
            _env = env;
            _storageService = storageService;
        }

        private sealed record NguoiDung(int MaTaiKhoan, string Quyen, int? MaSinhVien)
        {
            public bool LaSinhVien =>
                Quyen.Equals(VaiTro.SinhVien, StringComparison.OrdinalIgnoreCase) ||
                Quyen.StartsWith("Sinh", StringComparison.OrdinalIgnoreCase);
        }

        // Xác định người dùng từ Session (Module 1 lưu MaTaiKhoan, VaiTro). Trả về (người dùng, kết quả chặn).
        private async Task<(NguoiDung? NguoiDung, IActionResult? Chan)> XacDinhNguoiDungAsync()
        {
            var maTaiKhoan = HttpContext.Session.GetInt32("MaTaiKhoan");
            var quyen = HttpContext.Session.GetString("VaiTro");
            if (maTaiKhoan == null || string.IsNullOrEmpty(quyen))
                return (null, RedirectToAction("DangNhap", "TaiKhoan"));

            if (quyen.Equals(VaiTro.SinhVien, StringComparison.OrdinalIgnoreCase) ||
                quyen.StartsWith("Sinh", StringComparison.OrdinalIgnoreCase))
            {
                var maSv = await _context.SinhVien.AsNoTracking()
                    .Where(s => s.MaTaiKhoan == maTaiKhoan.Value)
                    .Select(s => (int?)s.MaSinhVien)
                    .FirstOrDefaultAsync();
                if (maSv == null) return (null, NotFound());
                return (new NguoiDung(maTaiKhoan.Value, quyen, maSv), null);
            }

            if (quyen.Equals(VaiTro.Admin, StringComparison.OrdinalIgnoreCase) ||
                quyen.Equals(VaiTro.CanBoHocBong, StringComparison.OrdinalIgnoreCase) ||
                quyen.StartsWith("C", StringComparison.OrdinalIgnoreCase) ||
                quyen.StartsWith("Admin", StringComparison.OrdinalIgnoreCase))
                return (new NguoiDung(maTaiKhoan.Value, quyen, null), null);

            return (null, StatusCode(StatusCodes.Status403Forbidden));
        }

        // GET: /TaiLieuHoSo/Xem/5  -> mở ngay trên trình duyệt (PDF/ảnh)
        public Task<IActionResult> Xem(int id) => PhucVuTepAsync(id, hienTrucTiep: true);

        // GET: /TaiLieuHoSo/Tai/5  -> tải về
        public Task<IActionResult> Tai(int id) => PhucVuTepAsync(id, hienTrucTiep: false);

        private async Task<IActionResult> PhucVuTepAsync(int id, bool hienTrucTiep)
        {
            var (nd, chan) = await XacDinhNguoiDungAsync();
            if (chan != null) return chan;

            var tl = await _context.TaiLieuHoSo.AsNoTracking()
                .Where(t => t.MaTaiLieu == id)
                .Select(t => new
                {
                    t.TenFileGoc,
                    t.TenFileLuu,
                    t.LoaiNoiDung,
                    MaSinhVienSoHuu = t.HoSoHocBong.MaSinhVien,
                    FileMinhChungCloud = t.HoSoHocBong.FileMinhChung
                })
                .FirstOrDefaultAsync();
            if (tl == null) return NotFound();

            // Sinh viên chỉ mở được tệp của hồ sơ mình; tệp của người khác trả 404 (không lộ là có tồn tại).
            if (nd!.LaSinhVien && tl.MaSinhVienSoHuu != nd.MaSinhVien) return NotFound();

            var duongDan = TaiLieuHoSoStorage.DuongDan(_env.ContentRootPath, tl.TenFileLuu);
            if (duongDan == null || !System.IO.File.Exists(duongDan))
            {
                if (!string.IsNullOrEmpty(tl.FileMinhChungCloud) &&
                    tl.FileMinhChungCloud.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                {
                    return Redirect(tl.FileMinhChungCloud);
                }
                return NotFound();
            }

            var cd = new ContentDispositionHeaderValue(hienTrucTiep ? "inline" : "attachment");
            cd.SetHttpFileName(tl.TenFileGoc);
            Response.Headers[HeaderNames.ContentDisposition] = cd.ToString();
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers[HeaderNames.CacheControl] = "private, no-store";

            // Content-Type lấy từ DB (do server quyết định lúc tải lên), không dựa vào tên tệp người dùng.
            return PhysicalFile(duongDan, tl.LoaiNoiDung, enableRangeProcessing: true);
        }

        // GET: /TaiLieuHoSo/DanhSach/5  -> cán bộ/Admin xem toàn bộ tệp của một hồ sơ (id = mã hồ sơ)
        public async Task<IActionResult> DanhSach(int id)
        {
            var (nd, chan) = await XacDinhNguoiDungAsync();
            if (chan != null) return chan;

            // Sinh viên dùng trang chi tiết hồ sơ của chính mình (trang đó tự kiểm tra chủ sở hữu).
            if (nd!.LaSinhVien) return RedirectToAction("Details", "HoSoCuaToi", new { id });

            var hoSo = await _context.HoSoHocBong.AsNoTracking()
                .Where(h => h.MaHoSoHocBong == id)
                .Select(h => new TaiLieuHoSoNhanVienViewModel
                {
                    MaHoSoHocBong = h.MaHoSoHocBong,
                    MaSinhVien = h.MaSinhVien,
                    HoTenSinhVien = h.SinhVien.HoTen,
                    TenHocBong = h.HocBong.TenHocBong,
                    TrangThai = h.TrangThai,
                    NgayNop = h.NgayNop
                })
                .FirstOrDefaultAsync();
            if (hoSo == null) return NotFound();

            hoSo.TaiLieu = new TaiLieuHoSoListViewModel
            {
                MaHoSoHocBong = id,
                Items = await TaiLieuHoSoStorage.DanhSachAsync(_context, id),
                CoTheThem = false,
                CoTheXoa = false
            };
            ViewData["Title"] = "Tài liệu hồ sơ";
            return View(hoSo);
        }

        // POST: /TaiLieuHoSo/Them  -> sinh viên thêm tệp vào hồ sơ của mình (chỉ khi còn Chờ duyệt)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(TaiLieuHoSoRules.GioiHanRequest)]
        [RequestFormLimits(MultipartBodyLengthLimit = TaiLieuHoSoRules.GioiHanRequest)]
        public async Task<IActionResult> Them(int maHoSo, string? loai, List<IFormFile>? taiLieus)
        {
            var (nd, chan) = await XacDinhNguoiDungAsync();
            if (chan != null) return chan;
            if (!nd!.LaSinhVien) return StatusCode(StatusCodes.Status403Forbidden);   // cán bộ chỉ xem/tải
            var maSv = nd.MaSinhVien!.Value;

            var hoSo = await _context.HoSoHocBong
                .FirstOrDefaultAsync(h => h.MaHoSoHocBong == maHoSo && h.MaSinhVien == maSv);
            if (hoSo == null) return NotFound();

            var quayLai = RedirectToAction("Details", "HoSoCuaToi", new { id = maHoSo });

            if (hoSo.TrangThai != TrangThaiHoSo.ChoDuyet)
            {
                TempData["Loi"] = $"Hồ sơ đang ở trạng thái \"{hoSo.TrangThai}\", không thể thêm tệp đính kèm.";
                return quayLai;
            }

            var tep = TaiLieuHoSoStorage.LayTepDuocChon(taiLieus);
            if (tep.Count == 0)
            {
                TempData["Loi"] = "Vui lòng chọn ít nhất một tệp để tải lên.";
                return quayLai;
            }

            var soDaCo = await _context.TaiLieuHoSo.CountAsync(t => t.MaHoSoHocBong == maHoSo);
            var loi = await TaiLieuHoSoStorage.KiemTraAsync(tep, soDaCo);
            if (loi.Any())
            {
                TempData["Loi"] = string.Join("\n", loi);
                return quayLai;
            }

            var daLuu = new List<TepDaLuu>();
            try
            {
                daLuu = await TaiLieuHoSoStorage.LuuAsync(_env.ContentRootPath, tep);
                if (string.IsNullOrEmpty(hoSo.FileMinhChung))
                {
                    var cloudUrl = await _storageService.UploadFileAsync(tep[0], "minhchung");
                    if (!string.IsNullOrEmpty(cloudUrl))
                    {
                        hoSo.FileMinhChung = cloudUrl;
                    }
                }
                _context.TaiLieuHoSo.AddRange(TaiLieuHoSoStorage.TaoBanGhi(maHoSo, daLuu, loai, nd.MaTaiKhoan));
                await _context.SaveChangesAsync();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DbUpdateException)
            {
                TaiLieuHoSoStorage.Xoa(_env.ContentRootPath, daLuu);   // không để lại tệp mồ côi
                TempData["Loi"] = "Không thể lưu tệp đính kèm. Vui lòng thử lại.";
                return quayLai;
            }

            TempData["ThongBao"] = $"Đã tải lên {daLuu.Count} tệp.";
            return quayLai;
        }

        // POST: /TaiLieuHoSo/Xoa/5  -> sinh viên xóa tệp của hồ sơ mình (chỉ khi còn Chờ duyệt)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Xoa(int id)
        {
            var (nd, chan) = await XacDinhNguoiDungAsync();
            if (chan != null) return chan;
            if (!nd!.LaSinhVien) return StatusCode(StatusCodes.Status403Forbidden);
            var maSv = nd.MaSinhVien!.Value;

            var tl = await _context.TaiLieuHoSo
                .Include(t => t.HoSoHocBong)
                .FirstOrDefaultAsync(t => t.MaTaiLieu == id && t.HoSoHocBong.MaSinhVien == maSv);
            if (tl == null) return NotFound();

            var maHoSo = tl.MaHoSoHocBong;
            var quayLai = RedirectToAction("Details", "HoSoCuaToi", new { id = maHoSo });

            if (tl.HoSoHocBong.TrangThai != TrangThaiHoSo.ChoDuyet)
            {
                TempData["Loi"] = $"Hồ sơ đang ở trạng thái \"{tl.HoSoHocBong.TrangThai}\", không thể xóa tệp đính kèm.";
                return quayLai;
            }

            var tenLuu = tl.TenFileLuu;
            _context.TaiLieuHoSo.Remove(tl);
            await _context.SaveChangesAsync();
            TaiLieuHoSoStorage.XoaFile(_env.ContentRootPath, tenLuu);   // xóa tệp vật lý sau khi DB đã xóa thành công

            TempData["ThongBao"] = "Đã xóa tệp đính kèm.";
            return quayLai;
        }
    }
}
