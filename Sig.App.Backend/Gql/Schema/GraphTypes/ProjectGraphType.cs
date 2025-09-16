using GraphQL.Conventions;
using GraphQL.DataLoader;
using MediatR;
using Sig.App.Backend.Authorization;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Interfaces;
using Sig.App.Backend.Requests.Queries.Beneficiaries;
using Sig.App.Backend.Requests.Queries.Cards;
using Sig.App.Backend.Requests.Queries.Markets;
using Sig.App.Backend.Requests.Queries.Organizations;
using Sig.App.Backend.Requests.Queries.Projects;
using Sig.App.Backend.Services.Permission.Enums;
using Sig.App.Backend.Utilities;
using Sig.App.Backend.Utilities.Sorting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Sig.App.Backend.Requests.Queries.Cards.SearchCards;
using static Sig.App.Backend.Requests.Queries.Markets.SearchMarkets;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class ProjectGraphType
    {
        private readonly Project project;

        public Id Id => project.GetIdentifier();
        public NonNull<string> Name => project.Name;
        public string Url => project.Url;
        public string CardImageFileId => project.CardImageFileId;
        public bool AllowOrganizationsAssignCards => project.AllowOrganizationsAssignCards;
        public bool BeneficiariesAreAnonymous => project.BeneficiariesAreAnonymous;
        public bool AdministrationSubscriptionsOffPlatform => project.AdministrationSubscriptionsOffPlatform;
        public ReconciliationReportDate ReconciliationReportDate => project.ReconciliationReportDate;

        public ProjectGraphType(Project project)
        {
            this.project = project;
        }

        public IDataLoaderResult<IEnumerable<MarketGraphType>> Markets(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadProjectMarkets(Id.LongIdentifierForType<Project>());
        }

        public IDataLoaderResult<IEnumerable<MarketGroupGraphType>> MarketGroups(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadProjectMarketGroups(Id.LongIdentifierForType<Project>());
        }

        public async Task<Pagination<MarketGraphType>> MarketsSearch([Inject] IMediator mediator, int page, int limit, Id[] marketGroups,
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            [Description("If specified, only that match text is returned.")] string? searchText = "",
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            Sort<MarketSort> sort = null
            )
        {
            var results = await mediator.Send(new SearchMarkets.Query
            {
                ProjectId = project.Id,
                Page = new Page(page, limit),
                SearchText = searchText,
                Sort = sort,
                MarketGroups = marketGroups
            });

            return results.Map(x =>
            {
                return new MarketGraphType(x);
            });
        }

        public IDataLoaderResult<IEnumerable<OrganizationGraphType>> Organizations(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadProjectOrganizations(Id.LongIdentifierForType<Project>());
        }

        public IDataLoaderResult<IEnumerable<SubscriptionGraphType>> Subscriptions(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadProjectSubscriptions(Id.LongIdentifierForType<Project>());
        }

        [Description("The list of project managers for this project.")]
        public async Task<IEnumerable<UserGraphType>> Managers([Inject] IMediator mediator)
        {
            var projectManagers = await mediator.Send(new GetProjectProjectManagers.Query
            {
                ProjectId = Id.LongIdentifierForType<Project>()
            });

            return projectManagers.Select(x => new UserGraphType(x));
        }

        public IDataLoaderResult<CardStatsGraphType> CardStats(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadCardStatsByProjectId(Id.LongIdentifierForType<Project>());
        }

        public IDataLoaderResult<IEnumerable<BeneficiaryTypeGraphType>> BeneficiaryTypes(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadProjectBeneficiaryTypes(Id.LongIdentifierForType<Project>());
        }

        public IDataLoaderResult<IEnumerable<ProductGroupGraphType>> ProductGroups(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadProjectProductGroups(Id.LongIdentifierForType<Project>());
        }

        [RequirePermission(GlobalPermission.ManageSpecificProject)]
        public async Task<ProjectStatsGraphType> ProjectStats([Inject] IMediator mediator)
        {
            var result = await mediator.Send(new GetProjectsStats.Input()
            {
                ProjectId = Id
            });

            return new ProjectStatsGraphType()
            {
                BeneficiaryCount = result.BeneficiaryCount,
                TotalActiveSubscriptionsEnvelopes = result.TotalActiveSubscriptionsEnvelopes,
                UnspentLoyaltyFund = result.UnspentLoyaltyFund
            };
        }

        public async Task<IEnumerable<OrganizationStatsGraphType>> OrganizationsStats([Inject] IMediator mediator,
            [Description("If specified, only organization with one of those subscription are returned.")] Id[] subscriptions = null)
        {
            var result = await mediator.Send(new GetOrganizationsStats.Input()
            {
                ProjectId = Id,
                Subscriptions = subscriptions?.Select(y => y.LongIdentifierForType<Subscription>())
            });

            return result.Items.Select(x => new OrganizationStatsGraphType()
            {
                Organization = new OrganizationGraphType(x.Organization),
                TotalActiveSubscriptionsEnvelopes = x.TotalActiveSubscriptionsEnvelopes,
                TotalAllocatedOnCards = x.TotalAllocatedOnCards,
                RemainingPerEnvelope = x.RemainingPerEnvelope,
                BalanceOnCards = x.BalanceOnCards,
                CardSpendingAmounts = x.CardSpendingAmounts,
                ExpiredAmounts = x.ExpiredAmounts
            });
        }

        public async Task<Pagination<CardGraphType>> Cards([Inject] IMediator mediator, int page, int limit,
            [Description("If specified, only card with specific status are returned")] CardStatus[] status = null,
            [Description("If specified, only card enabled or disabled is returned.")] bool? withCardDisabled = null,
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            [Description("If specified, only that match text is returned.")] string? searchText = "",
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            Sort<CardSort> sort = null)
        {
            var results = await mediator.Send(new SearchCards.Query
            {
                ProjectId = project.Id,
                Page = new Page(page, limit),
                Status = status,
                SearchText = searchText,
                WithCardDisabled = withCardDisabled,
                Sort = sort
            });

            return results.Map(x =>
            {
                return new CardGraphType(x);
            });
        }

        public async Task<PaymentConflictPagination<IBeneficiaryGraphType>> Beneficiaries([Inject] IMediator mediator, int page, int limit,
            [Description("If specified, only beneficiaries without or with a subscription are returned.")] bool? withoutSubscription = null,
            [Description("If specified, only beneficiaries with one of those subscription are returned.")] Id[] subscriptions = null,
            [Description("If specified, only beneficiaries without one of those subscription are returned.")] Id[] withoutSpecificSubscriptions = null,
            [Description("If specified, only beneficiaries with one of those category are returned")] Id[] categories = null,
            [Description("If specified, only beneficiaries without one of those category are returned")] Id[] withoutSpecificCategories = null,
            [Description("If specified, only beneficiaries active/inactive are returned")] BeneficiaryStatus[] status = null,
            [Description("If specified, only beneficiaries with or without card is returned.")] bool? withCard = null,
            [Description("If specified, only beneficiaries with or without payment conflict is returned.")] bool? withConflictPayment = null,
            [Description("If specified, only card enabled or disabled is returned.")] bool? withCardDisabled = null,
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            [Description("If specified, only that match text is returned.")] string? searchText = "",
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            Sort<BeneficiarySort> sort = null)
        {
            var results = await mediator.Send(new SearchBeneficiaries.Query
            {
                ProjectId = project.Id,
                Page = new Page(page, limit),
                WithoutSubscription = withoutSubscription,
                Subscriptions = subscriptions?.Select(y => y.LongIdentifierForType<Subscription>()),
                WithoutSpecificSubscriptions = withoutSpecificSubscriptions?.Select(y => y.LongIdentifierForType<Subscription>()),
                Categories = categories?.Select(y => y.LongIdentifierForType<BeneficiaryType>()),
                WithoutSpecificCategories = withoutSpecificCategories?.Select(y => y.LongIdentifierForType<BeneficiaryType>()),
                Status = status,
                WithCard = withCard,
                WithConflictPayment = withConflictPayment,
                SearchText = searchText,
                Sort = sort,
                WithCardDisabled = withCardDisabled
            });

            return results.Map(x =>
            {
                switch (x)
                {
                    case null:
                        return null as IBeneficiaryGraphType;
                    case OffPlatformBeneficiary opb:
                        return new OffPlatformBeneficiaryGraphType(opb);
                    default:
                        return new BeneficiaryGraphType(x);
                }
            });
        }

        public async Task<MarketAmountOwedPagination<MarketAmountOwedGraphType>> MarketsAmountOwed([Inject] IMediator mediator, int page, int limit, DateTime startDate, DateTime endDate)
        {
            var results = await mediator.Send(new SearchProjectMarketAmountOweds.Query
            {
                ProjectId = project.Id,
                Page = new Page(page, limit),
                StartDate = startDate,
                EndDate = endDate
            });

            return results;
        }

        public async Task<SubscriptionEndReportTotalGraphType> SubscriptionEndReportTotal([Inject] IMediator mediator, DateTime startDate, DateTime endDate,
            [Description("If specified, only transactions with one of those subscription are returned.")] Id[] withSpecificSubscriptions = null,
            [Description("If specified, only transactions with one of those organization are returned.")] Id[] withSpecificOrganizations = null)
        {
            var result = await mediator.Send(new SearchProjectSubscriptionEndReportTotal.Query
            {
                ProjectId = project.Id,
                StartDate = startDate,
                EndDate = endDate,
                Subscriptions = withSpecificSubscriptions?.Select(y => y.LongIdentifierForType<Subscription>()),
                Organizations = withSpecificOrganizations?.Select(y => y.LongIdentifierForType<Organization>())
            });

            return result;
        }

        public async Task<Pagination<SubscriptionEndReportGraphType>> SubscriptionEndReport([Inject] IMediator mediator, int page, int limit, DateTime startDate, DateTime endDate,
            [Description("If specified, only transactions with one of those subscription are returned.")] Id[] withSpecificSubscriptions = null,
            [Description("If specified, only transactions with one of those organization are returned.")] Id[] withSpecificOrganizations = null)
        {
            var results = await mediator.Send(new SearchProjectSubscriptionEndReport.Query
            {
                ProjectId = project.Id,
                Page = new Page(page, limit),
                StartDate = startDate,
                EndDate = endDate,
                Subscriptions = withSpecificSubscriptions?.Select(y => y.LongIdentifierForType<Subscription>()),
                Organizations = withSpecificOrganizations?.Select(y => y.LongIdentifierForType<Organization>())
            });

            return results;
        }
    }
}
