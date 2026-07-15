using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    /// <inheritdoc />
    public partial class CashRegisterKiosk : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KioskAccessToken",
                table: "CashRegisters",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KioskPassword",
                table: "CashRegisters",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisters_KioskAccessToken",
                table: "CashRegisters",
                column: "KioskAccessToken",
                unique: true,
                filter: "[KioskAccessToken] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CashRegisters_KioskAccessToken",
                table: "CashRegisters");

            migrationBuilder.DropColumn(
                name: "KioskAccessToken",
                table: "CashRegisters");

            migrationBuilder.DropColumn(
                name: "KioskPassword",
                table: "CashRegisters");
        }
    }
}
