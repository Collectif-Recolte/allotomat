using MediatR;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Services.Kiosk;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.CashRegisters
{
    public class GetCashRegisterByKioskToken : IRequestHandler<GetCashRegisterByKioskToken.Input, KioskCashRegisterInfoGraphType>
    {
        private readonly AppDbContext db;

        public GetCashRegisterByKioskToken(AppDbContext db)
        {
            this.db = db;
        }

        public async Task<KioskCashRegisterInfoGraphType> Handle(Input request, CancellationToken cancellationToken)
        {
            var resolved = await KioskCashRegisterResolver.Resolve(db, request.Token, cancellationToken);

            if (!resolved.TokenFound)
            {
                return new KioskCashRegisterInfoGraphType
                {
                    IsValid = false
                };
            }

            var programNames = resolved.CashRegister.MarketGroups
                .Where(x => x.MarketGroup != null && !x.MarketGroup.IsArchived && x.MarketGroup.Project != null)
                .GroupBy(x => x.MarketGroup.ProjectId)
                .Select(g => g.First().MarketGroup.Project.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .OrderBy(name => name)
                .ToList();

            return new KioskCashRegisterInfoGraphType
            {
                IsValid = resolved.IsOperational && !resolved.MarketIsDisabled,
                CashRegisterName = resolved.CashRegister.Name,
                MarketIsDisabled = resolved.MarketIsDisabled,
                ProgramNames = programNames
            };
        }

        public class Input : IRequest<KioskCashRegisterInfoGraphType>
        {
            public string Token { get; set; }
        }
    }
}
