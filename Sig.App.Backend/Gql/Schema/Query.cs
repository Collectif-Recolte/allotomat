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
using Sig.App.Backend.Requests.Commands.Queries.Cards;
using Sig.App.Backend.Requests.Commands.Queries.Transactions;
using Sig.App.Backend.Requests.Queries.Transactions;
using Sig.App.Backend.Requests.Commands.Queries.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.ProductGroups;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Requests.Queries.Beneficiaries;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Utilities.Sorting;
using Sig.App.Backend.DbModel.Entities.MarketGroups;
using Sig.App.Backend.Requests.Queries.Markets;
using Sig.App.Backend.DbModel.Entities.CashRegisters;

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
                    case TokenType.MarketGroupManagerInvitation:
                        return (TokenProviders.EmailInvites, TokenPurposes.MarketGroupManagerInvite);
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

        [RequirePermission(GlobalPermission.ManageMarkets, GlobalPermission.ManageSpecificMarket, GlobalPermission.ManageOrganizations)]
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
            else if (globalPermissions.Contains(GlobalPermission.ManageOrganizations))
            {
                var project = await ctx.DataLoader.LoadProjectOwnedByUser(ctx.CurrentUserId).GetResultAsync();
                return await ctx.DataLoader.LoadProjectMarkets(project.First().Id.LongIdentifierForType<Project>()).GetResultAsync();
            }
            else
            {
                return new MarketGraphType[0];
            }
        }

        [RequirePermission(GlobalPermission.ManageMarkets)]
        [Description("All markets with pagination")]
        public static async Task<Pagination<MarketGraphType>> AllMarketsSearch(this GqlQuery _, [Inject] IMediator mediator, int page, int limit, string searchText, Id[] marketGroups)
        {
            var results = await mediator.Send(new SearchMarkets.Query
            {
                Page = new Page(page, limit),
                SearchText = searchText,
                MarketGroups = marketGroups
            });

            return results.Map(x =>
            {
                return new MarketGraphType(x);
            });
        }

        [RequirePermission(GlobalPermission.ManageMarkets, GlobalPermission.ManageOrganizations)]
        [Description("All markets in Tomat")]
        public static async Task<IEnumerable<MarketGraphType>> AllMarkets(this GqlQuery _, [Inject] AppDbContext db)
        {
            return await db.Markets.Where(x => !x.IsArchived).Select(x => new MarketGraphType(x)).ToListAsync();
        }   

        [RequirePermission(GlobalPermission.ManageMarketGroups, GlobalPermission.ManageSpecificMarketGroup)]
        [Description("All market groups manageable by current user")]
        public static async Task<IEnumerable<MarketGroupGraphType>> MarketGroups(this GqlQuery _, IAppUserContext ctx, [Inject] AppDbContext db, [Inject] PermissionService permissionService)
        {
            var globalPermissions = await permissionService.GetGlobalPermissions(ctx.CurrentUser);
            if (globalPermissions.Contains(GlobalPermission.ManageMarketGroups) || globalPermissions.Contains(GlobalPermission.ManageSpecificMarketGroup))
            {
                return await ctx.DataLoader.LoadMarketGroupOwnedByUser(ctx.CurrentUserId).GetResultAsync();
            }
            else if (globalPermissions.Contains(GlobalPermission.ManageSpecificMarketGroup))
            {
                return await ctx.DataLoader.LoadMarketGroupOwnedByUser(ctx.CurrentUserId).GetResultAsync();
            }
            else
            {
                return new MarketGroupGraphType[0];
            }
        }

        public static IDataLoaderResult<MarketGraphType> Market(this GqlQuery _, IAppUserContext ctx, Id id)
        {
            return ctx.DataLoader.LoadMarket(id.LongIdentifierForType<Market>());
        }

        public static IDataLoaderResult<CashRegisterGraphType> CashRegister(this GqlQuery _, IAppUserContext ctx, Id id)
        {
            return ctx.DataLoader.LoadCashRegister(id.LongIdentifierForType<CashRegister>());
        }

        public static IDataLoaderResult<MarketGroupGraphType> MarketGroup(this GqlQuery _, IAppUserContext ctx, Id id)
        {
            return ctx.DataLoader.LoadMarketGroup(id.LongIdentifierForType<MarketGroup>());
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

        public static async Task<string> GenerateTransactionsReport(this GqlQuery _, Id projectId, DateTime startDate, DateTime endDate, Id[] organizations, Id[] subscriptions, bool? withoutSubscription, Id[] categories, Id[] markets, Id[] cashRegisters, string[] transactionTypes, string searchText, string timeZoneId, Language language, [Inject] IMediator mediator)
        {
            return await mediator.Send(new GenerateTransactionsReport.Input()
            {
                ProjectId = projectId,
                StartDate = startDate,
                EndDate = endDate,
                Organizations = organizations,
                Subscriptions = subscriptions,
                Markets = markets,
                CashRegisters = cashRegisters,
                WithoutSubscription = withoutSubscription,
                Categories = categories,
                TransactionTypes = transactionTypes,
                SearchText = searchText,
                TimeZoneId = timeZoneId,
                Language = language
            });
        }

        public static async Task<string> GenerateTransactionsReportForMarket(this GqlQuery _, Id marketId, DateTime startDate, DateTime endDate, string timeZoneId, Language language, [Inject] IMediator mediator)
        {
            return await mediator.Send(new GenerateTransactionsReportForMarket.Input()
            {
                MarketId = marketId,
                StartDate = startDate,
                EndDate = endDate,
                TimeZoneId = timeZoneId,
                Language = language
            });
        }

        public static async Task<string> ExportBeneficiariesList(this GqlQuery _, [Inject] IMediator mediator, Id id, string timeZoneId, Language language,
            [Description("If specified, only beneficiaries without or with a subscription are returned.")] bool? withoutSubscription = null,
            [Description("If specified, only beneficiaries with one of those subscription are returned.")] Id[] subscriptions = null,
            [Description("If specified, only beneficiaries without one of those subscription are returned.")] Id[] withoutSpecificSubscriptions = null,
            [Description("If specified, only beneficiaries with one of those category are returned")] Id[] categories = null,
            [Description("If specified, only beneficiaries without one of those category are returned")] Id[] withoutSpecificCategories = null,
            [Description("If specified, only beneficiaries active/inactive are returned")] BeneficiaryStatus[] status = null,
            [Description("If specified, only beneficiaries with or without card is returned.")] bool? withCard = null,
            [Description("If specified, only beneficiaries with or without payment conflict is returned.")] bool? withConflictPayment = null,
            [Description("If specified, only card enabled or disabled is returned.")] bool? withCardDisabled = null,
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            [Description("If specified, only that match text is returned.")] string? searchText = "",
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
            Sort<BeneficiarySort> sort = null)
        {
            return await mediator.Send(new ExportBeneficiariesList.Input()
            {
                Id = id,
                TimeZoneId = timeZoneId,
                Language = language,
                WithoutSubscription = withoutSubscription,
                Subscriptions = subscriptions?.Select(y => y.LongIdentifierForType<Subscription>()),
                WithoutSpecificSubscriptions = withoutSpecificSubscriptions?.Select(y => y.LongIdentifierForType<Subscription>()),
                Categories = categories?.Select(y => y.LongIdentifierForType<BeneficiaryType>()),
                WithoutSpecificCategories = withoutSpecificCategories?.Select(y => y.LongIdentifierForType<BeneficiaryType>()),
                Status = status,
                WithCard = withCard,
                WithConflictPayment = withConflictPayment,
                SearchText = searchText,
                WithCardDisabled = withCardDisabled,
                Sort = sort,
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
        public static async Task<TransactionLogsPagination<TransactionLogGraphType>> TransactionLogs(this GqlQuery _, [Inject] IMediator mediator,
            int page, int limit, Id projectId, DateTime startDate, DateTime endDate, Id[] organizations, Id[] subscriptions, Id[] markets, bool? withoutSubscription, Id[] categories, string[] transactionTypes, Id[] cashRegisters, string searchText, string timeZoneId)
        {
            var results = await mediator.Send(new SearchTransactionLogs.Query
            {
                Page = new Page(page, limit),
                ProjectId = projectId,
                StartDate = startDate,
                EndDate = endDate,
                Organizations = organizations,
                Subscriptions = subscriptions,
                Markets = markets,
                CashRegisters = cashRegisters,
                WithoutSubscription = withoutSubscription,
                Categories = categories,
                TransactionTypes = transactionTypes,
                SearchText = searchText,
                TimeZoneId = timeZoneId,
            });

            return results.Map(x => new TransactionLogGraphType(x));
        }
    }
}