using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Requests.Commands.Mutations.Accounts;

namespace Sig.App.BackendTests.Requests.Commands.Mutations.Accounts
{
    public class ChangePasswordTests : TestBase
    {
        private readonly AppUser user;
        private readonly ChangePassword handler;

        public ChangePasswordTests()
        {
            user = AddUser("test@example.com", UserType.PCAAdmin, password: "Abcd1234!!");
            SetLoggedInUser(user);

            handler = new ChangePassword(UserManager, HttpContextAccessor, NullLogger<ChangePassword>.Instance);
        }

        [Fact]
        public async Task ChangesThePassword()
        {
            var input = new ChangePassword.Input
            {
                CurrentPassword = "Abcd1234!!",
                NewPassword = "Abcd1234!!!"
            };

            await handler.Handle(input, CancellationToken.None);

            (await UserManager.CheckPasswordAsync(user, "Abcd1234!!!")).Should().BeTrue();
        }

        [Fact]
        public async Task ThrowsIfCurrentPasswordIncorrect()
        {
            var input = new ChangePassword.Input
            {
                CurrentPassword = "bad-password",
                NewPassword = "Abcd1234!!!"
            };

            await F(() => handler.Handle(input, CancellationToken.None))
                .Should().ThrowAsync<ChangePassword.WrongPasswordException>();
        }
    }
}
