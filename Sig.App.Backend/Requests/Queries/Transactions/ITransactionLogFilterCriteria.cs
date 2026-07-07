using System;
using System.Collections.Generic;
using GraphQL.Conventions;
using Sig.App.Backend.Gql.Schema.Types;

namespace Sig.App.Backend.Requests.Queries.Transactions;

public interface ITransactionLogFilterCriteria
{
    Id ProjectId { get; set; }
    DateTime StartDate { get; set; }
    DateTime EndDate { get; set; }
    IEnumerable<Id> Organizations { get; }
    IEnumerable<Id> Subscriptions { get; }
    Maybe<bool> WithoutSubscription { get; }
    IEnumerable<Id> Categories { get; }
    IEnumerable<Id> Markets { get; }
    IEnumerable<Id> MarketGroups { get; }
    IEnumerable<string> TransactionTypes { get; }
    IEnumerable<string> GiftCardTransactionTypes { get; }
    Maybe<string> SearchText { get; }
}