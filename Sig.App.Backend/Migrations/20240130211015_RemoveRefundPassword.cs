using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    public partial class RemoveRefundPassword : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefundTransactionPassword",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "RefundTransactionPasswordSalt",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "RefundTransactionPassword",
                table: "Markets");

            migrationBuilder.DropColumn(
                name: "RefundTransactionPasswordSalt",
                table: "Markets");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RefundTransactionPassword",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RefundTransactionPasswordSalt",
                table: "Projects",
                type: "varbinary(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundTransactionPassword",
                table: "Markets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RefundTransactionPasswordSalt",
                table: "Markets",
                type: "varbinary(max)",
                nullable: true);
        }
    }
}
