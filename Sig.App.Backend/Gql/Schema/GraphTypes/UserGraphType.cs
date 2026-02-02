using GraphQL.Conventions;
using GraphQL.DataLoader;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NodaTime;
using Sig.App.Backend.Authorization;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;
using Sig.App.Backend.Helpers;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Services.Permission;
using Sig.App.Backend.Services.Permission.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class UserGraphType
    {
        private readonly AppUser user;

        public Id Id => user.GetIdentifier();
        public NonNull<string> Email => user.Email;
        public UserType Type => user.Type;
        public bool IsConfirmed => user.EmailConfirmed;
        public UserStatus Status => user.Status;

        public EmailOptIn[] EmailOptIn => user.GetEmailOptIns();

        public UserGraphType(AppUser user)
        {
            this.user = user;
        }

        public IDataLoaderResult<IProfileGraphType> Profile(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadProfileByUserId(user.Id);
        }

        [ApplyPolicy(AuthorizationPolicies.IsPCAAdmin)]
        public OffsetDateTime? LastConnectionTime()
        {
            if (user.LastAccessTimeUtc.HasValue)
            {
                return user.LastAccessTimeUtc.Value.FromUtcToOffsetDateTime();
            }

            return null;
        }

        public PermissionsGraphType Permissions(IAppUserContext ctx)
        {
            return new PermissionsGraphType(ctx.CurrentUser);
        }

        public async Task<string> ConfirmationLink([Inject] UserManager<AppUser> userManager, [Inject] IConfiguration config)
        {
            if (user.EmailConfirmed)
            {
                return null;
            }

            string token;
            switch (user.Type)
            {
                case UserType.PCAAdmin:
                    token = await userManager.GenerateUserTokenAsync(user, TokenProviders.EmailInvites, TokenPurposes.AdminInvite);
                    return $"{config["Mailer:BaseUrl"]}/{UrlHelper.RegistrationAdmin(user.Email, token)}";
                case UserType.ProjectManager:
                    token = await userManager.GenerateUserTokenAsync(user, TokenProviders.EmailInvites, TokenPurposes.ProjectManagerInvite);
                    return $"{config["Mailer:BaseUrl"]}/{UrlHelper.RegistrationProjectManager(user.Email, token)}";
                case UserType.OrganizationManager:
                    token = await userManager.GenerateUserTokenAsync(user, TokenProviders.EmailInvites, TokenPurposes.OrganizationManagerInvite);
                    return $"{config["Mailer:BaseUrl"]}/{UrlHelper.RegistrationOrganizationManager(user.Email, token)}";
                case UserType.Merchant:
                    token = await userManager.GenerateUserTokenAsync(user, TokenProviders.EmailInvites, TokenPurposes.MerchantInvite);
                    return $"{config["Mailer:BaseUrl"]}/{UrlHelper.RegistrationMarketManager(user.Email, token)}";
                case UserType.MarketGroupManager:
                    token = await userManager.GenerateUserTokenAsync(user, TokenProviders.EmailInvites, TokenPurposes.MarketGroupManagerInvite);
                    return $"{config["Mailer:BaseUrl"]}/{UrlHelper.RegistrationMarketGroupManager(user.Email, token)}";
            }

            return null;
        }

        public async Task<string> ResetPasswordLink([Inject] UserManager<AppUser> userManager, [Inject] IConfiguration config)
        {
            if (!user.EmailConfirmed)
            {
                return null;
            }

            string token = await userManager.GeneratePasswordResetTokenAsync(user);
            return $"{config["Mailer:BaseUrl"]}/{UrlHelper.ResetPassword(user.UserName, token)}";
        }

        [ApplyPolicy(AuthorizationPolicies.IsPCAAdmin)]
        [Description("All projects manageable by current user")]
        public async Task<IEnumerable<ProjectGraphType>> Projects(IAppUserContext ctx)
        {
            if (user.Type == UserType.ProjectManager)
            {
                return await ctx.DataLoader.LoadProjectOwnedByUser(user.Id).GetResultAsync();
            }
            else
            {
                return new ProjectGraphType[0];
            }
        }

        [ApplyPolicy(AuthorizationPolicies.IsPCAAdmin)]
        [Description("All organizations manageable by current user")]
        public async Task<IEnumerable<OrganizationGraphType>> Organizations(IAppUserContext ctx)
        {
            if (user.Type == UserType.OrganizationManager)
            { 
                return await ctx.DataLoader.LoadOrganizationsOwnedByUser(user.Id).GetResultAsync();
            }
            else
            {
                return new OrganizationGraphType[0];
            }
        }

        [ApplyPolicy(AuthorizationPolicies.IsPCAAdmin)]
        [Description("All markets manageable by current user")]
        public async Task<IEnumerable<MarketGraphType>> Markets(IAppUserContext ctx)
        {
            if (user.Type == UserType.Merchant)
            {
                return await ctx.DataLoader.LoadMarketOwnedByUser(user.Id).GetResultAsync();
            }
            else
            {
                return new MarketGraphType[0];
            }
        }

        [ApplyPolicy(AuthorizationPolicies.IsPCAAdmin)]
        [Description("All market groups manageable by current user")]
        public async Task<IEnumerable<MarketGroupGraphType>> MarketGroups(IAppUserContext ctx)
        {
            if (user.Type == UserType.MarketGroupManager)
            {
                return await ctx.DataLoader.LoadMarketGroupOwnedByUser(user.Id).GetResultAsync();
            }
            else
            {
                return new MarketGroupGraphType[0];
            }
        }
    }
}