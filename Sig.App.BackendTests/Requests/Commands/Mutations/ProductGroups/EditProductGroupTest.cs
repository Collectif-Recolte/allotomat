using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Schema.Types;
using Sig.App.Backend.Requests.Commands.Mutations.ProductGroups;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.ProductGroups
{
    public class EditProductGroupTest : TestBase
    {
        private readonly EditProductGroup handler;
        private readonly ProductGroup productGroup;

        public EditProductGroupTest()
        {
            var project = new Project()
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

            handler = new EditProductGroup(NullLogger<EditProductGroup>.Instance, DbContext);
        }

        [Fact]
        public async Task EditProductGroup()
        {
            var input = new EditProductGroup.Input()
            {
                ProductGroupId = productGroup.GetIdentifier(),
                Color = ProductGroupColor.Color_2,
                Name = new Maybe<NonNull<string>>("Product group 2"),
                OrderOfAppearance = 2
            };

            await handler.Handle(input, CancellationToken.None);

            var localProductGroup = await DbContext.ProductGroups.FirstAsync();

            localProductGroup.Name.Should().Be("Product group 2");
            localProductGroup.Color.Should().Be(ProductGroupColor.Color_2);
            localProductGroup.OrderOfAppearance.Should().Be(2);
        }

        [Fact]
        public async Task ThrowsIfProductGroupNotFound()
        {
            var input = new EditProductGroup.Input()
            {
                ProductGroupId = Id.New<ProductGroup>(123456),
                Color = ProductGroupColor.Color_2,
                Name = new Maybe<NonNull<string>>("Product group 2"),
                OrderOfAppearance = 2
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<EditProductGroup.ProductGroupNotFoundException>();
        }
    }
}
