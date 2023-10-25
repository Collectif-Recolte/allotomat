using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.Profiles;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Projects;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Projects
{
    public class RemoveManagerFromProjectTest : TestBase
    {
        private readonly RemoveManagerFromProject handler;
        private readonly Project project;
        private readonly AppUser manager;

        public RemoveManagerFromProjectTest()
        {
            project = new Project()
            {
                Name = "Project 1"
            };
            DbContext.Projects.Add(project);

            manager = new AppUser("test1@example.com")
            {
                Type = UserType.ProjectManager,
                Profile = new UserProfile()
            };

            UserManager.CreateAsync(manager);
            UserManager.AddClaimAsync(manager, new Claim(AppClaimTypes.ProjectManagerOf, project.Id.ToString()));

            DbContext.SaveChanges();

            handler = new RemoveManagerFromProject(NullLogger<RemoveManagerFromProject>.Instance, DbContext, UserManager);
        }

        [Fact]
        public async Task RemoveManagerFromProject()
        {
            var input = new RemoveManagerFromProject.Input()
            {
                ProjectId = project.GetIdentifier(),
                ManagerId = manager.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var firstUser = await DbContext.Users.FirstAsync();
            var claims = await UserManager.GetClaimsAsync(firstUser);

            firstUser.Email.Should().Be("test1@example.com");
            claims.Count.Should().Be(0);
        }

        [Fact]
        public async Task ThrowsIfProjectNotFound()
        {
            var input = new RemoveManagerFromProject.Input()
            {
                ProjectId = Id.New<Project>(123456),
                ManagerId = manager.GetIdentifier()
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<RemoveManagerFromProject.ProjectNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfManagerNotFound()
        {
            var input = new RemoveManagerFromProject.Input()
            {
                ProjectId = project.GetIdentifier(),
                ManagerId = Id.New<AppUser>(123456),
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<RemoveManagerFromProject.ManagerNotFoundException>();
        }
    }
}
