using GraphQL.Conventions;
using Sig.App.Backend.Gql.Schema.Enums;
using Sig.App.Backend.Gql.Schema.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Sig.App.Backend.Services.Reports
{
    public interface IReportService
    {
        Task<Stream> GenerateTransactionReport(IReportInput request);
        Task<Stream> GenerateTransactionReportForMarket(IReportForMarketInput request);
    }

    public interface IReportInput
    {
        public Id ProjectId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public IEnumerable<Id> Organizations { get; set; }
        public IEnumerable<Id> Subscriptions { get; set; }
        public Maybe<bool> WithoutSubscription { get; set; }
        public IEnumerable<Id> Categories { get; set; }
        public IEnumerable<Id> Markets { get; set; }
        public IEnumerable<Id> CashRegisters { get; set; }
        public IEnumerable<string> TransactionTypes { get; set; }
        public IEnumerable<string> GiftCardTransactionTypes { get; set; }
        public Maybe<string> SearchText { get; set; }
        public string TimeZoneId { get; set; }
        public Language Language { get; set; }
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
