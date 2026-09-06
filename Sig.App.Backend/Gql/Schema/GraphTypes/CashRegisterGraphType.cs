using GraphQL.Conventions;
using GraphQL.DataLoader;
using Microsoft.AspNetCore.Identity;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.CashRegisters;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;
using Sig.App.Backend.Services.Permission;
using Sig.App.Backend.Services.Permission.Enums;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class CashRegisterGraphType
    {
        private readonly CashRegister cashRegister;

        public Id Id => cashRegister.GetIdentifier();
        public NonNull<string> Name => cashRegister.Name;
        public bool IsArchived => cashRegister.IsArchived;
        public bool IsKioskEnabled => !string.IsNullOrEmpty(cashRegister.KioskAccessToken);

        public CashRegisterGraphType(CashRegister cashRegister)
        {
            this.cashRegister = cashRegister;
        }

        // FILETS-36: this manual guard skipped the account status check that RequirePermissionAttribute
        // does before looking at permissions, so a disabled account managing this market could still
        // read the kiosk password.
        public async Task<string> KioskPassword(IAppUserContext ctx, [Inject] PermissionService permissionService, [Inject] UserManager<AppUser> userManager)
        {
            var currentUser = await userManager.FindByIdAsync(ctx.CurrentUser.GetUserId());
            if (currentUser?.Status != UserStatus.Actived)
            {
                return null;
            }

            var marketPermissions = await permissionService.GetMarketPermissions(
                ctx.CurrentUser,
                cashRegister.MarketId.ToString());

            if (!marketPermissions.Contains(MarketPermission.ManageCashRegister))
            {
                return null;
            }

            return cashRegister.KioskPassword;
        }

        // FILETS-36: same defect as KioskPassword, on the sibling resolver.
        public async Task<string> KioskAccessToken(IAppUserContext ctx, [Inject] PermissionService permissionService, [Inject] UserManager<AppUser> userManager)
        {
            var currentUser = await userManager.FindByIdAsync(ctx.CurrentUser.GetUserId());
            if (currentUser?.Status != UserStatus.Actived)
            {
                return null;
            }

            var marketPermissions = await permissionService.GetMarketPermissions(
                ctx.CurrentUser,
                cashRegister.MarketId.ToString());

            if (!marketPermissions.Contains(MarketPermission.ManageCashRegister))
            {
                return null;
            }

            return cashRegister.KioskAccessToken;
        }

        public IDataLoaderResult<MarketGraphType> Market(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadMarket(cashRegister.MarketId);
        }

        public IDataLoaderResult<IEnumerable<MarketGroupGraphType>> MarketGroups(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadCashRegisterMarketGroups(Id.LongIdentifierForType<CashRegister>());
        }
    }
}
