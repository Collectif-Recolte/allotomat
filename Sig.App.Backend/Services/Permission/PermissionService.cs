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
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.DbModel.Entities.Markets;
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
            GlobalPermission.ManageProductGroup,
            GlobalPermission.CreateTransaction,
            GlobalPermission.RefundTransaction,
            GlobalPermission.ManageMarketGroups,
            GlobalPermission.ManageMarkets
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
            GlobalPermission.ManageProductGroup,
            GlobalPermission.CreateTransaction,
            GlobalPermission.RefundTransaction,
            GlobalPermission.ManageMarkets
        };

        private static GlobalPermission[] OrganizationManagerGlobalPermissions = new[]
        {
            GlobalPermission.ManageSpecificOrganization,
            GlobalPermission.ManageBeneficiaries,
            GlobalPermission.ManageOrganizationManagers,
            GlobalPermission.ManageTransactions,
            GlobalPermission.RefundTransaction
        };

        private static GlobalPermission[] MarketManagerGlobalPermissions = new[]
        {
            GlobalPermission.CreateTransaction,
            GlobalPermission.ManageSpecificMarket,
            GlobalPermission.RefundTransaction
        };

        private static GlobalPermission[] MarketGroupManagerGlobalPermissions = new[]
        {
            GlobalPermission.CreateTransaction,
            GlobalPermission.ManageBeneficiaries,
            GlobalPermission.ManageTransactions,
            GlobalPermission.RefundTransaction,
            GlobalPermission.ManageSpecificMarketGroup,
            GlobalPermission.ManageMarketGroupManagers,
            GlobalPermission.ManageMarkets
        };

        private static readonly ProjectPermission[] AdminProjectPermission = new[]
        {
            ProjectPermission.CreateProject,
            ProjectPermission.ManageProject,
            ProjectPermission.DeleteProject,
            ProjectPermission.ManageAllProjects
        };

        private static readonly ProjectPermission[] ProjectManagerProjectPermission = new[]
        {
            ProjectPermission.ManageProject,
            ProjectPermission.CreateOrganization,
            ProjectPermission.CreateCard,
            ProjectPermission.AddLoyaltyFundToCard,
            ProjectPermission.EditLoyaltyFundOnCard
        };

        private static readonly BeneficiaryTypePermission[] ProjectManagerBeneficiaryTypePermission = new[]
        {
            BeneficiaryTypePermission.EditBeneficiaryType,
            BeneficiaryTypePermission.DeleteBeneficiaryType
        };

        private static readonly MarketPermission[] AdminMarketPermissions = new[]
        {
            MarketPermission.CreateMarket,
            MarketPermission.ManageMarket,
            MarketPermission.DeleteMarket,
            MarketPermission.ArchiveMarket,
            MarketPermission.ManageAllMarkets
        };

        private static readonly MarketPermission[] MarketManagerMarketPermission = new[]
        {
            MarketPermission.ManageMarket,
            MarketPermission.CreateTransaction,
            MarketPermission.RefundTransaction,
            MarketPermission.CreateCashRegister,
            MarketPermission.ManageCashRegister,
            MarketPermission.DeleteCashRegister,
            MarketPermission.ArchiveCashRegister
        };
        
        private static readonly MarketPermission[] ProjectManagerMarketPermission = new[]
        {
            MarketPermission.CreateTransaction,
            MarketPermission.RefundTransaction,
            MarketPermission.ManageMarket,
            MarketPermission.ArchiveMarket
        };

        private static readonly MarketPermission[] ProjectManagerMarketPermissionGeneric = new[]
        {
            MarketPermission.ManageAllMarkets,
            MarketPermission.CreateMarket
        };

        private static readonly MarketPermission[] OrganizationManagerMarketPermission = new[]
        {
            MarketPermission.CreateTransaction,
            MarketPermission.RefundTransaction
        };

        private static readonly MarketPermission[] MarketGroupManagerCreateMarketPermission = new[]
        {
            MarketPermission.CreateMarket
        };

        private static readonly MarketPermission[] MarketGroupManagerMarketPermission = new[]
        {
            MarketPermission.CreateTransaction,
            MarketPermission.RefundTransaction,
            MarketPermission.ManageMarket
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

        private static readonly CardPermission[] ProjectManagerCardPermissions = new[]
        {
            CardPermission.EnableDisableCard,
            CardPermission.TransfertCard
        };

        private static readonly CardPermission[] OrganizationManagerCardPermissionsWithAssignCard = new[]
        {
            CardPermission.EnableDisableCard,
            CardPermission.TransfertCard
        };

        private static readonly SubscriptionPermission[] ProjectManagerSubscriptionPermission = new []
        {
            SubscriptionPermission.EditSubscription,
            SubscriptionPermission.DeleteSubscription,
            SubscriptionPermission.ArchiveSubscription,
            SubscriptionPermission.UnarchiveSubscription
        };

        private static readonly MarketGroupPermission[] ProjectManagerMarketGroupPermission = new[]
        {
            MarketGroupPermission.CreateMarketGroup,
            MarketGroupPermission.ManageMarketGroup,
            MarketGroupPermission.DeleteMarketGroup,
            MarketGroupPermission.ArchiveMarketGroup
        };

        private static readonly MarketGroupPermission[] MarketGroupManagerMarketGroupPermission = new[]
        {
            MarketGroupPermission.ManageMarketGroup
        };


        private const string UserTypePCAAdmin = nameof(UserType.PCAAdmin);
        private const string UserTypeProjectManager = nameof(UserType.ProjectManager);
        private const string UserTypeOrganizationManager = nameof(UserType.OrganizationManager);
        private const string UserTypeMerchant = nameof(UserType.Merchant);
        private const string UserTypeMarketGroupManager = nameof(UserType.MarketGroupManager);
        
        public async Task<GlobalPermission[]> GetGlobalPermissions(ClaimsPrincipal claimsPrincipal)
        {
            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserTypePCAAdmin))
            {
                return AdminGlobalPermissions;
            }

            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserTypeProjectManager))
            {
                var claim = claimsPrincipal.Claims.FirstOrDefault(x => x.Type == AppClaimTypes.ProjectManagerOf);
                var projectId = Convert.ToInt64(claim?.Value);
                var project = await db.Projects.FirstOrDefaultAsync(x => x.Id == projectId);
                if (project?.AdministrationSubscriptionsOffPlatform == true)
                {
                    return ProjectManagerSubscriptionsOffPlatformGlobalPermissions;
                }

                return ProjectManagerGlobalPermissions;
            }

            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserTypeOrganizationManager))
            {
                var claim = claimsPrincipal.Claims.FirstOrDefault(x => x.Type == AppClaimTypes.OrganizationManagerOf);
                if (claim != null)
                {
                    var organizationId = Convert.ToInt64(claim.Value);
                    var project = await db.Projects.FirstOrDefaultAsync(x => x.Organizations.Any(y => y.Id == organizationId));

                    if (project != null && project.AllowOrganizationsAssignCards)
                    {
                        var result = new List<GlobalPermission>() { GlobalPermission.ManageCards };
                        result.AddRange(OrganizationManagerGlobalPermissions);
                        return result.ToArray();
                    }
                }

                return OrganizationManagerGlobalPermissions;
            }

            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserTypeMerchant))
            {
                return MarketManagerGlobalPermissions;
            }

            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserTypeMarketGroupManager))
            {
                return MarketGroupManagerGlobalPermissions;
            }

            return Array.Empty<GlobalPermission>();
        }

        public PermissionService(AppDbContext db)
        {
            this.db = db;
        }

        public async Task<BeneficiaryTypePermission[]> GetBeneficiaryTypePermissions(ClaimsPrincipal claimsPrincipal, long beneficiaryTypeId)
        {
            var beneficiaryType = await db.BeneficiaryTypes.Include(x => x.Project).FirstAsync(x => x.Id == beneficiaryTypeId);

            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserTypeProjectManager) && 
                claimsPrincipal.HasClaim(AppClaimTypes.ProjectManagerOf, beneficiaryType.Project.GetIdentifier().IdentifierForType<Project>()))
            {
                return ProjectManagerBeneficiaryTypePermission;
            }

            return Array.Empty<BeneficiaryTypePermission>();
        }

        public Task<ProjectPermission[]> GetProjectPermissions(ClaimsPrincipal claimsPrincipal, string projectId)
        {
            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserTypePCAAdmin))
            {
                return Task.FromResult(AdminProjectPermission);
            }

            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserTypeProjectManager) && claimsPrincipal.HasClaim(AppClaimTypes.ProjectManagerOf, projectId))
            {
                return Task.FromResult(ProjectManagerProjectPermission);
            }

            return Task.FromResult(Array.Empty<ProjectPermission>());
        }

        public async Task<MarketPermission[]> GetMarketPermissions(ClaimsPrincipal claimsPrincipal, string marketId)
        {
            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserTypePCAAdmin))
            {
                return AdminMarketPermissions;
            }

            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserTypeMerchant) && claimsPrincipal.HasClaim(AppClaimTypes.MarketManagerOf, marketId))
            {
                return MarketManagerMarketPermission;
            }

            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserTypeProjectManager))
            {
                if (marketId != null)
                {
                    var marketLongId = Id.New<Market>(marketId).LongIdentifierForType<Market>();
                    var projectMarkets = await db.ProjectMarkets.Where(x => x.MarketId == marketLongId).ToListAsync();

                    foreach (var projectMarket in projectMarkets)
                    {
                        if (claimsPrincipal.HasClaim(AppClaimTypes.ProjectManagerOf, projectMarket.ProjectId.ToString()))
                        {
                            return ProjectManagerMarketPermission;
                        }
                    }
                }

                return ProjectManagerMarketPermissionGeneric;
            }
            
            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserTypeOrganizationManager))
            {
                var marketLongId = Id.New<Market>(marketId).LongIdentifierForType<Market>();
                var projectMarkets = await db.ProjectMarkets.Where(x => x.MarketId == marketLongId).ToListAsync();
                var organizationForProjects = await db.Organizations.Where(x => projectMarkets.Select(y => y.ProjectId).Contains(x.ProjectId)).ToListAsync();

                foreach (var organization in organizationForProjects)
                {
                    if (claimsPrincipal.HasClaim(AppClaimTypes.OrganizationManagerOf, organization.GetIdentifier().IdentifierForType<Organization>()))
                    {
                        return OrganizationManagerMarketPermission;
                    }
                }
            }

            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserTypeMarketGroupManager))
            {
                if (!string.IsNullOrEmpty(marketId))
                { 
                    var marketLongId = Id.New<Market>(marketId).LongIdentifierForType<Market>();
                    var marketGroupMarkets = await db.MarketGroupMarkets.Where(x => x.MarketId == marketLongId).ToListAsync();

                    foreach (var marketGroupMarket in marketGroupMarkets)
                    {
                        if (claimsPrincipal.HasClaim(AppClaimTypes.MarketGroupManagerOf, marketGroupMarket.MarketGroupId.ToString()))
                        {
                            return MarketGroupManagerMarketPermission;
                        }
                    }
                }

                return MarketGroupManagerCreateMarketPermission;
            }

            return Array.Empty<MarketPermission>();
        }

        public async Task<OrganizationPermission[]> GetOrganizationPermissions(ClaimsPrincipal claimsPrincipal, string organizationId)
        {
            var organizationLongId = Id.New<Organization>(organizationId).LongIdentifierForType<Organization>();
            var organization = await db.Organizations.FirstAsync(x => x.Id == organizationLongId);
            var projectId = organization.ProjectId.ToString();

            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserTypeProjectManager) && claimsPrincipal.HasClaim(AppClaimTypes.ProjectManagerOf, projectId))
            {
                return ProjectManagerOrganizationPermission;
            }

            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserTypeOrganizationManager) && claimsPrincipal.HasClaim(AppClaimTypes.OrganizationManagerOf, organizationId))
            {
                return OrganizationManagerOrganizationPermission;
            }

            return Array.Empty<OrganizationPermission>();
        }

        public async Task<BeneficiaryPermission[]> GetBeneficiaryPermissions(ClaimsPrincipal claimsPrincipal, string beneficiaryId)
        {
            long beneficiaryLongId;
            try {
                beneficiaryLongId = Id.New<Beneficiary>(beneficiaryId).LongIdentifierForType<Beneficiary>();
            }
            catch
            {
                beneficiaryLongId = Id.New<OffPlatformBeneficiary>(beneficiaryId).LongIdentifierForType<OffPlatformBeneficiary>();
            }
            var beneficiary = await db.Beneficiaries.FirstAsync(x => x.Id == beneficiaryLongId);
            var organizationId = beneficiary.OrganizationId.ToString();
            var project = await db.Organizations.Where(x => x.Id == beneficiary.OrganizationId).Select(x => x.Project).FirstAsync();

            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserTypeProjectManager) && claimsPrincipal.HasClaim(AppClaimTypes.ProjectManagerOf, project.Id.ToString()))
            {
                return ProjectManagerBeneficiaryPermissions;
            }

            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserTypeOrganizationManager) && claimsPrincipal.HasClaim(AppClaimTypes.OrganizationManagerOf, organizationId))
            {
                var p = await db.Projects.FirstAsync(x => x.Organizations.Any(y => y.Id == Convert.ToInt64(organizationId)));
                if (p.AllowOrganizationsAssignCards)
                {
                    return OrganizationManagerBeneficiaryPermissionsWithAssignCard;
                }

                return OrganizationManagerBeneficiaryPermissions;
            }

            return Array.Empty<BeneficiaryPermission>();
        }

        public async Task<CardPermission[]> GetCardPermissions(ClaimsPrincipal claimsPrincipal, string cardId)
        {
            var cardLongId = Id.New<Card>(cardId).LongIdentifierForType<Card>();

            var card = await db.Cards
                .Include(x => x.Beneficiary)
                .Include(x => x.Project)
                .FirstAsync(x => x.Id == cardLongId);

            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserTypeProjectManager) && claimsPrincipal.HasClaim(AppClaimTypes.ProjectManagerOf, card.ProjectId.ToString()))
            {
                return ProjectManagerCardPermissions;
            }

            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserTypeOrganizationManager) && claimsPrincipal.HasClaim(AppClaimTypes.OrganizationManagerOf, card.Beneficiary.OrganizationId.ToString()))
            {
                var p = await db.Projects.FirstAsync(x => x.Organizations.Any(y => y.Id == Convert.ToInt64(card.Beneficiary.OrganizationId)));
                if (p.AllowOrganizationsAssignCards)
                {
                    return OrganizationManagerCardPermissionsWithAssignCard;
                }
            }

            return Array.Empty<CardPermission>();
        }

        public async Task<SubscriptionPermission[]> GetSubscriptionPermissions(ClaimsPrincipal claimsPrincipal, string subscriptionId)
        {
            var subscriptionLongId = Id.New<Subscription>(subscriptionId).LongIdentifierForType<Subscription>();
            var projectId = await db.Subscriptions.Where(x => x.Id == subscriptionLongId).Select(x => x.ProjectId).FirstOrDefaultAsync();

            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserTypeProjectManager) && claimsPrincipal.HasClaim(AppClaimTypes.ProjectManagerOf, projectId.ToString()))
            {
                return ProjectManagerSubscriptionPermission;
            }

            return Array.Empty<SubscriptionPermission>();
        }

        public async Task<MarketGroupPermission[]> GetMarketGroupPermissions(ClaimsPrincipal claimsPrincipal, string marketGroupId)
        {
            var marketGroupLongId = Id.New<MarketGroup>(marketGroupId).LongIdentifierForType<MarketGroup>();
            var projectId = await db.MarketGroups.Where(x => x.Id == marketGroupLongId).Select(x => x.ProjectId).FirstOrDefaultAsync();

            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserTypeProjectManager) && claimsPrincipal.HasClaim(AppClaimTypes.ProjectManagerOf, projectId.ToString()))
            {
                return ProjectManagerMarketGroupPermission;
            }

            if (claimsPrincipal.HasClaim(AppClaimTypes.UserType, UserTypeMarketGroupManager) && claimsPrincipal.HasClaim(AppClaimTypes.MarketGroupManagerOf, marketGroupLongId.ToString()))
            {
                return MarketGroupManagerMarketGroupPermission;
            }

            return Array.Empty<MarketGroupPermission>();
        }
    }
}