using System;
using System.Threading.Tasks;
using QuanLyHocBong_UNETI2_TI17A3HN.Helpers;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.Services;
using QuanLyHocBong_UNETI2_TI17A3HN.Tests.Helpers;
using Xunit;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Tests.Business
{
    public class NopHoSoRulesTests
    {
        private static SinhVien CreateValidStudent(decimal gpa = 3.5m) => new()
        {
            MaSinhVien = 1,
            MaTaiKhoan = 1,
            HoTen = "Nguyen Van A",
            NgaySinh = new DateTime(2005, 1, 1),
            GioiTinh = "Nam",
            SoDienThoai = "0987654321",
            Email = "sv@uneti.edu.vn",
            BacDaoTao = "Đại học",
            Nganh = "Công nghệ thông tin",
            DiemTrungBinhTichLuy = gpa,
            TrangThai = TrangThaiSinhVien.DangHoc
        };

        private static HocBong CreateOpenScholarship(decimal minGpa = 3.0m) => new()
        {
            MaHocBong = 1,
            TenHocBong = "Học bổng Khuyến khích",
            MaDonViTaiTro = 1,
            SoSuat = 5,
            DiemTrungBinhToiThieu = minGpa,
            NgayBatDauNhanHoSo = DateTime.Today.AddDays(-5),
            HanNopHoSo = DateTime.Today.AddDays(5),
            TrangThai = TrangThaiHocBong.DangNhanHoSo
        };

        [Fact]
        public async Task KiemTraAsync_ValidStudentAndOpenScholarship_ReturnsNoErrors()
        {
            using var db = TestDbContextFactory.Create();
            var sv = CreateValidStudent(3.6m);
            var hb = CreateOpenScholarship(3.2m);

            var errors = await NopHoSoRules.KiemTraAsync(db, sv, hb, DateTime.Now);

            Assert.Empty(errors);
        }

        [Fact]
        public async Task KiemTraAsync_LowGpa_ReturnsGpaError()
        {
            using var db = TestDbContextFactory.Create();
            var sv = CreateValidStudent(2.4m);
            var hb = CreateOpenScholarship(3.0m);

            var errors = await NopHoSoRules.KiemTraAsync(db, sv, hb, DateTime.Now);

            Assert.Contains(errors, e => e.Contains("Điểm trung bình tích lũy"));
        }

        [Fact]
        public async Task KiemTraAsync_ExpiredScholarship_ReturnsDeadlineError()
        {
            using var db = TestDbContextFactory.Create();
            var sv = CreateValidStudent(3.8m);
            var hb = CreateOpenScholarship();
            hb.HanNopHoSo = DateTime.Today.AddDays(-1);

            var errors = await NopHoSoRules.KiemTraAsync(db, sv, hb, DateTime.Now);

            Assert.Contains(errors, e => e.Contains("quá hạn"));
        }

        [Fact]
        public async Task KiemTraAsync_ExistingCancelledApplication_AllowsReapply()
        {
            using var db = TestDbContextFactory.Create();
            var sv = CreateValidStudent(3.8m);
            var hb = CreateOpenScholarship();
            db.HoSoHocBong.Add(new HoSoHocBong
            {
                MaSinhVien = sv.MaSinhVien,
                MaHocBong = hb.MaHocBong,
                TrangThai = TrangThaiHoSo.DaHuy,
                ThuXinHocBong = "Test"
            });
            await db.SaveChangesAsync();

            var errors = await NopHoSoRules.KiemTraAsync(db, sv, hb, DateTime.Now);

            Assert.Empty(errors);
        }

        [Fact]
        public async Task KiemTraAsync_ExistingPendingApplication_BlocksDuplicate()
        {
            using var db = TestDbContextFactory.Create();
            var sv = CreateValidStudent(3.8m);
            var hb = CreateOpenScholarship();
            db.HoSoHocBong.Add(new HoSoHocBong
            {
                MaSinhVien = sv.MaSinhVien,
                MaHocBong = hb.MaHocBong,
                TrangThai = TrangThaiHoSo.ChoDuyet,
                ThuXinHocBong = "Test"
            });
            await db.SaveChangesAsync();

            var errors = await NopHoSoRules.KiemTraAsync(db, sv, hb, DateTime.Now);

            Assert.Contains(errors, e => e.Contains("đang xử lý"));
        }
    }
}
