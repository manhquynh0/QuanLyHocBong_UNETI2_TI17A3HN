using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QuanLyHocBong_UNETI2_TI17A3HN.Controllers;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.Tests.Helpers;
using Xunit;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Tests.Business
{
    public class LichPhongVanTests
    {
        private static LichPhongVanController CreateController(Data.QuanLyHocBong_UNETI2_TI17A3HNContext db)
        {
            var ctrl = new LichPhongVanController(db, new FakeEmailService(), new FakeNotificationService());
            ControllerTestHelper.AttachHttpContext(ctrl);
            return ctrl;
        }

        [Fact]
        public async Task Create_OverlappingInterviewerTime_ReturnsViewWithModelError()
        {
            using var db = TestDbContextFactory.Create();
            db.DonViTaiTro.Add(new DonViTaiTro { MaDonViTaiTro = 1, TenDonViTaiTro = "UNETI", TrangThai = true });
            db.HocBong.Add(new HocBong { MaHocBong = 1, TenHocBong = "HB1", MaDonViTaiTro = 1, SoSuat = 5, BacDaoTao = "Đại học", DieuKienXetHocBong = "Đạt", MoTaHocBong = "Mô tả", TrangThai = "Đang nhận hồ sơ" });
            db.SinhVien.AddRange(
                new SinhVien { MaSinhVien = 1, MaTaiKhoan = 1, HoTen = "SV 1" },
                new SinhVien { MaSinhVien = 2, MaTaiKhoan = 2, HoTen = "SV 2" });
            db.HoSoHocBong.AddRange(
                new HoSoHocBong { MaHoSoHocBong = 1, MaSinhVien = 1, MaHocBong = 1, TrangThai = "Chờ phỏng vấn" },
                new HoSoHocBong { MaHoSoHocBong = 2, MaSinhVien = 2, MaHocBong = 1, TrangThai = "Đạt xét duyệt sơ bộ" });

            var start = DateTime.Today.AddDays(2).AddHours(9);
            var end = start.AddMinutes(30);
            db.LichPhongVan.Add(new LichPhongVan
            {
                MaLichPhongVan = 1,
                MaHoSoHocBong = 1,
                ThoiGianBatDau = start,
                ThoiGianKetThuc = end,
                HinhThucPhongVan = "Trực tiếp",
                DiaDiemHoacLienKet = "P.301",
                NguoiPhongVan = "Thầy A",
                TrangThai = "Chờ phỏng vấn"
            });
            await db.SaveChangesAsync();

            var ctrl = CreateController(db);
            var result = await ctrl.Create(new LichPhongVan
            {
                MaHoSoHocBong = 2,
                ThoiGianBatDau = start.AddMinutes(10),
                ThoiGianKetThuc = end.AddMinutes(10),
                HinhThucPhongVan = "Trực tiếp",
                DiaDiemHoacLienKet = "P.301",
                NguoiPhongVan = "Thầy A"
            });

            Assert.IsType<ViewResult>(result);
            Assert.False(ctrl.ModelState.IsValid);
        }

        [Fact]
        public async Task HoanThanh_ApplicationAlreadyAwarded_DoesNotRevertStatus()
        {
            using var db = TestDbContextFactory.Create();
            db.HoSoHocBong.Add(new HoSoHocBong
            {
                MaHoSoHocBong = 1,
                MaSinhVien = 1,
                MaHocBong = 1,
                TrangThai = "Được cấp học bổng"
            });
            db.LichPhongVan.Add(new LichPhongVan
            {
                MaLichPhongVan = 1,
                MaHoSoHocBong = 1,
                ThoiGianBatDau = DateTime.Now.AddDays(-1),
                ThoiGianKetThuc = DateTime.Now.AddDays(-1).AddMinutes(30),
                TrangThai = "Chờ phỏng vấn"
            });
            await db.SaveChangesAsync();

            var ctrl = CreateController(db);
            await ctrl.HoanThanh(1);

            var hoSo = await db.HoSoHocBong.FindAsync(1);
            Assert.Equal("Được cấp học bổng", hoSo!.TrangThai);
        }

        [Fact]
        public async Task HuyLich_AlreadyCompleted_DoesNotCancel()
        {
            using var db = TestDbContextFactory.Create();
            db.HoSoHocBong.Add(new HoSoHocBong
            {
                MaHoSoHocBong = 1,
                MaSinhVien = 1,
                MaHocBong = 1,
                TrangThai = "Đã phỏng vấn"
            });
            db.LichPhongVan.Add(new LichPhongVan
            {
                MaLichPhongVan = 1,
                MaHoSoHocBong = 1,
                ThoiGianBatDau = DateTime.Now.AddDays(-1),
                ThoiGianKetThuc = DateTime.Now.AddDays(-1).AddMinutes(30),
                TrangThai = "Đã hoàn thành"
            });
            await db.SaveChangesAsync();

            var ctrl = CreateController(db);
            await ctrl.HuyLich(1);

            var lich = await db.LichPhongVan.FindAsync(1);
            Assert.Equal("Đã hoàn thành", lich!.TrangThai);
        }
    }
}
