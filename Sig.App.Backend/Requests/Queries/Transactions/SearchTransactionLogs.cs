using System;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Utilities;
using Sig.App.Backend.Utilities.Sorting;
using GraphQL.Conventions;
using System.Collections.Generic;
using Sig.App.Backend.Gql.Schema.Types;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.Services.Beneficiaries;
using Sig.App.Backend.Services.Permission.Enums;
using Sig.App.Backend.Services.Permission;
using Sig.App.Backend.Gql.Interfaces;
using Microsoft.AspNetCore.Identity;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Gql.Bases;

namespace Sig.App.Backend.Requests.Queries.Transactions
{
    public class SearchTransactionLogs : IRequestHandler<SearchTransactionLogs.Query, TransactionLogsPagination<TransactionLog>>
    {
        private readonly IAppUserContext ctx;
        private readonly AppDbContext db;
        private readonly UserManager<AppUser> userManager;
        private readonly IBeneficiaryService beneficiaryService;
        private readonly PermissionService permissionService;

        public SearchTransactionLogs(IAppUserContext ctx, AppDbContext db, UserManager<AppUser> userManager, IBeneficiaryService beneficiaryService, PermissionService permissionService)
        {
            this.ctx = ctx;
            this.db = db;
            this.userManager = userManager;
            this.beneficiaryService = beneficiaryService; 
            this.permissionService = permissionService;
        }

        public async Task<TransactionLogsPagination<TransactionLog>> Handle(Query request, CancellationToken cancellationToken)
        {
            var currentUserCanSeeAllBeneficiaryInfo = await beneficiaryService.CurrentUserCanSeeAllBeneficiaryInfo();
            var globalPermissions = await permissionService.GetGlobalPermissions(ctx.CurrentUser);
            var query = db.TransactionLogs
                .Include(x => x.TransactionLogProductGroups)
                .Where(x => x.Discriminator != TransactionLogDiscriminator.ExpireFundTransactionLog || (x.TotalAmount > 0));

            var canManageOrganizations = globalPermissions.Contains(GlobalPermission.ManageOrganizations);
            string organizationManagerClaimValue = null;

            if (!canManageOrganizations)
            {
                var user = await db.Users.Where(c => c.Id == ctx.CurrentUserId).FirstAsync(cancellationToken: cancellationToken);
                var existingClaims = await userManager.GetClaimsAsync(user);
                organizationManagerClaimValue = existingClaims.Where(x => x.Type == AppClaimTypes.OrganizationManagerOf).Select(x => x.Value).FirstOrDefault();
            }

            query = query
                .FilterByOrganizationScope(canManageOrganizations, organizationManagerClaimValue, request.Organizations)
                .FilterByCriteria(request, currentUserCanSeeAllBeneficiaryInfo);

            var sorted = Sort(query, TransactionLogSort.Default, SortOrder.Desc);
            return await TransactionLogsPagination.For(sorted, request.Page);
        }

        public class Query : IRequest<TransactionLogsPagination<TransactionLog>>, ITransactionLogFilterCriteria
        {
            public Page Page { get; set; }
            public Id ProjectId { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public IEnumerable<Id> Organizations { get; set; }
            public IEnumerable<Id> Subscriptions { get; set; }
            public IEnumerable<Id> Markets { get; set; }
            public IEnumerable<Id> MarketGroups { get; set; }
            public Maybe<bool> WithoutSubscription { get; set; }
            public IEnumerable<Id> Categories { get; set; }
            public IEnumerable<string> TransactionTypes { get; set; }
            public IEnumerable<string> GiftCardTransactionTypes { get; set; }
            public Maybe<string> SearchText { get; set; }
            public string TimeZoneId { get; set; }
        }

        private static IOrderedQueryable<TransactionLog> Sort(IQueryable<TransactionLog> query, TransactionLogSort sort, SortOrder order)
        {
            switch (sort)
            {
                case TransactionLogSort.Default:
                    return query.SortBy(x => x.CreatedAtUtc, order);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public enum TransactionLogSort
    {
        Default
    }
}
