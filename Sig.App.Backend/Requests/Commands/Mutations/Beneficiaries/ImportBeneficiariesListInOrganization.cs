using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.Organizations;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Beneficiaries
{
    public class ImportBeneficiariesListInOrganization : IRequestHandler<ImportBeneficiariesListInOrganization.Input, ImportBeneficiariesListInOrganization.Payload>
    {
        private readonly ILogger<ImportBeneficiariesListInOrganization> logger;
        private readonly AppDbContext db;

        public ImportBeneficiariesListInOrganization(ILogger<ImportBeneficiariesListInOrganization> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] ImportBeneficiariesListInOrganization({request.Items.Count})");
            var organizationId = request.OrganizationId.LongIdentifierForType<Organization>();
            var organization = await db.Organizations.Include(x => x.Project).FirstOrDefaultAsync(x => x.Id == organizationId, cancellationToken);

            if (organization == null)
            {
                logger.LogWarning("[Mutation] ImportBeneficiariesListInOrganization - OrganizationNotFoundException");
                throw new OrganizationNotFoundException();
            }
            if (organization.Project.AdministrationSubscriptionsOffPlatform)
            {
                logger.LogWarning("[Mutation] ImportBeneficiariesListInOrganization - ProjectAdministrateSubscriptionOffPlatformException");
                throw new ProjectAdministrateSubscriptionOffPlatformException();
            }

            var beneficiaryTypes = await db.BeneficiaryTypes.Where(x => x.ProjectId == organization.ProjectId).ToListAsync();

            var beneficiaries = new List<Beneficiary>();
            var sortOrder = 0;
            var currentBeneficiaries = await db.Beneficiaries.Where(x => x.OrganizationId == organization.Id).OrderBy(x => x.SortOrder).ToListAsync();

            foreach (var item in request.Items)
            {
                BeneficiaryType beneficiaryType = beneficiaryTypes.FirstOrDefault(x => x.GetKeys().Contains(item.Key.Trim().ToLower()));

                if (beneficiaryType == null)
                {
                    logger.LogWarning($"[Mutation] ImportBeneficiariesListInOrganization - BeneficiaryTypeNotFoundException ({item.Key})");
                    throw new BeneficiaryTypeNotFoundException();
                }

                var beneficiary = currentBeneficiaries.Where(x => x.ID1 == item.Id1).FirstOrDefault();

                if (beneficiary == null) {
                    beneficiary = new Beneficiary()
                    {
                        ID1 = item.Id1,
                        Organization = organization,
                    };
                    db.Beneficiaries.Add(beneficiary);
                    logger.LogInformation($"[Mutation] ImportBeneficiariesListInOrganization - New beneficiary created {beneficiary.Firstname} {beneficiary.Lastname}");
                }
                else
                {
                    currentBeneficiaries.Remove(beneficiary);
                }

                beneficiary.Firstname = item.Firstname?.Trim();
                beneficiary.Lastname = item.Lastname?.Trim();
                beneficiary.Email = item.Email?.Trim();
                beneficiary.Address = item.Address?.Trim();
                beneficiary.Phone = item.Phone?.Trim();
                beneficiary.Notes = item.Notes?.Trim();
                beneficiary.BeneficiaryType = beneficiaryType;
                beneficiary.SortOrder = sortOrder++;
                beneficiary.PostalCode = item.PostalCode?.Trim();
                beneficiary.ID1 = item.Id1?.Trim();
                beneficiary.ID2 = item.Id2?.Trim();

                beneficiaries.Add(beneficiary);
            }

            foreach (var beneficiary in currentBeneficiaries)
            {
                beneficiary.SortOrder = sortOrder++;
            }

            await db.SaveChangesAsync(cancellationToken);

            return new Payload
            {
                Beneficiaries = beneficiaries.Select(x => new BeneficiaryGraphType(x))
            };
        }

        [MutationInput]
        public class Input : HaveOrganizationId, IRequest<Payload>
        {
            public List<BeneficiaryItem> Items { get; set; }
        }

        [InputType]
        public class BeneficiaryItem
        {
            public string Firstname { get; set; }
            public string Lastname { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public string Address { get; set; }
            public string Notes { get; set; }
            public string Key { get; set; }
            public string PostalCode { get; set; }
            public string Id1 { get; set; }
            public string Id2 { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public IEnumerable<BeneficiaryGraphType> Beneficiaries { get; set; }
        }

        public class OrganizationNotFoundException : RequestValidationException { }
        public class ProjectAdministrateSubscriptionOffPlatformException : RequestValidationException { }
        public class BeneficiaryTypeNotFoundException : RequestValidationException { }
    }
}
