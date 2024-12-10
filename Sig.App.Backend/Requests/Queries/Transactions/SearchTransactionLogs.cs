using System;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Utilities;
using Sig.App.Backend.Utilities.Sorting;
using GraphQL.Conventions;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.DbModel.Entities.Projects;
using System.Collections.Generic;
using Sig.App.Backend.Gql.Schema.Types;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.Services.Beneficiaries;
using Sig.App.Backend.Services.Permission.Enums;
using Sig.App.Backend.Services.Permission;
using Sig.App.Backend.Gql.Interfaces;
using Microsoft.AspNetCore.Identity;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.DbModel.Entities.CashRegisters;

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
            var longProjectId = request.ProjectId.LongIdentifierForType<Project>();

            var startDate = request.StartDate.ToUniversalTime();
            var endDate = request.EndDate.ToUniversalTime();

            IQueryable<TransactionLog> query = db.TransactionLogs.Include(x => x.TransactionLogProductGroups).Where(x =>
                x.CreatedAtUtc > startDate && x.CreatedAtUtc < endDate && x.ProjectId == longProjectId)
                .Where(x => x.Discriminator != TransactionLogDiscriminator.ExpireFundTransactionLog || (x.TotalAmount > 0));

            if(!globalPermissions.Contains(GlobalPermission.ManageOrganizations))
            {
                var user = await db.Users.Where(c => c.Id == ctx.CurrentUserId).FirstAsync(cancellationToken: cancellationToken);

                var existingClaims = await userManager.GetClaimsAsync(user);
                var existingOrganizationsClaims = existingClaims.Where(x => x.Type == AppClaimTypes.OrganizationManagerOf).Select(x => x.Value).FirstOrDefault();
                query = query.Where(x => x.OrganizationId.ToString() == existingOrganizationsClaims);
            }
            else if (request.Organizations?.Any() ?? false)
            {
                var organizationsLongIdentifiers = request.Organizations.Select(x => x.LongIdentifierForType<Organization>());
                query = query.Where(x => organizationsLongIdentifiers.Contains(x.OrganizationId ?? 0));
            }

            if (request.Subscriptions?.Any() ?? false)
            {
                var withoutSubscription = request.WithoutSubscription?.Value ?? false;
                var subscriptionLongIdentifiers = request.Subscriptions.Select(x => x.LongIdentifierForType<Subscription>());
                query = query.Where(x => (withoutSubscription && !x.SubscriptionId.HasValue) || subscriptionLongIdentifiers.Contains(x.SubscriptionId.GetValueOrDefault()));
            }
            else if (request.WithoutSubscription.IsSet() && request.WithoutSubscription.Value)
            {
                query = query.Where(x => !x.SubscriptionId.HasValue);
            }

            if (request.Categories?.Any() ?? false)
            {
                var categoriesLongIdentifiers = request.Categories.Select(x => x.LongIdentifierForType<BeneficiaryType>());
                query = query.Where(x => categoriesLongIdentifiers.Contains(x.BeneficiaryTypeId.GetValueOrDefault()));
            }

            if (request.Markets?.Any() ?? false)
            {
                var marketsLongIdentifiers = request.Markets.Select(x => x.LongIdentifierForType<Market>());
                query = query.Where(x => marketsLongIdentifiers.Contains(x.MarketId.GetValueOrDefault()));
            }

            if (request.CashRegisters?.Any() ?? false)
            {
                var cashRegistersLongIdentifiers = request.CashRegisters.Select(x => x.LongIdentifierForType<CashRegister>());
                query = query.Where(x => cashRegistersLongIdentifiers.Contains(x.CashRegisterId.GetValueOrDefault()));
            }

            if (request.TransactionTypes?.Any() ?? false)
            {
                var transactionLogDiscriminators =
                    request.TransactionTypes.Select(x => Enum.Parse(typeof(TransactionLogDiscriminator), x));
                query = query.Where(x => transactionLogDiscriminators.Contains(x.Discriminator));
            }

            if (request.SearchText.IsSet() && !string.IsNullOrEmpty(request.SearchText.Value))
            {
                var searchText = request.SearchText.Value.Split(' ').AsEnumerable();
                foreach (var text in searchText)
                {
                    if (currentUserCanSeeAllBeneficiaryInfo)
                    {
                        query = query.Where(x =>
                            x.BeneficiaryID1.Contains(text) || x.BeneficiaryID1.Contains(text) ||
                            x.BeneficiaryEmail.Contains(text) || x.BeneficiaryFirstname.Contains(text) ||
                            x.BeneficiaryLastname.Contains(text));
                    }
                    else
                    {
                        query = query.Where(x => x.BeneficiaryID1.Contains(text) || x.BeneficiaryID2.Contains(text));
                    }
                }
            }

            var sorted = Sort(query, TransactionLogSort.Default, SortOrder.Desc);
            return await TransactionLogsPagination.For(sorted, request.Page);
        }

        public class Query : IRequest<TransactionLogsPagination<TransactionLog>>
        {
            public Page Page { get; set; }
            public Id ProjectId { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public IEnumerable<Id> Organizations { get; set; }
            public IEnumerable<Id> Subscriptions { get; set; }
            public IEnumerable<Id> Markets { get; set; }
            public IEnumerable<Id> CashRegisters { get; set; }
            public Maybe<bool> WithoutSubscription { get; set; }
            public IEnumerable<Id> Categories { get; set; }
            public IEnumerable<string> TransactionTypes { get; set; }
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
