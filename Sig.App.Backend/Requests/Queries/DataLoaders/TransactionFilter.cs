using NodaTime;

namespace Sig.App.Backend.Requests.Queries.DataLoaders
{
    public record TransactionFilter(Instant StartDate, Instant EndDate, long[] CashRegisterIds);
}
