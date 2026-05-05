using GraphQL.Conventions;
using NodaTime;
using NodaTime.Extensions;
using Sig.App.Backend.DbModel.Entities.CashRegisters;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;
using System.Linq;

namespace Sig.App.Backend.Gql.Schema.GraphTypes;

public static class TransactionGraphTypeHelper
{
    public static bool IsTransactionBetweenDate(ITransactionGraphType x, Instant startInstant, Instant endInstant)
    {
        var createdAtInstant = x.CreatedAt().ToInstant();
        return startInstant <= createdAtInstant && createdAtInstant < endInstant;
    }

    public static bool IsTransactionInCashRegister(ITransactionGraphType x, Id[] cashRegisters)
    {
        if (cashRegisters.Length > 0)
        {
            var cashRegisterIds = cashRegisters.Select(cr => cr.LongIdentifierForType<CashRegister>());
            if (x is RefundTransactionGraphType rtgt && cashRegisterIds.Any(id => id == rtgt.CashRegisterId))
                return true;
            if (x is PaymentTransactionGraphType ptgt && cashRegisterIds.Any(id => id == ptgt.CashRegisterId))
                return true;
            return false;
        }
        return true;
    }
}
