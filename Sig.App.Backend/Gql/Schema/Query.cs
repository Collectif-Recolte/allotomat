using GraphQL.Conventions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using MediatR;
using GraphQL.DataLoader;
using Sig.App.Backend.Authorization;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities;
using Sig.App.Backend.Gql.Interfaces;
using Sig.App.Backend.Gql.Schema.Enums;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Gql.Schema.Types;
using Sig.App.Backend.Requests.Queries.Users;
using Sig.App.Backend.Services.Permission.Enums;
using Sig.App.Backend.Utilities;
using System.Collections.Generic;
using System.Linq;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Entities.Markets;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.Services.Permission;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.Requests.Queries.Cards;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Entities.Subscriptions;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.Requests.Commands.Queries.Subscriptions;
using Sig.App.Backend.Requests.Commands.Queries.Cards;
using Sig.App.Backend.Requests.Commands.Queries.Transactions;
using Sig.App.Backend.Requests.Queries.Transactions;
using Sig.App.Backend.Requests.Commands.Queries.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Requests.Queries.Beneficiaries;

namespace Sig.App.Backend.Gql.Schema
{
    [SchemaExtension]
    public static class Query
    {
        [ApplyPolicy(AuthorizationPolicies.LoggedIn)]
        [Description("The currently authenticated user.")]
        public static IDataLoaderResult<UserGraphType> Me(this GqlQuery _, IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadUser(ctx.CurrentUserId);
        }

        [RequirePermission(GlobalPermission.ManageAllUsers)]
        [Description("All users")]
        public static async Task<Pagination<UserGraphType>> Users(
            this GqlQuery _,
            [Inject] IMediator mediator,
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            int page, int limit, string? searchText, UserType[] userTypes = null)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        {
            var results = await mediator.Send(new SearchUsers.Query
            {
                Page = new Page(page, limit),
                SearchText = searchText,
                UserTypes = userTypes
            });

            return results.Map(x => new UserGraphType(x));
        }
        
        public static IDataLoaderResult<UserGraphType> User(this GqlQuery _, IAppUserContext ctx, Id id)
        {
            return ctx.DataLoader.LoadUser(id.IdentifierForType<AppUser>());
        }

        [RequirePermission(GlobalPermission.ManageAllUsers)]
        public static async Task<UserGraphType> UserByEmail(
            this GqlQuery _,
            [Inject] AppDbContext db,
            string email)
        {
            var user = await db.Users.FirstOrDefaultAsync(x => x.Email == email);
            return user == null 
                ? null 
                : new UserGraphType(user);
        }

        public static async Task<VerifyTokenPayload> VerifyToken(
            this GqlQuery _,
            [Inject] UserManager<AppUser> userManager,
            [Inject] IOptions<IdentityOptions> identityOptions,
            string email, string token, TokenType type)
        {
            var user = await userManager.FindByEmailAsync(email);
            var status = TokenStatus.Invalid;

            if (user != null)
            {
                var (provider, purpose) = GetTokenProviderAndPurpose();
                if (await userManager.VerifyUserTokenAsync(user, provider, purpose, token))
                    status = TokenStatus.Valid;
            }

            if (status == TokenStatus.Invalid)
            {
                if (type == TokenType.ConfirmEmail || type == TokenType.AdminInvitation || type == TokenType.MerchantInvitation || type == TokenType.OrganizationManagerInvitation || type == TokenType.ProjectManagerInvitation)
                {
                    if (user?.EmailConfirmed == true)
                    {
                        status = TokenStatus.UserConfirmed;
                    }
                }
            }

            return new VerifyTokenPayload
            {
                Status = status,
                User = status == TokenStatus.Valid ? new UserGraphType(user) : null
            };

            (string provider, string purpose) GetTokenProviderAndPurpose()
            {
                switch (type)
                {
                    case TokenType.ResetPassword:
                        return (identityOptions.Value.Tokens.PasswordResetTokenProvider, "ResetPassword");
                    case TokenType.ConfirmEmail:
                        return (identityOptions.Value.Tokens.EmailConfirmationTokenProvider, "EmailConfirmation");
                    case TokenType.AdminInvitation:
                        return (TokenProviders.EmailInvites, TokenPurposes.AdminInvite);
                    case TokenType.ProjectManagerInvitation:
                        return (TokenProviders.EmailInvites, TokenPurposes.ProjectManagerInvite);
                    case TokenType.MerchantInvitation:
                        return (TokenProviders.EmailInvites, TokenPurposes.MerchantInvite);
                    case TokenType.OrganizationManagerInvitation:
                        return (TokenProviders.EmailInvites, TokenPurposes.OrganizationManagerInvite);
                    default: throw new ArgumentOutOfRangeException(nameof(type));
                }
            }
        }

