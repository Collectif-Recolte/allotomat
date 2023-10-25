using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Organizations;
using System;

namespace Sig.App.Backend.DbModel.Entities.Transactions
{
    public abstract class Transaction : IHaveLongIdentifier, ITransaction
    {
        public long Id { get; set; }
        public string TransactionUniqueId { get; set; }

        public string Discriminator { get; private set; }

        public long? CardId { get; set; }
        public Card Card { get; set; }
        
        public long? BeneficiaryId { get; set; }
        public Beneficiary Beneficiary { get; set; }

        public long? OrganizationId { get; set; }
        public Organization Organization { get; set; }

        public decimal Amount { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
