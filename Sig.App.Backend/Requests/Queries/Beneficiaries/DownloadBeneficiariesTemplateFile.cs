using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Sig.App.Backend.Constants;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Helpers;
using Sig.App.Backend.Plugins.MediatR;
using Sig.App.Backend.Services.Files;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using static Sig.App.Backend.Helpers.ExcelGenerator;

namespace Sig.App.Backend.Requests.Commands.Queries.Beneficiaries
{
    public class DownloadBeneficiariesTemplateFile : IRequestHandler<DownloadBeneficiariesTemplateFile.Input, string>
    {
        private readonly AppDbContext db;
        private readonly IMediator mediator;

        public DownloadBeneficiariesTemplateFile(AppDbContext db, IMediator mediator)
        {
            this.mediator = mediator;
            this.db = db;
        }

        public async Task<string> Handle(Input request, CancellationToken cancellationToken)
        {
            var organizationId = request.OrganizationId.LongIdentifierForType<Organization>();
            var organization = await db.Organizations.Include(x => x.Project).ThenInclude(x => x.ProductGroups).FirstOrDefaultAsync(x => x.Id == organizationId, cancellationToken);

            if (organization == null) throw new OrganizationNotFoundException();

            var generator = new ExcelGenerator();
            var dataWorksheet = generator.AddDataWorksheet("Liste des participants", new List<string>());

            dataWorksheet.Column("Prénom/Firstname", x => "");
            dataWorksheet.Column("Nom de famille/Lastname", x => "");
            dataWorksheet.Column("Courriel/Email", x => "");
            dataWorksheet.Column("Téléphone/Phone", x => "");
            dataWorksheet.Column("Adresse/Address", x => "");
            dataWorksheet.Column("Code postal/Postal code", x => "");
            dataWorksheet.Column("Notes/Briefing", x => "");
            dataWorksheet.Column("Id 1", x => "");
            dataWorksheet.Column("Id 2", x => "");
            dataWorksheet.Column("Date début/Start Date", x => "");
            dataWorksheet.Column("Fréquence versement/Payment Frequency", x => "");
            dataWorksheet.Column("Date de fin/End date", x => "");

            foreach (var productGroup in organization.Project.ProductGroups)
            {
                if (productGroup.Name != ProductGroupType.LOYALTY)
                {
                    dataWorksheet.Column("Montant - " + productGroup.Name, x => "");
                }
            }

            var result = await mediator.Send(new SaveTemporaryFile.Command
            {
                File = new FileInfos
                {
                    Content = generator.Render(),
                    ContentType = ContentTypes.Xlsx,
                    FileName = $"Participants_{organization.Name.Replace(" ", "")}.xlsx"
                }
            });

            return result.FileUrl;
        }

        public class Input : IRequest<string>
        {
            public Id OrganizationId { get; set; }
        }

        public class Payload
        {
            public string FileUrl { get; set; }
        }

        public class OrganizationNotFoundException : RequestValidationException { }
    }
}
