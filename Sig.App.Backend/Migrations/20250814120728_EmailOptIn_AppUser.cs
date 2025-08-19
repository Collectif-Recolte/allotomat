using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    /// <inheritdoc />
    public partial class EmailOptIn_AppUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserEmailOptIns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedCardPdfEmail = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    MonthlyBalanceReportEmail = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    MonthlyCardBalanceReportEmail = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SubscriptionExpirationEmail = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEmailOptIns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserEmailOptIns_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserEmailOptIns_UserId",
                table: "UserEmailOptIns",
                column: "UserId",
                unique: true);

            migrationBuilder.Sql("INSERT INTO UserEmailOptIns (UserId) SELECT u.Id AS UserId FROM AspNetUsers u");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserEmailOptIns");
        }
    }
}
