using MediatR;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Services.Kiosk;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.CashRegisters
{
    public class ValidateKioskSession : IRequestHandler<ValidateKioskSession.Input, bool>
    {
        private readonly AppDbContext db;
        private readonly KioskJwtService kioskJwtService;

        public ValidateKioskSession(AppDbContext db, KioskJwtService kioskJwtService)
        {
            this.db = db;
            this.kioskJwtService = kioskJwtService;
        }

        public async Task<bool> Handle(Input request, CancellationToken cancellationToken)
        {
            await kioskJwtService.ResolveFromAuthToken(db, request.KioskToken, cancellationToken);
            return true;
        }

        public class Input : IRequest<bool>
        {
            public string KioskToken { get; set; }
        }
    }
}
