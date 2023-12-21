using GraphQL.Conventions;
using NodaTime;
using Sig.App.Backend.Gql.Schema.Types;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Sig.App.Backend.Services.Reports
{
    public interface IReportService
    {
        Task<Stream> GenerateTransactionReport(IReportInput request);
    }

    public interface IReportInput
    {
        public Id ProjectId { get; set; }
        public LocalDate StartDate { get; set; }
        public LocalDate EndDate { get; set; }
        public IEnumerable<Id> Organizations { get; set; }
        public IEnumerable<Id> Subscriptions { get; set; }
        public Maybe<bool> WithoutSubscription { get; set; }
        public IEnumerable<Id> Categories { get; set; }
        public IEnumerable<string> TransactionTypes { get; set; }
        public Maybe<string> SearchText { get; set; }
        public string TimeZoneId { get; set; }
    }
}
