using Sig.App.Backend.Extensions;
using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NodaTime;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.Profiles;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Gql.Schema.Types;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Gql.Bases;

namespace Sig.App.Backend.Requests.Commands.Mutations.Profiles
{
    public class UpdateUserProfile : IRequestHandler<UpdateUserProfile.Input, UpdateUserProfile.Payload>
    {
        protected readonly AppDbContext DbContext;
        private readonly IClock clock;
        private readonly ILogger<UpdateUserProfile> logger;

        public UpdateUserProfile(AppDbContext db, IClock clock, ILogger<UpdateUserProfile> logger)
        {
            DbContext = db;
            this.clock = clock;
            this.logger = logger;
        }

        public async Task<UpdateUserProfile.Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] UpdateUserProfile({request.UserId}, {request.FirstName}, {request.LastName})");
            var userId = request.UserId.IdentifierForType<AppUser>();
            var profile = await GetProfileWithUser(userId, cancellationToken);

            if (profile == null)
            {
                var user = await DbContext.Users
                    .Include(x => x.Profile)
                    .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

                if (user == null)
                {
                    logger.LogWarning("[Mutation] UpdateUserProfile - UserNotFoundException");
                    throw new UserNotFoundException();
                }

                user.Profile = profile = CreateDefaultProfile(user);
            }

            await UpdateProfileFromRequest(profile, request);

            await DbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"User profile {userId} updated ({typeof(UserProfile).Name})");

            return CreateOutput(profile);
        }

        private UserProfile CreateDefaultProfile(AppUser user)
        {
            return new UserProfile(user.Profile) { User = user };
        }

        private Task<UserProfile> GetProfileWithUser(string userId, CancellationToken cancellationToken)
        {
            return DbContext.UserProfiles
                .OfType<UserProfile>()
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        }

        private Task UpdateProfileFromRequest(UserProfile profile, Input request)
        {
            request.FirstName.IfSet(v => profile.FirstName = v.Trim());
            request.LastName.IfSet(v => profile.LastName = v.Trim());

            profile.UpdateTimeUtc = clock.GetCurrentInstant().ToDateTimeUtc();

            return Task.CompletedTask;
        }

        private Payload CreateOutput(UserProfile profile)
        {
            return new Payload
            {
                User = new UserGraphType(profile.User)
            };
        }

        public abstract class UpdateProfileException : RequestValidationException { }
        public class UserNotFoundException : UpdateProfileException { }

        [MutationInput]
        public class Input : HaveUserId, IRequest<Payload>
        {
            public Maybe<NonNull<string>> FirstName { get; set; }
            public Maybe<NonNull<string>> LastName { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public UserGraphType User { get; set; }
        }
    }
}
