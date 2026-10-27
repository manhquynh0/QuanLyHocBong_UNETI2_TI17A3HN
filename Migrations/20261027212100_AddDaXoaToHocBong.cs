using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using QuanLyHocBong_UNETI2_TI17A3HN.Data;

#nullable disable

namespace QuanLyHocBong_UNETI2_TI17A3HN.Migrations
{
    [DbContext(typeof(QuanLyHocBong_UNETI2_TI17A3HNContext))]
    [Migration("20261027212100_AddDaXoaToHocBong")]
    public partial class AddDaXoaToHocBong : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DaXoa",
                table: "HocBong",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DaXoa",
                table: "HocBong");
        }
    }
}
