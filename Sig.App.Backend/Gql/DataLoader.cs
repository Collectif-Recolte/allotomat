using GraphQL.DataLoader;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL.Conventions;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Requests.Queries.DataLoaders;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;

namespace Sig.App.Backend.Gql
{
    public class DataLoader : DataLoaderContext
    {
        private readonly ScopedDependencyInjectorFactory scopeFactory;

        public DataLoader(ScopedDependencyInjectorFactory scopeFactory)
        {
            this.scopeFactory = scopeFactory;
        }

        public IDataLoaderResult<UserGraphType> LoadUser(string id) =>
            LoadOne<GetUsersByIds.Query, UserGraphType, string>(id);

        public IDataLoaderResult<ProjectGraphType> LoadProject(long projectId) =>
            LoadOne<GetProjectsByIds.Query, ProjectGraphType, long>(projectId);

        public IDataLoaderResult<IProfileGraphType> LoadProfileByUserId(string userId) =>
            LoadOne<GetUserProfilesByUserIds.Query, IProfileGraphType, string>(userId);

        public IDataLoaderResult<OrganizationGraphType> LoadOrganization(long organizationId) =>
            LoadOne<GetOrganizationsByIds.Query, OrganizationGraphType, long>(organizationId);

        public IDataLoaderResult<MarketGraphType> LoadMarket(long marketId) =>
            LoadOne<GetMarketsByIds.Query, MarketGraphType, long>(marketId);

        public IDataLoaderResult<SubscriptionGraphType> LoadSubscriptionById(long subscriptionId) =>
            LoadOne<GetSubscriptionByIds.Query, SubscriptionGraphType, long>(subscriptionId);

        public IDataLoaderResult<IBeneficiaryGraphType> LoadBeneficiary(long beneficiaryId) =>
            LoadOne<GetBeneficiaryByIds.Query, IBeneficiaryGraphType, long>(beneficiaryId);

        public IDataLoaderResult<CardStatsGraphType> LoadCardStatsByProjectId(long projectId) =>
            LoadOne<GetCardStatsByIds.Query, CardStatsGraphType, long>(projectId);

        public IDataLoaderResult<BeneficiaryStatsGraphType> LoadBeneficiaryStatsByOrganizationId(long projectId) =>
            LoadOne<LoadBeneficiaryStatsByOrganizationIds.Query, BeneficiaryStatsGraphType, long>(projectId);

        public IDataLoaderResult<CardGraphType> LoadCardById(long cardId) =>
            LoadOne<GetCardByIds.Query, CardGraphType, long>(cardId);

        public IDataLoaderResult<CardGraphType> LoadCardByCardNumber(string cardNumber) =>
            LoadOne<GetCardByCardNumbers.Query, CardGraphType, string>(cardNumber);

        public IDataLoaderResult<CardGraphType> LoadBeneficiaryCard(long beneficiaryId) =>
            LoadOne<GetCardByBeneficiaryIds.Query, CardGraphType, long>(beneficiaryId);

        public IDataLoaderResult<IBeneficiaryGraphType> LoadCardBeneficiary(long cardId) =>
            LoadOne<GetBeneficiaryByCardIds.Query, IBeneficiaryGraphType, long>(cardId);

        public IDataLoaderResult<SubscriptionTypeGraphType> LoadSubscriptionType(long subscriptionTypeId) =>
            LoadOne<GetSubscriptionTypeByIds.Query, SubscriptionTypeGraphType, long>(subscriptionTypeId);

        public IDataLoaderResult<BeneficiaryTypeGraphType> LoadBeneficiaryTypeByBeneficiaryId(long beneficiaryId) =>
            LoadOne<GetBeneficiaryTypeByBeneficiaryId.Query, BeneficiaryTypeGraphType, long>(beneficiaryId);

        public IDataLoaderResult<BeneficiaryTypeGraphType> LoadBeneficiaryType(long beneficiaryTypeId) =>
            LoadOne<GetBeneficiaryType.Query, BeneficiaryTypeGraphType, long>(beneficiaryTypeId);

        public IDataLoaderResult<decimal> LoadOrganizationBudgetAllowanceTotal(long organizationId) =>
            LoadOne<GetOrganizationBudgetAllowanceTotal.Query, decimal, long>(organizationId);

        public IDataLoaderResult<bool> LoadSubscriptionHaveAnyBeneficiaries(long subscriptionId) =>
            LoadOne<GetSubscriptionHaveAnyBeneficiaries.Query, bool, long>(subscriptionId);

