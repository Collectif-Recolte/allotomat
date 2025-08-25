using System.Collections.Generic;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class SubscriptionEndReportGraphType
    {
        public OrganizationGraphType Organization { get; set; }
        public IEnumerable<SubscriptionEndTransactionGraphType> SubscriptionEndTransactions { get; set; }
    }
}