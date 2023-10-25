using MediatR;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Plugins.GraphQL;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.Services.Mailer;
using Microsoft.AspNetCore.Identity;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Gql.Interfaces;
using GraphQL.Conventions;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.DbModel.Entities.Projects;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.Requests.Queries.Projects;
using System.Security.Claims;
using Sig.App.Backend.Constants;
using NodaTime;
using System.Linq;
using System;

namespace Sig.App.Backend.Requests.Commands.Mutations.Projects
{
    public class DeleteProject : AsyncRequestHandler<DeleteProject.Input>
    {
        private readonly ILogger<DeleteProject> logger;
        private readonly IMailer mailer;
        private readonly UserManager<AppUser> userManager;
        private readonly AppDbContext db;
        private readonly IClock clock;
        private readonly IMediator mediator;

        public DeleteProject(ILogger<DeleteProject> logger, AppDbContext db, UserManager<AppUser> userManager, IMailer mailer, IMediator mediator, IClock clock)
        {
            this.logger = logger;
            this.userManager = userManager;
            this.mailer = mailer;
            this.db = db;
            this.mediator = mediator;
            this.clock = clock;
        }

        protected override async Task Handle(Input request, CancellationToken cancellationToken)
        {
            var projectId = request.ProjectId.LongIdentifierForType<Project>();
            var project = await db.Projects
                .Include(x => x.Markets)
                .Include(x => x.ProductGroups).ThenInclude(x => x.Types)
                .Include(x => x.Subscriptions).ThenInclude(x => x.Beneficiaries)
                .Include(x => x.Organizations).ThenInclude(x => x.Beneficiaries)
                .Include(x => x.Cards).ThenInclude(x => x.Transactions)
                .Include(x => x.Cards).ThenInclude(x => x.Funds)
                .FirstOrDefaultAsync(x => x.Id == projectId, cancellationToken);

            if (project == null) throw new ProjectNotFoundException();

            if (HaveAnyActiveSubscription(project))
            {
                throw new ProjectCantHaveActiveSubscription();
            }

            var projectManagers = await mediator.Send(new GetProjectProjectManagers.Query
            {
                ProjectId = projectId
            });

            if (projectManagers != null) {
                foreach (var manager in projectManagers)
                {
                    await userManager.RemoveClaimAsync(manager, new Claim(AppClaimTypes.ProjectManagerOf, projectId.ToString()));
                }
            }

            db.Transactions.RemoveRange(project.Cards.SelectMany(x => x.Transactions));
            db.Cards.RemoveRange(project.Cards);
            db.SubscriptionBeneficiaries.RemoveRange(project.Subscriptions.SelectMany(x => x.Beneficiaries));
            db.Beneficiaries.RemoveRange(project.Organizations.SelectMany(x => x.Beneficiaries));
            db.SubscriptionTypes.RemoveRange(project.ProductGroups.SelectMany(x => x.Types));
            db.Funds.RemoveRange(project.Cards.SelectMany(x => x.Funds));
            db.ProductGroups.RemoveRange(project.ProductGroups);
            db.Subscriptions.RemoveRange(project.Subscriptions);
            db.ProjectMarkets.RemoveRange(project.Markets);
            db.Organizations.RemoveRange(project.Organizations);

            db.Projects.Remove(project);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (Exception error)
            {
                var test = 0;
            }
            logger.LogInformation($"Project deleted ({projectId}, {project.Name})");
        }

        private bool HaveAnyActiveSubscription(Project project)
        {
            var haveAnyActiveSubscription = false;
            var today = clock
                .GetCurrentInstant()
                .ToDateTimeUtc();

            foreach (var subscription in project.Subscriptions)
            {
                if (subscription.StartDate <= today && subscription.EndDate >= today)
                {
                    haveAnyActiveSubscription = true;
                }
            }

            return haveAnyActiveSubscription;
        }

        [MutationInput]
        public class Input : IRequest, IHaveProjectId
        {
            public Id ProjectId { get; set; }
        }

        public class ProjectNotFoundException : RequestValidationException { }
        public class ProjectCantHaveActiveSubscription : RequestValidationException { }
    }
}
