using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;
using QuanLyHocBong_UNETI2_TI17A3HN.Hubs;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Services.Business
{
    public class NotificationService : INotificationService
    {
        private readonly QuanLyHocBong_UNETI2_TI17A3HNContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            QuanLyHocBong_UNETI2_TI17A3HNContext context,
            IHubContext<NotificationHub> hubContext,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task<ThongBao?> GuiThongBaoAsync(
            int maTaiKhoan,
            string tieuDe,
            string noiDung,
            string loaiThongBao,
            string? lienKet = null,
            int? maHoSoHocBong = null)
        {
            try
            {
                var thongBao = new ThongBao
                {
                    MaTaiKhoan = maTaiKhoan,
                    TieuDe = tieuDe,
                    NoiDung = noiDung,
                    LoaiThongBao = string.IsNullOrWhiteSpace(loaiThongBao) ? "HeThong" : loaiThongBao,
                    LienKet = lienKet,
                    MaHoSoHocBong = maHoSoHocBong,
                    DaDoc = false,
                    NgayTao = DateTime.Now
                };

                _context.ThongBao.Add(thongBao);
                await _context.SaveChangesAsync();

                // Đẩy thông báo Realtime qua SignalR
                try
                {
                    var unreadCount = await DemChuaDocAsync(maTaiKhoan);
                    var payload = new
                    {
                        maThongBao = thongBao.MaThongBao,
                        tieuDe = thongBao.TieuDe,
                        noiDung = thongBao.NoiDung,
                        loaiThongBao = thongBao.LoaiThongBao,
                        lienKet = thongBao.LienKet,
                        daDoc = thongBao.DaDoc,
                        ngayTao = thongBao.NgayTao.ToString("HH:mm dd/MM/yyyy"),
                        unreadCount = unreadCount
                    };

                    await _hubContext.Clients.Group($"user_{maTaiKhoan}").SendAsync("NhanThongBao", payload);
                }
                catch (Exception hubEx)
                {
                    _logger.LogWarning(hubEx, "Lỗi khi gửi SignalR thông báo tới group user_{MaTaiKhoan}", maTaiKhoan);
                }

                return thongBao;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu thông báo cho tài khoản {MaTaiKhoan}", maTaiKhoan);
                return null;
            }
        }

        public async Task GuiThongBaoChoCanBoAsync(
            string tieuDe,
            string noiDung,
            string loaiThongBao,
            string? lienKet = null,
            int? maHoSoHocBong = null)
        {
            try
            {
                var officerAccounts = await _context.TaiKhoan
                    .AsNoTracking()
                    .Where(t => t.TrangThai && (t.VaiTro == "Cán bộ học bổng" || t.VaiTro == "Admin"))
                    .Select(t => t.MaTaiKhoan)
                    .ToListAsync();

                foreach (var accId in officerAccounts)
                {
                    await GuiThongBaoAsync(accId, tieuDe, noiDung, loaiThongBao, lienKet, maHoSoHocBong);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi thông báo tới danh sách Cán bộ và Admin");
            }
        }

        public async Task<List<ThongBao>> LayDanhSachAsync(int maTaiKhoan, int top = 15)
        {
            return await _context.ThongBao
                .AsNoTracking()
                .Where(t => t.MaTaiKhoan == maTaiKhoan)
                .OrderByDescending(t => t.NgayTao)
                .Take(top)
                .ToListAsync();
        }

        public async Task<int> DemChuaDocAsync(int maTaiKhoan)
        {
            return await _context.ThongBao
                .AsNoTracking()
                .CountAsync(t => t.MaTaiKhoan == maTaiKhoan && !t.DaDoc);
        }

        public async Task<bool> DanhDauDaDocAsync(int maThongBao, int maTaiKhoan)
        {
            var tb = await _context.ThongBao
                .FirstOrDefaultAsync(t => t.MaThongBao == maThongBao && t.MaTaiKhoan == maTaiKhoan);

            if (tb == null) return false;

            if (!tb.DaDoc)
            {
                tb.DaDoc = true;
                await _context.SaveChangesAsync();
            }

            return true;
        }

        public async Task<bool> DanhDauTatCaDaDocAsync(int maTaiKhoan)
        {
            var unreadList = await _context.ThongBao
                .Where(t => t.MaTaiKhoan == maTaiKhoan && !t.DaDoc)
                .ToListAsync();

            if (unreadList.Count == 0) return true;

            foreach (var item in unreadList)
            {
                item.DaDoc = true;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}

