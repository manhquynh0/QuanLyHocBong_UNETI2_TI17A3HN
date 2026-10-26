using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyHocBong_UNETI2_TI17A3HN.Migrations
{
    /// <inheritdoc />
    public partial class UploadFilesFromSinhVien : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaiLieuHoSo",
                columns: table => new
                {
                    MaTaiLieu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaHoSoHocBong = table.Column<int>(type: "int", nullable: false),
                    LoaiTaiLieu = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TenFileGoc = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    TenFileLuu = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    LoaiNoiDung = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    DungLuong = table.Column<long>(type: "bigint", nullable: false),
                    NgayTai = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaTaiKhoanTai = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiLieuHoSo", x => x.MaTaiLieu);
                    table.ForeignKey(
                        name: "FK_TaiLieuHoSo_HoSoHocBong_MaHoSoHocBong",
                        column: x => x.MaHoSoHocBong,
                        principalTable: "HoSoHocBong",
                        principalColumn: "MaHoSoHocBong",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaiLieuHoSo_MaHoSoHocBong",
                table: "TaiLieuHoSo",
                column: "MaHoSoHocBong");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaiLieuHoSo");
        }
    }
}
