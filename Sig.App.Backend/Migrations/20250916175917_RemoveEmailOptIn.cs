using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEmailOptIn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserEmailOptIns");

            migrationBuilder.AddColumn<string>(
                name: "EmailOptIn",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.Sql("UPDATE u SET u.EmailOptIn = 'CreatedCardPdfEmail;MonthlyBalanceReportEmailJanuary;MonthlyBalanceReportEmailFebruary;MonthlyBalanceReportEmailMarch;MonthlyBalanceReportEmailApril;MonthlyBalanceReportEmailMay;MonthlyBalanceReportEmailJune;MonthlyBalanceReportEmailJuly;MonthlyBalanceReportEmailAugust;MonthlyBalanceReportEmailSeptember;MonthlyBalanceReportEmailOctober;MonthlyBalanceReportEmailNovember;MonthlyBalanceReportEmailDecember;MonthlyCardBalanceReportEmailJanuary;MonthlyCardBalanceReportEmailFebruary;MonthlyCardBalanceReportEmailMarch;MonthlyCardBalanceReportEmailApril;MonthlyCardBalanceReportEmailMay;MonthlyCardBalanceReportEmailJune;MonthlyCardBalanceReportEmailJuly;MonthlyCardBalanceReportEmailAugust;MonthlyCardBalanceReportEmailSeptember;MonthlyCardBalanceReportEmailOctober;MonthlyCardBalanceReportEmailNovember;MonthlyCardBalanceReportEmailDecember;SubscriptionExpirationEmail' FROM AspNetUsers u");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailOptIn",
                table: "AspNetUsers");

            migrationBuilder.CreateTable(
                name: "UserEmailOptIns",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedCardPdfEmail = table.Column<bool>(type: "bit", nullable: false),
                    MonthlyBalanceReportEmail = table.Column<bool>(type: "bit", nullable: false),
                    MonthlyCardBalanceReportEmail = table.Column<bool>(type: "bit", nullable: false),
                    SubscriptionExpirationEmail = table.Column<bool>(type: "bit", nullable: false)
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
        }
    }
}
