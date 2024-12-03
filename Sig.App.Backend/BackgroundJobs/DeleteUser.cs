using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NodaTime;
using Sig.App.Backend.DbModel;
using System.Linq;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Services.Mailer;
using Sig.App.Backend.EmailTemplates.Models;
using DocumentFormat.OpenXml.ExtendedProperties;
using Microsoft.AspNetCore.Identity;
using Sig.App.Backend.Constants;
using System.Security.Claims;
using Sig.App.Backend.DbModel.Entities;
using MediatR;
using Sig.App.Backend.Requests.Commands.Mutations.Projects;
using Sig.App.Backend.Requests.Commands.Mutations.Markets;
using Sig.App.Backend.Requests.Commands.Mutations.Organizations;
using Sig.App.Backend.Requests.Commands.Mutations.MarketGroups;
using GraphQL.Conventions;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.DbModel.Entities.Organizations;

namespace Sig.App.Backend.BackgroundJobs
{
    public class DeleteUser
    {
        private readonly AppDbContext db;
        private readonly IClock clock;
        private readonly ILogger<DeleteUser> logger;
        private readonly IMailer mailer;
        private readonly UserManager<AppUser> userManager;
        private readonly IMediator mediator;

        public DeleteUser(AppDbContext db, IClock clock, ILogger<DeleteUser> logger, IMailer mailer, UserManager<AppUser> userManager, IMediator mediator)
        {
            this.db = db;
            this.clock = clock;
            this.logger = logger;
            this.mailer = mailer;
            this.userManager = userManager;
            this.mediator = mediator;
        }

        public static void RegisterJob(IConfiguration config)
        {
            RecurringJob.AddOrUpdate<DeleteUser>("DeleteUser",
                x => x.Run(),
                Cron.Daily(),
                new RecurringJobOptions() { TimeZone = TimeZoneInfo.FindSystemTimeZoneById(config["systemLocalTimezone"]) });
        }

        public async Task Run()
        {
            var today = clock.GetCurrentInstant().ToDateTimeUtc();

            var fy = today.AddYears(-5);
            var fyem = today.AddYears(-4).AddMonths(-11);
            var fyemtw = today.AddYears(-4).AddMonths(-11).AddDays(-14);

            var usersToDelete = await db.Users.Include(x => x.Profile)
                .Where(x => (x.LastAccessTimeUtc.HasValue && x.LastAccessTimeUtc.Value < fy || !x.LastAccessTimeUtc.HasValue && x.CreatedAtUtc < fy) && x.State == UserState.ReminderSentPendingDeletion)
                .ToListAsync();
            var userToRemind = await db.Users.Include(x => x.Profile)
                .Where(x => (x.LastAccessTimeUtc.HasValue && x.LastAccessTimeUtc.Value < fyemtw || !x.LastAccessTimeUtc.HasValue && x.CreatedAtUtc < fyemtw) && x.State == UserState.InactivePendingDeletion)
                .ToListAsync();
            var userToDeactivate = await db.Users.Include(x => x.Profile)
                .Where(x => (x.LastAccessTimeUtc.HasValue && x.LastAccessTimeUtc.Value < fyem || !x.LastAccessTimeUtc.HasValue && x.CreatedAtUtc < fyem) && x.State == UserState.Active)
            .ToListAsync();

            foreach (var user in userToDeactivate)
            {
                var date = user.LastAccessTimeUtc.HasValue ? user.LastAccessTimeUtc.Value : user.CreatedAtUtc;
                var email = new DeactivateUserEmail(user.Email, $"{user.Profile.FirstName} {user.Profile.LastName}", date.AddYears(5));
                await mailer.Send(email);
                user.State = UserState.InactivePendingDeletion;
            }

            foreach (var user in userToRemind)
            {
                var date = user.LastAccessTimeUtc.HasValue ? user.LastAccessTimeUtc.Value : user.CreatedAtUtc;
                var email = new DeactivateUserReminderEmail(user.Email, $"{user.Profile.FirstName} {user.Profile.LastName}", date.AddYears(5));
                await mailer.Send(email);
                user.State = UserState.ReminderSentPendingDeletion;
            }

            foreach (var user in usersToDelete)
            {
                var email = new DeleteUserEmail(user.Email, $"{user.Profile.FirstName} {user.Profile.LastName}", today);
                await mailer.Send(email);

                var transactionLogsToAnonymized = await db.TransactionLogs.Where(x => x.TransactionInitiatorId == user.Id).ToListAsync();
                foreach (var transactionLog in transactionLogsToAnonymized)
                {
                    transactionLog.TransactionInitiatorId = "-Anonymized-";
                    transactionLog.TransactionInitiatorEmail = "-Anonymized-";
                    transactionLog.TransactionInitiatorFirstname = "-Anonymized-";
                    transactionLog.TransactionInitiatorLastname = "-Anonymized-";
                }

                var userClaims = await userManager.GetClaimsAsync(user);
                foreach (var claim in userClaims)
                {
                    if (claim.Type == AppClaimTypes.ProjectManagerOf || claim.Type == AppClaimTypes.MarketManagerOf || claim.Type == AppClaimTypes.OrganizationManagerOf || claim.Type == AppClaimTypes.MarketGroupManagerOf)
                    {
                        var usersWithClaim = await userManager.GetUsersForClaimAsync(claim);
                        if (usersWithClaim.Count == 1)
                        {
                            switch (claim.Type)
                            {
                                case AppClaimTypes.ProjectManagerOf:
                                {
                                    await mediator.Send(new DeleteProject.Input() { ProjectId = Id.New<Project>(claim.Value) });
                                    break;
                                }
                                case AppClaimTypes.MarketManagerOf:
                                {
                                    await mediator.Send(new DeleteMarket.Input() { MarketId = Id.New<Market>(claim.Value) });
                                    break;
                                }
                                case AppClaimTypes.OrganizationManagerOf:
                                {
                                    await mediator.Send(new DeleteOrganization.Input() { OrganizationId = Id.New<Organization>(claim.Value) });
                                    break;
                                }
                                case AppClaimTypes.MarketGroupManagerOf:
                                {
                                    await mediator.Send(new DeleteMarketGroup.Input() { MarketGroupId = Id.New<MarketGroup>(claim.Value) });
                                    break;
                                }
                            }
                        }
                    }
                    await userManager.RemoveClaimAsync(user, claim);
                }

                await userManager.DeleteAsync(user);
            }

            await db.SaveChangesAsync();
        }
    }
}
