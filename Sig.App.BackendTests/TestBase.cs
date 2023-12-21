using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Plugins.Identity;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NodaTime;
using NodaTime.Testing;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Sig.App.BackendTests
{
    public abstract class TestBase
    {
        protected readonly UserManager<AppUser> UserManager;
        protected readonly AppDbContext DbContext;
        protected readonly FakeClock Clock;
        protected readonly IHttpContextAccessor HttpContextAccessor;
        protected readonly Mock<IMediator> MediatorMock;
        private readonly string databaseName;

        protected ILogger<T> Logger<T>() => NullLogger<T>.Instance;
        protected IMediator Mediator => MediatorMock.Object;

        protected TestBase()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            
            databaseName = Guid.NewGuid().ToString();
            Clock = new FakeClock(SystemClock.Instance.GetCurrentInstant());
            MediatorMock = new Mock<IMediator>();
            DbContext = CreateDbContext();
            UserManager = CreateUserManager();
            HttpContextAccessor = new HttpContextAccessor {
                HttpContext = new DefaultHttpContext()
            };
        }

        protected AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName)
                /*.ConfigureWarnings(x => {
                    // In memory db does not support transactions. Disable the warning about this.
                    x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning);
                })*/
                .EnableDetailedErrors()
                .Options;

            return new AppDbContext(options);
        }

        private UserManager<AppUser> CreateUserManager()
        {
            var store = new UserStore<AppUser>(DbContext);

            var options = new Mock<IOptions<IdentityOptions>>();
            var idOptions = new IdentityOptions
            {
                Password = {
                    RequireNonAlphanumeric = true,
                    RequireDigit = true,
                    RequireLowercase = false,
                    RequireUppercase = true,
                    RequiredLength = 10
                }
            };

            options.Setup(o => o.Value).Returns(idOptions);

            var userValidators = new List<IUserValidator<AppUser>>();
            var validator = new Mock<IUserValidator<AppUser>>();
            userValidators.Add(validator.Object);

            var pwdValidators = new List<IPasswordValidator<AppUser>> {
                new PasswordValidator<AppUser>()
            };

            var userManager = new UserManager<AppUser>(store, options.Object, new PasswordHasher<AppUser>(),
                userValidators, pwdValidators, new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                new DefaultServiceProviderFactory().CreateServiceProvider(new ServiceCollection()),
                new Mock<ILogger<UserManager<AppUser>>>().Object);

            validator.Setup(v => v.ValidateAsync(userManager, It.IsAny<AppUser>()))
                .Returns(Task.FromResult(IdentityResult.Success)).Verifiable();

            var dataProtectionProvider = new EphemeralDataProtectionProvider();
            userManager.RegisterTokenProvider(TokenOptions.DefaultProvider, new DataProtectorTokenProvider<AppUser>(dataProtectionProvider, null, new NullLogger<DataProtectorTokenProvider<AppUser>>()));
            userManager.RegisterTokenProvider(TokenProviders.EmailInvites, new LongLivedTokenProvider(dataProtectionProvider, null, new NullLogger<DataProtectorTokenProvider<AppUser>>()));

            return userManager;
        }

        protected AppUser AddUser(string username, UserType type, string password = "1234aAuuuuuu!", params Claim[] claims)
        {
            var user = new AppUser(username)
            {
                Type = type,
                EmailConfirmed = true
            };

            UserManager.CreateAsync(user).Result.AssertSuccess();

            if (!string.IsNullOrWhiteSpace(password))
            {
                UserManager.AddPasswordAsync(user, password).Result.AssertSuccess();
            }

            if (claims != null)
            {
                foreach (var claim in claims)
                {
                    UserManager.AddClaimAsync(user, claim).Result.AssertSuccess();
                }
            }

            return user;
        }

        protected void SetLoggedInUser(AppUser user)
        {
            HttpContextAccessor.HttpContext!.User = CreatePrincipal(user);
        }

        public void SetupRequestHandler<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> handler)
            where TRequest : IRequest<TResponse>
        {
            MediatorMock
                .Setup(x => x.Send(It.IsAny<TRequest>(), It.IsAny<CancellationToken>()))
                .Returns<TRequest, CancellationToken>(handler.Handle);
        }

        protected ClaimsPrincipal CreatePrincipal(AppUser user)
        {
            var factory = new AppUserClaimsPrincipalFactory(
                UserManager,
                new OptionsWrapper<IdentityOptions>(new IdentityOptions()));

            return factory.CreateAsync(user).GetAwaiter().GetResult();
        }

        protected Func<T> F<T>(Func<T> func) => func;
        protected Action F(Action func) => func;
    }
}
