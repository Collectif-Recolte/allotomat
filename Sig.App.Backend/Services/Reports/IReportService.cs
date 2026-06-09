using GraphQL.Conventions;
using Sig.App.Backend.Gql.Schema.Enums;
using Sig.App.Backend.Requests.Queries.Transactions;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Sig.App.Backend.Services.Reports
{
    public interface IReportService
    {
        Task<Stream> GenerateTransactionReport(IReportInput request);
        Task<Stream> GenerateTransactionReportForMarket(IReportForMarketInput request);
    }

    public interface IReportInput : ITransactionLogFilterCriteria
    {
        string TimeZoneId { get; set; }
        Language Language { get; set; }
    }

    public interface IReportForMarketInput
    {
        public Id MarketId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string TimeZoneId { get; set; }
        public Language Language { get; set; }
    }
}
