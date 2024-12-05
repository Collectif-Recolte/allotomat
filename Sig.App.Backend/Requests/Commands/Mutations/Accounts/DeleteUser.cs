using MediatR;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Plugins.GraphQL;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Extensions;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.DbModel.Enums;

namespace Sig.App.Backend.Requests.Commands.Mutations.Markets
{
    public class DeleteUser : IRequestHandler<DeleteUser.Input>
    {
        private readonly ILogger<DeleteUser> logger;
        private readonly UserManager<AppUser> userManager;
        private readonly AppDbContext db;
        private readonly IMediator mediator;
            
        public DeleteUser(ILogger<DeleteUser> logger, AppDbContext db, UserManager<AppUser> userManager, IMediator mediator)
        {
            this.logger = logger;
            this.userManager = userManager;
            this.db = db;
            this.mediator = mediator;
        }

        public async Task Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] DeleteUser({request.UserId})");
            var userId = request.UserId.StringIdentifierForType<AppUser>();
            var user = await db.Users.Include(x => x.Profile).FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

            if (user == null)
            {
                logger.LogWarning("[Mutation] DeleteUser - UserNotFoundException");
                throw new UserNotFoundException();
            }

            if (user.Status != UserStatus.Disabled)
            {
                logger.LogWarning("[Mutation] DeleteUser - UserNotDisabledException");
                throw new UserNotDisabledException();
            }

            var claims = await userManager.GetClaimsAsync(user);
            await userManager.RemoveClaimsAsync(user, claims);
            await userManager.DeleteAsync(user);

            await db.SaveChangesAsync();
            logger.LogInformation($"[Mutation] DeleteUser - User deleted ({userId}, {user.Profile.FirstName} {user.Profile.LastName})");
        }

        [MutationInput]
        public class Input : HaveUserId, IRequest { }

        public class UserNotFoundException : RequestValidationException { }
        public class UserNotDisabledException : RequestValidationException { }
    }
}
