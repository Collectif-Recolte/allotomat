using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GraphQL.Conventions;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Services.Permission;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class PermissionsGraphType
    {
        private readonly ClaimsPrincipal claimsPrincipal;

        public PermissionsGraphType(ClaimsPrincipal claimsPrincipal)
        {
            this.claimsPrincipal = claimsPrincipal;
        }

        public async Task<IEnumerable<string>> Global(IResolutionContext context)
        {
            var permissionService = context.DependencyInjector.Resolve<PermissionService>();
            var permissions = await permissionService.GetGlobalPermissions(claimsPrincipal);
            return permissions.Select(x => x.ToString());
        }

        public async Task<IEnumerable<string>> Project(IResolutionContext context, Id projectId)
        {
            var permissionService = context.DependencyInjector.Resolve<PermissionService>();
            var permissions = await permissionService.GetProjectPermissions(claimsPrincipal, projectId.IdentifierForType<Project>());
            return permissions.Select(x => x.ToString());
        }

        public async Task<IEnumerable<string>> Market(IResolutionContext context, Id marketId)
        {
            var permissionService = context.DependencyInjector.Resolve<PermissionService>();
            var permissions = await permissionService.GetMarketPermissions(claimsPrincipal, marketId.IdentifierForType<Market>());
            return permissions.Select(x => x.ToString());
        }

        public async Task<IEnumerable<string>> Organization(IResolutionContext context, Id organizationId)
        {
            var permissionService = context.DependencyInjector.Resolve<PermissionService>();
            var permissions = await permissionService.GetOrganizationPermissions(claimsPrincipal, organizationId.IdentifierForType<Organization>());
            return permissions.Select(x => x.ToString());
        }

        public async Task<IEnumerable<string>> Beneficiary(IResolutionContext context, Id beneficiaryId)
        {
            var permissionService = context.DependencyInjector.Resolve<PermissionService>();
            var permissions = await permissionService.GetBeneficiaryPermissions(claimsPrincipal, beneficiaryId.IdentifierForType<Beneficiary>());
            return permissions.Select(x => x.ToString());
        }

        public async Task<IEnumerable<string>> BeneficiaryType(IResolutionContext context, Id beneficiaryTypeId)
        {
            var permissionService = context.DependencyInjector.Resolve<PermissionService>();
            var permissions = await permissionService.GetBeneficiaryTypePermissions(claimsPrincipal, beneficiaryTypeId.LongIdentifierForType<BeneficiaryType>());
            return permissions.Select(x => x.ToString());
        }
    }
}