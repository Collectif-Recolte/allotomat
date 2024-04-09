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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sig.App.Backend.Requests.Commands.Mutations.Beneficiaries
{
    public class EditBeneficiaryType : IRequestHandler<EditBeneficiaryType.Input, EditBeneficiaryType.Payload>
    {
        private readonly ILogger<EditBeneficiaryType> logger;
        private readonly AppDbContext db;

        public EditBeneficiaryType(ILogger<EditBeneficiaryType> logger, AppDbContext db)
        {
            this.logger = logger;
            this.db = db;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            logger.LogInformation($"[Mutation] EditBeneficiaryType({request.BeneficiaryTypeId}, {request.Name}, {request.Keys})");
            var beneficiaryTypeId = request.BeneficiaryTypeId.LongIdentifierForType<BeneficiaryType>();
            var beneficiaryType = await db.BeneficiaryTypes.FirstOrDefaultAsync(x => x.Id == beneficiaryTypeId, cancellationToken);

            if (beneficiaryType == null)
            {
                logger.LogWarning("[Mutation] EditBeneficiaryType - BeneficiaryTypeNotFoundException");
                throw new BeneficiaryTypeNotFoundException();
            }

            var beneficiaryTypes = await db.BeneficiaryTypes.Where(x => x.ProjectId == beneficiaryType.ProjectId && x.Id != beneficiaryType.Id).ToListAsync();
            var beneficiaryTypesKeys = beneficiaryTypes.SelectMany(x => x.GetKeys());

            if (request.Keys.Where(x => beneficiaryTypesKeys.Contains(x.Trim().ToLower())).Any())
            {
                logger.LogWarning("[Mutation] EditBeneficiaryType - BeneficiaryTypeKeyAlreadyInUseException");
                throw new BeneficiaryTypeKeyAlreadyInUseException();
            }

            request.Name.IfSet(v => beneficiaryType.Name = v.Trim());
            beneficiaryType.SetKeys(request.Keys);

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"[Mutation] EditBeneficiaryType - Beneficiary type edited {beneficiaryType.Name} ({beneficiaryType.Id})");

            return new Payload
            {
                BeneficiaryType = new BeneficiaryTypeGraphType(beneficiaryType)
            };
        }

        [MutationInput]
        public class Input : HaveBeneficiaryTypeId, IRequest<Payload>
        {
            public Maybe<NonNull<string>> Name { get; set; }
            public string[] Keys { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public BeneficiaryTypeGraphType BeneficiaryType { get; set; }
        }

        public class BeneficiaryTypeNotFoundException : RequestValidationException { }
        public class BeneficiaryTypeKeyAlreadyInUseException: RequestValidationException { }
    }
}
