﻿using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Beneficiaries
{
    public class DeleteBeneficiaryType : AsyncRequestHandler<DeleteBeneficiaryType.Input>
    {
        private readonly ILogger<DeleteBeneficiaryType> logger;
        private readonly AppDbContext db;

        public DeleteBeneficiaryType(ILogger<DeleteBeneficiaryType> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        protected override async Task Handle(Input request, CancellationToken cancellationToken)
        {
            var beneficiaryTypeId = request.BeneficiaryTypeId.LongIdentifierForType<BeneficiaryType>();
            var beneficiaryType = await db.BeneficiaryTypes
                .Include(x => x.Beneficiaries)
                .FirstOrDefaultAsync(x => x.Id == beneficiaryTypeId, cancellationToken);

            if (beneficiaryType == null) throw new BeneficiaryTypeNotFoundException();

            if (HaveAnyBeneficiaries(beneficiaryType)) throw new BeneficiaryTypeCantHaveBeneficiariesException();

            db.BeneficiaryTypes.Remove(beneficiaryType);

            await db.SaveChangesAsync();
            logger.LogInformation($"Beneficiary type deleted ({beneficiaryTypeId}, {beneficiaryType.Name})");
        }

        private bool HaveAnyBeneficiaries(BeneficiaryType beneficiaryType)
        {
            return beneficiaryType.Beneficiaries.Any();
        }

        [MutationInput]
        public class Input : IRequest, IHaveBeneficiaryTypeId
        {
            public Id BeneficiaryTypeId { get; set; }
        }

        public class BeneficiaryTypeNotFoundException : RequestValidationException { }
        public class BeneficiaryTypeCantHaveBeneficiariesException : RequestValidationException { }
    }
}
