using GraphQL.Conventions;
using GraphQL.DataLoader;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class BudgetAllowanceGraphType
    {
        private readonly BudgetAllowance budgetAllowance;

        public BudgetAllowanceGraphType(BudgetAllowance budgetAllowance)
        {
            this.budgetAllowance = budgetAllowance;
        }

        public Id Id => budgetAllowance.GetIdentifier();

        public IDataLoaderResult<OrganizationGraphType> Organization(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadOrganization(budgetAllowance.OrganizationId);
        }

        public IDataLoaderResult<SubscriptionGraphType> Subscription(IAppUserContext ctx)
        {
            return ctx.DataLoader.LoadSubscriptionById(budgetAllowance.SubscriptionId);
        }

        public decimal OriginalFund => budgetAllowance.OriginalFund;
        public decimal AvailableFund => budgetAllowance.AvailableFund;
    }
}