        public IDataLoaderResult<BudgetAllowanceGraphType> LoadBudgetAllowance(long budgetAllowanceId) =>
            LoadOne<GetBudgetAllowance.Query, BudgetAllowanceGraphType, long>(budgetAllowanceId);

        public IDataLoaderResult<ProductGroupGraphType> LoadProductGroup(long productGroupId) =>
            LoadOne<GetProductGroup.Query, ProductGroupGraphType, long>(productGroupId);

        public IDataLoaderResult<PaymentTransactionGraphType> LoadPaymentTransactionById(long paymentTransactionId) =>
            LoadOne<GetPaymentTransactionById.Query, PaymentTransactionGraphType, long>(paymentTransactionId);

        public IDataLoaderResult<IAddingFundTransactionGraphType> LoadAddingFundTransactionById(long addingFundTransactionId) =>
            LoadOne<GetAddingFundTransactionById.Query, IAddingFundTransactionGraphType, long>(addingFundTransactionId);

        public IDataLoaderResult<RefundTransactionGraphType> LoadRefundTransactionById(long refundTransactionId) =>
            LoadOne<GetRefundTransactionById.Query, RefundTransactionGraphType, long>(refundTransactionId);

        public IDataLoaderResult<FundGraphType> LoadLoyaltyCardFund(long? cardId) =>
            LoadOne<GetLoyaltyFundByCardId.Query, FundGraphType, long?>(cardId);
        
        public IDataLoaderResult<OffPlatformBeneficiaryGraphType> LoadOffPlatformBeneficiary(long beneficiaryId) =>
            LoadOne<GetOffPlatformBeneficiaryByIds.Query, OffPlatformBeneficiaryGraphType, long>(beneficiaryId);

        public IDataLoaderResult<ProjectStatsGraphType> LoadProjectStats(long projectId) =>
            LoadOne<GetProjectStatsByIds.Query, ProjectStatsGraphType, long>(projectId);

        public IDataLoaderResult<ITransactionGraphType> LoadTransactionByUniqueId(string transactionUniqueId) =>
            LoadOne<GetTransactionByUniqueId.Query, ITransactionGraphType, string>(transactionUniqueId);

        public IDataLoaderResult<IEnumerable<ProjectGraphType>> LoadProjectOwnedByUser(string userId) =>
            LoadCollection<GetProjectOwnedByUserId.Query, ProjectGraphType, string>(userId);

        public IDataLoaderResult<IEnumerable<MarketGraphType>> LoadProjectMarkets(long projectId) =>
            LoadCollection<GetMarketByProjectId.Query, MarketGraphType, long>(projectId);

        public IDataLoaderResult<IEnumerable<ProjectGraphType>> LoadMarketProjects(long marketId) =>
            LoadCollection<GetProjectByMarketId.Query, ProjectGraphType, long>(marketId);

        public IDataLoaderResult<IEnumerable<OrganizationGraphType>> LoadProjectOrganizations(long projectId) =>
            LoadCollection<GetOrganizationByProjectId.Query, OrganizationGraphType, long>(projectId);

        public IDataLoaderResult<IEnumerable<SubscriptionGraphType>> LoadProjectSubscriptions(long projectId) =>
            LoadCollection<GetSubscriptionsByProjectId.Query, SubscriptionGraphType, long>(projectId);

        public IDataLoaderResult<IEnumerable<SubscriptionTypeGraphType>> LoadSubscriptionSubscriptionTypes(long subscriptionId) =>
            LoadCollection<GetSubscriptionTypesBySubscriptionId.Query, SubscriptionTypeGraphType, long>(subscriptionId);

        public IDataLoaderResult<IEnumerable<OrganizationGraphType>> LoadOrganizationsOwnedByUser(string userId) =>
            LoadCollection<GetOrganizationOwnedByUserId.Query, OrganizationGraphType, string>(userId);

        public IDataLoaderResult<IEnumerable<IBeneficiaryGraphType>> LoadOrganizationBeneficiaires(long organizationId) =>
            LoadCollection<GetBeneficiaryByOrganizationId.Query, IBeneficiaryGraphType, long>(organizationId);

        public IDataLoaderResult<IEnumerable<MarketGraphType>> LoadMarketOwnedByUser(string userId) =>
            LoadCollection<GetMarketOwnedByUserId.Query, MarketGraphType, string>(userId);

        public IDataLoaderResult<IEnumerable<Transaction>> LoadTransactionByCardId(long cardId) =>
            LoadCollection<GetTransactionsByCardId.Query, Transaction, long>(cardId);

