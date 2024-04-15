using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Plugins.MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Queries.Beneficiaries
{
    public class ForecastImportOffPlatformBeneficiariesListInOrganization : IRequestHandler<ForecastImportOffPlatformBeneficiariesListInOrganization.Input, ForecastImportOffPlatformBeneficiariesListInOrganization.ImportOffPlatformBeneficiariesListPayload>
    {
        private IClock clock;
        private readonly AppDbContext db;

        public ForecastImportOffPlatformBeneficiariesListInOrganization(IClock clock, AppDbContext db)
        {
            this.clock = clock;
            this.db = db;
        }

        public async Task<ImportOffPlatformBeneficiariesListPayload> Handle(Input request, CancellationToken cancellationToken)
        {
            var today = clock.GetCurrentInstant().ToDateTimeUtc();

            var organizationId = request.OrganizationId.LongIdentifierForType<Organization>();
            var organization = await db.Organizations.Include(x => x.Project).FirstOrDefaultAsync(x => x.Id == organizationId, cancellationToken);

            if (organization == null) throw new OrganizationNotFoundException();

            if (!organization.Project.AdministrationSubscriptionsOffPlatform) throw new ProjectDontAdministrateSubscriptionOffPlatformException();

            var addedBeneficiaries = 0;
            var modifiedBeneficiaries = 0;
            var missingBeneficiaries = 0;

            var currentBeneficiaries = await db.Beneficiaries.Where(x => x.OrganizationId == organization.Id).ToListAsync();

            foreach (var item in request.Items)
            {
                var beneficiary = currentBeneficiaries.FirstOrDefault(x => x.ID1 == item.Id1);
                if (beneficiary == null)
                {
                    addedBeneficiaries++;
                }
                else
                {
                    currentBeneficiaries.Remove(beneficiary);
                    if (item.EndDate > today)
                    {
                        if (!(beneficiary as OffPlatformBeneficiary).IsActive)
                        {
                            addedBeneficiaries++;
                        }
                        else
                        {
                            modifiedBeneficiaries++;
                        }
                    }
                    else
                    {
                        missingBeneficiaries++;
                    }
                }
            }

            return new ImportOffPlatformBeneficiariesListPayload()
            {
                AddedBeneficiaries = addedBeneficiaries,
                ModifiedBeneficiaries = modifiedBeneficiaries,
                MissingBeneficiaries = currentBeneficiaries.Count + missingBeneficiaries
            };
        }

        public class Input : HaveOrganizationId, IRequest<ImportOffPlatformBeneficiariesListPayload>
        {
            public ForecastOffPlatformBeneficiaryItem[] Items { get; set; }
        }

        public class ForecastOffPlatformBeneficiaryItem
        {
            public string Id1 { get; set; }
            public DateTime EndDate { get; set; }
        }

        public class ImportOffPlatformBeneficiariesListPayload
        {
            public int AddedBeneficiaries { get; set; }
            public int ModifiedBeneficiaries { get; set; }
            public decimal MissingBeneficiaries { get; set; }
        }

        public class OrganizationNotFoundException : RequestValidationException { }
        public class ProjectDontAdministrateSubscriptionOffPlatformException : RequestValidationException { }
    }
}