

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Organizations;
using Sig.App.Backend.Requests.Commands.Mutations.Projects;
using Sig.App.Backend.Services.Beneficiaries;
using Sig.App.Backend.Services.Mailer;
using Sig.App.Backend.Services.System;
using Xunit;

namespace Sig.App.BackendTests.Services
{
    public class BeneficiaryServiceTests : TestBase
    {
        private Mock<IMailer> mailer;
        private Mock<ICurrentUserAccessor> currentUserAccessor;
        private readonly AppUser organizationManager1;
        private readonly AppUser organizationManager2;
        private readonly AppUser projectManager1;
        private readonly AppUser projectManager2;
        private readonly Project project1;
        private readonly Project project2;
        private readonly Organization organization1;
        private readonly Organization organization2;
        
        public BeneficiaryServiceTests()
        {
            mailer = new Mock<IMailer>();
            currentUserAccessor = new Mock<ICurrentUserAccessor>();
            
            // Users
            organizationManager1 = AddUser("organization-manager1@example.com", UserType.OrganizationManager, password: "Abcd1234!!");
            organizationManager2 = AddUser("organization-manager2@example.com", UserType.OrganizationManager, password: "Abcd1234!!");
            projectManager1 = AddUser("project-manager1@example.com", UserType.ProjectManager, password: "Abcd1234!!");
            projectManager2 = AddUser("project-manager2@example.com", UserType.ProjectManager, password: "Abcd1234!!");
            
            // Projects
            project1 = new Project()
            {
                Name = "Project 1",
                BeneficiariesAreAnonymous = false
            };
            
            project2 = new Project()
            {
                Name = "Project 1",
                BeneficiariesAreAnonymous = true
            };
            
            DbContext.Projects.Add(project1);
            DbContext.Projects.Add(project2);
            DbContext.SaveChanges();

            // Organizations
            organization1 = new Organization()
            {
                Name = "Organization 1",
                Project = project1
            };
            
            organization2 = new Organization()
            {
                Name = "Organization 2",
                Project = project2
            };
            
            DbContext.Organizations.Add(organization1);
            DbContext.Organizations.Add(organization2);
            DbContext.SaveChanges();
            
            // Assign users
            var addManagerToOrganization = new AddManagerToOrganization(NullLogger<AddManagerToOrganization>.Instance, DbContext, UserManager, mailer.Object);
            
            var addManagerToOrganization1Input = new AddManagerToOrganization.Input()
            {
                OrganizationId = organization1.GetIdentifier(),
                ManagerEmails = new string[1] { "organization-manager1@example.com" }
            };
            
            var addManagerToOrganization2Input = new AddManagerToOrganization.Input()
            {
                OrganizationId = organization2.GetIdentifier(),
                ManagerEmails = new string[1] { "organization-manager2@example.com" }
            };
            
            addManagerToOrganization.Handle(addManagerToOrganization1Input, CancellationToken.None);
            addManagerToOrganization.Handle(addManagerToOrganization2Input, CancellationToken.None);
            
            var addManagerToProject = new AddManagerToProject(NullLogger<AddManagerToProject>.Instance, DbContext, UserManager, mailer.Object);
            
            var addManagerToProject1Input = new AddManagerToProject.Input()
            {
                ProjectId = project1.GetIdentifier(),
                ManagerEmails = new string[1] { "project-manager1@example.com" }
            };
            
            var addManagerToProject2Input = new AddManagerToProject.Input()
            {
                ProjectId = project2.GetIdentifier(),
                ManagerEmails = new string[1] { "project-manager2@example.com" }
            };

            addManagerToProject.Handle(addManagerToProject1Input, CancellationToken.None);
            addManagerToProject.Handle(addManagerToProject2Input, CancellationToken.None);
        }
        
        [Fact]
        public async Task OrganizationManagerCanAccessBeneficiaries()
        {
            currentUserAccessor.Setup(x => x.GetCurrentUser()).ReturnsAsync(organizationManager1);
            var beneficiaryService = new BeneficiaryService(DbContext, currentUserAccessor.Object, UserManager);

            var currentUserCanSeeAllBeneficiaryInfo = await beneficiaryService.CurrentUserCanSeeAllBeneficiaryInfo();

            currentUserCanSeeAllBeneficiaryInfo.Should().BeTrue();
        }
        
        [Fact]
        public async Task OrganizationManagerCanAccessAnonymousBeneficiaries()
        {
            currentUserAccessor.Setup(x => x.GetCurrentUser()).ReturnsAsync(organizationManager2);
            var beneficiaryService = new BeneficiaryService(DbContext, currentUserAccessor.Object, UserManager);

            var currentUserCanSeeAllBeneficiaryInfo = await beneficiaryService.CurrentUserCanSeeAllBeneficiaryInfo();

            currentUserCanSeeAllBeneficiaryInfo.Should().BeTrue();
        }
        
        [Fact]
        public async Task ProjectManagerCanAccessBeneficiaries()
        {
            currentUserAccessor.Setup(x => x.GetCurrentUser()).ReturnsAsync(projectManager1);
            var beneficiaryService = new BeneficiaryService(DbContext, currentUserAccessor.Object, UserManager);

            var currentUserCanSeeAllBeneficiaryInfo = await beneficiaryService.CurrentUserCanSeeAllBeneficiaryInfo();

            currentUserCanSeeAllBeneficiaryInfo.Should().BeTrue();
        }
        
        [Fact]
        public async Task ProjectManagerCanNotAccessAnonymousBeneficiaries()
        {
            currentUserAccessor.Setup(x => x.GetCurrentUser()).ReturnsAsync(projectManager2);
            var beneficiaryService = new BeneficiaryService(DbContext, currentUserAccessor.Object, UserManager);

            var currentUserCanSeeAllBeneficiaryInfo = await beneficiaryService.CurrentUserCanSeeAllBeneficiaryInfo();

            currentUserCanSeeAllBeneficiaryInfo.Should().BeFalse();
        }
    }
}