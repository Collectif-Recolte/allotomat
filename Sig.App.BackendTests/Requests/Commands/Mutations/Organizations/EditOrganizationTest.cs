using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Schema.Types;
using Sig.App.Backend.Requests.Commands.Mutations.Organizations;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Organizations
{
    public class EditOrganizationTest : TestBase
    {
        private readonly EditOrganization handler;
        private readonly Organization organization;
        public EditOrganizationTest()
        {
            organization = new Organization()
            {
                Name = "Organization 1"
            };
            DbContext.Organizations.Add(organization);

            DbContext.SaveChanges();

            handler = new EditOrganization(NullLogger<EditOrganization>.Instance, DbContext);
        }

        [Fact]
        public async Task EditTheOrganization()
        {
            var input = new EditOrganization.Input()
            {
                OrganizationId = organization.GetIdentifier(),
                Name = new Maybe<NonNull<string>>("Organization 1 test")
            };

            await handler.Handle(input, CancellationToken.None);

            var localOrganization = await DbContext.Organizations.FirstAsync();

            localOrganization.Name.Should().Be("Organization 1 test");
        }

        [Fact]
        public async Task ThrowsIfOrganizationNotFound()
        {
            var input = new EditOrganization.Input()
            {
                OrganizationId = Id.New<Organization>(123456),
                Name = new Maybe<NonNull<string>>("Organization 1 test")
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<EditOrganization.OrganizationNotFoundException>();
        }
    }
}
