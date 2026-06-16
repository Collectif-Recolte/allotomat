using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.CashRegisters;
using Sig.App.Backend.DbModel.Entities.Markets;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Services.Kiosk
{
    public class ResolvedKioskCashRegister
    {
        public CashRegister CashRegister { get; set; }
        public Market Market { get; set; }
        public bool TokenFound { get; set; }
        public bool IsOperational { get; set; }
        public bool MarketIsDisabled { get; set; }
    }

    public static class KioskCashRegisterResolver
    {
        public static async Task<ResolvedKioskCashRegister> Resolve(AppDbContext db, string token, CancellationToken cancellationToken)
        {
            var notFound = new ResolvedKioskCashRegister { TokenFound = false, IsOperational = false };

            if (string.IsNullOrWhiteSpace(token))
            {
                return notFound;
            }

            var cashRegister = await db.CashRegisters
                .Include(x => x.Market)
                .Include(x => x.MarketGroups)
                .FirstOrDefaultAsync(x => x.KioskAccessToken == token, cancellationToken);

            if (cashRegister == null)
            {
                return notFound;
            }

            var tokenFound = !string.IsNullOrEmpty(cashRegister.KioskAccessToken);
            var hasActiveMarketGroups = cashRegister.MarketGroups.Any();
            var isOperational = tokenFound
                && !cashRegister.IsArchived
                && !cashRegister.Market.IsArchived
                && hasActiveMarketGroups;

            return new ResolvedKioskCashRegister
            {
                CashRegister = cashRegister,
                Market = cashRegister.Market,
                TokenFound = tokenFound,
                IsOperational = isOperational,
                MarketIsDisabled = cashRegister.Market.IsDisabled
            };
        }
    }
}
