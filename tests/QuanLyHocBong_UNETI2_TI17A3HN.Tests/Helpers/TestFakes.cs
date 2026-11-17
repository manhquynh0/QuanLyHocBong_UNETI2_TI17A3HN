using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.SignalR;
using QuanLyHocBong_UNETI2_TI17A3HN.Hubs;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.Services;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Tests.Helpers
{
    public class FakeEmailService : IEmailService
    {
        public List<(string To, string Subject, string Body)> SentEmails { get; } = new();

        public Task<bool> SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            SentEmails.Add((toEmail, subject, htmlMessage));
            return Task.FromResult(true);
        }

        public Task<bool> SendEmailDirectlyAsync(string toEmail, string subject, string htmlMessage)
        {
            SentEmails.Add((toEmail, subject, htmlMessage));
            return Task.FromResult(true);
        }
    }

    public class FakeNotificationService : INotificationService
    {
        public List<(int AccountId, string Title, string Content)> SentNotifications { get; } = new();

        public Task<ThongBao?> GuiThongBaoAsync(int maTaiKhoan, string tieuDe, string noiDung, string loaiThongBao, string? lienKet = null, int? maHoSoHocBong = null)
        {
            SentNotifications.Add((maTaiKhoan, tieuDe, noiDung));
            return Task.FromResult<ThongBao?>(new ThongBao
            {
                MaTaiKhoan = maTaiKhoan,
                TieuDe = tieuDe,
                NoiDung = noiDung,
                LoaiThongBao = loaiThongBao,
                LienKet = lienKet,
                MaHoSoHocBong = maHoSoHocBong
            });
        }

        public Task GuiThongBaoChoCanBoAsync(string tieuDe, string noiDung, string loaiThongBao, string? lienKet = null, int? maHoSoHocBong = null)
            => Task.CompletedTask;

        public Task<List<ThongBao>> LayDanhSachAsync(int maTaiKhoan, int top = 15)
            => Task.FromResult(new List<ThongBao>());

        public Task<int> DemChuaDocAsync(int maTaiKhoan)
            => Task.FromResult(0);

        public Task<bool> DanhDauDaDocAsync(int maThongBao, int maTaiKhoan)
            => Task.FromResult(true);

        public Task<bool> DanhDauTatCaDaDocAsync(int maTaiKhoan)
            => Task.FromResult(true);
    }

    public class FakeHubContext : IHubContext<NotificationHub>
    {
        public IHubClients Clients { get; } = new FakeHubClients();
        public IGroupManager Groups { get; } = new FakeGroupManager();

        private class FakeHubClients : IHubClients
        {
            private readonly IClientProxy _proxy = new FakeClientProxy();
            public IClientProxy All => _proxy;
            public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => _proxy;
            public IClientProxy Client(string connectionId) => _proxy;
            public IClientProxy Clients(IReadOnlyList<string> connectionIds) => _proxy;
            public IClientProxy Group(string groupName) => _proxy;
            public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => _proxy;
            public IClientProxy Groups(IReadOnlyList<string> groupNames) => _proxy;
            public IClientProxy User(string userId) => _proxy;
            public IClientProxy Users(IReadOnlyList<string> userIds) => _proxy;
        }

        private class FakeClientProxy : IClientProxy
        {
            public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
                => Task.CompletedTask;
        }

        private class FakeGroupManager : IGroupManager
        {
            public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
                => Task.CompletedTask;
            public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
                => Task.CompletedTask;
        }
    }

    public class FakeTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

    public static class ControllerTestHelper
    {
        public static void AttachHttpContext(Controller controller)
        {
            var httpContext = new DefaultHttpContext();
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
            controller.TempData = new TempDataDictionary(httpContext, new FakeTempDataProvider());
        }
    }
}
