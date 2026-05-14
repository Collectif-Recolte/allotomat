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
        private const string Anonymous = "*******";

        protected readonly Beneficiary beneficiary;
        protected readonly bool beneficiariesAreAnonymous;

        public Id Id => beneficiary.GetIdentifier();
        public NonNull<string> Firstname => beneficiariesAreAnonymous ? Anonymous : beneficiary.Firstname;
        public NonNull<string> Lastname => beneficiariesAreAnonymous ? Anonymous : beneficiary.Lastname;
        public string Email => beneficiariesAreAnonymous ? Anonymous : beneficiary.Email;
        public string Phone => beneficiariesAreAnonymous ? Anonymous : beneficiary.Phone;
        public string Address => beneficiariesAreAnonymous ? Anonymous : beneficiary.Address;
        public string Notes => beneficiariesAreAnonymous ? Anonymous : beneficiary.Notes;
        public string Id1 => beneficiary.ID1;
        public string Id2 => beneficiary.ID2;
        public string PostalCode => beneficiariesAreAnonymous ? Anonymous : beneficiary.PostalCode;
        public bool IsUnsubscribeToReceipt => beneficiary.IsUnsubscribeToReceipt;

        // beneficiariesAreAnonymous is set to true by default to avoid exposing beneficiary data in the beneficiary
        public BeneficiaryGraphType(Beneficiary beneficiary, bool beneficiariesAreAnonymous = true)
        {
            this.beneficiary = beneficiary;
            this.beneficiariesAreAnonymous = beneficiariesAreAnonymous;
        }

        public IDataLoaderResult<OrganizationGraphType> Organization(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadOrganization(beneficiary.OrganizationId);
        }

        public IDataLoaderResult<BeneficiaryTypeGraphType> BeneficiaryType(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadBeneficiaryTypeByBeneficiaryId(Id.LongIdentifierForType<Beneficiary>());
        }

        public IDataLoaderResult<IEnumerable<BeneficiarySubscriptionTypeGraphType>> BeneficiarySubscriptions(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadBeneficiarySubscriptionsGraphType(Id.LongIdentifierForType<Beneficiary>());
        }

        public IDataLoaderResult<CardGraphType> Card(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadBeneficiaryCard(Id.LongIdentifierForType<Beneficiary>());
        }
    }
}
