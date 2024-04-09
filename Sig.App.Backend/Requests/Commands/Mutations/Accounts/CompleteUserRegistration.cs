using GraphQL.Conventions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.Profiles;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Schema.Enums;

namespace Sig.App.Backend.Requests.Commands.Mutations.Accounts
{
    public class CompleteUserRegistration : IRequestHandler<CompleteUserRegistration.Input, CompleteUserRegistration.Payload>
    {
        protected readonly AppDbContext dbContext;
        private readonly UserManager<AppUser> userManager;
        private readonly ILogger<CompleteUserRegistration> logger;

        public CompleteUserRegistration(AppDbContext dbContext, UserManager<AppUser> userManager, ILogger<CompleteUserRegistration> logger)
        {
            this.dbContext = dbContext;
            this.userManager = userManager;
            this.logger = logger;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] CompleteUserRegistration({request.UserId}, {request.FirstName}, {request.LastName}, {request.EmailAddress})");
            var userType = request.TokenType == TokenType.AdminInvitation ? UserType.PCAAdmin :
                request.TokenType == TokenType.ProjectManagerInvitation ? UserType.ProjectManager :
                request.TokenType == TokenType.MerchantInvitation ? UserType.Merchant : UserType.OrganizationManager;

            var tokenPurpose = request.TokenType == TokenType.AdminInvitation ? TokenPurposes.AdminInvite :
                request.TokenType == TokenType.ProjectManagerInvitation ? TokenPurposes.ProjectManagerInvite :
                request.TokenType == TokenType.MerchantInvitation ? TokenPurposes.MerchantInvite : TokenPurposes.OrganizationManagerInvite;

            var user = await userManager.FindByEmailAsync(request.EmailAddress);
            if (user == null)
            {
                logger.LogWarning("[Mutation] CompleteUserRegistration - UserNotFoundException");
                throw new UserNotFoundException();
            }

            if (user.Type != userType)
            {
                logger.LogWarning("[Mutation] CompleteUserRegistration - UserNotCorrectTypeException");
                throw new UserNotCorrectTypeException();
            }

            var tokenValid = await userManager.VerifyUserTokenAsync(user, TokenProviders.EmailInvites, tokenPurpose, request.InviteToken);
            if (tokenValid == false)
            {
                logger.LogWarning("[Mutation] CompleteUserRegistration - InvalidInviteTokenException");
                throw new InvalidInviteTokenException();
            }

            var identityResult = await userManager.AddPasswordAsync(user, request.Password);
            identityResult.AssertSuccess();

            user.EmailConfirmed = true;
            await userManager.UpdateAsync(user);

            var profile = await GetProfile(user.Id, cancellationToken);
            profile.FirstName = request.FirstName.Trim();
            profile.LastName = request.LastName.Trim();
            await dbContext.SaveChangesAsync();

            logger.LogInformation($"[Mutation] CompleteUserRegistration - {userType} registration completed for {user.Email}");

            return new Payload
            {
                User = new UserGraphType(user)
            };
        }

        private Task<UserProfile> GetProfile(string userId, CancellationToken cancellationToken)
        {
            return dbContext.UserProfiles
                .OfType<UserProfile>()
                .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        }

        [MutationInput]
        public class Input : IRequest<Payload>
        {
            public Id UserId { get; set; }
            public NonNull<string> EmailAddress { get; set; }
            public NonNull<string> Password { get; set; }
            public NonNull<string> InviteToken { get; set; }
            public NonNull<string> FirstName { get; set; }
            public NonNull<string> LastName { get; set; }
            public TokenType TokenType { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public NonNull<UserGraphType> User { get; set; }
        }

        public class UserNotFoundException : RequestValidationException { }
        public class UserNotCorrectTypeException : RequestValidationException { }
        public class InvalidInviteTokenException : RequestValidationException { }
    }
}
