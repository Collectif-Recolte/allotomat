using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.CashRegisters;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Gql.Interfaces;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Services.Permission;

namespace Sig.App.BackendTests.Gql
{
    // KioskPassword and KioskAccessToken are plain CashRegisterGraphType resolver methods, not wrapped
    // by RequirePermissionAttribute (a GraphQL.Conventions execution filter): calling them directly,
    // without routing a query through a real GraphQL engine, exercises exactly what production runs.
    public class CashRegisterKioskGuardTest : TestBase
    {
        private const long MarketId = 1;
        private const long OtherMarketId = 2;

        private readonly PermissionService permissionService;

        public CashRegisterKioskGuardTest()
        {
            permissionService = new PermissionService(DbContext);
        }

        // FILETS-36: KioskPassword checked MarketPermission.ManageCashRegister without ever reading the
        // account Status, unlike RequirePermissionAttribute which refuses a non-Actived account before
        // looking at permissions. A disabled merchant managing the market must still be refused.
        [Fact]
        public async Task KioskPassword_DisabledMerchantManagingMarket_IsRefused()
        {
            var merchant = AddDisabledMarketManager(MarketId);
            var cashRegister = BuildCashRegister(MarketId);

            var result = await new CashRegisterGraphType(cashRegister).KioskPassword(
                BuildUserContext(merchant), permissionService, UserManager);

            result.Should().BeNull();
        }

        // FILETS-36: same defect as KioskPassword, on the sibling resolver.
        [Fact]
        public async Task KioskAccessToken_DisabledMerchantManagingMarket_IsRefused()
        {
            var merchant = AddDisabledMarketManager(MarketId);
            var cashRegister = BuildCashRegister(MarketId);

            var result = await new CashRegisterGraphType(cashRegister).KioskAccessToken(
                BuildUserContext(merchant), permissionService, UserManager);

            result.Should().BeNull();
        }

        [Fact]
        public async Task KioskPassword_ActiveMerchantManagingMarket_IsAccepted()
        {
            var merchant = AddActiveMarketManager(MarketId);
            var cashRegister = BuildCashRegister(MarketId);

            var result = await new CashRegisterGraphType(cashRegister).KioskPassword(
                BuildUserContext(merchant), permissionService, UserManager);

            result.Should().Be(cashRegister.KioskPassword);
        }

        [Fact]
        public async Task KioskAccessToken_ActiveMerchantManagingMarket_IsAccepted()
        {
            var merchant = AddActiveMarketManager(MarketId);
            var cashRegister = BuildCashRegister(MarketId);

            var result = await new CashRegisterGraphType(cashRegister).KioskAccessToken(
                BuildUserContext(merchant), permissionService, UserManager);

            result.Should().Be(cashRegister.KioskAccessToken);
        }

        // An active merchant managing a different market must still be refused: the guard checks the
        // caller against THIS cash register's own market, not merely that the caller manages some market.
        [Fact]
        public async Task KioskPassword_ActiveMerchantManagingOtherMarket_IsRefused()
        {
            var merchant = AddActiveMarketManager(OtherMarketId);
            var cashRegister = BuildCashRegister(MarketId);

            var result = await new CashRegisterGraphType(cashRegister).KioskPassword(
                BuildUserContext(merchant), permissionService, UserManager);

            result.Should().BeNull();
        }

        [Fact]
        public async Task KioskAccessToken_ActiveMerchantManagingOtherMarket_IsRefused()
        {
            var merchant = AddActiveMarketManager(OtherMarketId);
            var cashRegister = BuildCashRegister(MarketId);

            var result = await new CashRegisterGraphType(cashRegister).KioskAccessToken(
                BuildUserContext(merchant), permissionService, UserManager);

            result.Should().BeNull();
        }

        private static CashRegister BuildCashRegister(long marketId)
        {
            return new CashRegister
            {
                Id = 1,
                MarketId = marketId,
                KioskPassword = "kiosk-password",
                KioskAccessToken = "kiosk-access-token"
            };
        }

        private AppUser AddActiveMarketManager(long marketId)
        {
            return AddUser("merchant@example.com", UserType.Merchant,
                claims: new[] { new Claim(AppClaimTypes.MarketManagerOf, marketId.ToString()) });
        }

        private AppUser AddDisabledMarketManager(long marketId)
        {
            var merchant = AddActiveMarketManager(marketId);
            merchant.Status = UserStatus.Disabled;
            DbContext.SaveChangesAsync().Wait();
            return merchant;
        }

        private IAppUserContext BuildUserContext(AppUser user)
        {
            var mock = new Mock<IAppUserContext>();
            mock.Setup(x => x.CurrentUser).Returns(CreatePrincipal(user));
            return mock.Object;
        }
    }
}
