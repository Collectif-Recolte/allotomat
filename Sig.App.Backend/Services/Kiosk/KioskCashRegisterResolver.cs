using System.Diagnostics.CodeAnalysis;
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

        public bool CanBeUsed([NotNullWhen(false)] out string reason)
        {
            reason = null;
            
            if (!TokenFound) reason = "Token not found"; 
            else if (!IsOperational) reason = "Cash register is not operational";
            else if (MarketIsDisabled) reason = "Market is disabled";
            
            return reason == null;
        }
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
                .Include(x => x.MarketGroups).ThenInclude(x => x.MarketGroup).ThenInclude(x => x.Project)
                .FirstOrDefaultAsync(x => x.KioskAccessToken == token, cancellationToken);

            if (cashRegister == null)
            {
                return notFound;
            }

            var tokenFound = !string.IsNullOrEmpty(cashRegister.KioskAccessToken);
            var hasActiveMarketGroups = cashRegister.MarketGroups.Any(x => !x.MarketGroup.IsArchived);
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
