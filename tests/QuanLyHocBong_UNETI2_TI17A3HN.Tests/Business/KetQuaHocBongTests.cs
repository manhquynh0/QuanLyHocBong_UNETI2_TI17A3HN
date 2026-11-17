using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QuanLyHocBong_UNETI2_TI17A3HN.Controllers;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.Tests.Helpers;
using Xunit;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Tests.Business
{
    public class KetQuaHocBongTests
    {
        private static KetQuaHocBongController CreateController(
            Data.QuanLyHocBong_UNETI2_TI17A3HNContext db,
            FakeEmailService? email = null,
            FakeNotificationService? notif = null)
        {
            var ctrl = new KetQuaHocBongController(
                db,
                email ?? new FakeEmailService(),
                notif ?? new FakeNotificationService());
            ControllerTestHelper.AttachHttpContext(ctrl);
            return ctrl;
        }

        [Fact]
        public async Task NhapKetQua_ReachesSlotLimit_ClosesScholarshipAndNotifiesStudent()
        {
            using var db = TestDbContextFactory.Create();
            db.DonViTaiTro.Add(new DonViTaiTro { MaDonViTaiTro = 1, TenDonViTaiTro = "UNETI", TrangThai = true });
            db.TaiKhoan.Add(new TaiKhoan { MaTaiKhoan = 10, TenDangNhap = "sv1", MatKhau = "123", HoTen = "SV 1", Email = "sv1@uneti.edu.vn", VaiTro = "Sinh viên", TrangThai = true });
            db.HocBong.Add(new HocBong { MaHocBong = 1, TenHocBong = "HB Tài năng", MaDonViTaiTro = 1, SoSuat = 1, TrangThai = "Đang nhận hồ sơ", BacDaoTao = "Đại học", DieuKienXetHocBong = "Đạt", MoTaHocBong = "Mô tả" });
            db.SinhVien.Add(new SinhVien { MaSinhVien = 1, MaTaiKhoan = 10, HoTen = "SV 1", Email = "sv1@uneti.edu.vn" });
            db.HoSoHocBong.Add(new HoSoHocBong { MaHoSoHocBong = 1, MaSinhVien = 1, MaHocBong = 1, TrangThai = "Đã phỏng vấn" });
            await db.SaveChangesAsync();

            var fakeEmail = new FakeEmailService();
            var fakeNotif = new FakeNotificationService();
            var ctrl = CreateController(db, fakeEmail, fakeNotif);

            var result = await ctrl.NhapKetQua(1, "Được cấp học bổng", 95m, "Xuất sắc");

            Assert.IsType<RedirectToActionResult>(result);
            var hb = await db.HocBong.FindAsync(1);
            var hoSo = await db.HoSoHocBong.FindAsync(1);
            Assert.Equal("Đã đóng", hb!.TrangThai);
            Assert.Equal("Được cấp học bổng", hoSo!.TrangThai);
            Assert.Single(fakeEmail.SentEmails);
            Assert.Single(fakeNotif.SentNotifications);
        }

        [Fact]
        public async Task NhapKetQua_ExceedsSlotLimit_ReturnsViewWithError()
        {
            using var db = TestDbContextFactory.Create();
            db.DonViTaiTro.Add(new DonViTaiTro { MaDonViTaiTro = 1, TenDonViTaiTro = "UNETI", TrangThai = true });
            db.TaiKhoan.AddRange(
                new TaiKhoan { MaTaiKhoan = 10, TenDangNhap = "sv1", MatKhau = "123", HoTen = "SV 1", Email = "sv1@uneti.edu.vn", VaiTro = "Sinh viên", TrangThai = true },
                new TaiKhoan { MaTaiKhoan = 11, TenDangNhap = "sv2", MatKhau = "123", HoTen = "SV 2", Email = "sv2@uneti.edu.vn", VaiTro = "Sinh viên", TrangThai = true });
            db.HocBong.Add(new HocBong { MaHocBong = 1, TenHocBong = "HB Tài năng", MaDonViTaiTro = 1, SoSuat = 1, TrangThai = "Đã đóng", BacDaoTao = "Đại học", DieuKienXetHocBong = "Đạt", MoTaHocBong = "Mô tả" });
            db.SinhVien.AddRange(
                new SinhVien { MaSinhVien = 1, MaTaiKhoan = 10, HoTen = "SV 1" },
                new SinhVien { MaSinhVien = 2, MaTaiKhoan = 11, HoTen = "SV 2" });
            db.HoSoHocBong.AddRange(
                new HoSoHocBong { MaHoSoHocBong = 1, MaSinhVien = 1, MaHocBong = 1, TrangThai = "Được cấp học bổng" },
                new HoSoHocBong { MaHoSoHocBong = 2, MaSinhVien = 2, MaHocBong = 1, TrangThai = "Đã phỏng vấn" });
            await db.SaveChangesAsync();

            var ctrl = CreateController(db);
            var result = await ctrl.NhapKetQua(2, "Được cấp học bổng", 90m, "Tốt");

            Assert.IsType<ViewResult>(result);
            Assert.False(ctrl.ModelState.IsValid);
            var hoSo2 = await db.HoSoHocBong.FindAsync(2);
            Assert.Equal("Đã phỏng vấn", hoSo2!.TrangThai);
        }
    }
}
