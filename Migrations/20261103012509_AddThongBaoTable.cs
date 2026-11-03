using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyHocBong_UNETI2_TI17A3HN.Migrations
{
    /// <inheritdoc />
    public partial class AddThongBaoTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF OBJECT_ID('ThongBao', 'U') IS NULL
                BEGIN
                    CREATE TABLE [ThongBao] (
                        [MaThongBao] int NOT NULL IDENTITY,
                        [MaTaiKhoan] int NOT NULL,
                        [TieuDe] nvarchar(255) NOT NULL,
                        [NoiDung] nvarchar(1000) NOT NULL,
                        [LoaiThongBao] nvarchar(50) NOT NULL DEFAULT 'HeThong',
                        [LienKet] nvarchar(500) NULL,
                        [MaHoSoHocBong] int NULL,
                        [DaDoc] bit NOT NULL DEFAULT 0,
                        [NgayTao] datetime2 NOT NULL DEFAULT GETDATE(),
                        CONSTRAINT [PK_ThongBao] PRIMARY KEY ([MaThongBao]),
                        CONSTRAINT [FK_ThongBao_HoSoHocBong_MaHoSoHocBong] FOREIGN KEY ([MaHoSoHocBong]) REFERENCES [HoSoHocBong] ([MaHoSoHocBong]) ON DELETE SET NULL,
                        CONSTRAINT [FK_ThongBao_TaiKhoan_MaTaiKhoan] FOREIGN KEY ([MaTaiKhoan]) REFERENCES [TaiKhoan] ([MaTaiKhoan]) ON DELETE CASCADE
                    );
                    CREATE INDEX [IX_ThongBao_MaHoSoHocBong] ON [ThongBao] ([MaHoSoHocBong]);
                    CREATE INDEX [IX_ThongBao_MaTaiKhoan] ON [ThongBao] ([MaTaiKhoan]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF OBJECT_ID('ThongBao', 'U') IS NOT NULL
                BEGIN
                    DROP TABLE [ThongBao];
                END
            ");
        }
    }
}
