using FluentAssertions;
using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.ProductGroups;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.ProductGroups
{
    public class DeleteProductGroupTest : TestBase
    {
        private readonly IRequestHandler<DeleteProductGroup.Input> handler;
        private readonly Project project;
        private readonly ProductGroup productGroup;

        public DeleteProductGroupTest()
        {
            project = new Project()
            {
                Name = "Project 1"
            };
            DbContext.Projects.Add(project);

            productGroup = new ProductGroup()
            {
                Project = project,
                Color = ProductGroupColor.Color_1,
                Name = "Product group 1",
                OrderOfAppearance = 1
            };
            DbContext.ProductGroups.Add(productGroup);

            DbContext.SaveChanges();

            handler = new DeleteProductGroup(NullLogger<DeleteProductGroup>.Instance, DbContext);
        }

        [Fact]
        public async Task DeleteProductGroup()
        {
            var input = new DeleteProductGroup.Input()
            {
                ProductGroupId = productGroup.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            var productGroupCount = await DbContext.ProductGroups.CountAsync();
            productGroupCount.Should().Be(0);
        }

        [Fact]
        public async Task ThrowsIfProductGroupNotFound()
        {
            var input = new DeleteProductGroup.Input()
            {
                ProductGroupId = Id.New<ProductGroup>(123456)
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<DeleteProductGroup.ProductGroupNotFoundException>();
        }
    }
}
