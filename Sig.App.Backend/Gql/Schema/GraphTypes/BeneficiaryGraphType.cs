using GraphQL.Conventions;
using GraphQL.DataLoader;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;
using System.Collections.Generic;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class BeneficiaryGraphType : IBeneficiaryGraphType
    {
        protected readonly Beneficiary beneficiary;

        public Id Id => beneficiary.GetIdentifier();
        public NonNull<string> Firstname => beneficiary.Firstname;
        public NonNull<string> Lastname => beneficiary.Lastname;
        public string Email => beneficiary.Email;
        public string Phone => beneficiary.Phone;
        public string Address => beneficiary.Address;
        public string Notes => beneficiary.Notes;
        public string Id1 => beneficiary.ID1;
        public string Id2 => beneficiary.ID2;
        public string PostalCode => beneficiary.PostalCode;

        public BeneficiaryGraphType(Beneficiary beneficiary)
        {
            this.beneficiary = beneficiary;
        }

        public IDataLoaderResult<OrganizationGraphType> Organization(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadOrganization(beneficiary.OrganizationId);
        }

        public IDataLoaderResult<BeneficiaryTypeGraphType> BeneficiaryType(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadBeneficiaryTypeByBeneficiaryId(Id.LongIdentifierForType<Beneficiary>());
        }

        public IDataLoaderResult<IEnumerable<SubscriptionGraphType>> Subscriptions(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadBeneficiarySubscriptions(Id.LongIdentifierForType<Beneficiary>());
        }

        public IDataLoaderResult<CardGraphType> Card(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadBeneficiaryCard(Id.LongIdentifierForType<Beneficiary>());
        }
    }
}
