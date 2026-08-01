using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDuplicateSubscriptionAddingFundTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove duplicate SubscriptionAddingFundTransaction records
            // Keep the first transaction (lowest ID) for each CardId + SubscriptionTypeId created on the same day
            migrationBuilder.Sql(@"
                DELETE FROM Transactions
                WHERE Id IN (
                    SELECT t.Id
                    FROM Transactions t
                    INNER JOIN (
                        SELECT CardId, SubscriptionTypeId, CAST(CreatedAtUtc AS DATE) as CreatedDate, MIN(Id) as FirstId
                        FROM Transactions
                        WHERE Discriminator = 'SubscriptionAddingFundTransaction'
                          AND CardId IS NOT NULL
                        GROUP BY CardId, SubscriptionTypeId, CAST(CreatedAtUtc AS DATE)
                        HAVING COUNT(*) > 1
                    ) duplicates ON t.CardId = duplicates.CardId
                      AND t.SubscriptionTypeId = duplicates.SubscriptionTypeId
                      AND CAST(t.CreatedAtUtc AS DATE) = duplicates.CreatedDate
                      AND t.Id > duplicates.FirstId
                    WHERE t.Discriminator = 'SubscriptionAddingFundTransaction'
                )
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Note: This migration removes data, so Down() is not reversible
        }
    }
}
