using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyHocBong_UNETI2_TI17A3HN.Models;

namespace QuanLyHocBong_UNETI2_TI17A3HN.Data
{
    public class QuanLyHocBong_UNETI2_TI17A3HNContext : DbContext
    {
        public QuanLyHocBong_UNETI2_TI17A3HNContext (DbContextOptions<QuanLyHocBong_UNETI2_TI17A3HNContext> options)
            : base(options)
        {
        }


        public DbSet<TaiKhoan> TaiKhoan { get; set; } = default!;
        public DbSet<DonViTaiTro> DonViTaiTro { get; set; } = default!;
        public DbSet<HocBong> HocBong { get; set; } = default!;
        public DbSet<SinhVien> SinhVien { get; set; } = default!;
        public DbSet<HoSoHocBong> HoSoHocBong { get; set; } = default!;
        public DbSet<LichPhongVan> LichPhongVan { get; set; } = default!;
        public DbSet<KetQuaHocBong> KetQuaHocBong { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<TaiKhoan>()
                .HasIndex(t => t.TenDangNhap)
                .IsUnique();

            modelBuilder.Entity<DonViTaiTro>().HasIndex(d => d.TenDonViTaiTro).IsUnique();

            modelBuilder.Entity<SinhVien>().HasIndex(s => s.MaTaiKhoan).IsUnique();

            modelBuilder.Entity<KetQuaHocBong>().HasIndex(k => k.MaHoSoHocBong).IsUnique();


            modelBuilder.Entity<HocBong>().HasOne(h => h.DonViTaiTro)
                .WithMany(d => d.HocBongs)
                .HasForeignKey(h => h.MaDonViTaiTro)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HoSoHocBong>()
                .HasOne(hs => hs.SinhVien)
                .WithMany(s => s.HoSoHocBongs)
                .HasForeignKey(hs => hs.MaSinhVien)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<HoSoHocBong>()
                .HasOne(hs => hs.HocBong)
                .WithMany(hb => hb.HoSoHocBongs)
                .HasForeignKey(hs => hs.MaHocBong)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TaiKhoan>().HasData(
                new TaiKhoan { MaTaiKhoan = 1, TenDangNhap = "admin", MatKhau = "123456", HoTen = "Quản trị viên", Email = "admin@uneti.edu.vn", VaiTro = "Admin", TrangThai = true },
                new TaiKhoan { MaTaiKhoan = 2, TenDangNhap = "canbo", MatKhau = "123456", HoTen = "Cán bộ Nguyễn Văn A", Email = "canboa@uneti.edu.vn", VaiTro = "Cán bộ học bổng", TrangThai = true },
                new TaiKhoan { MaTaiKhoan = 3, TenDangNhap = "sv001", MatKhau = "123456", HoTen = "Trần Sinh Viên", Email = "sv001@uneti.edu.vn", VaiTro = "Sinh viên", TrangThai = true }
             );

            modelBuilder.Entity<DonViTaiTro>().HasData(
                new DonViTaiTro
                {
                    MaDonViTaiTro = 1,
                    TenDonViTaiTro = "Quỹ Khuyến học Vingroup",
                    MoTa = "Tập đoàn Vingroup tài trợ cho sinh viên xuất sắc",
                    EmailLienHe = "contact@vingroup.com",
                    TrangThai = true
                }
            );

            modelBuilder.Entity<SinhVien>().HasData(
                new SinhVien
                {
                    MaSinhVien = 1,
                    MaTaiKhoan = 3,
                    HoTen = "Trần Sinh Viên",
                    NgaySinh = new DateTime(2004, 5, 10),
                    GioiTinh = "Nam",
                    SoDienThoai = "0912345678",
                    Email = "sv001@uneti.edu.vn",
                    DiaChi = "Hà Nội",
                    BacDaoTao = "Đại học",
                    Nganh = "Hệ thống thông tin",
                    DiemTrungBinhTichLuy = 3.8m,
                    ThanhTichHocTap = "Giải Nhất Olympic Tin học",
                    TrangThai = "Đang học"
                }
            );

            modelBuilder.Entity<HocBong>().HasData(
                new HocBong
                {
                    MaHocBong = 1,
                    MaDonViTaiTro = 1,
                    TenHocBong = "Học bổng Vingroup Xuất sắc 2026",
                    SoSuat = 10,
                    BacDaoTao = "Đại học",
                    DiemTrungBinhToiThieu = 3.6m,
                    NgayBatDauNhanHoSo = new DateTime(2026, 9, 1),
                    HanNopHoSo = new DateTime(2026, 11, 30),
                    MoTaHocBong = "Dành cho sinh viên xuất sắc khối ngành CNTT",
                    DieuKienXetHocBong = "Không nợ môn, ĐTB >= 3.6",
                    TrangThai = "Đang nhận hồ sơ"
                }
            );

            modelBuilder.Entity<HoSoHocBong>().HasData(
                new HoSoHocBong
                {
                    MaHoSoHocBong = 1,
                    MaSinhVien = 1,
                    MaHocBong = 1,
                    NgayNop = new DateTime(2026, 9, 5),
                    ThuXinHocBong = "Em mong muốn nhận học bổng để phát triển chuyên môn Backend.",
                    GhiChu = "Đã nộp kèm chứng nhận ngoại khóa",
                    TrangThai = "Chờ duyệt",
                    NgayXuLy = null,
                    NhanXetXetDuyet = null
                }
            );

            modelBuilder.Entity<LichPhongVan>().HasData(
                new LichPhongVan
                {
                    MaLichPhongVan = 1,
                    MaHoSoHocBong = 1,
                    ThoiGianBatDau = new DateTime(2026, 10, 10, 8, 0, 0),
                    ThoiGianKetThuc = new DateTime(2026, 10, 10, 9, 0, 0),
                    DiaDiemHoacLienKet = "Phòng họp 1 - Cơ sở Lĩnh Nam",
                    HinhThucPhongVan = "Trực tiếp",
                    NguoiPhongVan = "Hội đồng xét duyệt",
                    GhiChu = "Sinh viên mang theo thẻ sinh viên và CMND",
                    TrangThai = "Đã lên lịch"
                }
            );

            modelBuilder.Entity<KetQuaHocBong>().HasData(
                new KetQuaHocBong
                {
                    MaKetQuaHocBong = 1,
                    MaHoSoHocBong = 1,
                    KetQua = "Được cấp học bổng",
                    DiemDanhGia = 95.5m,
                    NgayCapNhat = new DateTime(2026, 10, 15),
                    NhanXet = "Sinh viên xuất sắc, thái độ phỏng vấn tốt"
                }
            );
        }
    }
}
