using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QuanLyHocBong_UNETI2_TI17A3HN.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DonViTaiTro",
                columns: table => new
                {
                    MaDonViTaiTro = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDonViTaiTro = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    EmailLienHe = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DonViTaiTro", x => x.MaDonViTaiTro);
                });

            migrationBuilder.CreateTable(
                name: "TaiKhoan",
                columns: table => new
                {
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDangNhap = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    MatKhau = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    VaiTro = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    TrangThai = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoan", x => x.MaTaiKhoan);
                });

            migrationBuilder.CreateTable(
                name: "HocBong",
                columns: table => new
                {
                    MaHocBong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenHocBong = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MaDonViTaiTro = table.Column<int>(type: "int", nullable: false),
                    SoSuat = table.Column<int>(type: "int", nullable: false),
                    BacDaoTao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DiemTrungBinhToiThieu = table.Column<decimal>(type: "decimal(4,2)", nullable: false),
                    NgayBatDauNhanHoSo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HanNopHoSo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MoTaHocBong = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DieuKienXetHocBong = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HocBong", x => x.MaHocBong);
                    table.ForeignKey(
                        name: "FK_HocBong_DonViTaiTro_MaDonViTaiTro",
                        column: x => x.MaDonViTaiTro,
                        principalTable: "DonViTaiTro",
                        principalColumn: "MaDonViTaiTro",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SinhVien",
                columns: table => new
                {
                    MaSinhVien = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaTaiKhoan = table.Column<int>(type: "int", nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NgaySinh = table.Column<DateTime>(type: "datetime2", nullable: false),
                    GioiTinh = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    SoDienThoai = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    DiaChi = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    BacDaoTao = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Nganh = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DiemTrungBinhTichLuy = table.Column<decimal>(type: "decimal(4,2)", nullable: false),
                    ThanhTichHocTap = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SinhVien", x => x.MaSinhVien);
                    table.ForeignKey(
                        name: "FK_SinhVien_TaiKhoan_MaTaiKhoan",
                        column: x => x.MaTaiKhoan,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTaiKhoan",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HoSoHocBong",
                columns: table => new
                {
                    MaHoSoHocBong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaSinhVien = table.Column<int>(type: "int", nullable: false),
                    MaHocBong = table.Column<int>(type: "int", nullable: false),
                    NgayNop = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ThuXinHocBong = table.Column<string>(type: "nvarchar(2000)", maxLength: 4000, nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NgayXuLy = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NhanXetXetDuyet = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HoSoHocBong", x => x.MaHoSoHocBong);
                    table.ForeignKey(
                        name: "FK_HoSoHocBong_HocBong_MaHocBong",
                        column: x => x.MaHocBong,
                        principalTable: "HocBong",
                        principalColumn: "MaHocBong",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HoSoHocBong_SinhVien_MaSinhVien",
                        column: x => x.MaSinhVien,
                        principalTable: "SinhVien",
                        principalColumn: "MaSinhVien",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "KetQuaHocBong",
                columns: table => new
                {
                    MaKetQuaHocBong = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaHoSoHocBong = table.Column<int>(type: "int", nullable: false),
                    KetQua = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DiemDanhGia = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NhanXet = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KetQuaHocBong", x => x.MaKetQuaHocBong);
                    table.ForeignKey(
                        name: "FK_KetQuaHocBong_HoSoHocBong_MaHoSoHocBong",
                        column: x => x.MaHoSoHocBong,
                        principalTable: "HoSoHocBong",
                        principalColumn: "MaHoSoHocBong",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LichPhongVan",
                columns: table => new
                {
                    MaLichPhongVan = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaHoSoHocBong = table.Column<int>(type: "int", nullable: false),
                    ThoiGianBatDau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ThoiGianKetThuc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DiaDiemHoacLienKet = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HinhThucPhongVan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NguoiPhongVan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LichPhongVan", x => x.MaLichPhongVan);
                    table.ForeignKey(
                        name: "FK_LichPhongVan_HoSoHocBong_MaHoSoHocBong",
                        column: x => x.MaHoSoHocBong,
                        principalTable: "HoSoHocBong",
                        principalColumn: "MaHoSoHocBong",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "DonViTaiTro",
                columns: new[] { "MaDonViTaiTro", "EmailLienHe", "MoTa", "TenDonViTaiTro", "TrangThai" },
                values: new object[] { 1, "contact@vingroup.com", "Tập đoàn Vingroup tài trợ cho sinh viên xuất sắc", "Quỹ Khuyến học Vingroup", true });

            migrationBuilder.InsertData(
                table: "TaiKhoan",
                columns: new[] { "MaTaiKhoan", "Email", "HoTen", "MatKhau", "TenDangNhap", "TrangThai", "VaiTro" },
                values: new object[,]
                {
                    { 1, "admin@uneti.edu.vn", "Quản trị viên", "123456", "admin", true, "Admin" },
                    { 2, "canboa@uneti.edu.vn", "Cán bộ Nguyễn Văn A", "123456", "canbo", true, "Cán bộ học bổng" },
                    { 3, "sv001@uneti.edu.vn", "Trần Sinh Viên", "123456", "sv001", true, "Sinh viên" }
                });

            migrationBuilder.InsertData(
                table: "HocBong",
                columns: new[] { "MaHocBong", "BacDaoTao", "DiemTrungBinhToiThieu", "DieuKienXetHocBong", "HanNopHoSo", "MaDonViTaiTro", "MoTaHocBong", "NgayBatDauNhanHoSo", "SoSuat", "TenHocBong", "TrangThai" },
                values: new object[] { 1, "Đại học", 3.6m, "Không nợ môn, ĐTB >= 3.6", new DateTime(2026, 11, 30, 0, 0, 0, 0, DateTimeKind.Unspecified), 1, "Dành cho sinh viên xuất sắc khối ngành CNTT", new DateTime(2026, 9, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), 10, "Học bổng Vingroup Xuất sắc 2026", "Đang nhận hồ sơ" });

            migrationBuilder.InsertData(
                table: "SinhVien",
                columns: new[] { "MaSinhVien", "BacDaoTao", "DiaChi", "DiemTrungBinhTichLuy", "Email", "GioiTinh", "HoTen", "MaTaiKhoan", "Nganh", "NgaySinh", "SoDienThoai", "ThanhTichHocTap", "TrangThai" },
                values: new object[] { 1, "Đại học", "Hà Nội", 3.8m, "sv001@uneti.edu.vn", "Nam", "Trần Sinh Viên", 3, "Hệ thống thông tin", new DateTime(2004, 5, 10, 0, 0, 0, 0, DateTimeKind.Unspecified), "0912345678", "Giải Nhất Olympic Tin học", "Đang học" });

            migrationBuilder.InsertData(
                table: "HoSoHocBong",
                columns: new[] { "MaHoSoHocBong", "GhiChu", "MaHocBong", "MaSinhVien", "NgayNop", "NgayXuLy", "NhanXetXetDuyet", "ThuXinHocBong", "TrangThai" },
                values: new object[] { 1, "Đã nộp kèm chứng nhận ngoại khóa", 1, 1, new DateTime(2026, 9, 5, 0, 0, 0, 0, DateTimeKind.Unspecified), null, null, "Em mong muốn nhận học bổng để phát triển chuyên môn Backend.", "Chờ duyệt" });

            migrationBuilder.InsertData(
                table: "KetQuaHocBong",
                columns: new[] { "MaKetQuaHocBong", "DiemDanhGia", "KetQua", "MaHoSoHocBong", "NgayCapNhat", "NhanXet" },
                values: new object[] { 1, 95.5m, "Được cấp học bổng", 1, new DateTime(2026, 10, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Sinh viên xuất sắc, thái độ phỏng vấn tốt" });

            migrationBuilder.InsertData(
                table: "LichPhongVan",
                columns: new[] { "MaLichPhongVan", "DiaDiemHoacLienKet", "GhiChu", "HinhThucPhongVan", "MaHoSoHocBong", "NguoiPhongVan", "ThoiGianBatDau", "ThoiGianKetThuc", "TrangThai" },
                values: new object[] { 1, "Phòng họp 1 - Cơ sở Lĩnh Nam", "Sinh viên mang theo thẻ sinh viên và CMND", "Trực tiếp", 1, "Hội đồng xét duyệt", new DateTime(2026, 10, 10, 8, 0, 0, 0, DateTimeKind.Unspecified), new DateTime(2026, 10, 10, 9, 0, 0, 0, DateTimeKind.Unspecified), "Đã lên lịch" });

            migrationBuilder.CreateIndex(
                name: "IX_DonViTaiTro_TenDonViTaiTro",
                table: "DonViTaiTro",
                column: "TenDonViTaiTro",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HocBong_MaDonViTaiTro",
                table: "HocBong",
                column: "MaDonViTaiTro");

            migrationBuilder.CreateIndex(
                name: "IX_HoSoHocBong_MaHocBong",
                table: "HoSoHocBong",
                column: "MaHocBong");

            migrationBuilder.CreateIndex(
                name: "IX_HoSoHocBong_MaSinhVien",
                table: "HoSoHocBong",
                column: "MaSinhVien");

            migrationBuilder.CreateIndex(
                name: "IX_KetQuaHocBong_MaHoSoHocBong",
                table: "KetQuaHocBong",
                column: "MaHoSoHocBong",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LichPhongVan_MaHoSoHocBong",
                table: "LichPhongVan",
                column: "MaHoSoHocBong");

            migrationBuilder.CreateIndex(
                name: "IX_SinhVien_MaTaiKhoan",
                table: "SinhVien",
                column: "MaTaiKhoan",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_TenDangNhap",
                table: "TaiKhoan",
                column: "TenDangNhap",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KetQuaHocBong");

            migrationBuilder.DropTable(
                name: "LichPhongVan");

            migrationBuilder.DropTable(
                name: "HoSoHocBong");

            migrationBuilder.DropTable(
                name: "HocBong");

            migrationBuilder.DropTable(
                name: "SinhVien");

            migrationBuilder.DropTable(
                name: "DonViTaiTro");

            migrationBuilder.DropTable(
                name: "TaiKhoan");
        }
    }
}
