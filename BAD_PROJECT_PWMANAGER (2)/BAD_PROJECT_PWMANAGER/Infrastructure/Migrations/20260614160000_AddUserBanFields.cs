using System;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260614160000_AddUserBanFields")]
    public partial class AddUserBanFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BanReason",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BannedUntilUtc",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BanReason",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "BannedUntilUtc",
                table: "AspNetUsers");
        }
    }
}
