using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Schema.Types;
using Sig.App.Backend.Requests.Commands.Mutations.Beneficiaries;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Beneficiaries
{
    public class EditBeneficiaryTypeTest : TestBase
    {
        private readonly Project project;
        private readonly EditBeneficiaryType handler;
        private readonly BeneficiaryType beneficiaryType;

        public EditBeneficiaryTypeTest()
        {
            project = new Project()
            {
                Name = "Project 1"
            };
            DbContext.Projects.Add(project);

            beneficiaryType = new BeneficiaryType()
            {
                Keys = "a;b;c;d",
                Name = "Type ABCD",
                Project = project
            };
            DbContext.BeneficiaryTypes.Add(beneficiaryType);

            DbContext.SaveChanges();

            handler = new EditBeneficiaryType(NullLogger<EditBeneficiaryType>.Instance, DbContext);
        }

        [Fact]
        public async Task EditABeneficiaryType()
        {
            var input = new EditBeneficiaryType.Input()
            {
                BeneficiaryTypeId = beneficiaryType.GetIdentifier(),
                Name = new Maybe<NonNull<string>>("Type EFGH"),
                Keys = new string[4] { "E", "F", "G", "H" }
            };

            await handler.Handle(input, CancellationToken.None);

            var localBeneficiaryType = await DbContext.BeneficiaryTypes.FirstAsync();

            localBeneficiaryType.Name.Should().Be("Type EFGH");
            localBeneficiaryType.Keys.Should().Be("e;f;g;h");
        }

        [Fact]
        public async Task EditABeneficiaryTypeWithoutMutlipleSameKeys()
        {
            var input = new EditBeneficiaryType.Input()
            {
                BeneficiaryTypeId = beneficiaryType.GetIdentifier(),
                Name = new Maybe<NonNull<string>>("Type EFGH"),
                Keys = new string[8] { "E", "F", "G", "H", "E", "F", "G", "H" }
            };

            await handler.Handle(input, CancellationToken.None);

            var localBeneficiaryType = await DbContext.BeneficiaryTypes.FirstAsync();

            localBeneficiaryType.Name.Should().Be("Type EFGH");
            localBeneficiaryType.Keys.Should().Be("e;f;g;h");
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryTypeNotFound()
        {
            var input = new EditBeneficiaryType.Input()
            {
                BeneficiaryTypeId = Id.New<BeneficiaryType>(123456),
                Name = new Maybe<NonNull<string>>("Type EFGH"),
                Keys = new string[4] { "E", "F", "G", "H" }
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<EditBeneficiaryType.BeneficiaryTypeNotFoundException>();
        }

        [Fact]
        public async Task ThrowsIfBeneficiaryTypeKeyAlreadyInUse()
        {
            var beneficiaryType2 = new BeneficiaryType()
            {
                Keys = "e;f;g;h",
                Name = "Type EFGH",
                Project = project
            };
            DbContext.BeneficiaryTypes.Add(beneficiaryType2);
            DbContext.SaveChanges();

            var input = new EditBeneficiaryType.Input()
            {
                BeneficiaryTypeId = beneficiaryType.GetIdentifier(),
                Name = new Maybe<NonNull<string>>("Type EFGH"),
                Keys = new string[4] { "E", "F", "G", "H" }
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<EditBeneficiaryType.BeneficiaryTypeKeyAlreadyInUseException>();
        }
    }
}
