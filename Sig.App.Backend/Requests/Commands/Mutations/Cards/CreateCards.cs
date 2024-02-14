using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Projects;
using Sig.App.Backend.EmailTemplates.Models;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.PdfTemplates;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Requests.Queries.Projects;
using Sig.App.Backend.Services.Cards;
using Sig.App.Backend.Services.Files;
using Sig.App.Backend.Services.Mailer;
using Sig.App.Backend.Services.QRCode;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Cards
{
    public class CreateCards : IRequestHandler<CreateCards.Input, CreateCards.Payload>
    {
        private readonly ILogger<CreateCards> logger;
        private readonly AppDbContext db;
        private readonly IQRCodeService qrCodeService;
        private readonly IMailer mailer;
        private readonly IMediator mediator;
        private readonly ICardService cardService;

        public CreateCards(ILogger<CreateCards> logger, AppDbContext db, IQRCodeService qrCodeService, IMailer mailer, IMediator mediator, ICardService cardService)
        {
            this.logger = logger;
            this.db = db;
            this.qrCodeService = qrCodeService;
            this.mailer = mailer;
            this.mediator = mediator;
            this.cardService = cardService;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] CreateCards({request.ProjectId}, {request.Count})");
            if (request.Count <= 0) throw new CountMustBeHigherThanZeroException();

            var projectId = request.ProjectId.LongIdentifierForType<Project>();
            var project = await db.Projects.FirstOrDefaultAsync(x => x.Id == projectId, cancellationToken);

            if (project == null) throw new ProjectNotFoundException();

            var actualCount = await db.Cards.Where(x => x.ProjectId == projectId).CountAsync();
            var cards = new List<Card>();
            for (var i = 0; i < request.Count; i++)
            {
                actualCount++;
                var card = new Card()
                {
                    Project = project,
                    Status = DbModel.Enums.CardStatus.Unassigned,
                    ProgramCardId = actualCount,
                    CardNumber = await GenerateRandomUniqueNumber()
                };
                cards.Add(card);
            }

            db.Cards.AddRange(cards);
            await db.SaveChangesAsync();

            var items = cards.Select(x => new CardItem(qrCodeService)
            {
                Id = x.ProgramCardId,
                IdGQL = Id.New<Card>(x.Id).ToString(),
                UniqueCardId = x.CardNumber
            }).ToList();

            var xlsxStream = cardService.GenerateCardPrintFile(items);

            var xlsxFileName = $"{Guid.NewGuid()}.xlsx";
            var xlsxFile = await mediator.Send(new SaveTemporaryFile.Command
            {
                File = new FileInfos
                {
                    Content = xlsxStream,
                    ContentType = ContentTypes.Xlsx,
                    FileName = xlsxFileName
                }
            });

            var projectManagers = await mediator.Send(new GetProjectProjectManagers.Query
            {
                ProjectId = project.Id
            });

            if (projectManagers != null && projectManagers.Count > 0)
            {
                var email = new CreatedCardPdfEmail(string.Join(";", projectManagers.Select(x => x.Email)));

                xlsxStream.Position = 0;
                email.Attachments = new List<EmailAttachmentModel>() {
                    new EmailAttachmentModel(xlsxFileName, ContentTypes.Xlsx, xlsxStream)
                };

                await mailer.Send(email);
            }
            
            return new Payload()
            {
                Project = new ProjectGraphType(project),
                XlsxUrl = xlsxFile?.FileUrl
            };
        }

        private async Task<string> GenerateRandomUniqueNumber()
        {
            StringBuilder builder = null;
            Random RNG = new Random();

            do
            {
                builder = new StringBuilder();
                var count = 0;
                while (count < 4)
                {
                    var countIntern = 0;
                    while (countIntern < 4)
                    {
                        builder.Append(RNG.Next(10).ToString());
                        countIntern++;
                    }
                    count++;
                    if (count < 4)
                    {
                        builder.Append("-");
                    }
                }
            }
            while (await db.Cards.AnyAsync(x => x.CardNumber == builder.ToString()));

            return builder.ToString();
        }

        [MutationInput]
        public class Input : HaveProjectId, IRequest<Payload>
        {
            public long Count { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public ProjectGraphType Project { get; set; }
            public string XlsxUrl { get; set; }
        }

        public class CountMustBeHigherThanZeroException : RequestValidationException { }
        public class ProjectNotFoundException : RequestValidationException { }
    }
}
