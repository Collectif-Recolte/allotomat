using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.EmailTemplates.Models;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Schema.Types;
using Sig.App.Backend.Requests.Commands.Mutations.CashRegisters;
using Sig.App.Backend.Requests.Commands.Mutations.Markets;
using Sig.App.Backend.Services.Mailer;
using Sig.App.Backend.Services.System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Markets
{
    public class CreateMarketTest : TestBase
    {
        private readonly CreateMarket handler;
        private readonly Mock<IMailer> mailer;
        private readonly Project project;
        private readonly MarketGroup marketGroup;

        public CreateMarketTest()
        {
            mailer = new Mock<IMailer>();
            var currentUserAccessor = new CurrentUserAccessor(HttpContextAccessor, DbContext);
            handler = new CreateMarket(NullLogger<CreateMarket>.Instance, DbContext, UserManager, mailer.Object, Mediator, currentUserAccessor);
            SetupRequestHandler(new CreateCashRegister(NullLogger<CreateCashRegister>.Instance, DbContext));

            project = new Project { Name = "Project 1" };
            DbContext.Projects.Add(project);

            marketGroup = new MarketGroup { Name = "Market Group 1", Project = project };
            DbContext.MarketGroups.Add(marketGroup);

            DbContext.SaveChanges();

            SetLoggedInUser(AddUser("admin@example.com", UserType.PCAAdmin));
        }

        [Fact]
        public async Task When_PCAAdminWithoutAssociation_CreatesGlobalMarketWithoutProjectLink()
        {
            var input = new CreateMarket.Input
            {
                Name = "Global Market",
                ManagerEmails = new[] { "manager-global@example.com" }
            };

            await handler.Handle(input, CancellationToken.None);

            var market = await DbContext.Markets.SingleAsync();
            market.Name.Should().Be("Global Market");
            (await DbContext.ProjectMarkets.CountAsync()).Should().Be(0);
            (await DbContext.MarketGroupMarkets.CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task When_ValidAssociation_CreatesProjectAndMarketGroupLinks()
        {
            var input = new CreateMarket.Input
            {
                Name = "Associated Market",
                ManagerEmails = new[] { "manager-associated@example.com" },
                ProjectId = project.GetIdentifier(),
                MarketGroupId = marketGroup.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var market = await DbContext.Markets.SingleAsync();
            market.Name.Should().Be("Associated Market");

            var projectMarket = await DbContext.ProjectMarkets.SingleAsync();
            projectMarket.ProjectId.Should().Be(project.Id);
            projectMarket.MarketId.Should().Be(market.Id);

            var marketGroupMarket = await DbContext.MarketGroupMarkets.SingleAsync();
            marketGroupMarket.MarketGroupId.Should().Be(marketGroup.Id);
            marketGroupMarket.MarketId.Should().Be(market.Id);

            (await DbContext.CashRegisters.CountAsync()).Should().Be(1);
        }

        [Fact]
        public async Task SendsConfirmationEmail()
        {
            var input = new CreateMarket.Input
            {
                Name = "Market Test 1",
                ManagerEmails = new[] { "test1@example.com" }
            };

            await handler.Handle(input, CancellationToken.None);

            mailer.Verify(x => x.Send(It.IsAny<MarketManagerInviteEmail>()));
        }

        [Fact]
        public async Task When_OnlyProjectIdProvided_ThrowsIncompleteProgramAssociation()
        {
            var marketCountBefore = await DbContext.Markets.CountAsync();

            var input = new CreateMarket.Input
            {
                Name = "Incomplete Market",
                ManagerEmails = new[] { "manager-incomplete@example.com" },
                ProjectId = project.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateMarket.IncompleteProgramAssociationException>();

            (await DbContext.Markets.CountAsync()).Should().Be(marketCountBefore);
        }

        [Fact]
        public async Task When_OnlyMarketGroupIdProvided_ThrowsIncompleteProgramAssociation()
        {
            var marketCountBefore = await DbContext.Markets.CountAsync();

            var input = new CreateMarket.Input
            {
                Name = "Incomplete Market",
                ManagerEmails = new[] { "manager-incomplete@example.com" },
                MarketGroupId = marketGroup.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateMarket.IncompleteProgramAssociationException>();

            (await DbContext.Markets.CountAsync()).Should().Be(marketCountBefore);
        }

        [Fact]
        public async Task When_MaybeValueIsNull_TreatsAsNoAssociationForPCAAdmin()
        {
            var input = new CreateMarket.Input
            {
                Name = "Empty Maybe Market",
                ManagerEmails = new[] { "manager-empty-maybe@example.com" },
                ProjectId = new Maybe<Id>(),
                MarketGroupId = new Maybe<Id>()
            };

            await handler.Handle(input, CancellationToken.None);

            (await DbContext.ProjectMarkets.CountAsync()).Should().Be(0);
        }

        [Fact]
        public async Task When_ProjectManagerWithoutAssociation_ThrowsIncompleteProgramAssociation()
        {
            SetLoggedInUser(AddUser("pm@example.com", UserType.ProjectManager));

            var marketCountBefore = await DbContext.Markets.CountAsync();
            var input = new CreateMarket.Input
            {
                Name = "Unauthorized Global Market",
                ManagerEmails = new[] { "manager-pm@example.com" }
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateMarket.IncompleteProgramAssociationException>();

            (await DbContext.Markets.CountAsync()).Should().Be(marketCountBefore);
        }

        [Fact]
        public async Task When_MarketGroupNotFound_ThrowsBeforePersistingMarket()
        {
            var marketCountBefore = await DbContext.Markets.CountAsync();

            var input = new CreateMarket.Input
            {
                Name = "Invalid Group Market",
                ManagerEmails = new[] { "manager-invalid-group@example.com" },
                ProjectId = project.GetIdentifier(),
                MarketGroupId = Id.New<MarketGroup>(999999)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateMarket.MarketGroupNotFoundException>();

            (await DbContext.Markets.CountAsync()).Should().Be(marketCountBefore);
        }

        [Fact]
        public async Task When_MarketGroupNotInProject_ThrowsBeforePersistingMarket()
        {
            var otherProject = new Project { Name = "Other Project" };
            DbContext.Projects.Add(otherProject);
            await DbContext.SaveChangesAsync();

            var marketCountBefore = await DbContext.Markets.CountAsync();

            var input = new CreateMarket.Input
            {
                Name = "Mismatched Project Market",
                ManagerEmails = new[] { "manager-mismatch@example.com" },
                ProjectId = otherProject.GetIdentifier(),
                MarketGroupId = marketGroup.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateMarket.MarketGroupNotInProjectException>();

            (await DbContext.Markets.CountAsync()).Should().Be(marketCountBefore);
        }

        [Fact]
        public async Task When_ManagerAlreadyManagesMarket_ThrowsBeforePersistingMarket()
        {
            var existingManager = AddUser("existing-manager@example.com", UserType.Merchant);
            await UserManager.AddClaimAsync(existingManager, new Claim(AppClaimTypes.MarketManagerOf, "999"));

            var marketCountBefore = await DbContext.Markets.CountAsync();

            var input = new CreateMarket.Input
            {
                Name = "Duplicate Manager Market",
                ManagerEmails = new[] { existingManager.Email }
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateMarket.UserAlreadyManagerException>();

            (await DbContext.Markets.CountAsync()).Should().Be(marketCountBefore);
        }

        [Fact]
        public async Task When_SecondManagerAlreadyManagesMarket_DoesNotCreateFirstNewUser()
        {
            var existingManager = AddUser("blocked-manager@example.com", UserType.Merchant);
            await UserManager.AddClaimAsync(existingManager, new Claim(AppClaimTypes.MarketManagerOf, "999"));

            var userCountBefore = await DbContext.Users.CountAsync();
            var marketCountBefore = await DbContext.Markets.CountAsync();

            var input = new CreateMarket.Input
            {
                Name = "Multi Manager Market",
                ManagerEmails = new[] { "new-manager@example.com", existingManager.Email }
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateMarket.UserAlreadyManagerException>();

            (await DbContext.Users.CountAsync()).Should().Be(userCountBefore);
            (await DbContext.Markets.CountAsync()).Should().Be(marketCountBefore);
        }
    }
}
