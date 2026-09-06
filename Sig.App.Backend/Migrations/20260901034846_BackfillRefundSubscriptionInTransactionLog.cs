using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sig.App.Backend.Migrations
{
    /// <inheritdoc />
    public partial class BackfillRefundSubscriptionInTransactionLog : Migration
    {
        /// <summary>
        /// CRCL-2559 - Les logs de remboursement d'achat créés avant le correctif n'ont pas d'abonnement,
        /// parce que seuls les achats financés par un versement d'abonnement étaient reconnus; ceux financés
        /// par un versement manuel ne l'étaient pas. On reconstitue le lien de la même façon que
        /// <c>RefundTransaction.AssignSubscriptionFromFundSources</c> : le remboursement mène à l'achat,
        /// l'achat mène aux versements qui l'ont financé, et ces versements portent l'abonnement.
        /// Seules les deux colonnes d'affichage sont touchées; aucun solde de carte ni d'enveloppe n'en dépend.
        /// Les remboursements payés entièrement par carte-cadeau (aucun versement source) restent sans
        /// abonnement, ce qui est le comportement attendu.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE tl
                SET tl.SubscriptionId = src.SubscriptionId,
                    tl.SubscriptionName = src.SubscriptionName
                FROM TransactionLogs tl
                INNER JOIN Transactions rt
                    ON rt.Discriminator = 'RefundTransaction'
                   AND rt.TransactionUniqueId = tl.TransactionUniqueId
                CROSS APPLY (
                    SELECT TOP 1 s.Id AS SubscriptionId, s.Name AS SubscriptionName
                    FROM (
                        -- Lien courant : table de jonction explicite.
                        SELECT ptaft.AddingFundTransactionId AS AddingFundTransactionId,
                               0 AS LinkPriority,
                               ptaft.Id AS LinkId
                        FROM PaymentTransactionAddingFundTransactions ptaft
                        WHERE ptaft.PaymentTransactionId = rt.InitialTransactionId

                        UNION ALL

                        -- Lien historique, d'avant la table de jonction explicite.
                        SELECT afpt.TransactionsId,
                               1,
                               afpt.TransactionsId
                        FROM AddingFundTransactionPaymentTransaction afpt
                        WHERE afpt.TransactionsId1 = rt.InitialTransactionId
                    ) AS fundSources
                    INNER JOIN Transactions aft ON aft.Id = fundSources.AddingFundTransactionId
                    LEFT JOIN SubscriptionTypes st ON st.Id = aft.SubscriptionTypeId
                    INNER JOIN Subscriptions s
                        ON s.Id = CASE aft.Discriminator
                                      WHEN 'ManuallyAddingFundTransaction' THEN aft.SubscriptionId
                                      WHEN 'SubscriptionAddingFundTransaction' THEN st.SubscriptionId
                                  END
                    WHERE aft.Discriminator IN ('ManuallyAddingFundTransaction', 'SubscriptionAddingFundTransaction')
                    ORDER BY fundSources.LinkPriority, fundSources.LinkId
                ) AS src
                WHERE tl.Discriminator = 10
                  AND tl.SubscriptionId IS NULL
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
