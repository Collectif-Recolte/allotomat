﻿using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using System.Collections.Generic;

namespace Sig.App.Backend.DbModel.Entities.Transactions
{
    public class ManuallyAddingFundTransaction : AddingFundTransaction
    {
        public long SubscriptionId { get; set; }
        public Subscription Subscription { get; set; }
        public IList<AddingFundTransaction> AffectedNegativeFundTransactions { get; set; }
    }
}
