using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.ProductGroups;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.ProductGroups
{
    public class CreateProductGroupTest : TestBase
    {
        private readonly CreateProductGroup handler;
        private readonly Project project;

        public CreateProductGroupTest()
        {
            project = new Project()
            {
                Name = "Project 1"
            };
            DbContext.Projects.Add(project);

            DbContext.SaveChanges();

            handler = new CreateProductGroup(NullLogger<CreateProductGroup>.Instance, DbContext);
        }

        [Fact]
        public async Task AddProductGroupToProject()
        {
            var input = new CreateProductGroup.Input()
            {
                ProjectId = project.GetIdentifier(),
                Name = "Product group 1",
                Color = ProductGroupColor.Color_1,
                OrderOfAppearance = 1
            };

            await handler.Handle(input, CancellationToken.None);

            var localProductGroup = await DbContext.ProductGroups.FirstAsync();

            localProductGroup.ProjectId.Should().Be(project.Id);
            localProductGroup.OrderOfAppearance.Should().Be(1);
            localProductGroup.Name.Should().Be("Product group 1");
            localProductGroup.Color.Should().Be(ProductGroupColor.Color_1);
        }

        [Fact]
        public async Task ThrowsIfProjectNotFound()
        {
            var input = new CreateProductGroup.Input()
            {
                ProjectId = Id.New<Project>(123456),
                Name = "Product group 1",
                Color = ProductGroupColor.Color_1,
                OrderOfAppearance = 1
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<CreateProductGroup.ProjectNotFoundException>();
        }
    }
}
