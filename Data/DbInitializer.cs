// Họ và tên: Vũ Mạnh Quỳnh
// Nội dung thực hiện: Mục 16 - Khởi tạo dữ liệu mẫu đầy đủ (05 đơn vị tài trợ, 15 học bổng, 30 sinh viên, 42+ hồ sơ, 15-20 lịch phỏng vấn, kết quả).

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Data
{
    public static class DbInitializer
    {
        public static void Seed(QuanLyHocBong_UNETI2_TI17A3HNContext context)
        {
            context.Database.Migrate();

            // Tự động kiểm tra và khởi tạo bảng RefreshToken nếu chưa chuẩn trong SQL Server
            context.Database.ExecuteSqlRaw(@"
                IF OBJECT_ID('RefreshToken', 'U') IS NOT NULL AND COL_LENGTH('RefreshToken', 'MaTaiKhoan') IS NULL
                BEGIN
                    DROP TABLE [RefreshToken];
                END

                IF OBJECT_ID('RefreshToken', 'U') IS NULL
                BEGIN
                    CREATE TABLE [RefreshToken] (
                        [Id] int IDENTITY(1,1) NOT NULL,
                        [MaTaiKhoan] int NOT NULL,
                        [Token] varchar(255) NOT NULL,
                        [ExpiryDate] datetime2 NOT NULL,
                        [CreatedAt] datetime2 NOT NULL DEFAULT GETDATE(),
                        [IsRevoked] bit NOT NULL DEFAULT 0,
                        CONSTRAINT [PK_RefreshToken] PRIMARY KEY ([Id]),
                        CONSTRAINT [FK_RefreshToken_TaiKhoan_MaTaiKhoan] FOREIGN KEY ([MaTaiKhoan]) REFERENCES [TaiKhoan] ([MaTaiKhoan]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_RefreshToken_Token] ON [RefreshToken] ([Token]);
                END
                ELSE
                BEGIN
                    IF COL_LENGTH('RefreshToken', 'ExpiryDate') IS NULL AND COL_LENGTH('RefreshToken', 'NgayHetHan') IS NOT NULL
                    BEGIN
                        EXEC sp_rename 'RefreshToken.NgayHetHan', 'ExpiryDate', 'COLUMN';
                        EXEC sp_rename 'RefreshToken.NgayTao', 'CreatedAt', 'COLUMN';
                        EXEC sp_rename 'RefreshToken.DaThuHoi', 'IsRevoked', 'COLUMN';
                    END
                END
            ");

            // Nếu đã có đủ dữ liệu mẫu theo Mục 16 thì không seed lặp lại
            if (context.DonViTaiTro.Count() >= 5 && context.HocBong.Count(h => !h.DaXoa) >= 15 && context.SinhVien.Count() >= 30)
            {
                return;
            }

            var hasher = new PasswordHasher<TaiKhoan>();

            // 1. TÀI KHOẢN ADMIN & CÁN BỘ HỌC BỔNG (Mục 16: 01-02 Admin, 02-03 Cán bộ học bổng)
            if (!context.TaiKhoan.Any(t => t.TenDangNhap == "admin"))
            {
                var admin = new TaiKhoan
                {
                    TenDangNhap = "admin",
                    HoTen = "Quản trị viên Hệ thống",
                    Email = "admin@uneti.edu.vn",
                    VaiTro = "Admin",
                    TrangThai = true
                };
                admin.MatKhau = hasher.HashPassword(admin, "123456");
                context.TaiKhoan.Add(admin);
            }

            if (!context.TaiKhoan.Any(t => t.TenDangNhap == "canbo"))
            {
                var canbo = new TaiKhoan
                {
                    TenDangNhap = "canbo",
                    HoTen = "Cán bộ Nguyễn Văn A",
                    Email = "canboa@uneti.edu.vn",
                    VaiTro = "Cán bộ học bổng",
                    TrangThai = true
                };
                canbo.MatKhau = hasher.HashPassword(canbo, "123456");
                context.TaiKhoan.Add(canbo);
            }

            if (!context.TaiKhoan.Any(t => t.TenDangNhap == "canbo1"))
            {
                var canbo1 = new TaiKhoan
                {
                    TenDangNhap = "canbo1",
                    HoTen = "Lê Hoàng Long",
                    Email = "longlh@uneti.edu.vn",
                    VaiTro = "Cán bộ học bổng",
                    TrangThai = true
                };
                canbo1.MatKhau = hasher.HashPassword(canbo1, "123456");
                context.TaiKhoan.Add(canbo1);
            }
            context.SaveChanges();

            // 2. TỐI THIỂU 05 ĐƠN VỊ TÀI TRỢ (Mục 16)
            var sponsors = new List<DonViTaiTro>
            {
                new() { TenDonViTaiTro = "Quỹ Khuyến học Vingroup", MoTa = "Tập đoàn Vingroup tài trợ cho sinh viên xuất sắc khối ngành kỹ thuật và công nghệ.", EmailLienHe = "scholarship@vingroup.net", TrangThai = true },
                new() { TenDonViTaiTro = "Tập đoàn Viettel", MoTa = "Tập đoàn Công nghiệp - Viễn thông Quân đội Viettel đồng hành cùng tài năng trẻ UNETI.", EmailLienHe = "tuyendung@viettel.com.vn", TrangThai = true },
                new() { TenDonViTaiTro = "Công ty Cổ phần FPT", MoTa = "Chương trình FPT Chắp cánh ước mơ hỗ trợ sinh viên vượt khó vươn lên.", EmailLienHe = "scholarship@fpt.com.vn", TrangThai = true },
                new() { TenDonViTaiTro = "Ngân hàng Vietcombank", MoTa = "Quỹ hỗ trợ sinh viên tài năng và sinh viên hoàn cảnh khó khăn ngân hàng Ngoại thương.", EmailLienHe = "csr@vietcombank.com.vn", TrangThai = true },
                new() { TenDonViTaiTro = "Samsung Electronics Việt Nam", MoTa = "Học bổng tài năng công nghệ Samsung Talent Program dành cho sinh viên CNTT & Điện tử.", EmailLienHe = "samsung.talent@samsung.com", TrangThai = true }
            };

            foreach (var sp in sponsors)
            {
                if (!context.DonViTaiTro.Any(d => d.TenDonViTaiTro == sp.TenDonViTaiTro))
                {
                    context.DonViTaiTro.Add(sp);
                }
            }
            context.SaveChanges();

            var dv1 = context.DonViTaiTro.First(d => d.TenDonViTaiTro.Contains("Vingroup")).MaDonViTaiTro;
            var dv2 = context.DonViTaiTro.First(d => d.TenDonViTaiTro.Contains("Viettel")).MaDonViTaiTro;
            var dv3 = context.DonViTaiTro.First(d => d.TenDonViTaiTro.Contains("FPT")).MaDonViTaiTro;
            var dv4 = context.DonViTaiTro.First(d => d.TenDonViTaiTro.Contains("Vietcombank")).MaDonViTaiTro;
            var dv5 = context.DonViTaiTro.First(d => d.TenDonViTaiTro.Contains("Samsung")).MaDonViTaiTro;

            // 3. TỐI THIỂU 15 HỌC BỔNG (Đang nhận hồ sơ, Chưa mở, Tạm dừng, Đã đóng)
            var scholarships = new List<HocBong>
            {
                new() { TenHocBong = "Học bổng Vingroup Khởi nghiệp Công nghệ 2026", MaDonViTaiTro = dv1, SoSuat = 10, BacDaoTao = "Đại học", DiemTrungBinhToiThieu = 3.5m, NgayBatDauNhanHoSo = DateTime.Today.AddDays(-15), HanNopHoSo = DateTime.Today.AddDays(30), MoTaHocBong = "Dành cho sinh viên CNTT có dự án khởi nghiệp sáng tạo.", DieuKienXetHocBong = "GPA >= 3.5, không bị kỷ luật, có sản phẩm demo.", TrangThai = "Đang nhận hồ sơ", DaXoa = false },
                new() { TenHocBong = "Học bổng Viettel - Chắp cánh tài năng số", MaDonViTaiTro = dv2, SoSuat = 15, BacDaoTao = "Đại học", DiemTrungBinhToiThieu = 3.2m, NgayBatDauNhanHoSo = DateTime.Today.AddDays(-10), HanNopHoSo = DateTime.Today.AddDays(40), MoTaHocBong = "Tài trợ học phí và cơ hội thực tập trực tiếp tại Viettel R&D.", DieuKienXetHocBong = "GPA >= 3.2, tiếng Anh tương đương TOEIC 600.", TrangThai = "Đang nhận hồ sơ", DaXoa = false },
                new() { TenHocBong = "Học bổng FPT Software - Next Generation 2026", MaDonViTaiTro = dv3, SoSuat = 20, BacDaoTao = "Đại học", DiemTrungBinhToiThieu = 3.0m, NgayBatDauNhanHoSo = DateTime.Today.AddDays(-20), HanNopHoSo = DateTime.Today.AddDays(25), MoTaHocBong = "Tài trợ toàn phần sinh viên ngành Kỹ thuật phần mềm & Mạng máy tính.", DieuKienXetHocBong = "GPA >= 3.0, yêu thích lập trình ứng dụng.", TrangThai = "Đang nhận hồ sơ", DaXoa = false },
                new() { TenHocBong = "Học bổng Vietcombank - Đồng hành tài năng trẻ", MaDonViTaiTro = dv4, SoSuat = 12, BacDaoTao = "Đại học", DiemTrungBinhToiThieu = 3.2m, NgayBatDauNhanHoSo = DateTime.Today.AddDays(-5), HanNopHoSo = DateTime.Today.AddDays(35), MoTaHocBong = "Hỗ trợ học phí sinh viên xuất sắc khối ngành Kinh tế & CNTT tài chính.", DieuKienXetHocBong = "GPA >= 3.2, điểm rèn luyện tốt.", TrangThai = "Đang nhận hồ sơ", DaXoa = false },
                new() { TenHocBong = "Học bổng Samsung Talent Program (STP 2026)", MaDonViTaiTro = dv5, SoSuat = 10, BacDaoTao = "Đại học", DiemTrungBinhToiThieu = 3.4m, NgayBatDauNhanHoSo = DateTime.Today.AddDays(-12), HanNopHoSo = DateTime.Today.AddDays(18), MoTaHocBong = "Khóa đào tạo thuật toán Samsung và tài trợ học bổng giá trị cao.", DieuKienXetHocBong = "GPA >= 3.4, cam kết hoàn thành đồ án tốt nghiệp.", TrangThai = "Đang nhận hồ sơ", DaXoa = false },
                new() { TenHocBong = "Học bổng Vingroup Hạt giống Tương lai", MaDonViTaiTro = dv1, SoSuat = 8, BacDaoTao = "Đại học", DiemTrungBinhToiThieu = 3.6m, NgayBatDauNhanHoSo = DateTime.Today.AddDays(-3), HanNopHoSo = DateTime.Today.AddDays(20), MoTaHocBong = "Dành riêng cho sinh viên đạt giải Olympic hoặc NCKH cấp trường.", DieuKienXetHocBong = "GPA >= 3.6, có giải thưởng NCKH.", TrangThai = "Đang nhận hồ sơ", DaXoa = false },
                new() { TenHocBong = "Học bổng FPT Khát vọng Xanh - Cao đẳng", MaDonViTaiTro = dv3, SoSuat = 10, BacDaoTao = "Cao đẳng", DiemTrungBinhToiThieu = 2.8m, NgayBatDauNhanHoSo = DateTime.Today.AddDays(-8), HanNopHoSo = DateTime.Today.AddDays(22), MoTaHocBong = "Dành cho sinh viên hệ cao đẳng có tay nghề kỹ thuật giỏi.", DieuKienXetHocBong = "GPA >= 2.8, hạnh kiểm tốt.", TrangThai = "Đang nhận hồ sơ", DaXoa = false },
                new() { TenHocBong = "Học bổng Viettel Vượt khó Hiếu học", MaDonViTaiTro = dv2, SoSuat = 25, BacDaoTao = "Đại học", DiemTrungBinhToiThieu = 2.8m, NgayBatDauNhanHoSo = DateTime.Today.AddDays(-15), HanNopHoSo = DateTime.Today.AddDays(15), MoTaHocBong = "Trợ cấp sinh hoạt phí và dụng cụ học tập cho sinh viên gia đình khó khăn.", DieuKienXetHocBong = "Có sổ hộ nghèo/cận nghèo, GPA >= 2.8.", TrangThai = "Đang nhận hồ sơ", DaXoa = false },

                // Học bổng Chưa mở
                new() { TenHocBong = "Học bổng Vietcombank Thủ khoa Tuyển sinh Kỳ II", MaDonViTaiTro = dv4, SoSuat = 5, BacDaoTao = "Đại học", DiemTrungBinhToiThieu = 3.5m, NgayBatDauNhanHoSo = DateTime.Today.AddDays(20), HanNopHoSo = DateTime.Today.AddDays(60), MoTaHocBong = "Khen thưởng sinh viên đạt thành tích xuất sắc kỳ học mới.", DieuKienXetHocBong = "GPA kỳ học đạt loại xuất sắc.", TrangThai = "Chưa mở", DaXoa = false },
                new() { TenHocBong = "Học bổng Samsung R&D Research Grant", MaDonViTaiTro = dv5, SoSuat = 6, BacDaoTao = "Đại học", DiemTrungBinhToiThieu = 3.6m, NgayBatDauNhanHoSo = DateTime.Today.AddDays(30), HanNopHoSo = DateTime.Today.AddDays(75), MoTaHocBong = "Tài trợ đề tài nghiên cứu trí tuệ nhân tạo và xử lý ảnh.", DieuKienXetHocBong = "GPA >= 3.6, có bài báo công bố.", TrangThai = "Chưa mở", DaXoa = false },

                // Học bổng Tạm dừng
                new() { TenHocBong = "Học bổng FPT Global Internship 2026", MaDonViTaiTro = dv3, SoSuat = 5, BacDaoTao = "Đại học", DiemTrungBinhToiThieu = 3.3m, NgayBatDauNhanHoSo = DateTime.Today.AddDays(-40), HanNopHoSo = DateTime.Today.AddDays(10), MoTaHocBong = "Học bổng thực tập trao đổi tại chi nhánh nước ngoài FPT.", DieuKienXetHocBong = "Tiếng Anh IELTS 6.5, GPA >= 3.3.", TrangThai = "Tạm dừng", DaXoa = false },

                // Học bổng Đã đóng / Hết hạn
                new() { TenHocBong = "Học bổng Vingroup Học kỳ I năm học 2025-2026", MaDonViTaiTro = dv1, SoSuat = 15, BacDaoTao = "Đại học", DiemTrungBinhToiThieu = 3.2m, NgayBatDauNhanHoSo = DateTime.Today.AddDays(-120), HanNopHoSo = DateTime.Today.AddDays(-30), MoTaHocBong = "Chương trình khen thưởng học kỳ trước đã kết thúc xét duyệt.", DieuKienXetHocBong = "GPA >= 3.2.", TrangThai = "Đã đóng", DaXoa = false },
                new() { TenHocBong = "Học bổng Viettel Star Summer Camp 2025", MaDonViTaiTro = dv2, SoSuat = 10, BacDaoTao = "Đại học", DiemTrungBinhToiThieu = 3.0m, NgayBatDauNhanHoSo = DateTime.Today.AddDays(-150), HanNopHoSo = DateTime.Today.AddDays(-60), MoTaHocBong = "Trại hè công nghệ và cấp học bổng hè đã hoàn tất trao thưởng.", DieuKienXetHocBong = "GPA >= 3.0.", TrangThai = "Đã đóng", DaXoa = false },
                new() { TenHocBong = "Học bổng Vietcombank Thắp sáng Ước mơ 2025", MaDonViTaiTro = dv4, SoSuat = 10, BacDaoTao = "Cao đẳng", DiemTrungBinhToiThieu = 2.5m, NgayBatDauNhanHoSo = DateTime.Today.AddDays(-180), HanNopHoSo = DateTime.Today.AddDays(-90), MoTaHocBong = "Chương trình an sinh xã hội học kỳ 1 năm 2025.", DieuKienXetHocBong = "GPA >= 2.5.", TrangThai = "Đã đóng", DaXoa = false },
                new() { TenHocBong = "Học bổng Samsung Future Innovators 2025", MaDonViTaiTro = dv5, SoSuat = 8, BacDaoTao = "Đại học", DiemTrungBinhToiThieu = 3.4m, NgayBatDauNhanHoSo = DateTime.Today.AddDays(-200), HanNopHoSo = DateTime.Today.AddDays(-100), MoTaHocBong = "Đã hoàn thành cấp phát 8 suất cho sinh viên xuất sắc năm 2025.", DieuKienXetHocBong = "GPA >= 3.4.", TrangThai = "Đã đóng", DaXoa = false }
            };

            foreach (var sc in scholarships)
            {
                if (!context.HocBong.Any(h => h.TenHocBong == sc.TenHocBong))
                {
                    context.HocBong.Add(sc);
                }
            }
            context.SaveChanges();

            // 4. TỐI THIỂU 30 SINH VIÊN + TÀI KHOẢN TƯƠNG ỨNG (Mục 16)
            var hoTenList = new[]
            {
                "Trần Văn Minh", "Nguyễn Thu Hà", "Lê Quốc Bảo", "Phạm Hải Đăng", "Hoàng Kim Ngân",
                "Vũ Mạnh Quỳnh", "Đỗ Nhật Minh", "Bùi Phương Thảo", "Đặng Quang Huy", "Dương Thu Trang",
                "Ngô Gia Bảo", "Lý Hải Yến", "Trịnh Thành Đạt", "Phan Thanh Trúc", "Mai Văn Nam",
                "Lương Bích Hằng", "Đoàn Tiến Dũng", "Hà Kiều Anh", "Chu Đức Long", "Trương Mỹ Duyên",
                "Cao Bá Quát", "Tạ Đình Phong", "Võ Hoài Nam", "Đinh Gia Khiêm", "Phùng Khánh Linh",
                "Nghiêm Xuân Tú", "Bạch Thái Bưởi", "Tô Hoài An", "Lâm Thanh Hà", "Vương Quốc Huy"
            };

            var majors = new[] { "Công nghệ thông tin", "Hệ thống thông tin", "Khoa học máy tính", "Kỹ thuật phần mềm", "Mạng máy tính", "Thương mại điện tử" };

            for (int i = 0; i < 30; i++)
            {
                var username = $"sv{i + 1:D3}";
                var hoTen = hoTenList[i];
                var email = $"{username}@sv.uneti.edu.vn";

                var account = context.TaiKhoan.FirstOrDefault(t => t.TenDangNhap == username);
                if (account == null)
                {
                    account = new TaiKhoan
                    {
                        TenDangNhap = username,
                        HoTen = hoTen,
                        Email = email,
                        VaiTro = "Sinh viên",
                        TrangThai = true
                    };
                    account.MatKhau = hasher.HashPassword(account, "123456");
                    context.TaiKhoan.Add(account);
                    context.SaveChanges();
                }

                if (!context.SinhVien.Any(s => s.MaTaiKhoan == account.MaTaiKhoan))
                {
                    var gpa = Math.Round(2.8m + (decimal)(new Random(i * 17).NextDouble() * 1.15), 2);
                    if (gpa > 4.0m) gpa = 4.0m;

                    context.SinhVien.Add(new SinhVien
                    {
                        MaTaiKhoan = account.MaTaiKhoan,
                        HoTen = hoTen,
                        NgaySinh = new DateTime(2004, (i % 12) + 1, (i % 25) + 1),
                        GioiTinh = (i % 3 == 0) ? "Nữ" : "Nam",
                        SoDienThoai = $"098{i + 10:D3}{i * 123 % 1000:D3}",
                        Email = email,
                        DiaChi = (i % 2 == 0) ? "Hà Nội" : "Nam Định",
                        BacDaoTao = (i == 6 || i == 13) ? "Cao đẳng" : "Đại học",
                        Nganh = majors[i % majors.Length],
                        DiemTrungBinhTichLuy = gpa,
                        ThanhTichHocTap = gpa >= 3.6m ? "Học lực Xuất sắc, Đoàn viên ưu tú" : (gpa >= 3.2m ? "Học lực Giỏi, NCKH cấp Khoa" : "Học lực Khá, nhiệt tình tham gia hoạt động"),
                        TrangThai = "Đang học"
                    });
                }
            }
            context.SaveChanges();

            // 5. TỐI THIỂU 40 - 50 HỒ SƠ VỚI ĐỦ CÁC TRẠNG THÁI NGHIỆP VỤ (Mục 16)
            var allStudents = context.SinhVien.ToList();
            var openScholarships = context.HocBong.Where(h => !h.DaXoa && h.TrangThai == "Đang nhận hồ sơ").ToList();

            var statuses = new[]
            {
                "Chờ duyệt",
                "Đạt xét duyệt sơ bộ",
                "Chờ phỏng vấn",
                "Đã phỏng vấn",
                "Được cấp học bổng",
                "Không đạt xét duyệt sơ bộ",
                "Không được cấp học bổng",
                "Đã hủy"
            };

            int appCount = context.HoSoHocBong.Count();
            if (appCount < 40 && allStudents.Count >= 30 && openScholarships.Count > 0)
            {
                int index = 0;
                foreach (var sv in allStudents)
                {
                    var hb = openScholarships[index % openScholarships.Count];
                    if (!context.HoSoHocBong.Any(hs => hs.MaSinhVien == sv.MaSinhVien && hs.MaHocBong == hb.MaHocBong))
                    {
                        var status = statuses[index % statuses.Length];
                        var app = new HoSoHocBong
                        {
                            MaSinhVien = sv.MaSinhVien,
                            MaHocBong = hb.MaHocBong,
                            NgayNop = DateTime.Today.AddDays(-(index % 20 + 1)),
                            ThuXinHocBong = $"Kính gửi Hội đồng xét duyệt, em tên là {sv.HoTen}, sinh viên ngành {sv.Nganh}. Em xin ứng tuyển học bổng {hb.TenHocBong} để có thêm nguồn kinh phí trang trải việc học và tham gia các dự án nghiên cứu chuyên sâu.",
                            GhiChu = "Đã nộp đầy đủ bảng điểm kỳ gần nhất",
                            TrangThai = status,
                            NgayXuLy = status != "Chờ duyệt" ? DateTime.Today.AddDays(-(index % 10)) : null,
                            NhanXetXetDuyet = status == "Không đạt xét duyệt sơ bộ" ? "Điểm GPA chưa đạt chuẩn hoặc hồ sơ thiếu minh chứng" : (status != "Chờ duyệt" ? "Hồ sơ đạt yêu cầu sơ tuyển" : null)
                        };
                        context.HoSoHocBong.Add(app);
                        context.SaveChanges();

                        // 6. TẠO LỊCH PHỎNG VẤN CHO HỒ SƠ PHÙ HỢP (Mục 16: 15-20 lịch)
                        if (status is "Chờ phỏng vấn" or "Đã phỏng vấn" or "Được cấp học bổng" or "Không được cấp học bổng")
                        {
                            var schedStatus = status == "Chờ phỏng vấn" ? "Đã lên lịch" : "Đã hoàn thành";
                            var start = DateTime.Today.AddDays(status == "Chờ phỏng vấn" ? (index % 5 + 1) : -(index % 10 + 1)).AddHours(8 + (index % 8));
                            var sched = new LichPhongVan
                            {
                                MaHoSoHocBong = app.MaHoSoHocBong,
                                ThoiGianBatDau = start,
                                ThoiGianKetThuc = start.AddHours(1),
                                HinhThucPhongVan = (index % 2 == 0) ? "Trực tiếp" : "Trực tuyến",
                                DiaDiemHoacLienKet = (index % 2 == 0) ? "Phòng Hội đồng A1 - Cơ sở Minh Khai" : "https://meet.google.com/uneti-scholarship-interview",
                                NguoiPhongVan = (index % 2 == 0) ? "Hội đồng xét duyệt Phòng CTSV" : "TS. Nguyễn Văn Hùng - Trưởng khoa CNTT",
                                GhiChu = "Yêu cầu sinh viên mang thẻ SV và trang phục lịch sự",
                                TrangThai = schedStatus
                            };
                            context.LichPhongVan.Add(sched);
                            context.SaveChanges();

                            // 7. GHI KẾT QUẢ CHO HỒ SƠ ĐÃ PHỎNG VẤN (Mục 16)
                            if (status is "Được cấp học bổng" or "Không được cấp học bổng")
                            {
                                var isGranted = status == "Được cấp học bổng";
                                var kq = new KetQuaHocBong
                                {
                                    MaHoSoHocBong = app.MaHoSoHocBong,
                                    DiemDanhGia = isGranted ? (85.0m + (index % 15)) : (55.0m + (index % 10)),
                                    KetQua = isGranted ? "Được cấp học bổng" : "Không được cấp học bổng",
                                    NgayCapNhat = DateTime.Today.AddDays(-1),
                                    NhanXet = isGranted ? "Sinh viên thể hiện xuất sắc trong buổi phỏng vấn, mục tiêu học tập rõ ràng." : "Kỹ năng trình bày và định hướng chưa đáp ứng tiêu chí tài trợ."
                                };
                                context.KetQuaHocBong.Add(kq);
                                context.SaveChanges();
                            }
                        }
                    }
                    index++;
                }

                while (context.HoSoHocBong.Count() < 42 && index < 60)
                {
                    var sv = allStudents[index % allStudents.Count];
                    var hb = openScholarships[(index + 1) % openScholarships.Count];
                    if (!context.HoSoHocBong.Any(hs => hs.MaSinhVien == sv.MaSinhVien && hs.MaHocBong == hb.MaHocBong))
                    {
                        var status = statuses[index % statuses.Length];
                        context.HoSoHocBong.Add(new HoSoHocBong
                        {
                            MaSinhVien = sv.MaSinhVien,
                            MaHocBong = hb.MaHocBong,
                            NgayNop = DateTime.Today.AddDays(-(index % 15)),
                            ThuXinHocBong = $"Em xin nộp hồ sơ xét học bổng {hb.TenHocBong} để trang trải chi phí học tập năm học 2026.",
                            GhiChu = "Bản sao thành tích học tập",
                            TrangThai = status,
                            NgayXuLy = status != "Chờ duyệt" ? DateTime.Today.AddDays(-2) : null,
                            NhanXetXetDuyet = "Đã kiểm tra sơ bộ"
                        });
                        context.SaveChanges();
                    }
                    index++;
                }
            }
        }
    }
}

