using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using System.Collections.Generic;

namespace Sig.App.Backend.DbModel.Entities.Beneficiaries
{
    public class Beneficiary : IHaveLongIdentifier
    {
        public long Id { get; set; }

        public long OrganizationId { get; set; }
        public Organization Organization { get; set; }
        
        public long? CardId { get; set; }
        public Card Card { get; set; }

        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string Notes { get; set; }
        public string PostalCode { get; set; }
        public string ID1 { get; set; }
        public string ID2 { get; set; }

        public long? BeneficiaryTypeId { get; set; }
        public BeneficiaryType? BeneficiaryType { get; set; }

        public IList<SubscriptionBeneficiary> Subscriptions { get; set; }

        public long SortOrder { get; set; }
    }
}
