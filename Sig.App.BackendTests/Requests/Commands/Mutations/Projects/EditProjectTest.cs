using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Schema.Types;
using Sig.App.Backend.Requests.Commands.Mutations.Projects;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Projects
{
    public class EditProjectTest : TestBase
    {
        private readonly EditProject handler;
        private readonly Project project;
        public EditProjectTest()
        {
            project = new Project()
            {
                Name = "Project 1",
                Url = "https://www.example.com/"
            };
            DbContext.Projects.Add(project);

            DbContext.SaveChanges();

            handler = new EditProject(NullLogger<EditProject>.Instance, DbContext);
        }

        [Fact]
        public async Task EditTheProject()
        {
            var input = new EditProject.Input()
            {
                ProjectId = project.GetIdentifier(),
                Name = new Maybe<NonNull<string>>("Project 1 test"),
                Url = new Maybe<NonNull<string>>("https://www.example2.com/")
            };

            await handler.Handle(input, CancellationToken.None);

            var localProject = await DbContext.Projects.FirstAsync();

            localProject.Name.Should().Be("Project 1 test");
            localProject.Url.Should().Be("https://www.example2.com/");
            localProject.AllowOrganizationsAssignCards.Should().BeFalse();
        }

        [Fact]
        public async Task EditTheProjectAndAllowOrganizationsAssignCards()
        {
            var input = new EditProject.Input()
            {
                ProjectId = project.GetIdentifier(),
                Name = new Maybe<NonNull<string>>("Project 1 test"),
                Url = new Maybe<NonNull<string>>("https://www.example2.com/"),
                AllowOrganizationsAssignCards = true
            };

            await handler.Handle(input, CancellationToken.None);

            var localProject = await DbContext.Projects.FirstAsync();

            localProject.Name.Should().Be("Project 1 test");
            localProject.Url.Should().Be("https://www.example2.com/");
            localProject.AllowOrganizationsAssignCards.Should().BeTrue();
        }

        [Fact]
        public async Task ThrowsIfProjectNotFound()
        {
            var input = new EditProject.Input()
            {
                ProjectId = Id.New<Project>(123456),
                Name = new Maybe<NonNull<string>>("Project 1 test")
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<EditProject.ProjectNotFoundException>();
        }
    }
}
