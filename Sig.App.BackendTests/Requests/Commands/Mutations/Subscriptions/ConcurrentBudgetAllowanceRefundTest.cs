using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Helpers;
using Sig.App.Backend.Requests.Commands.Mutations.Subscriptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Subscriptions
{
    // ------------------------------------------------------------------------------------------
    // CRCL-2677 - Critère d'acceptation du correctif de concurrence sur BudgetAllowance.AvailableFund.
    //
    // Le défaut d'origine (GitHub #307) : `RemoveBeneficiaryFromSubscription` fait un
    // lire-modifier-écrire sur `AvailableFund` sans jeton de concurrence. Quand un admin retire des
    // participants en rafale, la requête N+1 lit le solde avant le SaveChanges de N, et le crédit de
    // N est écrasé — sans erreur, et avec un TransactionLog de remboursement qui survit (l'INSERT
    // n'entre jamais en conflit). D'où les écarts d'enveloppe mesurés en production, toujours des
    // multiples entiers du remboursement unitaire du groupe.
    //
    // Ce fichier est le pendant « assertions à l'endroit » de `ConcurrentPaymentTest` (!5464), qui
    // documentait la même famille de défaut sur les compteurs de carte avec des assertions inversées.
    // Ici les assertions sont celles du comportement CORRECT : N retraits entrelacés créditent
    // exactement N × remboursement. Si ce test devient rouge, la protection de concurrence a été
    // retirée ou contournée — ce n'est pas le test qu'il faut réparer.
    //
    // L'entrelacement est forcé à la main (un `AppDbContext` par retrait, toutes les lectures avant
    // toutes les écritures), donc déterministe : pas de `Task.WhenAll`, pas de dépendance à
    // l'ordonnanceur. Un test de concurrence qui échoue une fois sur dix ne prouve rien et finit
    // désactivé.
    //
    // Sans le correctif, ce test constate exactement la signature de production : l'enveloppe finit à
    // 216 (1 × le remboursement unitaire) au lieu de 648, deux crédits sur trois évaporés.
    // ------------------------------------------------------------------------------------------
    public class ConcurrentBudgetAllowanceRefundTest : TestBase
    {
        private const decimal RefundPerBeneficiary = 216m;
        private const int BeneficiaryCount = 3;

        private readonly Project project;
        private readonly Organization organization;
        private readonly BeneficiaryType beneficiaryType;
        private readonly Subscription subscription;
        private readonly BudgetAllowance budgetAllowance;
        private readonly List<Beneficiary> beneficiaries = new();

        public ConcurrentBudgetAllowanceRefundTest()
        {
            project = new Project { Name = "Project 1" };
            DbContext.Projects.Add(project);

            organization = new Organization { Name = "Organization 1", Project = project };
            DbContext.Organizations.Add(organization);

            beneficiaryType = new BeneficiaryType
            {
                Project = project,
                Keys = "Beneficiary type 1",
                Name = "Beneficiary type 1"
            };
            DbContext.BeneficiaryTypes.Add(beneficiaryType);

            var productGroup = new ProductGroup
            {
                Project = project,
                Color = ProductGroupColor.Color_1,
                Name = "Product group 1",
                OrderOfAppearance = 1
            };
            DbContext.ProductGroups.Add(productGroup);

            var today = Clock.GetCurrentInstant().ToDateTimeUtc();
            subscription = new Subscription
            {
                Name = "Subscription 1",
                StartDate = new DateTime(today.Year, today.Month, 1),
                EndDate = new DateTime(today.Year, today.Month, 2).AddMonths(1),
                MonthlyPaymentMoment = SubscriptionMonthlyPaymentMoment.FirstDayOfTheMonth,
                Project = project,
                Types = new List<SubscriptionType>
                {
                    new SubscriptionType
                    {
                        Amount = RefundPerBeneficiary,
                        BeneficiaryType = beneficiaryType,
                        ProductGroup = productGroup
                    }
                }
            };

            // L'enveloppe est vide au départ : tout ce qu'elle contiendra à la fin vient des retraits,
            // donc l'assertion ne peut pas être satisfaite par accident par un solde de départ.
            budgetAllowance = new BudgetAllowance
            {
                AvailableFund = 0m,
                OriginalFund = BeneficiaryCount * RefundPerBeneficiary,
                Organization = organization,
                Subscription = subscription
            };
            DbContext.BudgetAllowances.Add(budgetAllowance);

            subscription.Beneficiaries = new List<SubscriptionBeneficiary>();

            for (var i = 0; i < BeneficiaryCount; i++)
            {
                var beneficiary = new Beneficiary
                {
                    Firstname = $"Beneficiary {i}",
                    Lastname = "Doe",
                    Address = "123, example street",
                    Email = $"beneficiary{i}@example.com",
                    Phone = "555-555-1234",
                    BeneficiaryType = beneficiaryType,
                    Organization = organization
                };
                DbContext.Beneficiaries.Add(beneficiary);
                beneficiaries.Add(beneficiary);

                subscription.Beneficiaries.Add(new SubscriptionBeneficiary
                {
                    Beneficiary = beneficiary,
                    BeneficiaryType = beneficiaryType,
                    Subscription = subscription,
                    BudgetAllowance = budgetAllowance,
                    RemainingAllocatedAmount = RefundPerBeneficiary
                });
            }

            DbContext.Subscriptions.Add(subscription);

            DbContext.AddingFundToCardRuns.Add(new Sig.App.Backend.DbModel.Entities.BackgroundJobs.AddingFundToCardRun
            {
                Date = today,
                Name = SubscriptionHelper.AddingFundToCardFirstDayOfTheMonthJobName
            });

            DbContext.SaveChanges();
        }

        private RemoveBeneficiaryFromSubscription BuildHandler(AppDbContext context)
        {
            return new RemoveBeneficiaryFromSubscription(
                NullLogger<RemoveBeneficiaryFromSubscription>.Instance, context, Clock, HttpContextAccessor);
        }

        private RemoveBeneficiaryFromSubscription.Input BuildInput(Beneficiary beneficiary)
        {
            return new RemoveBeneficiaryFromSubscription.Input
            {
                BeneficiaryId = beneficiary.GetIdentifier(),
                SubscriptionId = subscription.GetIdentifier()
            };
        }

        [Fact]
        public async Task ConcurrentRemovals_CreditTheEnvelopeExactlyOncePerBeneficiary()
        {
            var contexts = Enumerable.Range(0, BeneficiaryCount).Select(_ => CreateDbContext()).ToList();

            // Toutes les lectures d'abord. Chaque contexte charge sa propre copie suivie de
            // l'abonnement, de ses paires et de l'enveloppe. EF Core réutilise ensuite ces instances
            // déjà suivies plutôt que de rafraîchir leurs valeurs depuis la base (résolution
            // d'identité), donc chaque handler travaillera sur une photographie prise avant toute
            // écriture — l'entrelacement exact que produit une rafale de retraits.
            foreach (var context in contexts)
            {
                await context.Subscriptions
                    .Include(x => x.Beneficiaries).ThenInclude(x => x.BudgetAllowance)
                    .FirstOrDefaultAsync(x => x.Id == subscription.Id);
            }

            // Puis toutes les écritures, l'une après l'autre, chacune sur sa photographie périmée.
            for (var i = 0; i < BeneficiaryCount; i++)
            {
                await BuildHandler(contexts[i]).Handle(BuildInput(beneficiaries[i]), CancellationToken.None);
            }

            // Un contexte neuf, jamais utilisé avant cet instant, pour lire l'état réellement persisté
            // sans hériter des valeurs suivies (donc périmées) des contextes ci-dessus.
            var verify = CreateDbContext();

            var finalAvailableFund = await verify.BudgetAllowances.AsNoTracking()
                .Where(x => x.Id == budgetAllowance.Id).Select(x => x.AvailableFund).SingleAsync();
            var refundLogCount = await verify.TransactionLogs.AsNoTracking().CountAsync(x =>
                x.Discriminator == TransactionLogDiscriminator
                    .RefundBudgetAllowanceFromRemovedBeneficiaryFromSubscriptionTransactionLog);
            var remainingPairs = await verify.SubscriptionBeneficiaries.AsNoTracking()
                .CountAsync(x => x.SubscriptionId == subscription.Id);

            // Le cœur du ticket : aucun crédit perdu. 3 × 216 = 648, jamais 216 ni 432.
            finalAvailableFund.Should().Be(BeneficiaryCount * RefundPerBeneficiary);

            // Et le journal reste le reflet fidèle de l'argent : autant de logs de remboursement que
            // de crédits réellement appliqués. C'est précisément la divergence observée en production
            // (des logs sans crédit) que cette égalité interdit désormais.
            refundLogCount.Should().Be(BeneficiaryCount);

            remainingPairs.Should().Be(0);
        }
    }
}
