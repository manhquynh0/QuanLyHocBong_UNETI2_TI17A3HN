// Họ và tên: Nguyễn Phi Hùng
// Mã sinh viên: 23103100148
// Nội dung thực hiện: Module 3 - nộp hồ sơ xin học bổng, theo dõi hồ sơ đã nộp và hủy hồ sơ của chính sinh viên.
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Helpers;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.Services;
using QuanLyHocBong_UNETI2_TI17A3HN.ViewModels;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Controllers
{
    public class HoSoCuaToiController : SinhVienBaseController
    {
        private readonly IWebHostEnvironment _env;
        private readonly ISupabaseStorageService _storageService;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;

        public HoSoCuaToiController(
            QuanLyHocBong_UNETI2_TI17A3HNContext context,
            IWebHostEnvironment env,
            ISupabaseStorageService storageService,
            INotificationService notificationService,
            IEmailService emailService) : base(context)
        {
            _env = env;
            _storageService = storageService;
            _notificationService = notificationService;
            _emailService = emailService;
        }

        // Truy vấn hồ sơ của MỘT sinh viên -> mọi action đọc dữ liệu đều đi qua đây nên không lộ hồ sơ người khác.
        private IQueryable<HoSoDaNopViewModel> HoSoCuaSinhVien(int maSinhVien)
        {
            return _context.HoSoHocBong.AsNoTracking()
                .Where(h => h.MaSinhVien == maSinhVien)
                .Select(h => new HoSoDaNopViewModel
                {
                    MaHoSoHocBong = h.MaHoSoHocBong,
                    MaHocBong = h.MaHocBong,
                    MaSinhVien = h.MaSinhVien,
                    HoTenSinhVien = h.SinhVien.HoTen,
                    Nganh = h.SinhVien.Nganh,
                    BacDaoTao = h.SinhVien.BacDaoTao,
                    DiemTrungBinhTichLuy = h.SinhVien.DiemTrungBinhTichLuy,
                    TenHocBong = h.HocBong.TenHocBong,
                    TenDonViTaiTro = h.HocBong.DonViTaiTro.TenDonViTaiTro,
                    NgayNop = h.NgayNop,
                    TrangThai = h.TrangThai,
                    NgayXuLy = h.NgayXuLy,
                    NhanXetXetDuyet = h.NhanXetXetDuyet,
                    ThuXinHocBong = h.ThuXinHocBong,
                    GhiChu = h.GhiChu,
                    LichPhongVan = h.LichPhongVans
                        .Where(l => l.TrangThai != TrangThaiLich.DaHuy)
                        .OrderByDescending(l => l.ThoiGianBatDau)
                        .Select(l => new LichPhongVanItem
                        {
                            ThoiGianBatDau = l.ThoiGianBatDau,
                            ThoiGianKetThuc = l.ThoiGianKetThuc,
                            HinhThucPhongVan = l.HinhThucPhongVan,
                            DiaDiemHoacLienKet = l.DiaDiemHoacLienKet,
                            NguoiPhongVan = l.NguoiPhongVan,
                            GhiChu = l.GhiChu,
                            TrangThai = l.TrangThai
                        })
                        .FirstOrDefault(),
                    KetQua = h.KetQuaHocBong == null ? null : new KetQuaItem
                    {
                        KetQua = h.KetQuaHocBong.KetQua,
                        DiemDanhGia = h.KetQuaHocBong.DiemDanhGia,
                        NhanXet = h.KetQuaHocBong.NhanXet,
                        NgayCapNhat = h.KetQuaHocBong.NgayCapNhat
                    }
                });
        }

        // GET: /HoSoHocBong  -> danh sách hồ sơ đã nộp
        public async Task<IActionResult> Index()
        {
            var sv = await LaySinhVienHienTaiAsync();
            if (sv == null) return RedirectToAction("Index", "HoSoCaNhan");

            var list = await HoSoCuaSinhVien(sv.MaSinhVien)
                .OrderByDescending(h => h.NgayNop)
                .ToListAsync();
            return View(list);
        }

        // GET: /HoSoHocBong/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var sv = await LaySinhVienHienTaiAsync();
            if (sv == null) return RedirectToAction("Index", "HoSoCaNhan");

            var hoSo = await HoSoCuaSinhVien(sv.MaSinhVien)
                .FirstOrDefaultAsync(h => h.MaHoSoHocBong == id);
            if (hoSo == null) return NotFound();   // hồ sơ của người khác cũng trả về NotFound

            hoSo.TaiLieus = await TaiLieuHoSoStorage.DanhSachAsync(_context, id);   // tệp đính kèm của chính hồ sơ này
            return View(hoSo);
        }

        // GET: /HoSoHocBong/Create?maHocBong=1
        public async Task<IActionResult> Create(int maHocBong)
        {
            var sv = await LaySinhVienHienTaiAsync();
            if (sv == null) return RedirectToAction("Index", "HoSoCaNhan");   // chưa có hồ sơ sinh viên
            var hb = await _context.HocBong.AsNoTracking()
                .Include(h => h.DonViTaiTro)
                .FirstOrDefaultAsync(h => h.MaHocBong == maHocBong);
            if (hb == null) return NotFound();

            var model = new NopHoSoViewModel { MaHocBong = maHocBong };
            if (sv != null && string.IsNullOrWhiteSpace(model.GhiChu))
                model.GhiChu = sv.ThanhTichHocTap;
            await NapDuLieuHienThiAsync(model, sv, hb);
            return View(model);
        }

        // POST: /HoSoHocBong/Create  (multipart/form-data: có tệp đính kèm)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(TaiLieuHoSoRules.GioiHanRequest)]
        [RequestFormLimits(MultipartBodyLengthLimit = TaiLieuHoSoRules.GioiHanRequest)]
        public async Task<IActionResult> Create(NopHoSoViewModel model)
        {
            var sv = await LaySinhVienHienTaiAsync();
            if (sv == null) return RedirectToAction("Index", "HoSoCaNhan");
            var hb = await _context.HocBong.AsNoTracking()
                .Include(h => h.DonViTaiTro)
                .FirstOrDefaultAsync(h => h.MaHocBong == model.MaHocBong);
            if (hb == null) return NotFound();

            // Kiểm tra tệp đính kèm (số lượng, dung lượng, định dạng, nội dung thật) TRƯỚC khi làm gì khác.
            var tep = TaiLieuHoSoStorage.LayTepDuocChon(model.TaiLieus);
            foreach (var loiTep in await TaiLieuHoSoStorage.KiemTraAsync(tep, 0))
                ModelState.AddModelError(nameof(model.TaiLieus), loiTep);
            if (TaiLieuHoSoRules.BatBuocTaiLieu && tep.Count == 0)
                ModelState.AddModelError(nameof(model.TaiLieus), "Vui lòng đính kèm ít nhất một tệp (bảng điểm hoặc minh chứng).");

            if (!ModelState.IsValid)
            {
                await NapDuLieuHienThiAsync(model, sv, hb);
                return View(model);
            }

            // Ghi tệp ra đĩa trước (tên GUID, ngoài wwwroot) để giao dịch DB bên dưới ngắn gọn.
            List<TepDaLuu> daLuu;
            try
            {
                daLuu = await TaiLieuHoSoStorage.LuuAsync(_env.ContentRootPath, tep);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                ModelState.AddModelError(string.Empty, "Không thể lưu tệp đính kèm. Vui lòng thử lại.");
                await NapDuLieuHienThiAsync(model, sv, hb);
                return View(model);
            }

            string? cloudUrl = null;
            if (tep.Count > 0)
            {
                cloudUrl = await _storageService.UploadFileAsync(tep[0], "minhchung");
            }

            var daLuuVaoDb = false;
            try
            {
                // Kiểm tra điều kiện + tạo hồ sơ + bản ghi tệp trong CÙNG một transaction Serializable:
                // bấm "Nộp" hai lần liên tiếp (hoặc mở 2 tab) cũng không tạo ra hai hồ sơ trùng.
                await using var tx = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

                // Kiểm tra nghiệp vụ lại ở server (không tin giao diện)
                var loi = await NopHoSoRules.KiemTraAsync(_context, sv, hb, DateTime.Now);
                if (loi.Any())
                {
                    await tx.RollbackAsync();
                    await NapDuLieuHienThiAsync(model, sv, hb);
                    return View(model);
                }

                var hoSo = new HoSoHocBong
                {
                    MaSinhVien = sv.MaSinhVien,
                    MaHocBong = hb.MaHocBong,
                    NgayNop = DateTime.Now,                       // do hệ thống xác định
                    ThuXinHocBong = model.ThuXinHocBong.Trim(),
                    GhiChu = model.GhiChu?.Trim() ?? string.Empty, // cột DB là NOT NULL
                    TrangThai = TrangThaiHoSo.ChoDuyet,           // hồ sơ mới luôn là Chờ duyệt
                    FileMinhChung = cloudUrl
                };
                _context.HoSoHocBong.Add(hoSo);
                await _context.SaveChangesAsync();

                if (daLuu.Count > 0)
                {
                    _context.TaiLieuHoSo.AddRange(
                        TaiLieuHoSoStorage.TaoBanGhi(hoSo.MaHoSoHocBong, daLuu, model.LoaiTaiLieu, MaTaiKhoanHienTai));
                    await _context.SaveChangesAsync();
                }

                await tx.CommitAsync();
                daLuuVaoDb = true;

                // Gửi thông báo Realtime (SignalR) cho Cán bộ & Admin và cho Sinh viên
                try
                {
                    await _notificationService.GuiThongBaoChoCanBoAsync(
                        tieuDe: "Hồ sơ ứng tuyển mới",
                        noiDung: $"Sinh viên {sv.HoTen} vừa nộp hồ sơ xin học bổng \"{hb.TenHocBong}\".",
                        loaiThongBao: "HoSo",
                        lienKet: $"/HoSoHocBong/Details/{hoSo.MaHoSoHocBong}",
                        maHoSoHocBong: hoSo.MaHoSoHocBong
                    );

                    await _notificationService.GuiThongBaoAsync(
                        maTaiKhoan: sv.MaTaiKhoan,
                        tieuDe: "Nộp hồ sơ thành công",
                        noiDung: $"Bạn đã nộp thành công hồ sơ xin học bổng \"{hb.TenHocBong}\". Trạng thái: Chờ duyệt.",
                        loaiThongBao: "HoSo",
                        lienKet: $"/HoSoCuaToi/Details/{hoSo.MaHoSoHocBong}",
                        maHoSoHocBong: hoSo.MaHoSoHocBong
                    );
                }
                catch
                {
                    // Lỗi thông báo không làm gián đoạn nghiệp vụ chính
                }

                // Gửi email xác nhận nộp hồ sơ thành công tới sinh viên
                try
                {
                    var studentEmail = !string.IsNullOrWhiteSpace(sv.Email) ? sv.Email : null;
                    if (!string.IsNullOrWhiteSpace(studentEmail))
                    {
                        var subject = $"[UNETI] Xác nhận tiếp nhận hồ sơ xin học bổng #{hoSo.MaHoSoHocBong} - {hb.TenHocBong}";
                        var message = $@"
                            <div style='background: #1e3a8a; color: #ffffff; padding: 18px 20px; border-radius: 8px; margin-bottom: 20px; text-align: center;'>
                                <h2 style='margin: 0; font-size: 18px; font-weight: 700;'>XÁC NHẬN TIẾP NHẬN HỒ SƠ ỨNG TUYỂN</h2>
                            </div>
                            <p>Kính gửi sinh viên <strong>{sv.HoTen}</strong>,</p>
                            <p>Cổng thông tin Học bổng UNETI xác nhận đã tiếp nhận thành công hồ sơ xin học bổng của bạn với các thông tin sau:</p>
                            <ul>
                                <li><strong>Mã hồ sơ:</strong> #{hoSo.MaHoSoHocBong}</li>
                                <li><strong>Chương trình:</strong> {hb.TenHocBong}</li>
                                <li><strong>Đơn vị tài trợ:</strong> {hb.DonViTaiTro?.TenDonViTaiTro}</li>
                                <li><strong>Thời gian nộp:</strong> {hoSo.NgayNop:HH:mm dd/MM/yyyy}</li>
                                <li><strong>Trạng thái hồ sơ:</strong> Chờ xét duyệt sơ bộ</li>
                            </ul>
                            <p>Hội đồng xét duyệt sẽ tiến hành thẩm tra thông tin và minh chứng đính kèm. Vui lòng theo dõi email và cổng thông tin sinh viên thường xuyên để cập nhật kết quả sơ tuyển.</p>
                            <p>Trân trọng,<br/><strong>Hội đồng Xét duyệt Học bổng UNETI</strong></p>";

                        await _emailService.SendEmailAsync(studentEmail, subject, message);
                    }
                }
                catch
                {
                    // Lỗi gửi email không làm ảnh hưởng nghiệp vụ chính
                }
            }
            finally
            {
                // Nộp không thành công (điều kiện sai hoặc lỗi) -> xóa tệp đã ghi để không để lại tệp mồ côi.
                if (!daLuuVaoDb) TaiLieuHoSoStorage.Xoa(_env.ContentRootPath, daLuu);
            }

            TempData["ThongBao"] = $"Đã nộp hồ sơ xin học bổng \"{hb.TenHocBong}\". Hồ sơ đang ở trạng thái Chờ duyệt.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /HoSoHocBong/Huy/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Huy(int id)
        {
            var sv = await LaySinhVienHienTaiAsync();
            if (sv == null) return RedirectToAction("Index", "HoSoCaNhan");

            var hoSo = await _context.HoSoHocBong
                .Include(h => h.LichPhongVans)
                .FirstOrDefaultAsync(h => h.MaHoSoHocBong == id && h.MaSinhVien == sv.MaSinhVien);
            if (hoSo == null) return NotFound();

            var coLichConHieuLuc = hoSo.LichPhongVans.Any(l => l.TrangThai != TrangThaiLich.DaHuy);
            var duocHuy = hoSo.TrangThai == TrangThaiHoSo.ChoDuyet
                || (hoSo.TrangThai == TrangThaiHoSo.DatSoBo && !coLichConHieuLuc);

            if (!duocHuy)
            {
                TempData["Loi"] = $"Hồ sơ ở trạng thái \"{hoSo.TrangThai}\" không thể hủy.";
                return RedirectToAction(nameof(Index));
            }

            hoSo.TrangThai = TrangThaiHoSo.DaHuy;
            await _context.SaveChangesAsync();

            TempData["ThongBao"] = "Đã hủy hồ sơ.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> KetQua()
        {
            var sv = await LaySinhVienHienTaiAsync();
            if (sv == null) return RedirectToAction("Index", "HoSoCaNhan");

            var list = await HoSoCuaSinhVien(sv.MaSinhVien)
                .OrderByDescending(h => h.NgayNop)
                .ToListAsync();
            ViewData["Nav"] = "KetQua";
            return View(list);
        }

        public async Task<IActionResult> LichPhongVan()
        {
            var sv = await LaySinhVienHienTaiAsync();
            if (sv == null) return RedirectToAction("Index", "HoSoCaNhan");

            var list = (await HoSoCuaSinhVien(sv.MaSinhVien).ToListAsync())
                .Where(h => h.LichPhongVan != null)
                .OrderByDescending(h => h.LichPhongVan!.ThoiGianBatDau)
                .ToList();
            ViewData["Nav"] = "LichPhongVan";
            return View(list);
        }

        private async Task NapDuLieuHienThiAsync(NopHoSoViewModel model, SinhVien? sv, HocBong hb)
        {
            model.HocBong = hb;
            model.SinhVien = sv;
            model.LyDoKhongHopLe = await NopHoSoRules.KiemTraAsync(_context, sv, hb, DateTime.Now);
        }
    }
}
