using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Profiles;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Organizations;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Organizations
{
    public class RemoveManagerFromOrganizationTest : TestBase
    {
        private readonly RemoveManagerFromOrganization handler;
        private readonly Organization Organization;
        private readonly AppUser manager;

        public RemoveManagerFromOrganizationTest()
        {
            Organization = new Organization()
            {
                Name = "Organization 1"
            };
            DbContext.Organizations.Add(Organization);

            manager = new AppUser("test1@example.com")
            {
                Type = UserType.Merchant,
                Profile = new UserProfile()
            };

            UserManager.CreateAsync(manager);
            UserManager.AddClaimAsync(manager, new Claim(AppClaimTypes.OrganizationManagerOf, Organization.Id.ToString()));

            DbContext.SaveChanges();

            handler = new RemoveManagerFromOrganization(NullLogger<RemoveManagerFromOrganization>.Instance, DbContext, UserManager);
        }

        [Fact]
        public async Task RemoveManagerFromOrganization()
        {
            var input = new RemoveManagerFromOrganization.Input()
            {
                OrganizationId = Organization.GetIdentifier(),
                ManagerId = manager.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var firstUser = await DbContext.Users.FirstAsync();
            var claims = await UserManager.GetClaimsAsync(firstUser);

            firstUser.Email.Should().Be("test1@example.com");
            claims.Count.Should().Be(0);
        }

        [Fact]
        public async Task ThrowsIfOrganizationNotFound()
        {
            var input = new RemoveManagerFromOrganization.Input()
            {
                OrganizationId = Id.New<Organization>(123456),
                ManagerId = manager.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<RemoveManagerFromOrganization.OrganizationNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfManagerNotFound()
        {
            var input = new RemoveManagerFromOrganization.Input()
            {
                OrganizationId = Organization.GetIdentifier(),
                ManagerId = Id.New<AppUser>(123456),
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<RemoveManagerFromOrganization.ManagerNotFoundException>();
        }
    }
}
