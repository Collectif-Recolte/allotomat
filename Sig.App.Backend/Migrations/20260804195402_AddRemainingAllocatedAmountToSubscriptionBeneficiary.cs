using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddRemainingAllocatedAmountToSubscriptionBeneficiary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RemainingAllocatedAmount",
                table: "SubscriptionBeneficiaries",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RemainingAllocatedAmount",
                table: "SubscriptionBeneficiaries");
        }
    }
}
