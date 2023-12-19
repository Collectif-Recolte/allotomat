using GraphQL.Conventions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sig.App.Backend.DbModel;
using Sig.App.Backend.DbModel.Entities.Cards;
using Sig.App.Backend.DbModel.Entities.Transactions;
using Sig.App.Backend.DbModel.Enums;
using Sig.App.Backend.Extensions;
using Sig.App.Backend.Gql.Interfaces;
using Sig.App.Backend.Gql.Schema.GraphTypes;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Plugins.MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NodaTime;
using Sig.App.Backend.DbModel.Entities.Beneficiaries;
using Sig.App.Backend.DbModel.Entities.TransactionLogs;
using Sig.App.Backend.Helpers;

namespace Sig.App.Backend.Requests.Commands.Mutations.Cards
{
    public class TransfertCard : IRequestHandler<TransfertCard.Input, TransfertCard.Payload>
    {
        private readonly ILogger<TransfertCard> logger;
        private readonly AppDbContext db;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IClock clock;

        public TransfertCard(ILogger<TransfertCard> logger, AppDbContext db, IClock clock, IHttpContextAccessor httpContextAccessor)
        {
            this.logger = logger;
            this.db = db;
            this.clock = clock;
            this.httpContextAccessor = httpContextAccessor;
        }

