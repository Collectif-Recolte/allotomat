using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    public partial class RemoveMarketUrlAddProjectUrl : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Url",
                table: "Markets");

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "Projects",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Url",
                table: "Projects");

            migrationBuilder.AddColumn<string>(
                name: "Url",
                table: "Markets",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
