using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Organizations;
using System;

namespace Sig.App.Backend.DbModel.Entities.Transactions
{
    public interface ITransaction
    {
        long Id { get; set; }

        long? CardId { get; set; }
        Card Card { get; set; }

        long? BeneficiaryId { get; set; }
        Beneficiary Beneficiary { get; set; }

        long? OrganizationId { get; set; }
        Organization Organization { get; set; }

        decimal Amount { get; set; }
        DateTime CreatedAtUtc { get; set; }
    }
}
