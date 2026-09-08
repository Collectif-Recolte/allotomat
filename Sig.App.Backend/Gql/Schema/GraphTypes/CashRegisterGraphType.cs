using GraphQL.Conventions;
using GraphQL.DataLoader;
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
        // read the kiosk password. Query.CashRegister ne porte aucun RequirePermission: cette garde est
        // le seul contrôle de statut sur ce chemin.
        //
        // CRCL-2692: le statut se lit par le DataLoader, jamais par un service injecté dans le résolveur.
        // Les champs d'une query sont résolus en parallèle, et ScopedFieldResolver échange l'injecteur de
        // dépendances sur un créneau partagé par toute l'exécution: deux résolveurs concurrents peuvent
        // donc se retrouver sur le même AppDbContext. Un UserManager injecté ici suffisait à casser
        // l'écran Caisses dès la première caisse, puisque KioskPassword et KioskAccessToken se résolvent
        // en même temps. Le DataLoader crée son propre scope par lot (Gql/DataLoader.cs) et déduplique
        // par clé: une seule lecture de l'utilisateur par requête, quel que soit le nombre de caisses.
        public async Task<string> KioskPassword(IAppUserContext ctx, [Inject] PermissionService permissionService)
        {
            if (!await CurrentUserCanReadKioskCredentials(ctx, permissionService))
            {
                return null;
            }

            return cashRegister.KioskPassword;
        }

        // FILETS-36: same defect as KioskPassword, on the sibling resolver.
        public async Task<string> KioskAccessToken(IAppUserContext ctx, [Inject] PermissionService permissionService)
        {
            if (!await CurrentUserCanReadKioskCredentials(ctx, permissionService))
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

        private async Task<bool> CurrentUserCanReadKioskCredentials(IAppUserContext ctx, PermissionService permissionService)
        {
            var currentUser = await ctx.DataLoader.LoadUser(ctx.CurrentUserId).GetResultAsync();
            if (currentUser?.Status != UserStatus.Actived)
            {
                return false;
            }

            var marketPermissions = await permissionService.GetMarketPermissions(
                ctx.CurrentUser,
                cashRegister.MarketId.ToString());

            return marketPermissions.Contains(MarketPermission.ManageCashRegister);
        }
    }
}