        [RequirePermission(GlobalPermission.ManageProjects, GlobalPermission.ManageSpecificProject)]
        [Description("All projects manageable by current user")]
        public static async Task<IEnumerable<ProjectGraphType>> Projects(this GqlQuery _, IAppUserContext ctx, [Inject] AppDbContext db, [Inject] PermissionService permissionService)
        {
            var globalPermissions = await permissionService.GetGlobalPermissions(ctx.CurrentUser);
            if (globalPermissions.Contains(GlobalPermission.ManageProjects))
            {
                return await db.Projects.Select(x => new ProjectGraphType(x)).ToListAsync();
            }
            else if (globalPermissions.Contains(GlobalPermission.ManageSpecificProject))
            {
                return await ctx.DataLoader.LoadProjectOwnedByUser(ctx.CurrentUserId).GetResultAsync();
            }
            else
            {
                return new ProjectGraphType[0];
            }
        }

        [RequirePermission(ProjectPermission.ManageProject)]
        public static IDataLoaderResult<ProjectGraphType> Project(this GqlQuery _, IAppUserContext ctx, Id id)
        {
            return ctx.DataLoader.LoadProject(id.LongIdentifierForType<Project>());
        }

        [RequirePermission(GlobalPermission.ManageSpecificOrganization)]
        [Description("All organizations manageable by current user")]
        public static async Task<IEnumerable<OrganizationGraphType>> Organizations(this GqlQuery _, IAppUserContext ctx, [Inject] PermissionService permissionService)
        {
            var globalPermissions = await permissionService.GetGlobalPermissions(ctx.CurrentUser);
            if (globalPermissions.Contains(GlobalPermission.ManageSpecificOrganization))
            {
                return await ctx.DataLoader.LoadOrganizationsOwnedByUser(ctx.CurrentUserId).GetResultAsync();
            }
            else
            {
                return new OrganizationGraphType[0];
            }
        }

        [RequirePermission(OrganizationPermission.ManageOrganization)]
        public static async Task<IDataLoaderResult<OrganizationGraphType>> Organization(this GqlQuery _, IAppUserContext ctx, Id id, [Inject] PermissionService permissionService)
        {
            var organizationPermissions = await permissionService.GetOrganizationPermissions(ctx.CurrentUser, id.IdentifierForType<Organization>());
            if (organizationPermissions.Contains(OrganizationPermission.ManageOrganization))
            {
                return ctx.DataLoader.LoadOrganization(id.LongIdentifierForType<Organization>());
            }
            else
            {
                return null;
            }
        }

        [RequirePermission(GlobalPermission.ManageMarkets, GlobalPermission.ManageSpecificMarket)]
        [Description("All markets manageable by current user")]
        public static async Task<IEnumerable<MarketGraphType>> Markets(this GqlQuery _, IAppUserContext ctx, [Inject] AppDbContext db, [Inject] PermissionService permissionService)
        {
            var globalPermissions = await permissionService.GetGlobalPermissions(ctx.CurrentUser);
            if (globalPermissions.Contains(GlobalPermission.ManageMarkets))
            {
                return await db.Markets.Where(x => !x.IsArchived).Select(x => new MarketGraphType(x)).ToListAsync();
            }
            else if (globalPermissions.Contains(GlobalPermission.ManageSpecificMarket))
            {
                return await ctx.DataLoader.LoadMarketOwnedByUser(ctx.CurrentUserId).GetResultAsync();
            }
            else
            {
                return new MarketGraphType[0];
            }
        }

        public static IDataLoaderResult<MarketGraphType> Market(this GqlQuery _, IAppUserContext ctx, Id id)
        {
            return ctx.DataLoader.LoadMarket(id.LongIdentifierForType<Market>());
        }

