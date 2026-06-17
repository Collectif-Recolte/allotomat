using GraphQL.Conventions;
using GraphQL.Conventions.Attributes;
using GraphQL.Conventions.Execution;
using GraphQL.Conventions.Types.Descriptors;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Sig.App.Backend.Services.Permission.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;
using Sig.App.Backend.Services.Permission;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Cards;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.DbModel.Entities.CashRegisters;
using Microsoft.AspNetCore.Identity;
using Sig.App.Backend.DbModel.Entities;

namespace Sig.App.Backend.Authorization
{
    public class RequirePermissionAttribute : ExecutionFilterAttributeBase, IMetaDataAttribute
    {
        private readonly object[] permissions;

        private PermissionService permissionService;
        private UserManager<AppUser> userManager;
        private AppDbContext db;
        private bool hasPermission;

        public RequirePermissionAttribute(params object[] permissions)
        {
            this.permissions = permissions;
        }

        public override async Task<object> Execute(IResolutionContext context, FieldResolutionDelegate next)
        {
            permissionService = context.DependencyInjector.Resolve<PermissionService>();
            userManager = context.DependencyInjector.Resolve<UserManager<AppUser>>();
            db = context.DependencyInjector.Resolve<AppDbContext>();
            var input = context.GetInputValue();
            var appUserContext = ((IAppUserContext)context.UserContext);

            var currentUser = await userManager.FindByIdAsync(appUserContext.CurrentUser.GetUserId());

            if (currentUser?.Status == DbModel.Enums.UserStatus.Actived)
            {
                foreach (var permission in permissions)
                {
                    if (await HasPermission(appUserContext.CurrentUser, permission, input))
                    {
                        hasPermission = true;
                        break;
                    }
                }
            }

            if (!hasPermission)
                throw new UnauthorizedAccessException();

            return await base.Execute(context, next);
        }

        private async Task<bool> HasPermission(ClaimsPrincipal claimsPrincipal, object permission, object input)
        {
            if (permission is GlobalPermission gp)
                return await HasGlobalPermission(claimsPrincipal, gp);
            if (permission is ProjectPermission pp)
                return await HasProjectPermission(claimsPrincipal, pp, input);
            if (permission is MarketPermission mp)
                return await HasMarketPermission(claimsPrincipal, mp, input);
            if (permission is OrganizationPermission op)
                return await HasOrganizationPermission(claimsPrincipal, op, input);
            if (permission is BeneficiaryPermission bp)
                return await HasBeneficiaryPermissions(claimsPrincipal, bp, input);
            if (permission is SubscriptionPermission sp)
                return await HasSubscriptionPermissions(claimsPrincipal, sp, input);
            if (permission is BeneficiaryTypePermission btp)
                return await HasBeneficiaryTypePermissions(claimsPrincipal, btp, input);
            if (permission is CardPermission cp)
                return await HasCardPermissions(claimsPrincipal, cp, input);
            if (permission is MarketGroupPermission mgp)
                return await HasMarketGroupPermissions(claimsPrincipal, mgp, input);
            return false;
        }

        private async Task<bool> HasGlobalPermission(ClaimsPrincipal claimsPrincipal, GlobalPermission permission)
        {
            var globalPermissions = await permissionService.GetGlobalPermissions(claimsPrincipal);
            return globalPermissions.Contains(permission);
        }

        private async Task<bool> HasProjectPermission(ClaimsPrincipal claimsPrincipal, ProjectPermission permission, object input)
        {
            var id = await GetProjectIdFromInput(input);
            var projectPermissions = await permissionService.GetProjectPermissions(claimsPrincipal, id);
            return projectPermissions.Contains(permission);
        }

        private async Task<bool> HasMarketPermission(ClaimsPrincipal claimsPrincipal, MarketPermission permission, object input)
        {
            var id = await GetMarketIdFromInput(input);
            var marketPermissions = await permissionService.GetMarketPermissions(claimsPrincipal, id);
            return marketPermissions.Contains(permission);
        }

        private async Task<bool> HasOrganizationPermission(ClaimsPrincipal claimsPrincipal, OrganizationPermission permission, object input)
        {
            var id = await GetOrganizationIdFromInput(input);
            var organizationPermissions = await permissionService.GetOrganizationPermissions(claimsPrincipal, id);
            return organizationPermissions.Contains(permission);
        }

        private async Task<bool> HasBeneficiaryPermissions(ClaimsPrincipal claimsPrincipal, BeneficiaryPermission permission, object input)
        {
            var id = await GetBeneficiaryIdFromInput(input);
            var beneficiaryPermissions = await permissionService.GetBeneficiaryPermissions(claimsPrincipal, id);
            return beneficiaryPermissions.Contains(permission);
        }

        private async Task<bool> HasSubscriptionPermissions(ClaimsPrincipal claimsPrincipal, SubscriptionPermission permission, object input)
        {
            var id = GetSubscriptionIdFromInput(input);
            var subscriptionPermissions = await permissionService.GetSubscriptionPermissions(claimsPrincipal, id);
            return subscriptionPermissions.Contains(permission);
        }

