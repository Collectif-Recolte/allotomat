
using System;
using System.Collections.Generic;
using System.Linq;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;

namespace Sig.App.Backend.Requests.Queries.Transactions
{
    public static class TransactionLogQueryFilters
    {
        public static IQueryable<TransactionLog> FilterByOrganizationScope(
            this IQueryable<TransactionLog> query,
            bool canManageOrganizations,
            string organizationManagerClaimValue,
            IEnumerable<Id> selectedOrganizations)
        {
            if (!canManageOrganizations)
            {
                return query.Where(x => x.OrganizationId.ToString() == organizationManagerClaimValue);
            }

            var organizationIds = selectedOrganizations?.ToArray() ?? [];
            if (organizationIds.Any())
            {
                var organizationsLongIdentifiers = organizationIds.Select(x => x.LongIdentifierForType<Organization>());
                return query.Where(x => organizationsLongIdentifiers.Contains(x.OrganizationId ?? 0));
            }

            return query;
        }

        public static IQueryable<TransactionLog> FilterByCriteria(
            this IQueryable<TransactionLog> query,
            ITransactionLogFilterCriteria criteria,
            bool currentUserCanSeeAllBeneficiaryInfo)
        {
            var longProjectId = criteria.ProjectId.LongIdentifierForType<Project>();
            var startDate = criteria.StartDate.ToUniversalTime();
            var endDate = criteria.EndDate.ToUniversalTime();
            query = query.Where(x => x.CreatedAtUtc > startDate && x.CreatedAtUtc < endDate && x.ProjectId == longProjectId);

            if (criteria.Subscriptions?.Any() ?? false)
            {
                var withoutSubscription = criteria.WithoutSubscription?.Value ?? false;
                var subscriptionLongIdentifiers = criteria.Subscriptions.Select(x => x.LongIdentifierForType<Subscription>());
                query = query.Where(x => (withoutSubscription && !x.SubscriptionId.HasValue) || subscriptionLongIdentifiers.Contains(x.SubscriptionId.GetValueOrDefault()));
            }
            else if (criteria.WithoutSubscription.IsSet() && criteria.WithoutSubscription.Value)
            {
                query = query.Where(x => !x.SubscriptionId.HasValue);
            }

            if (criteria.Categories?.Any() ?? false)
            {
                var categoriesLongIdentifiers = criteria.Categories.Select(x => x.LongIdentifierForType<BeneficiaryType>());
                query = query.Where(x => categoriesLongIdentifiers.Contains(x.BeneficiaryTypeId.GetValueOrDefault()));
            }

            if (criteria.Markets?.Any() ?? false)
            {
                var marketsLongIdentifiers = criteria.Markets.Select(x => x.LongIdentifierForType<Market>());
                query = query.Where(x => marketsLongIdentifiers.Contains(x.MarketId.GetValueOrDefault()));
            }

            if (criteria.MarketGroups?.Any() ?? false)
            {
                var marketGroupsLongIdentifiers = criteria.MarketGroups.Select(x => x.LongIdentifierForType<MarketGroup>());
                query = query.Where(x => marketGroupsLongIdentifiers.Contains(x.MarketGroupId.GetValueOrDefault()));
            }

            if (criteria.TransactionTypes?.Any() ?? false)
            {
                var transactionLogDiscriminators =
                    criteria.TransactionTypes.Select(x => Enum.Parse(typeof(TransactionLogDiscriminator), x));
                query = query.Where(x => transactionLogDiscriminators.Contains(x.Discriminator));
            }

            if (criteria.GiftCardTransactionTypes?.Any() ?? false)
            {
                var withGiftCard = criteria.GiftCardTransactionTypes.Any(x => x == "withGiftCard");
                var withoutGiftCard = criteria.GiftCardTransactionTypes.Any(x => x == "withoutGiftCard");

                if (withoutGiftCard && withGiftCard)
                {
                    // Nothing to do in the case it's with and without
                }
                else if (withGiftCard)
                {
                    query = query.Where(x => x.SubscriptionId == null);
                }
                else if (withoutGiftCard)
                {
                    query = query.Where(x => x.SubscriptionId != null);
                }
            }

            if (criteria.SearchText.IsSet() && !string.IsNullOrEmpty(criteria.SearchText.Value))
            {
                var searchText = criteria.SearchText.Value.Split(' ').AsEnumerable();
                foreach (var text in searchText)
                {
                    if (currentUserCanSeeAllBeneficiaryInfo)
                    {
                        query = query.Where(x =>
                            EF.Functions.Collate(x.BeneficiaryID1, SearchCollation.AccentInsensitive).Contains(text) ||
                            EF.Functions.Collate(x.BeneficiaryID2, SearchCollation.AccentInsensitive).Contains(text) ||
                            EF.Functions.Collate(x.BeneficiaryEmail, SearchCollation.AccentInsensitive).Contains(text) ||
                            EF.Functions.Collate(x.BeneficiaryFirstname, SearchCollation.AccentInsensitive).Contains(text) ||
                            EF.Functions.Collate(x.BeneficiaryLastname, SearchCollation.AccentInsensitive).Contains(text) ||
                            EF.Functions.Collate(x.CardNumber, SearchCollation.AccentInsensitive).Contains(text)
                        );
                    }
                    else
                    {
                        query = query.Where(x =>
                            EF.Functions.Collate(x.BeneficiaryID1, SearchCollation.AccentInsensitive).Contains(text) ||
                            EF.Functions.Collate(x.BeneficiaryID2, SearchCollation.AccentInsensitive).Contains(text) ||
                            EF.Functions.Collate(x.CardNumber, SearchCollation.AccentInsensitive).Contains(text)
                        );
                    }
                }
            }

            return query;
        }
    }
}
