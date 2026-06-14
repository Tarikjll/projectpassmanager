using System;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260614173000_AddSoftDeleteAndLoginAttempts")]
    public partial class AddSoftDeleteAndLoginAttempts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "Vaults",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "PasswordEntries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LoginAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LoginIdentifier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Succeeded = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttemptedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginAttempts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_AttemptedAtUtc",
                table: "LoginAttempts",
                column: "AttemptedAtUtc");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LoginAttempts");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "PasswordEntries");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "Vaults");
        }
    }
}