        private async Task<bool> HasBeneficiaryTypePermissions(ClaimsPrincipal claimsPrincipal, BeneficiaryTypePermission permission, object input)
        {
            var id = GetBeneficiaryTypeIdFromInput(input);
            var beneficiaryTypePermissions = await permissionService.GetBeneficiaryTypePermissions(claimsPrincipal, id);
            return beneficiaryTypePermissions.Contains(permission);
        }

        private async Task<bool> HasCardPermissions(ClaimsPrincipal claimsPrincipal, CardPermission permission, object input)
        {
            var id = GetCardIdFromInput(input);
            var cardPermissions = await permissionService.GetCardPermissions(claimsPrincipal, id);
            return cardPermissions.Contains(permission);
        }

        private async Task<bool> HasMarketGroupPermissions(ClaimsPrincipal claimsPrincipal, MarketGroupPermission permission, object input)
        {
            var id = GetMarketGroupIdFromInput(input);
            var marketGroupPermissions = await permissionService.GetMarketGroupPermissions(claimsPrincipal, id);
            return marketGroupPermissions.Contains(permission);
        }

        private async Task<string> GetProjectIdFromInput(object input)
        {
            if (input is HaveProjectId hpi)
            {
                return hpi.ProjectId.IdentifierForType<Project>();
            }
            if (input is HaveProjectIdAndMarketId hpiami)
            {
                return hpiami.ProjectId.IdentifierForType<Project>();
            }
            if (input is HaveOrganizationIdAndMarketId hoiami)
            {
                var organization = await db.Organizations
                    .Include(x => x.Project)
                    .Where(x => x.Id == hoiami.OrganizationId.LongIdentifierForType<Organization>())
                    .FirstOrDefaultAsync();
                return organization?.Project.GetIdentifier().IdentifierForType<Project>();
            }
            if (input is ProjectGraphType pgt)
            {
                return pgt.Id.IdentifierForType<Project>();
            }
            if (input is HaveCardId hci)
            {
                var cardId = hci.CardId.LongIdentifierForType<Card>();
                var card = await db.Cards.FindAsync(cardId);
                if (card != null)
                {
                    var projectId = Id.New<Project>(card.ProjectId);
                    return projectId.IdentifierForType<Project>();
                }
            }
            if (input is Id id)
            {
                return id.IdentifierForType<Project>();
            }

            return null;
        }

        private async Task<string> GetMarketIdFromInput(object input)
        {
            if (input is HaveInitialTransactionId hiti)
            {
                var transaction = await db.Transactions
                    .OfType<PaymentTransaction>()
                    .Include(x => x.Market)
                    .Where(x => x.Id == hiti.InitialTransactionId.LongIdentifierForType<PaymentTransaction>())
                    .FirstOrDefaultAsync();
                return transaction?.Market.GetIdentifier().IdentifierForType<Market>();
            }
            if (input is HaveCashRegisterId hcri)
            {
                var cashRegister = await db.CashRegisters
                    .Include(x => x.Market)
                    .Where(x => x.Id == hcri.CashRegisterId.LongIdentifierForType<CashRegister>())
                    .FirstOrDefaultAsync();
                return cashRegister?.Market.GetIdentifier().IdentifierForType<Market>();
            }
            if (input is HaveMarketId hmi)
            {
                return hmi.MarketId.IdentifierForType<Market>();
            }
            if (input is HaveMarketIdAndCardId hmiaci)
            {
                return hmiaci.MarketId.IdentifierForType<Market>();
            }
            if (input is HaveProjectIdAndMarketId hpiami)
            {
                return hpiami.MarketId.IdentifierForType<Market>();
            }
            if (input is HaveOrganizationIdAndMarketId hoiami)
            {
                return hoiami.MarketId.IdentifierForType<Market>();
            }
            if (input is MarketGraphType mgt)
            {
                return mgt.Id.IdentifierForType<Market>();
            }
            if (input is Id id)
            {
                return id.IdentifierForType<Market>();
            }

            return null;
        }

        private async Task<string> GetOrganizationIdFromInput(object input)
        {
            if (input is HaveOrganizationId hoi)
            {
                return hoi.OrganizationId.IdentifierForType<Organization>();
            }
            if (input is HaveOrganizationIdAndSubscriptionId hoiasi)
            {
                return hoiasi.OrganizationId.IdentifierForType<Organization>();
            }
            if (input is OrganizationGraphType ogt)
            {
                return ogt.Id.IdentifierForType<Organization>();
            }
            if (input is HaveOrganizationIdAndMarketId hoiami)
            {
                return hoiami.OrganizationId.IdentifierForType<Organization>();
            }
            if (input is HaveSubscriptionIdAndBeneficiaryId hsiabi)
            {
                var beneficiary = await db.Beneficiaries
                    .Include(x => x.Organization)
                    .Where(x => x.Id == hsiabi.BeneficiaryId.LongIdentifierForType<Beneficiary>())
                    .FirstOrDefaultAsync();
                return beneficiary?.Organization.GetIdentifier().IdentifierForType<Organization>();
            }
            if (input is Id id)
            {
                return id.IdentifierForType<Organization>();
            }

            return null;
        }

