using System;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Utilities;
using Sig.App.Backend.Utilities.Sorting;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.Gql.Schema.Types;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Services.Beneficiaries;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using NodaTime;

namespace Sig.App.Backend.Requests.Queries.Beneficiaries
{
    public class SearchBeneficiaries : IRequestHandler<SearchBeneficiaries.Query, PaymentConflictPagination<Beneficiary>>
    {
        private readonly AppDbContext db;
        private readonly IBeneficiaryService beneficiaryService;
        private readonly IClock clock;

        public SearchBeneficiaries(AppDbContext db, IBeneficiaryService beneficiaryService, IClock clock)
        {
            this.db = db;
            this.beneficiaryService = beneficiaryService;
            this.clock = clock;
        }

        public async Task<PaymentConflictPagination<Beneficiary>> Handle(Query request, CancellationToken cancellationToken)
        {
            IQueryable<Beneficiary> query = db.Beneficiaries.Include(x => x.Card.Funds).ThenInclude(x => x.ProductGroup).Include(x => x.Subscriptions).ThenInclude(x => x.BeneficiaryType);

            var today = clock
                .GetCurrentInstant()
                .ToDateTimeUtc();

            if (request.OrganizationId.IsSet())
            {
                var organizationId = request.OrganizationId.Value;
                query = query.Where(x => x.OrganizationId == organizationId);
            }

            if (request.Subscriptions != null)
            {
                var withoutSubscription = request.WithoutSubscription?.Value ?? false;
                query = query.Where(x => (withoutSubscription && x.Subscriptions.Count == 0) || x.Subscriptions.Any(y => request.Subscriptions.Contains(y.SubscriptionId)));
            }
            else if (request.WithoutSubscription.IsSet())
            {
                if (request.WithoutSubscription.Value)
                {
                    query = query.Where(x => x.Subscriptions.Count == 0);
                }
                else
                {
                    query = query.Where(x => x.Subscriptions.Count > 0);
                }
            }

            if (request.WithoutSpecificSubscriptions != null)
            {
                query = query.Where(x => !x.Subscriptions.Any(y => request.WithoutSpecificSubscriptions.Contains(y.SubscriptionId)));
            }

            if (request.Categories != null)
            {
                query = query.Where(x => request.Categories.Contains(x.BeneficiaryTypeId.GetValueOrDefault()));
            }

            if (request.WithoutSpecificCategories != null)
            {
                query = query.Where(x => !request.WithoutSpecificCategories.Contains(x.BeneficiaryTypeId.GetValueOrDefault()));
            }

            if (request.WithCard.IsSet())
            {
                if(request.WithCard.Value)
                {
                    query = query.Where(x => x.CardId != null);
                }
                else
                {
                    query = query.Where(x => x.CardId == null);
                }
            }

            if (request.WithConflictPayment.IsSet())
            {
                if (request.WithConflictPayment.Value)
                {
                    query = query.Where(x => x.Subscriptions.Any(y => y.BeneficiaryType != x.BeneficiaryType && y.Subscription.EndDate > today));
                }
                else
                {
                    query = query.Where(x => !x.Subscriptions.Any(y => y.BeneficiaryType != x.BeneficiaryType && y.Subscription.EndDate > today));
                }
            }

            if (request.WithCardDisabled.IsSet())
            {
                if (request.WithCardDisabled.Value)
                {
                    query = query.Where(x => x.Card.IsDisabled == request.WithCardDisabled.Value);
                }
                else
                {
                    query = query.Where(x => x.Card.IsDisabled == request.WithCardDisabled.Value || x.Card == null);
                }
            }

            if (request.Status != null)
            {
                var isActive = request.Status.Contains(BeneficiaryStatus.Active);
                var isInactive = request.Status.Contains(BeneficiaryStatus.Inactive);

                query = query.Where(x => ((x as OffPlatformBeneficiary).IsActive == true && isActive) || ((x as OffPlatformBeneficiary).IsActive == false && isInactive));
            }

            var currentUserCanSeeAllBeneficiaryInfo = await beneficiaryService.CurrentUserCanSeeAllBeneficiaryInfo();
            if (request.SearchText.IsSet() && !string.IsNullOrEmpty(request.SearchText.Value))
            {
                var searchText = request.SearchText.Value.Split(' ').AsEnumerable();

                foreach (var text in searchText)
                {
                    if (currentUserCanSeeAllBeneficiaryInfo)
                    {
                        query = query.Where(x => x.ID1.Contains(text) || x.ID2.Contains(text) || x.Email.Contains(text) || x.Firstname.Contains(text) || x.Lastname.Contains(text) || (x.Card != null && x.Card.CardNumber.Contains(text) || x.Card.ProgramCardId.ToString().Contains(text)));
                    }
                    else
                    {
                        query = query.Where(x => x.ID1.Contains(text) || x.ID2.Contains(text) || (x.Card != null && x.Card.CardNumber.Contains(text) || x.Card.ProgramCardId.ToString().Contains(text)));
                    }
                }
            }

            var conflictPaymentCount = await query.Where(x => x.Subscriptions.Any(y => y.BeneficiaryType != x.BeneficiaryType && y.Subscription.EndDate > today)).CountAsync();

            var sorted = Sort(query, request.Sort?.Field ?? BeneficiarySort.Default, request.Sort?.Order ?? SortOrder.Asc);
            return await PaymentConflictPagination.For(sorted, request.Page, conflictPaymentCount);
        }

        public class Query : IRequest<PaymentConflictPagination<Beneficiary>>
        {
            public Page Page { get; set; }
            public Sort<BeneficiarySort> Sort { get; set; }
            public Maybe<long> OrganizationId { get; set; }
            public Maybe<bool> WithoutSubscription { get; set; }
            public IEnumerable<long> Subscriptions { get; set; }
            public IEnumerable<long> WithoutSpecificSubscriptions { get; set; }
            public IEnumerable<long> Categories { get; set; }
            public IEnumerable<long> WithoutSpecificCategories { get; set; }
            public IEnumerable<BeneficiaryStatus> Status { get; set; }
            public Maybe<bool> WithCard { get; set; }
            public Maybe<bool> WithConflictPayment { get; set; }
            public Maybe<string> SearchText { get; set; }
            public Maybe<bool> WithCardDisabled { get; set; }
        }

        private static IOrderedQueryable<Beneficiary> Sort(IQueryable<Beneficiary> query, BeneficiarySort sort, SortOrder order)
        {
            switch (sort)
            {
                case BeneficiarySort.Default:
                    return query
                        .SortBy(x => x.SortOrder, order);
                case BeneficiarySort.ByFundAvailableOnCard:
                    return query
                        .SortBy(x => x.Card != null ? x.Card.Funds.Where(x => x.ProductGroup.Name != ProductGroupType.LOYALTY).Sum(x => x.Amount) : 0, order);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public enum BeneficiarySort
    {
        Default,
        ByFundAvailableOnCard
    }
}
