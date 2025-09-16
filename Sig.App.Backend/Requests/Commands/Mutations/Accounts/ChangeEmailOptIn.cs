using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Accounts
{
    public class ChangeEmailOptIn : IRequestHandler<ChangeEmailOptIn.Input, ChangeEmailOptIn.Payload>
    {
        private readonly AppDbContext db;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ILogger<ChangeEmailOptIn> logger;

        public ChangeEmailOptIn(IHttpContextAccessor httpContextAccessor, AppDbContext db, ILogger<ChangeEmailOptIn> logger)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.db = db;
            this.logger = logger;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] ChangeEmailOptIn");

            var user = await db.Users.FirstAsync(x => x.Id == httpContextAccessor.HttpContext.User.GetUserId());

            if (user == null)
            {
                throw new UserNotFoundException();
            }

            if (user.Type != UserType.ProjectManager)
            {
                throw new UserNotProjectManager();
            }

            user.EmailOptIn = request.EmailOptIn;
            
            await db.SaveChangesAsync();

            logger.LogInformation($"[Mutation] ChangeEmailOptIn - User {user.Email} change is email opt-in option.");

            return new Payload
            {
                User = new UserGraphType(user)
            };
        }

        [MutationInput]
        public class Input : IRequest<Payload>
        {
            public string EmailOptIn { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public UserGraphType User { get; set; }
        }

        public abstract class ChangeEmailOptInException : RequestValidationException { }
        public class UserNotFoundException : ChangeEmailOptInException { }
        public class UserNotProjectManager : ChangeEmailOptInException { }
    }
}
