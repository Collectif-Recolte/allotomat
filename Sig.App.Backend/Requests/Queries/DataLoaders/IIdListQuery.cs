using NodaTime;
using System.Collections.Generic;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public interface IIdListQuery<TId>
    {
        IEnumerable<TId> Ids { get; set; }
    }

    public interface IHaveGroup<TGroup>
    {
        TGroup Group { get; set; }
    }

    public record TransactionFilter(Instant StartDate, Instant EndDate, long[] CashRegisterIds);
}