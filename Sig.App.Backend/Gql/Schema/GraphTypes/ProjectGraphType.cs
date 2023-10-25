using GraphQL.Conventions;
using GraphQL.DataLoader;
using MediatR;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;
using Sig.App.Backend.Requests.Queries.Projects;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public ProjectGraphType(Project project)
        {
            this.project = project;
        }

        public IDataLoaderResult<IEnumerable<MarketGraphType>> Markets(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadProjectMarkets(Id.LongIdentifierForType<Project>());
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
    }
}