        public async Task<Payload> Handle(Input request, CancellationToken cancellationToken)
        {
            var currentUserId = httpContextAccessor.HttpContext?.User.GetUserId();
            var currentUser = db.Users.Include(x => x.Profile).FirstOrDefault(x => x.Id == currentUserId);
            
            var originalCardId = request.OriginalCardId.LongIdentifierForType<Card>();
            var originalCard = await db.Cards.Include(x => x.Beneficiary).ThenInclude(x => x.Organization).ThenInclude(x => x.Project).Include(x => x.Transactions).Include(x => x.Funds).FirstOrDefaultAsync(x => x.Id == originalCardId, cancellationToken);

            if (originalCard == null) throw new OriginalCardNotFoundException();

            var newCard = await db.Cards.FirstOrDefaultAsync(x => x.ProgramCardId == request.NewCardId && x.ProjectId == originalCard.ProjectId, cancellationToken);
            if (newCard == null) throw new NewCardNotFoundException();

            if (originalCard.Status != CardStatus.Assigned) throw new OriginalCardNotAssignException();
            if (newCard.Status == CardStatus.Assigned) throw new NewCardAlreadyAssignException();
            if (newCard.Status == CardStatus.GiftCard) throw new NewCardAlreadyGiftCardException();
            if (newCard.Status == CardStatus.Lost) throw new NewCardAlreadyLostException();

            if (originalCard.ProjectId != newCard.ProjectId) throw new NewCardNotInProjectException();
            
            var today = clock.GetCurrentInstant().InUtc().ToDateTimeUtc();

            foreach (var transaction in originalCard.Transactions)
            {
                transaction.CardId = newCard.Id;
            }

            foreach (var fund in originalCard.Funds)
            {
                fund.CardId = newCard.Id;
            }

            newCard.Funds = originalCard.Funds;
            newCard.Transactions = originalCard.Transactions;
            newCard.Status = CardStatus.Assigned;
            newCard.Beneficiary = originalCard.Beneficiary;

            var addingFundTransactionsBySubscriptionId =
                await TransactionHelper.GroupAddingFundTransactionsBySubscriptionId(db,
                    originalCard.Transactions.OfType<AddingFundTransaction>().Where(x => x.Status == FundTransactionStatus.Actived && x.AvailableFund > 0).ToList(), cancellationToken);

            var subscriptions = await db.Subscriptions
                .Where(x => addingFundTransactionsBySubscriptionId.Select(y => y.Key).Contains(x.Id))
                .ToListAsync(cancellationToken);
            var productGroups = await db.ProductGroups
                .Where(x => originalCard.Transactions.OfType<AddingFundTransaction>().Select(y => y.ProductGroupId)
                    .Contains(x.Id)).ToListAsync(cancellationToken);
            
            foreach (var group in addingFundTransactionsBySubscriptionId)
            {
                var transactions = group.ToList();
                var subscription = subscriptions.FirstOrDefault(x => x.Id == group.Key);
                
                var transactionLogProductGroups = new List<TransactionLogProductGroup>();
                foreach (var productGroup in transactions.GroupBy(x => x.ProductGroupId))
                {
                    var currentProductGroup = productGroups.First(x => x.Id == productGroup.First().ProductGroupId);
                    transactionLogProductGroups.Add(new TransactionLogProductGroup()
                    {
                        Amount = productGroup.Sum(x => x.AvailableFund),
                        ProductGroupId = currentProductGroup.Id,
                        ProductGroupName = currentProductGroup.Name
                    });
                }
                
                db.TransactionLogs.Add(new TransactionLog()
                {
                    Discriminator = TransactionLogDiscriminator.TransferFundTransactionLog,
                    CreatedAtUtc = today,
                    TotalAmount = transactions.Sum(x => x.AvailableFund),
                    FundTransferredFromProgramCardId = originalCard.ProgramCardId,
                    FundTransferredFromCardNumber = originalCard.CardNumber,
                    CardProgramCardId = newCard.ProgramCardId,
                    CardNumber = newCard.CardNumber,
                    BeneficiaryId = newCard.Beneficiary.Id,
                    BeneficiaryID1 = newCard.Beneficiary.ID1,
                    BeneficiaryID2 = newCard.Beneficiary.ID2,
                    BeneficiaryFirstname = newCard.Beneficiary.Firstname,
                    BeneficiaryLastname = newCard.Beneficiary.Lastname,
                    BeneficiaryEmail = newCard.Beneficiary.Email,
                    BeneficiaryPhone = newCard.Beneficiary.Phone,
                    BeneficiaryIsOffPlatform = newCard.Beneficiary is OffPlatformBeneficiary,
                    BeneficiaryTypeId = newCard.Beneficiary.BeneficiaryTypeId,
                    OrganizationId = newCard.Beneficiary.OrganizationId,
                    OrganizationName = newCard.Beneficiary.Organization.Name,
                    SubscriptionId = subscription?.Id,
                    SubscriptionName = subscription?.Name,
                    ProjectId = newCard.Beneficiary.Organization.ProjectId,
                    ProjectName = newCard.Beneficiary.Organization.Project.Name,
                    TransactionLogProductGroups = transactionLogProductGroups,
                    TransactionInitiatorId = currentUserId,
                    TransactionInitiatorFirstname = currentUser?.Profile.FirstName,
                    TransactionInitiatorLastname = currentUser?.Profile.LastName,
                    TransactionInitiatorEmail = currentUser?.Email
                });
            }

            originalCard.Funds = new List<Fund>();
            originalCard.Beneficiary = null;
            originalCard.Status = CardStatus.Lost;
            originalCard.Transactions = new List<Transaction>();

            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation($"Card ({originalCard.Id}) transferred to ({newCard.Id})");

            return new Payload()
            {
                Card = new CardGraphType(newCard)
            };
        }

        [MutationInput]
        public class Input : IRequest<Payload>, IHaveOriginalCardId
        {
            public Id OriginalCardId { get; set; }
            public long NewCardId { get; set; }
        }

        [MutationPayload]
        public class Payload
        {
            public CardGraphType Card { get; set; }
        }

        public class OriginalCardNotFoundException : RequestValidationException { }
        public class NewCardNotFoundException : RequestValidationException { }
        public class OriginalCardNotAssignException : RequestValidationException { }
        public class NewCardAlreadyAssignException : RequestValidationException { }
        public class NewCardAlreadyGiftCardException : RequestValidationException { }
        public class NewCardAlreadyLostException : RequestValidationException { }
        public class NewCardNotInProjectException : RequestValidationException { }
    }
}
