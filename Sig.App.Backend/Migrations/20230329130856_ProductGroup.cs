using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    public partial class ProductGroup : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ProductGroupId",
                table: "SubscriptionTypes",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "ProductGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Color = table.Column<int>(type: "int", nullable: false),
                    OrderOfAppearance = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductGroups_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionTypes_ProductGroupId",
                table: "SubscriptionTypes",
                column: "ProductGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductGroups_ProjectId",
                table: "ProductGroups",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_SubscriptionTypes_ProductGroups_ProductGroupId",
                table: "SubscriptionTypes",
                column: "ProductGroupId",
                principalTable: "ProductGroups",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubscriptionTypes_ProductGroups_ProductGroupId",
                table: "SubscriptionTypes");

            migrationBuilder.DropTable(
                name: "ProductGroups");

            migrationBuilder.DropIndex(
                name: "IX_SubscriptionTypes_ProductGroupId",
                table: "SubscriptionTypes");

            migrationBuilder.DropColumn(
                name: "ProductGroupId",
                table: "SubscriptionTypes");
        }
    }
}
