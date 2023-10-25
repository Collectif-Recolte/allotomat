using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Organizations;
using Sig.App.Backend.Services.Mailer;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Organizations
{
    public class AddManagerToOrganizationTest : TestBase
    {
        private readonly AddManagerToOrganization handler;
        private readonly Organization Organization;
        private Mock<IMailer> mailer;

        public AddManagerToOrganizationTest()
        {
            mailer = new Mock<IMailer>();

            Organization = new Organization()
            {
                Name = "Organization 1"
            };
            DbContext.Organizations.Add(Organization);

            DbContext.SaveChanges();

            handler = new AddManagerToOrganization(NullLogger<AddManagerToOrganization>.Instance, DbContext, UserManager, mailer.Object);
        }

        [Fact]
        public async Task AddOrganizationToProject()
        {
            var input = new AddManagerToOrganization.Input()
            {
                OrganizationId = Organization.GetIdentifier(),
                ManagerEmails = new string[1] { "test1@example.com" }
            };

            await handler.Handle(input, CancellationToken.None);

            var firstUser = await DbContext.Users.FirstAsync();
            var claims = await UserManager.GetClaimsAsync(firstUser);

            firstUser.Email.Should().Be("test1@example.com");
            claims.Count.Should().Be(1);
        }

        [Fact]
        public async Task ThrowsIfOrganizationNotFound()
        {
            var input = new AddManagerToOrganization.Input()
            {
                OrganizationId = Id.New<Organization>(123456),
                ManagerEmails = new string[1] { "test2@example.com" }
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddManagerToOrganization.OrganizationNotFoundException>();
        }
    }
}
