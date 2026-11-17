using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using QuanLyHocBong_UNETI2_TI17A3HN.Services.Business;
using QuanLyHocBong_UNETI2_TI17A3HN.Tests.Helpers;
using Xunit;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Tests.Services
{
    public class NotificationServiceTests
    {
        [Fact]
        public async Task GuiThongBaoAsync_SavesToDbAndIncrementsUnreadCount()
        {
            using var db = TestDbContextFactory.Create();
            var service = new NotificationService(db, new FakeHubContext(), NullLogger<NotificationService>.Instance);

            var tb = await service.GuiThongBaoAsync(5, "Tiêu đề", "Nội dung", "HoSo");

            Assert.NotNull(tb);
            Assert.False(tb!.DaDoc);
            Assert.Equal(1, await service.DemChuaDocAsync(5));
        }

        [Fact]
        public async Task DanhDauDaDocAsync_WrongOwner_ReturnsFalseAndKeepsUnread()
        {
            using var db = TestDbContextFactory.Create();
            var service = new NotificationService(db, new FakeHubContext(), NullLogger<NotificationService>.Instance);

            var tb = await service.GuiThongBaoAsync(5, "Thông báo riêng", "Nội dung", "HoSo");

            // Tài khoản 99 cố tình đánh dấu đọc thông báo của tài khoản 5 (IDOR)
            var result = await service.DanhDauDaDocAsync(tb!.MaThongBao, maTaiKhoan: 99);

            Assert.False(result);
            Assert.Equal(1, await service.DemChuaDocAsync(5));
        }

        [Fact]
        public async Task DanhDauTatCaDaDocAsync_MarksAllUserNotificationsAsRead()
        {
            using var db = TestDbContextFactory.Create();
            var service = new NotificationService(db, new FakeHubContext(), NullLogger<NotificationService>.Instance);

            await service.GuiThongBaoAsync(5, "TB 1", "ND 1", "HoSo");
            await service.GuiThongBaoAsync(5, "TB 2", "ND 2", "KetQua");

            await service.DanhDauTatCaDaDocAsync(5);

            Assert.Equal(0, await service.DemChuaDocAsync(5));
        }
    }
}
