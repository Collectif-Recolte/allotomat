using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.EmailTemplates.Models;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Services.Mailer;
using System.Linq;
using System;

namespace Sig.App.Backend.Requests.Commands.Mutations.Accounts
{
    public class ResendConfirmationEmail : IRequestHandler<ResendConfirmationEmail.Input>
    {
        private readonly AppDbContext db;
        private readonly UserManager<AppUser> userManager;
        private readonly IMailer mailer;
        private readonly ILogger<ResendConfirmationEmail> logger;

        public ResendConfirmationEmail(AppDbContext db, UserManager<AppUser> userManager, IMailer mailer, ILogger<ResendConfirmationEmail> logger)
        {
            this.db = db;
            this.userManager = userManager;
            this.mailer = mailer;
            this.logger = logger;
        }

        public async Task Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] ResendConfirmationEmail({request.Email})");
            var user = await db.Users
                .Include(x => x.Profile)
                .FirstOrDefaultAsync(x => x.Email == request.Email, cancellationToken);

            if (user == null || user.EmailConfirmed)
            {
                logger.LogWarning("[Mutation] ResendConfirmationEmail - NoNeedToConfirmException");
                throw new NoNeedToConfirmException();
            }

            logger.LogInformation($"User {user.Email} requested new email confirmation token.");

            if (user.Type == UserType.PCAAdmin)
            {
                var token = await userManager.GenerateUserTokenAsync(user, TokenProviders.EmailInvites, TokenPurposes.AdminInvite);
                await mailer.Send(new AdminInviteEmail(request.Email, token, user.Profile.FirstName));
            }
            else if (user.Type == UserType.ProjectManager)
            {
                var claims = await userManager.GetClaimsAsync(user);
                var projectClaim = claims.First(x => x.Type == AppClaimTypes.ProjectManagerOf);
                var project = await db.Projects.Where(x => x.Id == Convert.ToInt64(projectClaim.Value)).FirstAsync();

                await mailer.Send(new ProjectManagerInviteEmail(user.Email)
                {
                    InviteToken = await userManager.GenerateUserTokenAsync(user, TokenProviders.EmailInvites, TokenPurposes.ProjectManagerInvite),
                    ProjectName = project.Name
                });
            }
            else if (user.Type == UserType.OrganizationManager)
            {
                var claims = await userManager.GetClaimsAsync(user);
                var organizationClaim = claims.First(x => x.Type == AppClaimTypes.OrganizationManagerOf);
                var organization = await db.Organizations.Where(x => x.Id == Convert.ToInt64(organizationClaim.Value)).FirstAsync();

                await mailer.Send(new OrganizationManagerInviteEmail(user.Email)
                {
                    InviteToken = await userManager.GenerateUserTokenAsync(user, TokenProviders.EmailInvites, TokenPurposes.OrganizationManagerInvite),
                    OrganizationName = organization.Name
                });
            }
            else if (user.Type == UserType.Merchant)
            {
                var claims = await userManager.GetClaimsAsync(user);
                var marketClaim = claims.First(x => x.Type == AppClaimTypes.MarketManagerOf);
                var market = await db.Markets.Where(x => x.Id == Convert.ToInt64(marketClaim.Value)).FirstAsync();
                
                await mailer.Send(new MarketManagerInviteEmail(user.Email)
                {
                    InviteToken = await userManager.GenerateUserTokenAsync(user, TokenProviders.EmailInvites, TokenPurposes.MerchantInvite),
                    MarketName = market.Name
                });
            }
            else
            {
                var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                await mailer.Send(new ConfirmEmailEmail(user.Email, token, user.Profile.FirstName));
            }
        }

        [MutationInput]
        public class Input : IRequest
        {
            public string Email { get; set; }
        }

        public abstract class ResendConfirmationEmailException : RequestValidationException { }
        public class NoNeedToConfirmException : ResendConfirmationEmailException { }
    }
}
