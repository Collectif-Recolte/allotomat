using FluentAssertions;
using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Organizations;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Organizations
{
    public class DeleteOrganizationTest : TestBase
    {
        private readonly IRequestHandler<DeleteOrganization.Input> handler;
        private readonly Organization organization;

        public DeleteOrganizationTest()
        {
            organization = new Organization()
            {
                Name = "Organization 1"
            };
            DbContext.Organizations.Add(organization);

            DbContext.SaveChanges();

            handler = new DeleteOrganization(NullLogger<DeleteOrganization>.Instance, DbContext, UserManager, Mediator);
        }

        [Fact]
        public async Task DeleteTheOrganization()
        {
            var input = new DeleteOrganization.Input()
            {
                OrganizationId = organization.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var organizationCount = await DbContext.Organizations.CountAsync();
            organizationCount.Should().Be(0);
        }

        [Fact]
        public async Task ThrowsIfOrganizationNotFound()
        {
            var input = new DeleteOrganization.Input()
            {
                OrganizationId = Id.New<Organization>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<DeleteOrganization.OrganizationNotFoundException>();
        }
    }
}
