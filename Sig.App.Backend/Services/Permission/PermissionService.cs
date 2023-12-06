using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Services.Permission.Enums;

namespace Sig.App.Backend.Services.Permission
{
    public class PermissionService
    {
        private readonly AppDbContext db;

        private static readonly GlobalPermission[] AdminGlobalPermissions = new[]
        {
            GlobalPermission.ManageAllUsers,
            GlobalPermission.ManageProjects,
            GlobalPermission.ManageSpecificProject,
            GlobalPermission.ManageMarkets
        };

        private static GlobalPermission[] ProjectManagerGlobalPermissions = new[]
        {
            GlobalPermission.ManageOrganizations,
            GlobalPermission.ManageSpecificOrganization,
            GlobalPermission.ManageSubscriptions,
            GlobalPermission.ManageProjectManagers,
            GlobalPermission.ManageSpecificProject,
            GlobalPermission.ManageBeneficiaries,
            GlobalPermission.ManageCards,
            GlobalPermission.ManageTransactions,
            GlobalPermission.ManageCategories,
            GlobalPermission.ManageBudgetAllowance,
            GlobalPermission.ManageProductGroup
        };

        private static GlobalPermission[] ProjectManagerSubscriptionsOffPlatformGlobalPermissions = new[]
        {
            GlobalPermission.ManageOrganizations,
            GlobalPermission.ManageSpecificOrganization,
            GlobalPermission.ManageProjectManagers,
            GlobalPermission.ManageSpecificProject,
            GlobalPermission.ManageBeneficiaries,
            GlobalPermission.ManageCards,
            GlobalPermission.ManageTransactions,
            GlobalPermission.ManageBudgetAllowance,
            GlobalPermission.ManageProductGroup
        };

        private static GlobalPermission[] OrganizationManagerGlobalPermissions = new[]
        {
            GlobalPermission.ManageSpecificOrganization,
            GlobalPermission.ManageBeneficiaries,
            GlobalPermission.ManageOrganizationManagers,
            GlobalPermission.ManageTransactions,
        };

        private static GlobalPermission[] MarketManagerGlobalPermissions = new[]
        {
            GlobalPermission.CreateTransaction,
            GlobalPermission.ManageSpecificMarket
        };

        private static readonly ProjectPermission[] AdminProjectPermission = new[]
        {
            ProjectPermission.CreateProject,
            ProjectPermission.ManageProject,
            ProjectPermission.DeleteProject,
            ProjectPermission.ManageAllProjects
        };

        private static readonly MarketPermission[] AdminMarketPermissions = new[]
        {
            MarketPermission.CreateMarket,
            MarketPermission.ManageMarket,
            MarketPermission.DeleteMarket,
            MarketPermission.ArchiveMarket,
            MarketPermission.ManageAllMarkets
        };

        private static readonly ProjectPermission[] ProjectManagerProjectPermission = new[]
        {
            ProjectPermission.ManageProject,
            ProjectPermission.CreateOrganization,
            ProjectPermission.CreateCard,
            ProjectPermission.AddLoyaltyFundToCard
        };

        private static readonly BeneficiaryTypePermission[] ProjectManagerBeneficiaryTypePermission = new[]
        {
            BeneficiaryTypePermission.EditBeneficiaryType,
            BeneficiaryTypePermission.DeleteBeneficiaryType
        };

        private static readonly MarketPermission[] MarketManagerMarketPermission = new[]
        {
            MarketPermission.ManageMarket,
            MarketPermission.CreateTransaction,
            MarketPermission.RefundTransaction
        };

        private static readonly OrganizationPermission[] ProjectManagerOrganizationPermission = new[]
        {
            OrganizationPermission.DeleteOrganization,
            OrganizationPermission.ManageOrganization
        };

        private static readonly OrganizationPermission[] OrganizationManagerOrganizationPermission = new[]
        {
            OrganizationPermission.ManageOrganization
        };

        private static readonly BeneficiaryPermission[] ProjectManagerBeneficiaryPermissions = new[]
        {
            BeneficiaryPermission.ManageBeneficiary,
            BeneficiaryPermission.DeleteBeneficiary,
            BeneficiaryPermission.AssignCard,
            BeneficiaryPermission.ManuallyAddingFund
        };

