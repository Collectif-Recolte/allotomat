using GraphQL.Conventions;
using GraphQL.DataLoader;
using NodaTime;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;
using System.Collections.Generic;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class OffPlatformBeneficiaryGraphType : IBeneficiaryGraphType
    {
        protected readonly OffPlatformBeneficiary beneficiary;

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
        public SubscriptionMonthlyPaymentMoment? MonthlyPaymentMoment => beneficiary.MonthlyPaymentMoment;
        public bool IsActive => beneficiary.IsActive;

        public OffPlatformBeneficiaryGraphType(OffPlatformBeneficiary beneficiary)
        {
            this.beneficiary = beneficiary;
        }

        public IDataLoaderResult<OrganizationGraphType> Organization(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadOrganization(beneficiary.OrganizationId);
        }

        public IDataLoaderResult<BeneficiaryTypeGraphType> BeneficiaryType(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadBeneficiaryTypeByBeneficiaryId(Id.LongIdentifierForType<OffPlatformBeneficiary>());
        }

        public IDataLoaderResult<IEnumerable<BeneficiarySubscriptionTypeGraphType>> BeneficiarySubscriptions(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadBeneficiarySubscriptionsGraphType(Id.LongIdentifierForType<OffPlatformBeneficiary>());
        }

        public IDataLoaderResult<CardGraphType> Card(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadBeneficiaryCard(Id.LongIdentifierForType<OffPlatformBeneficiary>());
        }

        public OffsetDateTime? StartDate()
        {
            if (beneficiary.StartDate.HasValue)
            {
                return beneficiary.StartDate.Value.FromUtcToOffsetDateTime();
            }

            return null;
        }

        public OffsetDateTime? EndDate()
        {
            if (beneficiary.EndDate.HasValue)
            {
                return beneficiary.EndDate.Value.FromUtcToOffsetDateTime();
            }

            return null;
        }

        public IDataLoaderResult<IEnumerable<PaymentFundGraphType>> Funds(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadPaymentFundsByBeneficiary(Id.LongIdentifierForType<OffPlatformBeneficiary>());
        }
    }
}
