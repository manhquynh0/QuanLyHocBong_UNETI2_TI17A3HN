using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QuanLyHocBong_UNETI2_TI17A3HN.Attributes;
using QuanLyHocBong_UNETI2_TI17A3HN.Services;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Controllers
{
    [RoleAuthorize]
    public class ThongBaoController : Controller
    {
        private readonly INotificationService _notificationService;

        public ThongBaoController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        private int? CurrentUserId => HttpContext.Items["MaTaiKhoan"] as int?;

        // GET: /ThongBao/GetMyNotifications?top=15
        [HttpGet]
        public async Task<IActionResult> GetMyNotifications(int top = 15)
        {
            var userId = CurrentUserId;
            if (!userId.HasValue)
            {
                return Unauthorized(new { success = false, message = "Chưa đăng nhập." });
            }

            var notifications = await _notificationService.LayDanhSachAsync(userId.Value, top);
            var unreadCount = await _notificationService.DemChuaDocAsync(userId.Value);

            var items = notifications.Select(n => new
            {
                maThongBao = n.MaThongBao,
                tieuDe = n.TieuDe,
                noiDung = n.NoiDung,
                loaiThongBao = n.LoaiThongBao,
                lienKet = n.LienKet,
                daDoc = n.DaDoc,
                ngayTao = n.NgayTao.ToString("HH:mm dd/MM/yyyy"),
                thoiGianTuongDoi = GetRelativeTime(n.NgayTao)
            });

            return Json(new { success = true, unreadCount, data = items });
        }

        // GET: /ThongBao/DemChuaDoc
        [HttpGet]
        public async Task<IActionResult> DemChuaDoc()
        {
            var userId = CurrentUserId;
            if (!userId.HasValue)
            {
                return Unauthorized(new { success = false, message = "Chưa đăng nhập." });
            }

            var unreadCount = await _notificationService.DemChuaDocAsync(userId.Value);
            return Json(new { success = true, unreadCount });
        }

        // POST: /ThongBao/DanhDauDaDoc/5
        [HttpPost]
        public async Task<IActionResult> DanhDauDaDoc(int id)
        {
            var userId = CurrentUserId;
            if (!userId.HasValue)
            {
                return Unauthorized(new { success = false, message = "Chưa đăng nhập." });
            }

            // Bảo mật chống IDOR: chỉ đánh dấu đã đọc nếu thông báo thuộc về chính userId hiện tại
            var result = await _notificationService.DanhDauDaDocAsync(id, userId.Value);
            if (!result)
            {
                return NotFound(new { success = false, message = "Không tìm thấy thông báo hoặc bạn không có quyền." });
            }

            var unreadCount = await _notificationService.DemChuaDocAsync(userId.Value);
            return Json(new { success = true, unreadCount });
        }

        // POST: /ThongBao/DanhDauTatCaDaDoc
        [HttpPost]
        public async Task<IActionResult> DanhDauTatCaDaDoc()
        {
            var userId = CurrentUserId;
            if (!userId.HasValue)
            {
                return Unauthorized(new { success = false, message = "Chưa đăng nhập." });
            }

            await _notificationService.DanhDauTatCaDaDocAsync(userId.Value);
            return Json(new { success = true, unreadCount = 0 });
        }

        private static string GetRelativeTime(DateTime dateTime)
        {
            var span = DateTime.Now - dateTime;
            if (span.TotalMinutes < 1) return "Vừa xong";
            if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes} phút trước";
            if (span.TotalHours < 24) return $"{(int)span.TotalHours} giờ trước";
            if (span.TotalDays < 7) return $"{(int)span.TotalDays} ngày trước";
            return dateTime.ToString("dd/MM/yyyy");
        }
    }
}

