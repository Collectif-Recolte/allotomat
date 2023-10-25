using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Beneficiaries;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Beneficiaries
{
    public class AddBeneficiaryTypeInProjectTest : TestBase
    {
        private readonly AddBeneficiaryTypeInProject handler;
        private readonly Project project;

        public AddBeneficiaryTypeInProjectTest()
        {
            project = new Project()
            {
                Name = "Project 1"
            };
            DbContext.Projects.Add(project);
            DbContext.SaveChanges();

            handler = new AddBeneficiaryTypeInProject(NullLogger<AddBeneficiaryTypeInProject>.Instance, DbContext);
        }

        [Fact]
        public async Task CreateTheBeneficiaryType()
        {
            var input = new AddBeneficiaryTypeInProject.Input()
            {
                ProjectId = project.GetIdentifier(),
                Keys = new string[4] { "A", "B", "C", "D" },
                Name = "Type ABCD"
            };

            await handler.Handle(input, CancellationToken.None);

            var beneficiaryType = await DbContext.BeneficiaryTypes.FirstAsync();

            beneficiaryType.Name.Should().Be("Type ABCD");
            beneficiaryType.Keys.Should().Be("a;b;c;d");
            beneficiaryType.ProjectId.Should().Be(project.Id);
        }

        [Fact]
        public async Task CreateTheBeneficiaryTypeWithoutMutlipleSameKeys()
        {
            var input = new AddBeneficiaryTypeInProject.Input()
            {
                ProjectId = project.GetIdentifier(),
                Keys = new string[8] { "A", "B", "C", "D", "A", "B", "C", "D" },
                Name = "Type ABCD"
            };

            await handler.Handle(input, CancellationToken.None);

            var beneficiaryType = await DbContext.BeneficiaryTypes.FirstAsync();

            beneficiaryType.Name.Should().Be("Type ABCD");
            beneficiaryType.Keys.Should().Be("a;b;c;d");
            beneficiaryType.ProjectId.Should().Be(project.Id);
        }

        [Fact]
        public async Task ThrowsIfProjectNotFound()
        {
            var input = new AddBeneficiaryTypeInProject.Input()
            {
                ProjectId = Id.New<Project>(123456),
                Keys = new string[4] { "A", "B", "C", "D" },
                Name = "Type ABCD"
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddBeneficiaryTypeInProject.ProjectNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryTypeKeyAlreadyInUse()
        {
            var beneficiaryType = new BeneficiaryType()
            {
                Keys = "a;b;c;d",
                Name = "Type ABCD",
                Project = project
            };
            DbContext.BeneficiaryTypes.Add(beneficiaryType);

            DbContext.SaveChanges();

            var input = new AddBeneficiaryTypeInProject.Input()
            {
                ProjectId = project.GetIdentifier(),
                Keys = new string[4] { "A", "B", "C", "D" },
                Name = "Type ABCD"
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<AddBeneficiaryTypeInProject.BeneficiaryTypeKeyAlreadyInUseException>();
        }
    }
}
