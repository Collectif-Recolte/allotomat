using GraphQL.Conventions;
using Sig.App.Backend.DbModel.Entities.Profiles;
using Sig.App.Backend.Extensions;

namespace Sig.App.Backend.Gql.Schema.GraphTypes
{
    public class UserEmailOptInGraphType
    {
        private readonly UserEmailOptIn emailOptIn;

        public UserEmailOptInGraphType(UserEmailOptIn emailOptIn)
        {
            this.emailOptIn = emailOptIn;
        }

        public Id Id => emailOptIn.GetIdentifier();

        public bool CreatedCardPdfEmail => emailOptIn.CreatedCardPdfEmail;
        public bool MonthlyBalanceReportEmail => emailOptIn.MonthlyBalanceReportEmail;
        public bool MonthlyCardBalanceReportEmail => emailOptIn.MonthlyCardBalanceReportEmail;
        public bool SubscriptionExpirationEmail => emailOptIn.SubscriptionExpirationEmail;
    }
}