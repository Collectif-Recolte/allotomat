using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    /// <inheritdoc />
    public partial class MarketGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProjectId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketGroups_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketGroupMarkets",
                columns: table => new
                {
                    MarketGroupId = table.Column<long>(type: "bigint", nullable: false),
                    MarketId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketGroupMarkets", x => new { x.MarketId, x.MarketGroupId });
                    table.ForeignKey(
                        name: "FK_MarketGroupMarkets_MarketGroups_MarketGroupId",
                        column: x => x.MarketGroupId,
                        principalTable: "MarketGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MarketGroupMarkets_Markets_MarketId",
                        column: x => x.MarketId,
                        principalTable: "Markets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketGroupMarkets_MarketGroupId",
                table: "MarketGroupMarkets",
                column: "MarketGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketGroups_ProjectId",
                table: "MarketGroups",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketGroupMarkets");

            migrationBuilder.DropTable(
                name: "MarketGroups");
        }
    }
}
