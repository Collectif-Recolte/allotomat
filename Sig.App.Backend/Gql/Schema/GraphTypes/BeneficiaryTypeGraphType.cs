using GraphQL.Conventions;
using GraphQL.DataLoader;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;
using System.Collections.Generic;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class BeneficiaryTypeGraphType
    {
        private readonly BeneficiaryType beneficiaryType;
        public Id Id => beneficiaryType.GetIdentifier();
        public NonNull<string> Name => beneficiaryType.Name;
        public NonNull<string[]> Keys => beneficiaryType.GetKeys();

        public BeneficiaryTypeGraphType(BeneficiaryType beneficiaryType)
        {
            this.beneficiaryType = beneficiaryType;
        }

        public IDataLoaderResult<ProjectGraphType> Project(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadProject(beneficiaryType.ProjectId);
        }

        public IDataLoaderResult<IEnumerable<BeneficiaryGraphType>> Beneficiaries(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadBeneficiariesByBeneficiaryType(Id.LongIdentifierForType<BeneficiaryType>());
        }
    }
}
