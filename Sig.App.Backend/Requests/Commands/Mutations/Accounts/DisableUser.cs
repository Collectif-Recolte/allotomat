using MediatR;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.Plugins.GraphQL;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Extensions;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Gql.Schema.GraphTypes;

namespace Sig.App.Backend.Requests.Commands.Mutations.Accounts
{
    public class DisableUser : IRequestHandler<DisableUser.Input, DisableUser.Payload>
    {
        private readonly ILogger<DisableUser> logger;
        private readonly AppDbContext db;
            
        public DisableUser(ILogger<DisableUser> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] DisableUser({request.UserId})");
            var userId = request.UserId.StringIdentifierForType<AppUser>();
            var user = await db.Users.Include(x => x.Profile).FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

            if (user == null)
            {
                logger.LogWarning("[Mutation] DisableUser - UserNotFoundException");
                throw new UserNotFoundException();
            }

            user.Status = UserStatus.Disabled;

            await db.SaveChangesAsync();
            logger.LogInformation($"[Mutation] DisableUser - Disable user ({userId}, {user.Profile.FirstName} {user.Profile.LastName})");

            return new Payload
            {
                User = new UserGraphType(user)
            };
        }

        [MutationInput]
        public class Input : HaveUserId, IRequest<Payload> { }

        [MutationPayload]
        public class Payload
        {
            public UserGraphType User { get; set; }
        }

        public class UserNotFoundException : RequestValidationException { }
        public class UserNotDisabledException : RequestValidationException { }
    }
}