        private static readonly BeneficiaryPermission[] OrganizationManagerBeneficiaryPermissions = new[]
        {
            BeneficiaryPermission.ManageBeneficiary,
            BeneficiaryPermission.DeleteBeneficiary,
            BeneficiaryPermission.ManuallyAddingFund
        };

        private static readonly BeneficiaryPermission[] OrganizationManagerBeneficiaryPermissionsWithAssignCard = new[]
        {
            BeneficiaryPermission.ManageBeneficiary,
            BeneficiaryPermission.DeleteBeneficiary,
            BeneficiaryPermission.ManuallyAddingFund,
            BeneficiaryPermission.AssignCard
        };

        private static readonly SubscriptionPermission[] ProjectManagerSubscriptionPermission = new []
        {
            SubscriptionPermission.EditSubscription,
            SubscriptionPermission.DeleteSubscription
        };

        public Task<GlobalPermission[]> GetGlobalPermissions(ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserType.PCAAdmin.ToString()))
            {
                return Task.FromResult(AdminGlobalPermissions);
            }

            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserType.ProjectManager.ToString()))
            {
                var claim = claimsPrincipal.Claims.FirstOrDefault(x => x.Type == AppClaimTypes.ProjectManagerOf);
                var projectId = (long)Convert.ToDouble(claim.Value);
                var project = db.Projects.FirstOrDefault(x => x.Id == projectId);
                if (project.AdministrationSubscriptionsOffPlatform)
                {
                    return Task.FromResult(ProjectManagerSubscriptionsOffPlatformGlobalPermissions);
                }

                return Task.FromResult(ProjectManagerGlobalPermissions);
            }

            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserType.OrganizationManager.ToString()))
            {
                var claim = claimsPrincipal.Claims.FirstOrDefault(x => x.Type == AppClaimTypes.OrganizationManagerOf);
                if (claim != null)
                {
                    var organizationId = (long)Convert.ToDouble(claim.Value);
                    var project = db.Projects.FirstOrDefault(x => x.Organizations.Any(y => y.Id == organizationId));

                    if (project != null && project.AllowOrganizationsAssignCards)
                    {
                        var result = new List<GlobalPermission>() { GlobalPermission.ManageCards };
                        result.AddRange(OrganizationManagerGlobalPermissions);
                        return Task.FromResult(result.ToArray());
                    }
                }

                return Task.FromResult(OrganizationManagerGlobalPermissions);
            }

            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserType.Merchant.ToString()))
            {
                return Task.FromResult(MarketManagerGlobalPermissions);
            }

            return Task.FromResult(Array.Empty<GlobalPermission>());
        }

        public PermissionService(AppDbContext db)
        {
            this.db = db;
        }

        public Task<BeneficiaryTypePermission[]> GetBeneficiaryTypePermissions(ClaimsPrincipal claimsPrincipal, long beneficiaryTypeId)
        {
            var beneficiaryType = db.BeneficiaryTypes.Include(x => x.Project).First(x => x.Id == beneficiaryTypeId);

            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserType.ProjectManager.ToString()) && claimsPrincipal.HasClaim(AppClaimTypes.ProjectManagerOf, beneficiaryType.Project.GetIdentifier().IdentifierForType<Project>()))
            {
                return Task.FromResult(ProjectManagerBeneficiaryTypePermission);
            }

            return Task.FromResult(Array.Empty<BeneficiaryTypePermission>());
        }

        public Task<ProjectPermission[]> GetProjectPermissions(ClaimsPrincipal claimsPrincipal, string projectId)
        {
            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserType.PCAAdmin.ToString()))
            {
                return Task.FromResult(AdminProjectPermission);
            }

            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserType.ProjectManager.ToString()) && claimsPrincipal.HasClaim(AppClaimTypes.ProjectManagerOf, projectId))
            {
                return Task.FromResult(ProjectManagerProjectPermission);
            }

            return Task.FromResult(Array.Empty<ProjectPermission>());
        }

        public Task<MarketPermission[]> GetMarketPermissions(ClaimsPrincipal claimsPrincipal, string marketId)
        {
            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserType.PCAAdmin.ToString()))
            {
                return Task.FromResult(AdminMarketPermissions);
            }

            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserType.Merchant.ToString()) && claimsPrincipal.HasClaim(AppClaimTypes.MarketManagerOf, marketId))
            {
                return Task.FromResult(MarketManagerMarketPermission);
            }

            return Task.FromResult(Array.Empty<MarketPermission>());
        }

        public Task<OrganizationPermission[]> GetOrganizationPermissions(ClaimsPrincipal claimsPrincipal, string organizationId)
        {
            var organizationLongId = Id.New<Organization>(organizationId).LongIdentifierForType<Organization>();
            var organization = db.Organizations.FirstOrDefault(x => x.Id == organizationLongId);
            var projectId = organization.ProjectId.ToString();

            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserType.ProjectManager.ToString()) && claimsPrincipal.HasClaim(AppClaimTypes.ProjectManagerOf, projectId))
            {
                return Task.FromResult(ProjectManagerOrganizationPermission);
            }

            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserType.OrganizationManager.ToString()) && claimsPrincipal.HasClaim(AppClaimTypes.OrganizationManagerOf, organizationId))
            {
                return Task.FromResult(OrganizationManagerOrganizationPermission);
            }

            return Task.FromResult(Array.Empty<OrganizationPermission>());
        }

        public Task<BeneficiaryPermission[]> GetBeneficiaryPermissions(ClaimsPrincipal claimsPrincipal, string beneficiaryId)
        {
            long beneficiaryLongId;
            try {
                beneficiaryLongId = Id.New<Beneficiary>(beneficiaryId).LongIdentifierForType<Beneficiary>();
            }
            catch (Exception error)
            {
                beneficiaryLongId = Id.New<OffPlatformBeneficiary>(beneficiaryId).LongIdentifierForType<OffPlatformBeneficiary>();
            }
            var beneficiary = db.Beneficiaries.FirstOrDefault(x => x.Id == beneficiaryLongId);
            var organizationId = beneficiary.OrganizationId.ToString();
            var projectId = db.Organizations.Where(x => x.Id == beneficiary.OrganizationId).FirstOrDefault().ProjectId;
            var project = db.Projects.FirstOrDefault(x => x.Id == projectId);

            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserType.ProjectManager.ToString()) && claimsPrincipal.HasClaim(AppClaimTypes.ProjectManagerOf, projectId.ToString()) && (!project?.BeneficiariesAreAnonymous ?? true))
            {
                return Task.FromResult(ProjectManagerBeneficiaryPermissions);
            }

            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserType.OrganizationManager.ToString()) && claimsPrincipal.HasClaim(AppClaimTypes.OrganizationManagerOf, organizationId))
            {
                if (db.Projects.First(x => x.Organizations.Any(y => y.Id == (long)Convert.ToDouble(organizationId))).AllowOrganizationsAssignCards)
                {
                    return Task.FromResult(OrganizationManagerBeneficiaryPermissionsWithAssignCard);
                }

                return Task.FromResult(OrganizationManagerBeneficiaryPermissions);
            }

            return Task.FromResult(Array.Empty<BeneficiaryPermission>());
        }

        public Task<SubscriptionPermission[]> GetSubscriptionPermissions(ClaimsPrincipal claimsPrincipal, string subscriptionId)
        {
            var subscriptionLongId = Id.New<Subscription>(subscriptionId).LongIdentifierForType<Subscription>();
            var projectId = db.Subscriptions.Where(x => x.Id == subscriptionLongId).FirstOrDefault().ProjectId.ToString();

            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserType.ProjectManager.ToString()) && claimsPrincipal.HasClaim(AppClaimTypes.ProjectManagerOf, projectId))
            {
                return Task.FromResult(ProjectManagerSubscriptionPermission);
            }

            return Task.FromResult(Array.Empty<SubscriptionPermission>());
        }
    }
}