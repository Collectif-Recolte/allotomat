using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    public partial class ProjectRefundTransactionPassword : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefundTransactionPassword",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "RefundTransactionPasswordSalt",
                table: "Projects");
        }
    }
}
