using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Plugins.MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Queries.Cards
{
    public class ForecastNextUnassignedCard : IRequestHandler<ForecastNextUnassignedCard.Input, long>
    {
        private readonly AppDbContext db;

        public ForecastNextUnassignedCard(AppDbContext db)
        {
            this.db = db;
        }

        public async Task<long> Handle(Input request, CancellationToken cancellationToken)
        {
            var projectId = request.ProjectId.LongIdentifierForType<Project>();
            var project = await db.Projects.FirstOrDefaultAsync(x => x.Id == projectId);

            if (project == null) throw new ProjectNotFoundException();

            var card = await db.Cards.Where(x => x.ProjectId == projectId && x.Status == DbModel.Enums.CardStatus.Unassigned).FirstOrDefaultAsync();
            if (card == null) throw new NoUnassignedCardAvailableException();

            return card.ProgramCardId;
        }

        public class Input : IRequest<long>
        {
            public Id ProjectId { get; set; }
        }

        public class ProjectNotFoundException : RequestValidationException { }
        public class NoUnassignedCardAvailableException : RequestValidationException { }
    }
}
