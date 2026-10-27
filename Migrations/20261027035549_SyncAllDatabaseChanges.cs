using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyHocBong_UNETI2_TI17A3HN.Migrations
{
    /// <inheritdoc />
    public partial class SyncAllDatabaseChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- 1. Cột AnhDaiDien trong TaiKhoan
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TaiKhoan]') AND name = 'AnhDaiDien')
                BEGIN
                    ALTER TABLE [dbo].[TaiKhoan] ADD [AnhDaiDien] nvarchar(1000) NULL;
                END

                -- 2. Cột AnhDaiDien trong SinhVien
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SinhVien]') AND name = 'AnhDaiDien')
                BEGIN
                    ALTER TABLE [dbo].[SinhVien] ADD [AnhDaiDien] nvarchar(1000) NULL;
                END

                -- 3. Cột FileMinhChung trong HoSoHocBong
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HoSoHocBong]') AND name = 'FileMinhChung')
                BEGIN
                    ALTER TABLE [dbo].[HoSoHocBong] ADD [FileMinhChung] nvarchar(1000) NULL;
                END

                -- 4. Sửa các cột cho phép NULL nếu cần
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SinhVien]') AND name = 'ThanhTichHocTap' AND is_nullable = 0)
                BEGIN
                    ALTER TABLE [dbo].[SinhVien] ALTER COLUMN [ThanhTichHocTap] nvarchar(1000) NULL;
                END

                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SinhVien]') AND name = 'DiaChi' AND is_nullable = 0)
                BEGIN
                    ALTER TABLE [dbo].[SinhVien] ALTER COLUMN [DiaChi] nvarchar(500) NULL;
                END

                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HoSoHocBong]') AND name = 'GhiChu' AND is_nullable = 0)
                BEGIN
                    ALTER TABLE [dbo].[HoSoHocBong] ALTER COLUMN [GhiChu] nvarchar(1000) NULL;
                END

                -- 5. Bảng TaiLieuHoSo
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TaiLieuHoSo')
                BEGIN
                    CREATE TABLE [dbo].[TaiLieuHoSo] (
                        [MaTaiLieu] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [MaHoSoHocBong] INT NOT NULL,
                        [LoaiTaiLieu] NVARCHAR(50) NOT NULL,
                        [TenFileGoc] NVARCHAR(255) NOT NULL,
                        [TenFileLuu] VARCHAR(100) NOT NULL,
                        [LoaiNoiDung] VARCHAR(100) NOT NULL,
                        [DungLuong] BIGINT NOT NULL,
                        [NgayTai] DATETIME2(7) NOT NULL,
                        [MaTaiKhoanTai] INT NULL,
                        CONSTRAINT [FK_TaiLieuHoSo_HoSoHocBong_MaHoSoHocBong] FOREIGN KEY ([MaHoSoHocBong]) REFERENCES [dbo].[HoSoHocBong] ([MaHoSoHocBong]) ON DELETE CASCADE
                    );
                    CREATE NONCLUSTERED INDEX [IX_TaiLieuHoSo_MaHoSoHocBong] ON [dbo].[TaiLieuHoSo] ([MaHoSoHocBong]);
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[TaiKhoan]') AND name = 'AnhDaiDien')
                BEGIN
                    ALTER TABLE [dbo].[TaiKhoan] DROP COLUMN [AnhDaiDien];
                END

                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[SinhVien]') AND name = 'AnhDaiDien')
                BEGIN
                    ALTER TABLE [dbo].[SinhVien] DROP COLUMN [AnhDaiDien];
                END

                IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[HoSoHocBong]') AND name = 'FileMinhChung')
                BEGIN
                    ALTER TABLE [dbo].[HoSoHocBong] DROP COLUMN [FileMinhChung];
                END
            ");
        }
    }
}
