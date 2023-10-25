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
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Services.Beneficiaries;

namespace Sig.App.Backend.Requests.Queries.Beneficiaries
{
    public class SearchOffPlatformBeneficiaries : IRequestHandler<SearchOffPlatformBeneficiaries.Query, Pagination<OffPlatformBeneficiary>>
    {
        private readonly AppDbContext db;
        private readonly IBeneficiaryService beneficiaryService;

        public SearchOffPlatformBeneficiaries(AppDbContext db, IBeneficiaryService beneficiaryService)
        {
            this.db = db;
            this.beneficiaryService = beneficiaryService;
        }

        public async Task<Pagination<OffPlatformBeneficiary>> Handle(Query request, CancellationToken cancellationToken)
        {
            IQueryable<Beneficiary> query = db.Beneficiaries.Where(x => (x is OffPlatformBeneficiary)).Include(x => x.Card.Funds).ThenInclude(x => x.ProductGroup);

            if (request.OrganizationId.IsSet())
            {
                var organizationId = request.OrganizationId.Value;
                query = query.Where(x => x.OrganizationId == organizationId);
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

            var currentUserCanSeeAllBeneficiaryInfo = await beneficiaryService.CurrentUserCanSeeAllBeneficiaryInfo();
            if (request.SearchText.IsSet() && !string.IsNullOrEmpty(request.SearchText.Value))
            {
                var searchText = request.SearchText.Value.Split(' ').AsEnumerable();

                foreach (var text in searchText)
                {
                    if (currentUserCanSeeAllBeneficiaryInfo)
                    {
                        query = query.Where(x => x.ID1.Contains(text) || x.ID2.Contains(text) || x.Email.Contains(text) || x.Firstname.Contains(text) || x.Lastname.Contains(text));
                    }
                    else
                    {
                        query = query.Where(x => x.ID1.Contains(text) || x.ID2.Contains(text));
                    }
                }
            }

            var sorted = Sort(query.Select(x => x as OffPlatformBeneficiary), request.Sort?.Field ?? BeneficiarySort.Default, request.Sort?.Order ?? SortOrder.Asc);
            return await Pagination.For(sorted, request.Page);
        }

        public class Query : IRequest<Pagination<OffPlatformBeneficiary>>
        {
            public Page Page { get; set; }
            public Sort<BeneficiarySort> Sort { get; set; }
            public Maybe<long> OrganizationId { get; set; }
            public Maybe<bool> WithCard { get; set; }
            public Maybe<string> SearchText { get; set; }
        }

        private static IOrderedQueryable<OffPlatformBeneficiary> Sort(IQueryable<OffPlatformBeneficiary> query, BeneficiarySort sort, SortOrder order)
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
}
