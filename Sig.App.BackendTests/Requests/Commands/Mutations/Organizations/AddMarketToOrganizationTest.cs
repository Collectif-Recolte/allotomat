using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Organizations;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Organizations
{
    public class AddMarketToOrganizationTest : TestBase
    {
        private readonly AddMarketToOrganization handler;
        private readonly Market market;
        private readonly Organization organization;

        public AddMarketToOrganizationTest()
        {
            market = new Market()
            {
                Name = "Market 1"
            };
            DbContext.Markets.Add(market);

            organization = new Organization()
            {
                Name = "Organization 1"
            };
            DbContext.Organizations.Add(organization);

            DbContext.SaveChanges();

            handler = new AddMarketToOrganization(NullLogger<AddMarketToOrganization>.Instance, DbContext);
        }

        [Fact]
        public async Task AddMarketToOrganization()
        {
            var input = new AddMarketToOrganization.Input()
            {
                MarketId = market.GetIdentifier(),
                OrganizationId = organization.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var localOrganization = await DbContext.Organizations.FirstAsync();
            var localMarket = await DbContext.Markets.FirstAsync();

            localOrganization.Markets.Should().HaveCount(1);
            localMarket.Organizations.Should().HaveCount(1);
        }

        [Fact]
        public async Task ThrowsIfProjectNotFound()
        {
            var input = new AddMarketToOrganization.Input()
            {
                MarketId = market.GetIdentifier(),
                OrganizationId = Id.New<Organization>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddMarketToOrganization.OrganizationNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfMarketNotFound()
        {
            var input = new AddMarketToOrganization.Input()
            {
                MarketId = Id.New<Market>(123456),
                OrganizationId = organization.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddMarketToOrganization.MarketNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfMarketAlreadyInProject()
        {
            var organizationMarket = new OrganizationMarket()
            {
                Market = market,
                Organization = organization
            };
            DbContext.OrganizationMarkets.Add(organizationMarket);

            var input = new AddMarketToOrganization.Input()
            {
                MarketId = market.GetIdentifier(),
                OrganizationId = organization.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddMarketToOrganization.MarketAlreadyInOrganizationException>();
        }
    }
}
