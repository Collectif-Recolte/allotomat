using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    public partial class createtransactionlogs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create a TransactionLog for all Transactions with currently no TransactionLog
            migrationBuilder.Sql("INSERT INTO TransactionLogs " +
                "(TransactionUniqueId, CreatedAtUtc, Discriminator, TotalAmount, CardProgramCardId, CardNumber, BeneficiaryId, BeneficiaryID1, BeneficiaryID2, BeneficiaryFirstname, " +
                "BeneficiaryLastname, BeneficiaryEmail, BeneficiaryPhone, BeneficiaryIsOffPlatform, BeneficiaryTypeId, OrganizationId, OrganizationName, MarketId, MarketName, SubscriptionId, SubscriptionName, ProjectId) " +
                "SELECT t.TransactionUniqueId, t.CreatedAtUtc, " +
                "CASE t.Discriminator " +
                    "WHEN 'SubscriptionAddingFundTransaction' THEN 0 " +
                    "WHEN 'ManuallyAddingFundTransaction' THEN 1 " +
                    "WHEN 'LoyaltyAddingFundTransaction' THEN 2 " +
                    "WHEN 'OffPlatformAddingFundTransaction' THEN 3 " +
                    "WHEN 'ExpireFundTransaction' THEN 4 " +
                    "WHEN 'PaymentTransaction' THEN 5 END, " +
                "t.Amount, c.ProgramCardId, c.CardNumber, t.BeneficiaryId, b.ID1, b.ID2, b.Firstname, b.Lastname, b.Email, b.Phone, CASE b.Discriminator WHEN 'Beneficiary' THEN 0 ELSE 1 END, b.BeneficiaryTypeId, " +
                "t.OrganizationId, o.Name, t.MarketId, m.Name, " +
                "CASE WHEN t.SubscriptionTypeId IS NOT NULL THEN sts.Id WHEN t.Discriminator = 'PaymentTransaction' THEN (SELECT TOP (1) CASE WHEN pts1.Id IS NOT NULL THEN pts1.Id ELSE pts2.Id END FROM AddingFundTransactionPaymentTransaction aftpt INNER JOIN Transactions ptaft ON ptaft.Id = aftpt.TransactionsId LEFT JOIN Subscriptions pts1 ON pts1.Id = ptaft.SubscriptionId LEFT JOIN SubscriptionTypes ptst ON ptst.Id = ptaft.SubscriptionTypeId LEFT JOIN Subscriptions pts2 ON pts2.Id = ptst.SubscriptionId WHERE aftpt.TransactionsId1 = t.Id) ELSE s.Id END, " +
                "CASE WHEN t.SubscriptionTypeId IS NOT NULL THEN sts.Name WHEN t.Discriminator = 'PaymentTransaction' THEN (SELECT TOP (1) CASE WHEN pts1.Id IS NOT NULL THEN pts1.Name ELSE pts2.Name END FROM AddingFundTransactionPaymentTransaction aftpt INNER JOIN Transactions ptaft ON ptaft.Id = aftpt.TransactionsId LEFT JOIN Subscriptions pts1 ON pts1.Id = ptaft.SubscriptionId LEFT JOIN SubscriptionTypes ptst ON ptst.Id = ptaft.SubscriptionTypeId LEFT JOIN Subscriptions pts2 ON pts2.Id = ptst.SubscriptionId WHERE aftpt.TransactionsId1 = t.Id) ELSE s.Name END, " +
                "CASE WHEN t.OrganizationId IS NULL THEN pg.ProjectId ELSE o.ProjectId END " +
                "FROM Transactions t " +
                "LEFT JOIN Cards c ON c.Id = t.CardId " +
                "LEFT JOIN Beneficiaries b ON b.Id = t.BeneficiaryId " +
                "LEFT JOIN Organizations o ON o.Id = t.OrganizationId " +
                "LEFT JOIN Markets m ON m.Id = t.MarketId " +
                "LEFT JOIN Subscriptions s ON s.Id = t.SubscriptionId " +
                "LEFT JOIN ProductGroups pg ON pg.Id = t.ProductGroupId " +
                "LEFT JOIN SubscriptionTypes st ON st.Id = t.SubscriptionTypeId " +
                "LEFT JOIN Subscriptions sts ON sts.Id = st.SubscriptionId " +
                "WHERE t.TransactionUniqueId NOT IN (SELECT tl.TransactionUniqueId FROM TransactionLogs tl)");

            // Create a TransactionLogProductGroup for every PaymentTransactionProductGroup for which the TransactionLog currently has no TransactionLogProductGroup
            migrationBuilder.Sql("INSERT INTO TransactionLogProductGroups (TransactionLogId, ProductGroupId, ProductGroupName, Amount) " +
                                 "SELECT tl.Id, ptpg.ProductGroupId, pg.Name, ptpg.Amount " +
                                 "FROM PaymentTransactionProductGroups ptpg " +
                                 "INNER JOIN Transactions t ON t.Id = ptpg.PaymentTransactionId " +
                                 "LEFT JOIN ProductGroups pg ON pg.Id = ptpg.ProductGroupId " +
                                 "INNER JOIN TransactionLogs tl ON tl.TransactionUniqueId = t.TransactionUniqueId " +
                                 "LEFT JOIN TransactionLogProductGroups tlpg ON tlpg.TransactionLogId = tl.Id " +
                                 "WHERE tlpg.Id IS NULL");
            
            // Create a TransctionLogProductGroup for each Transaction of type SubscriptionAddingFundTransaction, ManuallyAddingFundTransaction, LoyaltyAddingFundTransaction, OffPlatformAddingFundTransaction or ExpireFundTransaction
            migrationBuilder.Sql("INSERT INTO TransactionLogProductGroups (TransactionLogId, ProductGroupId, ProductGroupName, Amount) " +
                                 "SELECT tl.Id, t.ProductGroupId, pg.Name, tl.TotalAmount " +
                                 "FROM Transactions t " +
                                 "INNER JOIN TransactionLogs tl ON tl.TransactionUniqueId = t.TransactionUniqueId " +
                                 "LEFT JOIN TransactionLogProductGroups tlpg ON tlpg.TransactionLogId = tl.Id " +
                                 "LEFT JOIN ProductGroups pg ON pg.Id = t.ProductGroupId " +
                                 "WHERE tlpg.Id IS NULL AND t.ProductGroupId IS NOT NULL AND t.Discriminator IN ('SubscriptionAddingFundTransaction', 'ManuallyAddingFundTransaction', 'LoyaltyAddingFundTransaction', 'OffPlatformAddingFundTransaction', 'ExpireFundTransaction')");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
