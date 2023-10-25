using System;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Entities.Profiles;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Profiles;
using FluentAssertions;
using GraphQL.Conventions;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Profiles
{
    public class UpdateUserProfileTests : TestBase
    {
        private readonly UpdateUserProfile handler;
        private readonly AppUser user;
        private const string UserFirstName = "FirstName";
        private const string UserLastName = "Lastname";

        public UpdateUserProfileTests()
        {
            user = AddUser("test@example.com", UserType.PCAAdmin);
            user.Profile = new UserProfile
            {
                FirstName = UserFirstName,
                LastName = UserLastName
            };
            DbContext.SaveChanges();

            handler = new UpdateUserProfile(DbContext, Clock, NullLogger<UpdateUserProfile>.Instance);
        }

        [Fact]
        public async Task UpdatesTheProfile()
        {
            var input = new UpdateUserProfile.Input
            {
                UserId = user.GetIdentifier(),
                FirstName = "Updated".NonNull()
            };

            await handler.Handle(input, CancellationToken.None);

            await DbContext.Entry(user.Profile).ReloadAsync();

            user.Profile.FirstName.Should().Be("Updated");
        }

        [Fact]
        public async Task CreatesTheProfileIfDoesNotExist()
        {
            var userWithoutProfile = AddUser("noprofile@example.com", UserType.PCAAdmin);

            var input = new UpdateUserProfile.Input
            {
                UserId = userWithoutProfile.GetIdentifier(),
                FirstName = "Name".NonNull()
            };

            await handler.Handle(input, CancellationToken.None);

            userWithoutProfile.Profile.Should().NotBeNull();
        }

        [Fact]
        public async Task OnlyUpdatesSpecifiedFields()
        {
            var input = new UpdateUserProfile.Input
            {
                UserId = user.GetIdentifier(),
                FirstName = "Updated".NonNull()
            };

            await handler.Handle(input, CancellationToken.None);

            user.Profile.LastName.Should().Be(UserLastName);
        }

        [Fact]
        public async Task ThrowsIfUserNotFound()
        {
            var input = new UpdateUserProfile.Input
            {
                UserId = Id.New<AppUser>("asdf")
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<UpdateUserProfile.UserNotFoundException>();
        }

        [Fact]
        public async Task TracksTheLastUpdateTime()
        {
            var input = new UpdateUserProfile.Input
            {
                UserId = user.GetIdentifier()
            };

            await handler.Handle(input, CancellationToken.None);

            user.Profile.UpdateTimeUtc.Should().BeCloseTo(Clock.GetCurrentInstant().ToDateTimeUtc(), TimeSpan.FromSeconds(1));
        }
    }
}
