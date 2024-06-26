﻿using GraphQL.Conventions;
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Beneficiaries
{
    public class CreateBeneficiaryInOrganization : IRequestHandler<CreateBeneficiaryInOrganization.Input, CreateBeneficiaryInOrganization.Payload>
    {
        private readonly ILogger<CreateBeneficiaryInOrganization> logger;
        private readonly AppDbContext db;

        public CreateBeneficiaryInOrganization(ILogger<CreateBeneficiaryInOrganization> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] CreateBeneficiaryInOrganization({request.Firstname}, {request.Lastname}, {request.Id1}, {request.Id2}, {request.BeneficiaryTypeId})");
            var organizationId = request.OrganizationId.LongIdentifierForType<Organization>();
            var organization = await db.Organizations.FirstOrDefaultAsync(x => x.Id == organizationId, cancellationToken);

            if (organization == null)
            {
                logger.LogWarning("[Mutation] CreateBeneficiaryInOrganization - OrganizationNotFoundException");
                throw new OrganizationNotFoundException();
            }

            var beneficiaryTypeId = request.BeneficiaryTypeId.LongIdentifierForType<BeneficiaryType>();
            var beneficiaryType = await db.BeneficiaryTypes.FirstOrDefaultAsync(x => x.Id == beneficiaryTypeId, cancellationToken);

            if (beneficiaryType == null)
            {
                logger.LogWarning("[Mutation] CreateBeneficiaryInOrganization - BeneficiaryTypeNotFoundException");
                throw new BeneficiaryTypeNotFoundException();
            }

            var lastBeneficiary = await db.Beneficiaries.Where(x => x.OrganizationId == organizationId).OrderBy(x => x.SortOrder).LastOrDefaultAsync();
            var sortOrder = lastBeneficiary != null ? lastBeneficiary.SortOrder + 1 : 0;

            var beneficiary = new Beneficiary()
            {
                ID1 = request.Id1 ?? Guid.NewGuid().ToString(),
                Firstname = request.Firstname,
                Lastname = request.Lastname,
                Email = request.Email,
                Address = request.Address,
                Phone = request.Phone,
                Organization = organization,
                Notes = request.Notes,
                BeneficiaryType = beneficiaryType,
                PostalCode = request.PostalCode,
                ID2 = request.Id2,
                SortOrder = sortOrder
            };

            db.Beneficiaries.Add(beneficiary);
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"[Mutation] CreateBeneficiaryInOrganization - New beneficiary created {beneficiary.Firstname} {beneficiary.Lastname} ({beneficiary.Id})");

            return new Payload
            {
                Beneficiary = new BeneficiaryGraphType(beneficiary)
            };
        }

        [MutationInput]
        public class Input : HaveOrganizationId, IRequest<Payload>
        {
            public string Firstname { get; set; }
            public string Lastname { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public string Address { get; set; }
            public string Notes { get; set; }
            public string PostalCode { get; set; }
            public string Id1 { get; set; }
            public string Id2 { get; set; }

            public Id BeneficiaryTypeId { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public BeneficiaryGraphType Beneficiary { get; set; }
        }

        public class OrganizationNotFoundException : RequestValidationException { }
        public class BeneficiaryTypeNotFoundException : RequestValidationException { }
    }
}
