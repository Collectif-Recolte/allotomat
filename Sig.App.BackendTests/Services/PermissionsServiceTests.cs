using System.Linq;
using FluentAssertions;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Xunit;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Plugins.Identity;
using Sig.App.Backend.Services.Permission;
using Sig.App.Backend.Services.Permission.Enums;

namespace Sig.App.BackendTests.Services
{
    public class PermissionsServiceTests : TestBase
    {
        private readonly AppUser admin;
        private readonly AppUser user1;
        private readonly AppUser projectManager;
        private readonly AppUser organizationManager;
        private readonly AppUser organizationManagerWithoutAssignCards;
        private readonly AppUser unrelatedProjectManager;
        private readonly AppUser unrelatedOrganizationManager;
        private readonly Card anonymousProjectCard;
        private readonly Card cardWithoutAssignCards;
        private readonly AppUserClaimsPrincipalFactory claimsPrincipalFactory;
        private readonly PermissionService permissionService;

        public PermissionsServiceTests()
        {
            admin = AddUser("admin@example.com", UserType.PCAAdmin, password: "Abcd1234!!");
            user1 = AddUser("user1@example.com", UserType.OrganizationManager, password: "Abcd1234!!");

            var anonymousProject = new Project()
            {
                Name = "Anonymous project",
                BeneficiariesAreAnonymous = true,
                AllowOrganizationsAssignCards = true
            };

            var anonymousProjectOrganization = new Organization()
            {
                Name = "Organization 1",
                Project = anonymousProject
            };

            anonymousProjectCard = new Card()
            {
                Status = CardStatus.Assigned,
                Project = anonymousProject,
                ProgramCardId = 1
            };
            DbContext.Cards.Add(anonymousProjectCard);

            DbContext.Beneficiaries.Add(new Beneficiary()
            {
                Firstname = "John",
                Lastname = "Doe",
                Organization = anonymousProjectOrganization,
                Card = anonymousProjectCard
            });

            var projectWithoutAssignCards = new Project()
            {
                Name = "Project without assign cards",
                AllowOrganizationsAssignCards = false
            };

            var organizationWithoutAssignCards = new Organization()
            {
                Name = "Organization 2",
                Project = projectWithoutAssignCards
            };

            cardWithoutAssignCards = new Card()
            {
                Status = CardStatus.Assigned,
                Project = projectWithoutAssignCards,
                ProgramCardId = 2
            };
            DbContext.Cards.Add(cardWithoutAssignCards);

            DbContext.Beneficiaries.Add(new Beneficiary()
            {
                Firstname = "Jane",
                Lastname = "Doe",
                Organization = organizationWithoutAssignCards,
                Card = cardWithoutAssignCards
            });

            DbContext.SaveChanges();

            projectManager = AddUser("pm@example.com", UserType.ProjectManager, "Abcd1234!!",
                new Claim(AppClaimTypes.ProjectManagerOf, anonymousProject.Id.ToString()));
            organizationManager = AddUser("om@example.com", UserType.OrganizationManager, "Abcd1234!!",
                new Claim(AppClaimTypes.OrganizationManagerOf, anonymousProjectOrganization.Id.ToString()));
            organizationManagerWithoutAssignCards = AddUser("om2@example.com", UserType.OrganizationManager, "Abcd1234!!",
                new Claim(AppClaimTypes.OrganizationManagerOf, organizationWithoutAssignCards.Id.ToString()));
            unrelatedProjectManager = AddUser("pm-unrelated@example.com", UserType.ProjectManager, "Abcd1234!!",
                new Claim(AppClaimTypes.ProjectManagerOf, projectWithoutAssignCards.Id.ToString()));
            unrelatedOrganizationManager = AddUser("om-unrelated@example.com", UserType.OrganizationManager, "Abcd1234!!",
                new Claim(AppClaimTypes.OrganizationManagerOf, organizationWithoutAssignCards.Id.ToString()));

            claimsPrincipalFactory = new AppUserClaimsPrincipalFactory(
                UserManager,
                new OptionsWrapper<IdentityOptions>(UserManager.Options));
            permissionService = new PermissionService(DbContext);
        }

        [Fact]
        public async Task AdminCanManageAllUsers()
        {
            var permission = GlobalPermission.ManageAllUsers;
            var claimPrincipal = await claimsPrincipalFactory.CreateAsync(admin);

            var permissions = await permissionService.GetGlobalPermissions(claimPrincipal);

            permissions.Contains(permission).Should().BeTrue();
        }

        [Fact]
        public async Task UserCanNotManageAllUsers()
        {
            var permission = GlobalPermission.ManageAllUsers;
            var claimPrincipal = await claimsPrincipalFactory.CreateAsync(user1);

            var permissions = await permissionService.GetGlobalPermissions(claimPrincipal);

            permissions.Contains(permission).Should().BeFalse();
        }

        [Fact]
        public async Task ProjectManagerReceivesCardPermissionsEvenWhenBeneficiariesAreAnonymous()
        {
            var claimPrincipal = await claimsPrincipalFactory.CreateAsync(projectManager);

            var permissions = await permissionService.GetCardPermissions(claimPrincipal, anonymousProjectCard.Id.ToString());

            permissions.Should().Contain(CardPermission.TransfertCard);
            permissions.Should().Contain(CardPermission.EnableDisableCard);
        }

        [Fact]
        public async Task OrganizationManagerWithAllowAssignCardsReceivesCardPermissions()
        {
            var claimPrincipal = await claimsPrincipalFactory.CreateAsync(organizationManager);

            var permissions = await permissionService.GetCardPermissions(claimPrincipal, anonymousProjectCard.Id.ToString());

            permissions.Should().Contain(CardPermission.TransfertCard);
            permissions.Should().Contain(CardPermission.EnableDisableCard);
        }

        [Fact]
        public async Task OrganizationManagerWithoutAllowAssignCardsReceivesNoCardPermissions()
        {
            var claimPrincipal = await claimsPrincipalFactory.CreateAsync(organizationManagerWithoutAssignCards);

            var permissions = await permissionService.GetCardPermissions(claimPrincipal, cardWithoutAssignCards.Id.ToString());

            permissions.Should().BeEmpty();
        }

        [Fact]
        public async Task UnrelatedProjectManagerReceivesNoCardPermissions()
        {
            var claimPrincipal = await claimsPrincipalFactory.CreateAsync(unrelatedProjectManager);

            var permissions = await permissionService.GetCardPermissions(claimPrincipal, anonymousProjectCard.Id.ToString());

            permissions.Should().BeEmpty();
        }

        [Fact]
        public async Task UnrelatedOrganizationManagerReceivesNoCardPermissions()
        {
            var claimPrincipal = await claimsPrincipalFactory.CreateAsync(unrelatedOrganizationManager);

            var permissions = await permissionService.GetCardPermissions(claimPrincipal, anonymousProjectCard.Id.ToString());

            permissions.Should().BeEmpty();
        }
    }
}
