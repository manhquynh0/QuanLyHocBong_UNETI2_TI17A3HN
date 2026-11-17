using System;
using System.Threading.Tasks;
using QuanLyHocBong_UNETI2_TI17A3HN.Constants;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.Repositories.Implementations;
using QuanLyHocBong_UNETI2_TI17A3HN.Services.Implementations;
using QuanLyHocBong_UNETI2_TI17A3HN.Tests.Helpers;
using QuanLyHocBong_UNETI2_TI17A3HN.ViewModels;
using Xunit;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Tests.Business
{
    public class HoSoHocBongServiceTests
    {
        private static HoSoHocBongService CreateService(Data.QuanLyHocBong_UNETI2_TI17A3HNContext db)
        {
            var uow = new UnitOfWork(db, new HocBongRepository(db), new HoSoHocBongRepository(db));
            return new HoSoHocBongService(uow);
        }

        [Theory]
        [InlineData("Dat", TrangThaiHoSo.DatXetDuyetSoBo)]
        [InlineData("KhongDat", TrangThaiHoSo.KhongDatXetDuyetSoBo)]
        public async Task XetDuyetSoBoAsync_ValidInput_UpdatesStatusCorrectly(string ketQua, string expectedStatus)
        {
            using var db = TestDbContextFactory.Create();
            db.DonViTaiTro.Add(new DonViTaiTro { MaDonViTaiTro = 1, TenDonViTaiTro = "UNETI", TrangThai = true });
            db.HocBong.Add(new HocBong { MaHocBong = 1, TenHocBong = "HB1", MaDonViTaiTro = 1, SoSuat = 5, BacDaoTao = "Đại học", DieuKienXetHocBong = "Đạt", MoTaHocBong = "Mô tả", TrangThai = "Đang nhận hồ sơ" });
            db.SinhVien.Add(new SinhVien { MaSinhVien = 1, MaTaiKhoan = 1, HoTen = "SV1", TrangThai = "Đang học" });
            db.HoSoHocBong.Add(new HoSoHocBong
            {
                MaHoSoHocBong = 1,
                MaSinhVien = 1,
                MaHocBong = 1,
                TrangThai = TrangThaiHoSo.ChoDuyet,
                ThuXinHocBong = "Xin học bổng"
            });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var (ok, error) = await service.XetDuyetSoBoAsync(new XetDuyetSoBoViewModel
            {
                MaHoSoHocBong = 1,
                KetQua = ketQua,
                NhanXetXetDuyet = "Đạt tiêu chuẩn sơ bộ"
            });

            Assert.True(ok);
            Assert.Null(error);
            var updated = await db.HoSoHocBong.FindAsync(1);
            Assert.Equal(expectedStatus, updated!.TrangThai);
        }

        [Fact]
        public async Task XetDuyetSoBoAsync_AlreadyAwarded_ReturnsFalse()
        {
            using var db = TestDbContextFactory.Create();
            db.DonViTaiTro.Add(new DonViTaiTro { MaDonViTaiTro = 1, TenDonViTaiTro = "UNETI", TrangThai = true });
            db.HocBong.Add(new HocBong { MaHocBong = 1, TenHocBong = "HB1", MaDonViTaiTro = 1, SoSuat = 5, BacDaoTao = "Đại học", DieuKienXetHocBong = "Đạt", MoTaHocBong = "Mô tả", TrangThai = "Đang nhận hồ sơ" });
            db.SinhVien.Add(new SinhVien { MaSinhVien = 1, MaTaiKhoan = 1, HoTen = "SV1", TrangThai = "Đang học" });
            db.HoSoHocBong.Add(new HoSoHocBong
            {
                MaHoSoHocBong = 1,
                MaSinhVien = 1,
                MaHocBong = 1,
                TrangThai = TrangThaiHoSo.DuocCapHocBong,
                ThuXinHocBong = "Xin học bổng"
            });
            await db.SaveChangesAsync();

            var service = CreateService(db);
            var (ok, error) = await service.XetDuyetSoBoAsync(new XetDuyetSoBoViewModel
            {
                MaHoSoHocBong = 1,
                KetQua = "KhongDat",
                NhanXetXetDuyet = "Thử sửa lại"
            });

            Assert.False(ok);
            Assert.NotNull(error);
        }
    }
}