        public IDataLoaderResult<IEnumerable<BeneficiaryTypeGraphType>> LoadProjectBeneficiaryTypes(long projectId) =>
            LoadCollection<GetBeneficiaryTypesByProjectId.Query, BeneficiaryTypeGraphType, long>(projectId);

        public IDataLoaderResult<IEnumerable<BeneficiaryGraphType>> LoadBeneficiariesByBeneficiaryType(long beneficiaryTypeId) =>
            LoadCollection<GetBeneficiariesByBeneficiaryTypeId.Query, BeneficiaryGraphType, long>(beneficiaryTypeId);
        
        public IDataLoaderResult<IEnumerable<SubscriptionGraphType>> LoadBeneficiarySubscriptions(long beneficiaryId) =>
            LoadCollection<GetBeneficiarySubscriptions.Query, SubscriptionGraphType, long>(beneficiaryId);

        public IDataLoaderResult<IEnumerable<BeneficiarySubscriptionTypeGraphType>> LoadBeneficiarySubscriptionsGraphType(long beneficiaryId) =>
            LoadCollection<GetBeneficiarySubscriptionTypes.Query, BeneficiarySubscriptionTypeGraphType, long>(beneficiaryId);

        public IDataLoaderResult<IEnumerable<BudgetAllowanceGraphType>> LoadOrganizationBudgetAllowance(long organizationId) =>
            LoadCollection<GetBudgetAllowanceByOrganizationId.Query, BudgetAllowanceGraphType, long>(organizationId);

        public IDataLoaderResult<IEnumerable<BudgetAllowanceGraphType>> LoadSubscriptionBudgetAllowance(long subscriptionId) =>
            LoadCollection<GetBudgetAllowanceBySubscriptionId.Query, BudgetAllowanceGraphType, long>(subscriptionId);

        public IDataLoaderResult<IEnumerable<ITransactionGraphType>> LoadMarketTransactions(long marketId) =>
            LoadCollection<GetMarketTransactions.Query, ITransactionGraphType, long>(marketId);

        public IDataLoaderResult<IEnumerable<ProductGroupGraphType>> LoadProjectProductGroups(long projectId) =>
            LoadCollection<GetProductGroupsByProjectId.Query, ProductGroupGraphType, long>(projectId);

        public IDataLoaderResult<IEnumerable<FundGraphType>> LoadSubscriptionCardFunds(long? cardId) =>
            LoadCollection<GetSubscriptionFundByCardId.Query, FundGraphType, long?>(cardId);
        
        public IDataLoaderResult<IEnumerable<SubscriptionTypeGraphType>> LoadProductGroupSubscriptionType(long productGroupId) =>
            LoadCollection<GetSubscriptionTypeByProductGroupId.Query, SubscriptionTypeGraphType, long>(productGroupId);

        public IDataLoaderResult<IEnumerable<PaymentTransactionProductGroupGraphType>> LoadPaymentTransactionsProductGroupByTransactionId(long paymentTransactionId) =>
            LoadCollection<GetPaymentTransactionProductGroupByTransactionId.Query, PaymentTransactionProductGroupGraphType, long>(paymentTransactionId);

        public IDataLoaderResult<IEnumerable<RefundTransactionProductGroupGraphType>> LoadRefundTransactionsProductGroupByTransactionId(long refundTransactionId) =>
            LoadCollection<GetRefundTransactionProductGroupByTransactionId.Query, RefundTransactionProductGroupGraphType, long>(refundTransactionId);

        public IDataLoaderResult<IEnumerable<TransactionLogProductGroupGraphType>> LoadTransactionLogProductGroupByTransactionLogId(long transactionLogId) =>
            LoadCollection<GetTransactionLogProductGroupByTransactionLogId.Query, TransactionLogProductGroupGraphType, long>(transactionLogId);

        public IDataLoaderResult<IEnumerable<PaymentFundGraphType>> LoadPaymentFundsByBeneficiary(long offPlatformBeneficiaryId) =>
            LoadCollection<GetPaymentFundsByBeneficiaryId.Query, PaymentFundGraphType, long>(offPlatformBeneficiaryId);

        public IDataLoaderResult<IEnumerable<TransactionGraphType>> LoadSubscriptionTransactionsByBeneficiaryAndSubscriptionId(long beneficiaryId, long subscriptionId) =>
            LoadCollection<GetSubscriptionTransactionsByBeneficiaryAndSubscriptionId.Query, TransactionGraphType, long, long>(beneficiaryId, subscriptionId, x => x.ToString());