        [RequirePermission(BeneficiaryPermission.ManageBeneficiary)]
        public static IDataLoaderResult<IBeneficiaryGraphType> Beneficiary(this GqlQuery _, IAppUserContext ctx, Id id)
        {
            long longId;
            if (id.IsIdentifierForType<Beneficiary>())
            {
                longId = id.LongIdentifierForType<Beneficiary>();
            }
            else
            {
                longId = id.LongIdentifierForType<OffPlatformBeneficiary>();
            }

            return ctx.DataLoader.LoadBeneficiary(longId);
        }
        
        public static IDataLoaderResult<CardGraphType> Card(this GqlQuery _, IAppUserContext ctx, Id id)
        {
            return ctx.DataLoader.LoadCardById(id.LongIdentifierForType<Card>());
        }

        public static IDataLoaderResult<CardGraphType> CardByNumber(this GqlQuery _, IAppUserContext ctx, string cardNumber)
        {
            return ctx.DataLoader.LoadCardByCardNumber(cardNumber);
        }

        public static IDataLoaderResult<SubscriptionGraphType> Subscription(this GqlQuery _, IAppUserContext ctx, Id id)
        {
            return ctx.DataLoader.LoadSubscriptionById(id.LongIdentifierForType<Subscription>());
        }

        public static IDataLoaderResult<BudgetAllowanceGraphType> BudgetAllowance(this GqlQuery _, IAppUserContext ctx, Id id)
        {
            return ctx.DataLoader.LoadBudgetAllowance(id.LongIdentifierForType<BudgetAllowance>());
        }

        public static IDataLoaderResult<ProductGroupGraphType> ProductGroup(this GqlQuery _, IAppUserContext ctx, Id id)
        {
            return ctx.DataLoader.LoadProductGroup(id.LongIdentifierForType<ProductGroup>());
        }

        [AnnotateErrorCodes(typeof(VerifyCardCanBeUsedInMarket))]
        public static async Task<bool> VerifyCardCanBeUsedInMarket(this GqlQuery _, Id cardId, Id marketId, [Inject] IMediator mediator)
        {
            return await mediator.Send(new VerifyCardCanBeUsedInMarket.Input() { CardId = cardId, MarketId = marketId});
        }

        public static IDataLoaderResult<BeneficiaryTypeGraphType> BeneficiaryType(this GqlQuery _, IAppUserContext ctx, Id id)
        {
            return ctx.DataLoader.LoadBeneficiaryType(id.LongIdentifierForType<BeneficiaryType>());
        }

        public static async Task<ForecastAssignBeneficiariesToSubscription.Payload> ForecastAssignBeneficiariesToSubscription(this GqlQuery _, Id organizationId, Id subscriptionId, int amount, bool withoutSubscription, Id[] withSubscriptions, Id[] withCategories, string searchText, bool? withCard, [Inject] IMediator mediator)
        {
            return await mediator.Send(new ForecastAssignBeneficiariesToSubscription.Input()
            {
                Amount = amount,
                OrganizationId = organizationId,
                SubscriptionId = subscriptionId,
                WithCategories = withCategories,
                WithoutSubscription = withoutSubscription,
                WithSubscriptions = withSubscriptions,
                WithCard = withCard,
                SearchText = searchText
            });
        }

        public static async Task<long> ForecastNextUnassignedCard(this GqlQuery _, Id projectId, [Inject] IMediator mediator)
        {
            return await mediator.Send(new ForecastNextUnassignedCard.Input()
            {
                ProjectId = projectId
            });
        }

        public static async Task<ForecastImportOffPlatformBeneficiariesListInOrganization.ImportOffPlatformBeneficiariesListPayload> ForecastImportOffPlatformBeneficiariesListInOrganization(this GqlQuery _, Id organizationId, string[] ids1, DateTime[] endDates, [Inject] IMediator mediator)
        {
            if (ids1.Length != endDates.Length) return null;

            var items = new List<ForecastImportOffPlatformBeneficiariesListInOrganization.ForecastOffPlatformBeneficiaryItem>();
            for (var i = 0; i < ids1.Length; i++)
            {
                items.Add(new ForecastImportOffPlatformBeneficiariesListInOrganization.ForecastOffPlatformBeneficiaryItem()
                {
                    Id1 = ids1[i],
                    EndDate = endDates[i]
                });
            }

            return await mediator.Send(new ForecastImportOffPlatformBeneficiariesListInOrganization.Input()
            {
                OrganizationId = organizationId,
                Items = items.ToArray()
            });
        }

