using GraphQL.Conventions;
using GraphQL.DataLoader;
using MediatR;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;
using Sig.App.Backend.Requests.Queries.Beneficiaries;
using Sig.App.Backend.Requests.Queries.Markets;
using Sig.App.Backend.Requests.Queries.Organizations;
using Sig.App.Backend.Utilities;
using Sig.App.Backend.Utilities.Sorting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class OrganizationGraphType
    {
        private readonly Organization organization;

        public Id Id => organization.GetIdentifier();
        public NonNull<string> Name => organization.Name;

        public OrganizationGraphType(Organization organization)
        {
            this.organization = organization;
        }

        public IDataLoaderResult<ProjectGraphType> Project(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadProject(organization.ProjectId);
        }

        public async Task<Pagination<IBeneficiaryGraphType>> Beneficiaries([Inject] IMediator mediator, int page, int limit,
            [Description("If specified, only beneficiaries without or with a subscription are returned.")] bool? withoutSubscription = null,
            [Description("If specified, only beneficiaries with one of those subscription are returned.")] Id[] subscriptions = null,
            [Description("If specified, only beneficiaries with one of those category are returned")] Id[] categories = null,
            [Description("If specified, only beneficiaries active/inactive are returned")] BeneficiaryStatus[] status = null,
            [Description("If specified, only beneficiaries with or without card is returned.")] bool? withCard = null,
            [Description("If specified, only that match text is returned.")] string? searchText = "",
            Sort<BeneficiarySort> sort = null)
        {
            var results = await mediator.Send(new SearchBeneficiaries.Query
            {
                OrganizationId = organization.Id,
                Page = new Page(page, limit),
                WithoutSubscription = withoutSubscription,
                Subscriptions = subscriptions?.Select(y => y.LongIdentifierForType<Subscription>()),
                Categories = categories?.Select(y => y.LongIdentifierForType<BeneficiaryType>()),
                Status = status,
                WithCard = withCard,
                SearchText = searchText,
                Sort = sort
            });

            var test = results.Map(x =>
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

        public IDataLoaderResult<BeneficiaryStatsGraphType> BeneficiaryStats(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadBeneficiaryStatsByOrganizationId(Id.LongIdentifierForType<Organization>());
        }

        [Description("The list of organization managers for this project.")]
        public async Task<IEnumerable<UserGraphType>> Managers([Inject] IMediator mediator)
        {
            var marketManagers = await mediator.Send(new GetOrganizationManagers.Query
            {
                OrganizationId = Id.LongIdentifierForType<Organization>()
            });

            return marketManagers.Select(x => new UserGraphType(x));
        }

        public IDataLoaderResult<IEnumerable<BudgetAllowanceGraphType>> BudgetAllowances(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadOrganizationBudgetAllowance(Id.LongIdentifierForType<Organization>());
        }

        public IDataLoaderResult<decimal> BudgetAllowancesTotal(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadOrganizationBudgetAllowanceTotal(Id.LongIdentifierForType<Organization>());
        }
    }
}
