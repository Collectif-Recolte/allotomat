using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Bases;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Gql.Schema.Types;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Beneficiaries
{
    public class EditBeneficiary : IRequestHandler<EditBeneficiary.Input, EditBeneficiary.Payload>
    {
        private readonly ILogger<EditBeneficiary> logger;
        private readonly AppDbContext db;

        public EditBeneficiary(ILogger<EditBeneficiary> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] EditBeneficiary({request.BeneficiaryId}, {request.Firstname}, {request.Lastname}, {request.Id1}, {request.Id2}, {request.BeneficiaryTypeId})");
            var beneficiaryId = request.BeneficiaryId.LongIdentifierForType<Beneficiary>();
            var beneficiary = await db.Beneficiaries.FirstOrDefaultAsync(x => x.Id == beneficiaryId, cancellationToken);

            if (beneficiary == null) throw new BeneficiaryNotFoundException();

            request.Firstname.IfSet(v => beneficiary.Firstname = v.Trim());
            request.Lastname.IfSet(v => beneficiary.Lastname = v.Trim());
            request.Email.IfSet(v => beneficiary.Email = v.Trim());
            request.Phone.IfSet(v => beneficiary.Phone = v.Trim());
            request.Address.IfSet(v => beneficiary.Address = v.Trim());
            request.Notes.IfSet(v => beneficiary.Notes = v.Trim());
            request.PostalCode.IfSet(v => beneficiary.PostalCode = v.Trim());
            request.Id1.IfSet(v => beneficiary.ID1 = v.Trim());
            request.Id2.IfSet(v => beneficiary.ID2 = v.Trim());

            await request.BeneficiaryTypeId.IfSet(async v => await UpdateBeneficiaryType(beneficiary, v, cancellationToken));

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"Beneficiary edited {beneficiary.Firstname} {beneficiary.Lastname} ({beneficiary.Id})");

            return new Payload
            {
                Beneficiary = new BeneficiaryGraphType(beneficiary)
            };
        }

        private async Task UpdateBeneficiaryType(Beneficiary beneficiary, Id id, CancellationToken cancellationToken)
        {
            var beneficiaryTypeId = id.LongIdentifierForType<BeneficiaryType>();
            var beneficiaryType = await db.BeneficiaryTypes.FirstOrDefaultAsync(x => x.Id == beneficiaryTypeId, cancellationToken);

            if (beneficiaryType == null) throw new BeneficiaryTypeNotFoundException();

            beneficiary.BeneficiaryType = beneficiaryType;
        }

        [MutationInput]
        public class Input : HaveBeneficiaryId, IRequest<Payload>
        {
            public Maybe<NonNull<string>> Firstname { get; set; }
            public Maybe<NonNull<string>> Lastname { get; set; }
            public Maybe<NonNull<string>> Email { get; set; }
            public Maybe<NonNull<string>> Phone { get; set; }
            public Maybe<NonNull<string>> Address { get; set; }
            public Maybe<NonNull<string>> Notes { get; set; }
            public Maybe<NonNull<string>> PostalCode { get; set; }
            public Maybe<NonNull<string>> Id1 { get; set; }
            public Maybe<NonNull<string>> Id2 { get; set; }
            public Maybe<Id> BeneficiaryTypeId { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public BeneficiaryGraphType Beneficiary { get; set; }
        }

        public class BeneficiaryNotFoundException : RequestValidationException { }
        public class BeneficiaryTypeNotFoundException : RequestValidationException { }
    }
}
