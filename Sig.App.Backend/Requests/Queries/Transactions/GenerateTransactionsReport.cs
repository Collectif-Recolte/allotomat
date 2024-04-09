using GraphQL.Conventions;
using MediatR;
using Sig.App.Backend.Constants;
using Sig.App.Backend.Gql.Schema.Types;
using Sig.App.Backend.Services.Files;
using Sig.App.Backend.Services.Reports;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sig.App.Backend.Gql.Schema.Enums;

namespace Sig.App.Backend.Requests.Commands.Queries.Transactions
{
    public class GenerateTransactionsReport : IRequestHandler<GenerateTransactionsReport.Input, string>
    {
        private readonly IReportService reportService;
        private readonly IMediator mediator;

        public GenerateTransactionsReport(IReportService reportService, IMediator mediator)
        {
            this.reportService = reportService;
            this.mediator = mediator;
        }

        public async Task<string> Handle(Input request, CancellationToken cancellationToken)
        {
            var report = await reportService.GenerateTransactionReport(request);

            var result = await mediator.Send(new SaveTemporaryFile.Command
            {
                File = new FileInfos
                {
                    Content = report,
                    ContentType = ContentTypes.Xlsx,
                    FileName = $"{Guid.NewGuid()}.xlsx"
                }
            }, cancellationToken);

            return result.FileUrl;
        }

        public class Input : IRequest<string>, IReportInput
        {
            public Id ProjectId { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public IEnumerable<Id> Organizations { get; set; }
            public IEnumerable<Id> Subscriptions { get; set; }
            public Maybe<bool> WithoutSubscription { get; set; }
            public IEnumerable<Id> Categories { get; set; }
            public IEnumerable<string> TransactionTypes { get; set; }
            public Maybe<string> SearchText { get; set; }
            public string TimeZoneId { get; set; }
            public Language Language { get; set; }
        }

        public class Payload
        {
            public string FileUrl { get; set; }
        }
    }
}
