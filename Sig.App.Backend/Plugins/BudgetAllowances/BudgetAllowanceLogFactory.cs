using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.BudgetAllowanceLogs;
using Sig.App.Backend.DbModel.Entities.BudgetAllowances;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using System.Threading.Tasks;

namespace Sig.App.Backend.Plugins.BudgetAllowances
{

    public class BudgetAllowanceLogFactory(
        IClock clock,
        IHttpContextAccessor httpContextAccessor,
        AppDbContext db)
    {
        public async Task<BudgetAllowanceLog> CreateLog(
            BudgetAllowanceLogDiscriminator discriminator,
            decimal amount,
            BudgetAllowance budgetAllowance,
            BudgetAllowance? targetBudgetAllowance = null
        )
        {
            var currentUserId = httpContextAccessor.HttpContext?.User.GetUserId();
            var currentUser = await db.Users.Include(x => x.Profile).FirstOrDefaultAsync(x => x.Id == currentUserId);

            var budgetAllowanceLog = new BudgetAllowanceLog()
            {
                Discriminator = discriminator,
                CreatedAtUtc = clock.GetCurrentInstant().ToDateTimeUtc(),
                Amount = amount,
                ProjectId = budgetAllowance.Organization.ProjectId,
                BudgetAllowanceId = budgetAllowance.Id,
                InitiatorId = currentUserId,
                InitiatorEmail = currentUser?.Email,
                InitiatorFirstname = currentUser?.Profile.FirstName,
                InitiatorLastname = currentUser?.Profile.LastName,
                OrganizationId = budgetAllowance.Organization.Id,
                OrganizationName = budgetAllowance.Organization.Name,
                SubscriptionId = budgetAllowance.Subscription.Id,
                SubscriptionName = budgetAllowance.Subscription.Name
            };

            if (targetBudgetAllowance != null) {
                budgetAllowanceLog.TargetBudgetAllowanceId = targetBudgetAllowance.Id;
                budgetAllowanceLog.TargetOrganizationId = targetBudgetAllowance.Organization.Id;
                budgetAllowanceLog.TargetOrganizationName = targetBudgetAllowance.Organization.Name;
                budgetAllowanceLog.TargetSubscriptionId = targetBudgetAllowance.Subscription.Id;
                budgetAllowanceLog.TargetSubscriptionName = targetBudgetAllowance.Subscription.Name;
            }

            return budgetAllowanceLog;
        }
    }
}
