using GraphQL.Conventions;
using MediatR;
using System.Threading.Tasks;
using Sig.App.Backend.Authorization;
using Sig.App.Backend.Constants;
using Sig.App.Backend.Plugins.GraphQL;
using Sig.App.Backend.Requests.Commands.Mutations.Accounts;
using Sig.App.Backend.Requests.Commands.Mutations.Profiles;
using Sig.App.Backend.Requests.Commands.Mutations.Examples;
using Sig.App.Backend.Services.Permission.Enums;
using Sig.App.Backend.Requests.Commands.Mutations.Projects;
using Sig.App.Backend.Requests.Commands.Mutations.Markets;
using Sig.App.Backend.Requests.Commands.Mutations.Organizations;
using Sig.App.Backend.Requests.Commands.Mutations.Subscriptions;
using Sig.App.Backend.Requests.Commands.Mutations.Beneficiaries;
using Sig.App.Backend.Requests.Commands.Mutations.Cards;
using Sig.App.Backend.Requests.Commands.Mutations.Transactions;
using Sig.App.Backend.Requests.Commands.Mutations.ProductGroups;
using Sig.App.Backend.Requests.Commands.Mutations.BudgetAllowances;

namespace Sig.App.Backend.Gql.Schema
{
    public class Mutation
    {
        [RequirePermission(GlobalPermission.ManageAllUsers)]
        [AnnotateErrorCodes(typeof(CreateAdminAccount))]
        public Task<CreateAdminAccount.Payload> CreateAdminAccount(
            [Inject] IMediator mediator,
            NonNull<CreateAdminAccount.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [AnnotateErrorCodes(typeof(CompleteUserRegistration))]
        public Task<CompleteUserRegistration.Payload> CompleteUserRegistration(
            [Inject] IMediator mediator,
            NonNull<CompleteUserRegistration.Input> input)
        {
            return mediator.Send(input.Value);
        }
        
        [ApplyPolicy(AuthorizationPolicies.ManageUser)]
        [AnnotateErrorCodes(typeof(UpdateUserProfile))]
        public Task<UpdateUserProfile.Payload> UpdateUserProfile(
            [Inject] IMediator mediator,
            NonNull<UpdateUserProfile.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [AnnotateErrorCodes(typeof(ResendConfirmationEmail))]
        public async Task<bool> ResendConfirmationEmail(
            [Inject] IMediator mediator,
            NonNull<ResendConfirmationEmail.Input> input)
        {
            await mediator.Send(input.Value);
            return true;
        }

        [AnnotateErrorCodes(typeof(ConfirmEmail))]
        public async Task<bool> ConfirmEmail(
            [Inject] IMediator mediator,
            NonNull<ConfirmEmail.Input> input)
        {
            await mediator.Send(input.Value);
            return true;
        }

        [Description("Sends a password-reset email to the specified user, if it exists. Always returns `true`, even if the email was unknown.")]
        [AnnotateErrorCodes(typeof(SendPasswordReset))]
        public async Task<bool> SendPasswordReset(
            [Inject] IMediator mediator,
            NonNull<SendPasswordReset.Input> input)
        {
            await mediator.Send(input.Value);

            return true;
        }

        [AnnotateErrorCodes(typeof(ResetPassword))]
        public Task<ResetPassword.Payload> ResetPassword(
            [Inject] IMediator mediator,
            NonNull<ResetPassword.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [ApplyPolicy(AuthorizationPolicies.LoggedIn)]
        [AnnotateErrorCodes(typeof(ChangePassword))]
        public Task<ChangePassword.Payload> ChangePassword(
            [Inject] IMediator mediator,
            NonNull<ChangePassword.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [ApplyPolicy(AuthorizationPolicies.LoggedIn)]
        [AnnotateErrorCodes(typeof(ChangeEmail))]
        public async Task<bool> ChangeEmail(
             [Inject] IMediator mediator,
             NonNull<ChangeEmail.Input> input)
        {
            await mediator.Send(input.Value);
            return true;
        }

        [ApplyPolicy(AuthorizationPolicies.LoggedIn)]
        [AnnotateErrorCodes(typeof(ConfirmChangeEmail))]
        public Task<ConfirmChangeEmail.Payload> ConfirmChangeEmail(
            [Inject] IMediator mediator,
            NonNull<ConfirmChangeEmail.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(ProjectPermission.CreateProject)]
        [AnnotateErrorCodes(typeof(CreateProject))]
        public Task<CreateProject.Payload> CreateProject(
            [Inject] IMediator mediator,
            NonNull<CreateProject.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(ProjectPermission.ManageProject)]
        [AnnotateErrorCodes(typeof(EditProject))]
        public Task<EditProject.Payload> EditProject(
            [Inject] IMediator mediator,
            NonNull<EditProject.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(ProjectPermission.DeleteProject)]
        [AnnotateErrorCodes(typeof(DeleteProject))]
        public async Task<bool> DeleteProject(
            [Inject] IMediator mediator,
            NonNull<DeleteProject.Input> input
            )
        {
            await mediator.Send(input.Value);

            return true;
        }

        [RequirePermission(ProjectPermission.ManageProject)]
        [AnnotateErrorCodes(typeof(AddManagerToProject))]
        public Task<AddManagerToProject.Payload> AddManagerToProject(
            [Inject] IMediator mediator,
            NonNull<AddManagerToProject.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(ProjectPermission.ManageProject)]
        [AnnotateErrorCodes(typeof(RemoveManagerFromProject))]
        public Task<RemoveManagerFromProject.Payload> RemoveManagerFromProject(
            [Inject] IMediator mediator,
            NonNull<RemoveManagerFromProject.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(MarketPermission.CreateMarket)]
        [AnnotateErrorCodes(typeof(CreateMarket))]
        public Task<CreateMarket.Payload> CreateMarket(
            [Inject] IMediator mediator,
            NonNull<CreateMarket.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(MarketPermission.ManageMarket)]
        [AnnotateErrorCodes(typeof(EditMarket))]
        public Task<EditMarket.Payload> EditMarket(
            [Inject] IMediator mediator,
            NonNull<EditMarket.Input> input
            )
        {
            return mediator.Send(input.Value);
        }


        [RequirePermission(MarketPermission.DeleteMarket)]
        [AnnotateErrorCodes(typeof(DeleteMarket))]
        public async Task<bool> DeleteMarket(
            [Inject] IMediator mediator,
            NonNull<DeleteMarket.Input> input
            )
        {
            await mediator.Send(input.Value);

            return true;
        }

        [RequirePermission(MarketPermission.ArchiveMarket)]
        [AnnotateErrorCodes(typeof(ArchiveMarket))]
        public async Task<bool> ArchiveMarket(
            [Inject] IMediator mediator,
            NonNull<ArchiveMarket.Input> input
            )
        {
            await mediator.Send(input.Value);

            return true;
        }

        [RequirePermission(MarketPermission.ManageMarket)]
        [AnnotateErrorCodes(typeof(AddManagerToMarket))]
        public Task<AddManagerToMarket.Payload> AddManagerToMarket(
            [Inject] IMediator mediator,
            NonNull<AddManagerToMarket.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(MarketPermission.ManageMarket)]
        [AnnotateErrorCodes(typeof(RemoveManagerFromMarket))]
        public Task<RemoveManagerFromMarket.Payload> RemoveManagerFromMarket(
            [Inject] IMediator mediator,
            NonNull<RemoveManagerFromMarket.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(MarketPermission.ManageAllMarkets)]
        [AnnotateErrorCodes(typeof(AddMarketToProject))]
        public Task<AddMarketToProject.Payload> AddMarketToProject(
            [Inject] IMediator mediator,
            NonNull<AddMarketToProject.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(MarketPermission.ManageAllMarkets)]
        [AnnotateErrorCodes(typeof(RemoveMarketFromProject))]
        public Task<RemoveMarketFromProject.Payload> RemoveMarketFromProject(
            [Inject] IMediator mediator,
            NonNull<RemoveMarketFromProject.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(ProjectPermission.ManageProject)]
        [AnnotateErrorCodes(typeof(CreateOrganizationInProject))]
        public Task<CreateOrganizationInProject.Payload> CreateOrganizationInProject(
            [Inject] IMediator mediator,
            NonNull<CreateOrganizationInProject.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(OrganizationPermission.ManageOrganization)]
        [AnnotateErrorCodes(typeof(AddManagerToOrganization))]
        public Task<AddManagerToOrganization.Payload> AddManagerToOrganization(
            [Inject] IMediator mediator,
            NonNull<AddManagerToOrganization.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(OrganizationPermission.ManageOrganization)]
        [AnnotateErrorCodes(typeof(RemoveManagerFromOrganization))]
        public Task<RemoveManagerFromOrganization.Payload> RemoveManagerFromOrganization(
            [Inject] IMediator mediator,
            NonNull<RemoveManagerFromOrganization.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(OrganizationPermission.ManageOrganization)]
        [AnnotateErrorCodes(typeof(EditOrganization))]
        public Task<EditOrganization.Payload> EditOrganization(
            [Inject] IMediator mediator,
            NonNull<EditOrganization.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(OrganizationPermission.DeleteOrganization)]
        [AnnotateErrorCodes(typeof(DeleteOrganization))]
        public async Task<bool> DeleteOrganization(
            [Inject] IMediator mediator,
            NonNull<DeleteOrganization.Input> input
            )
        {
            await mediator.Send(input.Value);

            return true;
        }

        [RequirePermission(ProjectPermission.ManageProject)]
        [AnnotateErrorCodes(typeof(CreateSubscriptionInProject))]
        public Task<CreateSubscriptionInProject.Payload> CreateSubscriptionInProject(
            [Inject] IMediator mediator,
            NonNull<CreateSubscriptionInProject.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(SubscriptionPermission.EditSubscription)]
        [AnnotateErrorCodes(typeof(EditSubscription))]
        public Task<EditSubscription.Payload> EditSubscription(
            [Inject] IMediator mediator,
            NonNull<EditSubscription.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(SubscriptionPermission.DeleteSubscription)]
        [AnnotateErrorCodes(typeof(DeleteSubscription))]
        public async Task<bool> DeleteSubscription(
            [Inject] IMediator mediator,
            NonNull<DeleteSubscription.Input> input
            )
        {
            await mediator.Send(input.Value);

            return true;
        }

        [RequirePermission(OrganizationPermission.ManageOrganization)]
        [AnnotateErrorCodes(typeof(CreateBeneficiaryInOrganization))]
        public Task<CreateBeneficiaryInOrganization.Payload> CreateBeneficiaryInOrganization(
            [Inject] IMediator mediator,
            NonNull<CreateBeneficiaryInOrganization.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(OrganizationPermission.ManageOrganization)]
        [AnnotateErrorCodes(typeof(ImportBeneficiariesListInOrganization))]
        public Task<ImportBeneficiariesListInOrganization.Payload> ImportBeneficiariesListInOrganization(
            [Inject] IMediator mediator,
            NonNull<ImportBeneficiariesListInOrganization.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(OrganizationPermission.ManageOrganization)]
        [AnnotateErrorCodes(typeof(ImportOffPlatformBeneficiariesListInOrganization))]
        public Task<ImportOffPlatformBeneficiariesListInOrganization.Payload> ImportOffPlatformBeneficiariesListInOrganization(
            [Inject] IMediator mediator,
            NonNull<ImportOffPlatformBeneficiariesListInOrganization.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(BeneficiaryPermission.ManageBeneficiary)]
        [AnnotateErrorCodes(typeof(RemoveBeneficiaryFromSubscription))]
        public async Task<bool> RemoveBeneficiaryFromSubscription(
            [Inject] IMediator mediator,
            NonNull<RemoveBeneficiaryFromSubscription.Input> input)
        {
            await mediator.Send(input.Value);

            return true;
        }

        [RequirePermission(BeneficiaryPermission.ManageBeneficiary)]
        [AnnotateErrorCodes(typeof(EditBeneficiary))]
        public Task<EditBeneficiary.Payload> EditBeneficiary(
            [Inject] IMediator mediator,
            NonNull<EditBeneficiary.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(BeneficiaryPermission.DeleteBeneficiary)]
        [AnnotateErrorCodes(typeof(DeleteBeneficiary))]
        public async Task<bool> DeleteBeneficiary(
            [Inject] IMediator mediator,
            NonNull<DeleteBeneficiary.Input> input
            )
        {
            await mediator.Send(input.Value);

            return true;
        }

        [RequirePermission(ProjectPermission.CreateCard)]
        [AnnotateErrorCodes(typeof(CreateCards))]
        public Task<CreateCards.Payload> CreateCards(
            [Inject] IMediator mediator,
            NonNull<CreateCards.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(BeneficiaryPermission.AssignCard)]
        [AnnotateErrorCodes(typeof(AssignCardToBeneficiary))]
        public Task<AssignCardToBeneficiary.Payload> AssignCardToBeneficiary(
            [Inject] IMediator mediator,
            NonNull<AssignCardToBeneficiary.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(BeneficiaryPermission.AssignCard)]
        [AnnotateErrorCodes(typeof(TransfertCard))]
        public Task<TransfertCard.Payload> TransfertCard(
            [Inject] IMediator mediator,
            NonNull<TransfertCard.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(BeneficiaryPermission.AssignCard)]
        [AnnotateErrorCodes(typeof(AssignUnassignedCardToBeneficiary))]
        public Task<AssignUnassignedCardToBeneficiary.Payload> AssignUnassignedCardToBeneficiary(
            [Inject] IMediator mediator,
            NonNull<AssignUnassignedCardToBeneficiary.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(BeneficiaryPermission.AssignCard)]
        [AnnotateErrorCodes(typeof(UnassignCardFromBeneficiary))]
        public Task<UnassignCardFromBeneficiary.Payload> UnassignCardFromBeneficiary(
            [Inject] IMediator mediator,
            NonNull<UnassignCardFromBeneficiary.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(MarketPermission.CreateTransaction)]
        [AnnotateErrorCodes(typeof(CreateTransaction))]
        public Task<CreateTransaction.Payload> CreateTransaction(
            [Inject] IMediator mediator,
            NonNull<CreateTransaction.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(BeneficiaryPermission.ManuallyAddingFund)]
        [AnnotateErrorCodes(typeof(CreateManuallyAddingFundTransaction))]
        public Task<CreateManuallyAddingFundTransaction.Payload> CreateManuallyAddingFundTransaction(
            [Inject] IMediator mediator,
            NonNull<CreateManuallyAddingFundTransaction.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(ProjectPermission.AddLoyaltyFundToCard)]
        [AnnotateErrorCodes(typeof(AddLoyaltyFundToCard))]
        public Task<AddLoyaltyFundToCard.Payload> AddLoyaltyFundToCard(
            [Inject] IMediator mediator,
            NonNull<AddLoyaltyFundToCard.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(ProjectPermission.ManageProject)]
        [AnnotateErrorCodes(typeof(AddBeneficiaryTypeInProject))]
        public Task<AddBeneficiaryTypeInProject.Payload> AddBeneficiaryTypeInProject(
            [Inject] IMediator mediator,
            NonNull<AddBeneficiaryTypeInProject.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(BeneficiaryTypePermission.EditBeneficiaryType)]
        [AnnotateErrorCodes(typeof(EditBeneficiaryType))]
        public Task<EditBeneficiaryType.Payload> EditBeneficiaryType(
            [Inject] IMediator mediator,
            NonNull<EditBeneficiaryType.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(BeneficiaryTypePermission.DeleteBeneficiaryType)]
        [AnnotateErrorCodes(typeof(DeleteBeneficiaryType))]
        public async Task<bool> DeleteBeneficiaryType(
            [Inject] IMediator mediator,
            NonNull<DeleteBeneficiaryType.Input> input
            )
        {
            await mediator.Send(input.Value);

            return true;
        }

        [RequirePermission(GlobalPermission.ManageBudgetAllowance)]
        [AnnotateErrorCodes(typeof(CreateBudgetAllowance))]
        public Task<CreateBudgetAllowance.Payload> CreateBudgetAllowance(
            [Inject] IMediator mediator,
            NonNull<CreateBudgetAllowance.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(GlobalPermission.ManageBudgetAllowance)]
        [AnnotateErrorCodes(typeof(EditBudgetAllowance))]
        public Task<EditBudgetAllowance.Payload> EditBudgetAllowance(
            [Inject] IMediator mediator,
            NonNull<EditBudgetAllowance.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(GlobalPermission.ManageBudgetAllowance)]
        [AnnotateErrorCodes(typeof(DeleteBudgetAllowance))]
        public async Task<bool> DeleteBudgetAllowance(
            [Inject] IMediator mediator,
            NonNull<DeleteBudgetAllowance.Input> input
            )
        {
            await mediator.Send(input.Value);

            return true;
        }

        [RequirePermission(OrganizationPermission.ManageOrganization)]
        [AnnotateErrorCodes(typeof(AssignBeneficiariesToSubscription))]
        public Task<AssignBeneficiariesToSubscription.Payload> AssignBeneficiariesToSubscription(
            [Inject] IMediator mediator,
            NonNull<AssignBeneficiariesToSubscription.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [AnnotateErrorCodes(typeof(ExampleFormError))]
        public Task<ExampleFormError.Payload> ExampleFormError(
            [Inject] IMediator mediator,
            NonNull<ExampleFormError.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(GlobalPermission.ManageProductGroup)]
        [AnnotateErrorCodes(typeof(CreateProductGroup))]
        public Task<CreateProductGroup.Payload> CreateProductGroup(
            [Inject] IMediator mediator,
            NonNull<CreateProductGroup.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(GlobalPermission.ManageProductGroup)]
        [AnnotateErrorCodes(typeof(EditProductGroup))]
        public Task<EditProductGroup.Payload> EditProductGroup(
            [Inject] IMediator mediator,
            NonNull<EditProductGroup.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(GlobalPermission.ManageProductGroup)]
        [AnnotateErrorCodes(typeof(DeleteProductGroup))]
        public async Task<bool> DeleteProductGroup(
            [Inject] IMediator mediator,
            NonNull<DeleteProductGroup.Input> input
            )
        {
            await mediator.Send(input.Value);

            return true;
        }
    }
}