        public IDataLoaderResult<IEnumerable<PaymentTransactionAddingFundTransactionGraphType>> LoadPaymentTransactionAddingFundTransactionsByTransactionId(long transactionId) =>
            LoadCollection<GetPaymentTransactionAddingFundTransactionsByTransactionId.Query, PaymentTransactionAddingFundTransactionGraphType, long>(transactionId);

        public IDataLoaderResult<IEnumerable<SubscriptionTypeGraphType>> LoadSubscriptionTypeByBeneficiaryAndSubscriptionId(long beneficiaryId, long subscriptionId) =>
            LoadCollection<GetSubscriptionTypeByBeneficiaryAndSubscriptionId.Query, SubscriptionTypeGraphType, long, long>(beneficiaryId, subscriptionId, x => x.ToString());

        private IDataLoaderResult<TResult> LoadOne<TQuery, TResult, TKey>(TKey id) where TQuery : IRequest<IDictionary<TKey, TResult>>, IIdListQuery<TKey>, new()
        {
            return LoadOne(typeof(TQuery).FullName, DoLoad, id);

            async Task<IDictionary<TKey, TResult>> DoLoad(IEnumerable<TKey> ids, CancellationToken cancellationToken)
            {
                using var scope = scopeFactory.CreateScopedInjector();
                var mediator = scope.Resolve<IMediator>();
                
                var query = new TQuery { Ids = ids };
                return await mediator.Send(query, cancellationToken);
            }
        }

        private IDataLoaderResult<TResult> LoadOne<TQuery, TResult, TGroup, TKey>(TGroup group, TKey id, Func<TGroup, string> stringifyGroup = null)
            where TQuery : IRequest<IDictionary<TKey, TResult>>, IIdListQuery<TKey>, IHaveGroup<TGroup>, new()
        {
            if (stringifyGroup == null) stringifyGroup = x => x.ToString();

            return LoadOne($"{typeof(TQuery).FullName}:{stringifyGroup(group)}", DoLoad, id);

            async Task<IDictionary<TKey, TResult>> DoLoad(IEnumerable<TKey> ids, CancellationToken cancellationToken)
            {
                using var scope = scopeFactory.CreateScopedInjector();
                var mediator = scope.Resolve<IMediator>();
                
                var query = new TQuery { Group = group, Ids = ids };
                return await mediator.Send(query, cancellationToken);
            }
        }

        private IDataLoaderResult<IEnumerable<TResult>> LoadCollection<TQuery, TResult, TKey>(TKey id) where TQuery : IRequest<ILookup<TKey, TResult>>, IIdListQuery<TKey>, new()
        {
            return LoadCollection(typeof(TQuery).FullName, DoLoad, id);

            async Task<ILookup<TKey, TResult>> DoLoad(IEnumerable<TKey> ids, CancellationToken cancellationToken)
            {
                using var scope = scopeFactory.CreateScopedInjector();
                var mediator = scope.Resolve<IMediator>();
                
                var query = new TQuery { Ids = ids };
                return await mediator.Send(query, cancellationToken);
            }
        }

        private IDataLoaderResult<IEnumerable<TResult>> LoadCollection<TQuery, TResult, TGroup, TKey>(TGroup group, TKey id, Func<TGroup, string> stringifyGroup = null)
            where TQuery : IRequest<ILookup<TKey, TResult>>, IIdListQuery<TKey>, IHaveGroup<TGroup>, new()
        {
            if (stringifyGroup == null) stringifyGroup = x => x.ToString();

            return LoadCollection($"{typeof(TQuery).FullName}:{stringifyGroup(group)}", DoLoad, id);

            async Task<ILookup<TKey, TResult>> DoLoad(IEnumerable<TKey> ids, CancellationToken cancellationToken)
            {
                using var scope = scopeFactory.CreateScopedInjector();
                var mediator = scope.Resolve<IMediator>();
                
                var query = new TQuery { Group = group, Ids = ids };
                return await mediator.Send(query, cancellationToken);
            }
        }


        private IDataLoaderResult<TResult> LoadOne<TKey, TResult>(string loaderKey, Func<IEnumerable<TKey>, CancellationToken, Task<IDictionary<TKey, TResult>>> loader, TKey key)
        {
            return this.GetOrAddBatchLoader(loaderKey, loader).LoadAsync(key);
        }

        private IDataLoaderResult<IEnumerable<TResult>> LoadCollection<TKey, TResult>(string loaderKey, Func<IEnumerable<TKey>, CancellationToken, Task<ILookup<TKey, TResult>>> loader, TKey key)
        {
            return this.GetOrAddCollectionBatchLoader(loaderKey, loader).LoadAsync(key);
        }
    }
}