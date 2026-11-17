using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using QuanLyHocBong_UNETI2_TI17A3HN.Controllers;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;
using QuanLyHocBong_UNETI2_TI17A3HN.Tests.Helpers;
using Xunit;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Tests.Reports
{
    public class ThongKeExcelTests
    {
        [Fact]
        public async Task ExportExcel_ReturnsXlsFileWithUtf8BomAndEncodedHtml()
        {
            using var db = TestDbContextFactory.Create();
            db.DonViTaiTro.Add(new DonViTaiTro { MaDonViTaiTro = 1, TenDonViTaiTro = "Công ty ABC", TrangThai = true });
            db.HocBong.Add(new HocBong { MaHocBong = 1, TenHocBong = "Học bổng <Test>", MaDonViTaiTro = 1, SoSuat = 5, BacDaoTao = "Đại học", DieuKienXetHocBong = "Đạt", MoTaHocBong = "Mô tả", TrangThai = "Đang nhận hồ sơ" });
            db.SinhVien.Add(new SinhVien { MaSinhVien = 1, MaTaiKhoan = 1, HoTen = "<script>alert(1)</script>", Email = "sv@uneti.edu.vn", Nganh = "CNTT", DiemTrungBinhTichLuy = 3.6m });
            db.HoSoHocBong.Add(new HoSoHocBong
            {
                MaHoSoHocBong = 1,
                MaSinhVien = 1,
                MaHocBong = 1,
                NgayNop = DateTime.Now,
                TrangThai = "Được cấp học bổng"
            });
            await db.SaveChangesAsync();

            var controller = new ThongKeController(db);
            var result = await controller.ExportExcel();

            var fileResult = Assert.IsType<FileContentResult>(result);
            Assert.Equal("application/vnd.ms-excel", fileResult.ContentType);
            Assert.EndsWith(".xls", fileResult.FileDownloadName);

            // Kiểm tra UTF-8 BOM (EF BB BF)
            Assert.True(fileResult.FileContents.Length > 3);
            Assert.Equal(0xEF, fileResult.FileContents[0]);
            Assert.Equal(0xBB, fileResult.FileContents[1]);
            Assert.Equal(0xBF, fileResult.FileContents[2]);

            // Kiểm tra chống HTML Injection
            var html = Encoding.UTF8.GetString(fileResult.FileContents, 3, fileResult.FileContents.Length - 3);
            Assert.DoesNotContain("<script>alert(1)</script>", html);
            Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", html);
        }
    }
}