        private async Task<string> GetBeneficiaryIdFromInput(object input)
        {
            if (input is HaveBeneficiaryId hbi)
            {
                if (hbi.BeneficiaryId.IsIdentifierForType<Beneficiary>())
                {
                    return hbi.BeneficiaryId.IdentifierForType<Beneficiary>();
                }
                else
                {
                    return hbi.BeneficiaryId.IdentifierForType<OffPlatformBeneficiary>();
                }
            }
            if (input is HaveBeneficiaryIdAndCardId hbiaci)
            {
                if (hbiaci.BeneficiaryId.IsIdentifierForType<Beneficiary>())
                {
                    return hbiaci.BeneficiaryId.IdentifierForType<Beneficiary>();
                }
                else
                {
                    return hbiaci.BeneficiaryId.IdentifierForType<OffPlatformBeneficiary>();
                }
            }
            if (input is HaveSubscriptionIdAndBeneficiaryId hsiabi)
            {
                if (hsiabi.BeneficiaryId.IsIdentifierForType<Beneficiary>())
                {
                    return hsiabi.BeneficiaryId.IdentifierForType<Beneficiary>();
                }
                else
                {
                    return hsiabi.BeneficiaryId.IdentifierForType<OffPlatformBeneficiary>();
                }
            }
            if (input is HaveOriginalCardId hoci)
            {
                var beneficiary = (await db.Cards
                    .Include(x => x.Beneficiary)
                    .Where(x => x.Id == hoci.OriginalCardId.LongIdentifierForType<Card>())
                    .FirstOrDefaultAsync())?.Beneficiary;
                if (beneficiary is OffPlatformBeneficiary)
                {
                    return beneficiary.GetIdentifier().IdentifierForType<OffPlatformBeneficiary>();
                }
                return beneficiary.GetIdentifier().IdentifierForType<Beneficiary>();
            }
            if (input is BeneficiaryGraphType bgt)
            {
                return bgt.Id.IdentifierForType<Beneficiary>();
            }
            if (input is OffPlatformBeneficiaryGraphType opbgt)
            {
                return opbgt.Id.IdentifierForType<OffPlatformBeneficiary>();
            }
            if (input is Id id)
            {
                if (id.IsIdentifierForType<Beneficiary>())
                {
                    return id.IdentifierForType<Beneficiary>();
                }
                else
                {
                    return id.IdentifierForType<OffPlatformBeneficiary>();
                }
            }

            return null;
        }

        private string GetSubscriptionIdFromInput(object input)
        {
            if (input is HaveSubscriptionId hbi)
            {
                return hbi.SubscriptionId.IdentifierForType<Subscription>();
            }
            if (input is HaveOrganizationIdAndSubscriptionId hoiasi)
            {
                return hoiasi.SubscriptionId.IdentifierForType<Subscription>();
            }
            if (input is HaveSubscriptionIdAndBeneficiaryId hsiabi)
            {
                return hsiabi.SubscriptionId.IdentifierForType<Subscription>();
            }
            if (input is SubscriptionGraphType bgt)
            {
                return bgt.Id.IdentifierForType<Subscription>();
            }
            if (input is Id id)
            {
                return id.IdentifierForType<Subscription>();
            }

            return null;
        }

        private long GetBeneficiaryTypeIdFromInput(object input)
        {
            if (input is HaveBeneficiaryTypeId bti)
            {
                return bti.BeneficiaryTypeId.LongIdentifierForType<BeneficiaryType>();
            }
            if (input is BeneficiaryTypeGraphType btgt)
            {
                return btgt.Id.LongIdentifierForType<BeneficiaryType>();
            }
            if (input is Id id)
            {
                return id.LongIdentifierForType<BeneficiaryType>();
            }

            return -1;
        }

        private string GetCardIdFromInput(object input)
        {
            if (input is HaveCardId hci)
            {
                return hci.CardId.IdentifierForType<Card>();
            }
            if (input is CardGraphType cgt)
            {
                return cgt.Id.IdentifierForType<Card>();
            }
            if (input is HaveOriginalCardId hoci)
            {
                return hoci.OriginalCardId.IdentifierForType<Card>();
            }
            if (input is Id id)
            {
                return id.IdentifierForType<Card>();
            }

            return null;
        }

        private string GetMarketGroupIdFromInput(object input)
        {
            if (input is HaveMarketGroupId hmgi)
            {
                return hmgi.MarketGroupId.IdentifierForType<MarketGroup>();
            }
            if (input is HaveMarketGroupIdAndMarketId hmgiami)
            {
                return hmgiami.MarketGroupId.IdentifierForType<MarketGroup>();
            }

            return null;
        }

        bool IMetaDataAttribute.ShouldBeApplied(GraphEntityInfo entity) => true;

        void IMetaDataAttribute.DeriveMetaData(GraphEntityInfo entity)
        {

            if (string.IsNullOrWhiteSpace(entity.Description))
                entity.Description = "";
            else
                entity.Description += "\n\n";

            entity.Description +=  $"Current user needs permission(s) [{string.Join(", ", permissions)}]";
        }
    }
}