using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Hubs
{
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var maTaiKhoan = httpContext?.Items["MaTaiKhoan"] as int?;

            if (!maTaiKhoan.HasValue && int.TryParse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var idClaim))
            {
                maTaiKhoan = idClaim;
            }

            if (maTaiKhoan.HasValue && maTaiKhoan.Value > 0)
            {
                // Tham gia group cá nhân dựa trên MaTaiKhoan lấy từ JWT server-side (chống giả mạo client)
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{maTaiKhoan.Value}");

                var vaiTro = (httpContext?.Items["VaiTro"] as string)
                    ?? Context.User?.FindFirst(ClaimTypes.Role)?.Value;

                if (!string.IsNullOrEmpty(vaiTro))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"role_{vaiTro}");
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var httpContext = Context.GetHttpContext();
            var maTaiKhoan = httpContext?.Items["MaTaiKhoan"] as int?;

            if (!maTaiKhoan.HasValue && int.TryParse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var idClaim))
            {
                maTaiKhoan = idClaim;
            }

            if (maTaiKhoan.HasValue && maTaiKhoan.Value > 0)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{maTaiKhoan.Value}");
            }

            await base.OnDisconnectedAsync(exception);
        }
    }
}