        public static async Task<string> GenerateTransactionsReport(this GqlQuery _, Id projectId, DateTime startDate, DateTime endDate, Id[] organizations, Id[] subscriptions, bool? withoutSubscription, Id[] categories, string[] transactionTypes, string searchText, string timeZoneId, Language language, [Inject] IMediator mediator)
        {
            return await mediator.Send(new GenerateTransactionsReport.Input()
            {
                ProjectId = projectId,
                StartDate = startDate,
                EndDate = endDate,
                Organizations = organizations,
                Subscriptions = subscriptions,
                WithoutSubscription = withoutSubscription,
                Categories = categories,
                TransactionTypes = transactionTypes,
                SearchText = searchText,
                TimeZoneId = timeZoneId,
                Language = language
            });
        }

        public static async Task<string> ExportBeneficiariesList(this GqlQuery _, Id id, string timeZoneId, Language language, [Inject] IMediator mediator)
        {
            return await mediator.Send(new ExportBeneficiariesList.Input()
            {
                Id = id,
                TimeZoneId = timeZoneId,
                Language = language
            });
        }

        public static async Task<string> ExportOffPlatformBeneficiariesList(this GqlQuery _, Id id, string timeZoneId, [Inject] IMediator mediator)
        {
            return await mediator.Send(new ExportOffPlatformBeneficiariesList.Input()
            {
                Id = id,
                TimeZoneId = timeZoneId
            });
        }

        public static async Task<string> DownloadBeneficiariesTemplateFile(this GqlQuery _, Id organizationId, [Inject] IMediator mediator)
        {
            return await mediator.Send(new DownloadBeneficiariesTemplateFile.Input()
            {
                OrganizationId = organizationId
            });
        }
        
        public static async Task<RefundTransactionGraphType> RefundTransaction(this GqlQuery _, Id id, [Inject] AppDbContext db)
        {
            var transaction = await db.Transactions.OfType<RefundTransaction>().Where(x => x.Id == id.LongIdentifierForType<RefundTransaction>()).FirstOrDefaultAsync();
            return new RefundTransactionGraphType(transaction);
        }

        public static async Task<ITransactionGraphType> Transaction(this GqlQuery _, Id id, [Inject] AppDbContext db)
        {
            var transaction = await db.Transactions.Where(x => x.Id == id.LongIdentifierForType<PaymentTransaction>()).FirstOrDefaultAsync();

            switch (transaction)
            {
                case null:
                    return null;
                case PaymentTransaction pt:
                    return new PaymentTransactionGraphType(pt);
                case SubscriptionAddingFundTransaction aft:
                    return new SubscriptionAddingFundTransactionGraphType(aft);
                case ManuallyAddingFundTransaction maft:
                    return new ManuallyAddingFundTransactionGraphType(maft);
                case LoyaltyAddingFundTransaction laft:
                    return new LoyaltyAddingFundTransactionGraphType(laft);
                case RefundTransaction rft:
                    return new RefundTransactionGraphType(rft);
                case OffPlatformAddingFundTransaction opaft:
                    return new OffPlatformAddingFundTransactionGraphType(opaft);
            }

            return null;
        }

        [RequirePermission(GlobalPermission.ManageTransactions)]
        [Description("All transactions")]
        public static async Task<Pagination<TransactionLogGraphType>> TransactionLogs(this GqlQuery _, [Inject] IMediator mediator,
            int page, int limit, Id projectId, DateTime startDate, DateTime endDate, Id[] organizations, Id[] subscriptions, bool? withoutSubscription, Id[] categories, string[] transactionTypes, string searchText)
        {
            var results = await mediator.Send(new SearchTransactionLogs.Query
            {
                Page = new Page(page, limit),
                ProjectId = projectId,
                StartDate = startDate,
                EndDate = endDate,
                Organizations = organizations,
                Subscriptions = subscriptions,
                WithoutSubscription = withoutSubscription,
                Categories = categories,
                TransactionTypes = transactionTypes,
                SearchText = searchText
            });

            return results.Map(x => new TransactionLogGraphType(x));
        }
    }
}