using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Transactions;

namespace Sig.App.Backend.Helpers;

public static class TransactionHelper
{
    public static async Task<IEnumerable<IGrouping<long, AddingFundTransaction>>> GroupAddingFundTransactionsBySubscriptionId(AppDbContext db, IEnumerable<AddingFundTransaction> addingFundTransactions, CancellationToken cancellationToken)
    {
        var saftSubscriptionTypes = await db.SubscriptionTypes
            .Include(x => x.Subscription)
            .Where(x => addingFundTransactions.OfType<SubscriptionAddingFundTransaction>().Select(x => x.SubscriptionTypeId)
                .Contains(x.Id)).ToListAsync(cancellationToken: cancellationToken);
        
        return addingFundTransactions.GroupBy(x =>
        {
            if (x is ManuallyAddingFundTransaction maft)
                return maft.SubscriptionId;
            if (x is SubscriptionAddingFundTransaction saft)
                return saftSubscriptionTypes.First(y => y.Id == saft.SubscriptionTypeId).SubscriptionId;
            return -1;
        });
    }

    public static DateTime? GetNearestExpirationDate(IList<Transaction> transactions)
    {
        var target = new DateTime(2024, 6, 25);
        var dates = transactions.Where(x => x is AddingFundTransaction).Select(x => (x as AddingFundTransaction).ExpirationDate);
        if (dates.Count() > 0)
        {
            return dates.Aggregate((x, y) => Math.Abs((x - target).Ticks) < Math.Abs((y - target).Ticks) ? x : y);
        }

        return null;
    }

    public static string CreateTransactionUniqueId()
    {
        return Guid.NewGuid().ToString();
    }
}