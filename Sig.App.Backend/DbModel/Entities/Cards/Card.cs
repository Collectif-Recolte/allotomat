using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sig.App.Backend.DbModel.Entities.Cards
{
    public class Card : IHaveLongIdentifier
    {
        public long Id { get; set; }

        public long ProjectId { get; set; }
        public Project Project { get; set; }

        public IList<Fund> Funds { get; set; }

        public CardStatus Status { get; set; }
        public bool IsDisabled { get; set; } = false;

        public Beneficiary Beneficiary { get; set; }

        public IList<Transaction> Transactions { get; set; }

        public long ProgramCardId { get; set; }
        public string CardNumber { get; set; }

        public decimal TotalFund()
        {
            return TotalSubscriptionFund() + LoyaltyFund();
        }

        public decimal TotalSubscriptionFund()
        {
            return Funds.Where(x => x.ProductGroup.Name != ProductGroupType.LOYALTY).Sum(x => x.Amount);
        }

        public decimal LoyaltyFund()
        {
            var loyaltyFund = Funds.FirstOrDefault(x => x.ProductGroup.Name == ProductGroupType.LOYALTY);
            if (loyaltyFund != null)
            {
                return loyaltyFund.Amount;
            }
            return 0;
        }
    }
}
