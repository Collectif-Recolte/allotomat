using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Organizations;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Organizations
{
    public class RemoveMarketFromOrganizationTest : TestBase
    {
        private readonly RemoveMarketFromOrganization handler;
        private readonly Market market;
        private readonly Organization organization;

        public RemoveMarketFromOrganizationTest()
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

            organization.Markets = new List<OrganizationMarket>() { new OrganizationMarket() { Market = market, Organization = organization } };

            DbContext.Organizations.Add(organization);

            DbContext.SaveChanges();

            handler = new RemoveMarketFromOrganization(NullLogger<RemoveMarketFromOrganization>.Instance, DbContext);
        }

        [Fact]
        public async Task RemoveMarketFromOrganization()
        {
            var input = new RemoveMarketFromOrganization.Input()
            {
                MarketId = market.GetIdentifier(),
                OrganizationId = organization.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var localOrganizationMarket = await DbContext.OrganizationMarkets.CountAsync();

            localOrganizationMarket.Should().Be(0);
        }

        [Fact]
        public async Task ThrowsIfOrganizationNotFound()
        {
            var input = new RemoveMarketFromOrganization.Input()
            {
                MarketId = market.GetIdentifier(),
                OrganizationId = Id.New<Organization>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<RemoveMarketFromOrganization.OrganizationNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfMarketNotFound()
        {
            var input = new RemoveMarketFromOrganization.Input()
            {
                MarketId = Id.New<Market>(123456),
                OrganizationId = organization.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<RemoveMarketFromOrganization.MarketNotFoundException>();
        }
    }
}
