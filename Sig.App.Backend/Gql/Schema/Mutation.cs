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
using Sig.App.Backend.Requests.Commands.Mutations.MarketGroups;

namespace Sig.App.Backend.Gql.Schema
{
    [SchemaExtension]
    public static class Mutation
    {
        [RequirePermission(GlobalPermission.ManageAllUsers)]
        [AnnotateErrorCodes(typeof(CreateAdminAccount))]
        public static Task<CreateAdminAccount.Payload> CreateAdminAccount(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<CreateAdminAccount.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [AnnotateErrorCodes(typeof(CompleteUserRegistration))]
        public static Task<CompleteUserRegistration.Payload> CompleteUserRegistration(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<CompleteUserRegistration.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [ApplyPolicy(AuthorizationPolicies.ManageUser)]
        [AnnotateErrorCodes(typeof(UpdateUserProfile))]
        public static Task<UpdateUserProfile.Payload> UpdateUserProfile(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<UpdateUserProfile.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [AnnotateErrorCodes(typeof(ResendConfirmationEmail))]
        public static async Task<bool> ResendConfirmationEmail(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<ResendConfirmationEmail.Input> input)
        {
            await mediator.Send(input.Value);
            return true;
        }

        [AnnotateErrorCodes(typeof(ConfirmEmail))]
        public static async Task<bool> ConfirmEmail(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<ConfirmEmail.Input> input)
        {
            await mediator.Send(input.Value);
            return true;
        }

        [Description("Sends a password-reset email to the specified user, if it exists. Always returns `true`, even if the email was unknown.")]
        [AnnotateErrorCodes(typeof(SendPasswordReset))]
        public static async Task<bool> SendPasswordReset(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<SendPasswordReset.Input> input)
        {
            await mediator.Send(input.Value);

            return true;
        }

        [AnnotateErrorCodes(typeof(ResetPassword))]
        public static Task<ResetPassword.Payload> ResetPassword(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<ResetPassword.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [ApplyPolicy(AuthorizationPolicies.LoggedIn)]
        [AnnotateErrorCodes(typeof(ChangePassword))]
        public static Task<ChangePassword.Payload> ChangePassword(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<ChangePassword.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [ApplyPolicy(AuthorizationPolicies.LoggedIn)]
        [AnnotateErrorCodes(typeof(ChangeEmail))]
        public static async Task<bool> ChangeEmail(
            this GqlMutation _,
             [Inject] IMediator mediator,
             NonNull<ChangeEmail.Input> input)
        {
            await mediator.Send(input.Value);
            return true;
        }

        [ApplyPolicy(AuthorizationPolicies.LoggedIn)]
        [AnnotateErrorCodes(typeof(ConfirmChangeEmail))]
        public static Task<ConfirmChangeEmail.Payload> ConfirmChangeEmail(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<ConfirmChangeEmail.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(ProjectPermission.CreateProject)]
        [AnnotateErrorCodes(typeof(CreateProject))]
        public static Task<CreateProject.Payload> CreateProject(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<CreateProject.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(ProjectPermission.ManageProject)]
        [AnnotateErrorCodes(typeof(EditProject))]
        public static Task<EditProject.Payload> EditProject(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<EditProject.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(ProjectPermission.DeleteProject)]
        [AnnotateErrorCodes(typeof(DeleteProject))]
        public static async Task<bool> DeleteProject(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<DeleteProject.Input> input
            )
        {
            await mediator.Send(input.Value);

            return true;
        }

        [RequirePermission(ProjectPermission.ManageProject)]
        [AnnotateErrorCodes(typeof(AddManagerToProject))]
        public static Task<AddManagerToProject.Payload> AddManagerToProject(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<AddManagerToProject.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(ProjectPermission.ManageProject)]
        [AnnotateErrorCodes(typeof(RemoveManagerFromProject))]
        public static Task<RemoveManagerFromProject.Payload> RemoveManagerFromProject(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<RemoveManagerFromProject.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(MarketPermission.CreateMarket)]
        [AnnotateErrorCodes(typeof(CreateMarket))]
        public static Task<CreateMarket.Payload> CreateMarket(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<CreateMarket.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(MarketPermission.ManageMarket)]
        [AnnotateErrorCodes(typeof(EditMarket))]
        public static Task<EditMarket.Payload> EditMarket(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<EditMarket.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(MarketPermission.DeleteMarket)]
        [AnnotateErrorCodes(typeof(DeleteMarket))]
        public static async Task<bool> DeleteMarket(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<DeleteMarket.Input> input
            )
        {
            await mediator.Send(input.Value);

            return true;
        }

        [RequirePermission(MarketPermission.ArchiveMarket)]
        [AnnotateErrorCodes(typeof(ArchiveMarket))]
        public static async Task<bool> ArchiveMarket(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<ArchiveMarket.Input> input
            )
        {
            await mediator.Send(input.Value);

            return true;
        }

        [RequirePermission(MarketPermission.ManageMarket)]
        [AnnotateErrorCodes(typeof(AddManagerToMarket))]
        public static Task<AddManagerToMarket.Payload> AddManagerToMarket(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<AddManagerToMarket.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(MarketPermission.ManageMarket)]
        [AnnotateErrorCodes(typeof(RemoveManagerFromMarket))]
        public static Task<RemoveManagerFromMarket.Payload> RemoveManagerFromMarket(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<RemoveManagerFromMarket.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(MarketPermission.ManageAllMarkets)]
        [AnnotateErrorCodes(typeof(AddMarketToProject))]
        public static Task<AddMarketToProject.Payload> AddMarketToProject(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<AddMarketToProject.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(MarketPermission.ManageAllMarkets)]
        [AnnotateErrorCodes(typeof(RemoveMarketFromProject))]
        public static Task<RemoveMarketFromProject.Payload> RemoveMarketFromProject(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<RemoveMarketFromProject.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(ProjectPermission.ManageProject)]
        [AnnotateErrorCodes(typeof(RemoveMarketFromOrganization))]
        public static Task<RemoveMarketFromOrganization.Payload> RemoveMarketFromOrganization(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<RemoveMarketFromOrganization.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(ProjectPermission.ManageProject)]
        [AnnotateErrorCodes(typeof(CreateOrganizationInProject))]
        public static Task<CreateOrganizationInProject.Payload> CreateOrganizationInProject(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<CreateOrganizationInProject.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(OrganizationPermission.ManageOrganization)]
        [AnnotateErrorCodes(typeof(AddManagerToOrganization))]
        public static Task<AddManagerToOrganization.Payload> AddManagerToOrganization(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<AddManagerToOrganization.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(ProjectPermission.ManageProject)]
        [AnnotateErrorCodes(typeof(AddMarketToOrganization))]
        public static Task<AddMarketToOrganization.Payload> AddMarketToOrganization(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<AddMarketToOrganization.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(OrganizationPermission.ManageOrganization)]
        [AnnotateErrorCodes(typeof(RemoveManagerFromOrganization))]
        public static Task<RemoveManagerFromOrganization.Payload> RemoveManagerFromOrganization(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<RemoveManagerFromOrganization.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(OrganizationPermission.ManageOrganization)]
        [AnnotateErrorCodes(typeof(EditOrganization))]
        public static Task<EditOrganization.Payload> EditOrganization(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<EditOrganization.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(OrganizationPermission.DeleteOrganization)]
        [AnnotateErrorCodes(typeof(DeleteOrganization))]
        public static async Task<bool> DeleteOrganization(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<DeleteOrganization.Input> input
            )
        {
            await mediator.Send(input.Value);

            return true;
        }

        [RequirePermission(ProjectPermission.ManageProject)]
        [AnnotateErrorCodes(typeof(CreateSubscriptionInProject))]
        public static Task<CreateSubscriptionInProject.Payload> CreateSubscriptionInProject(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<CreateSubscriptionInProject.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(SubscriptionPermission.EditSubscription)]
        [AnnotateErrorCodes(typeof(EditSubscription))]
        public static Task<EditSubscription.Payload> EditSubscription(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<EditSubscription.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(SubscriptionPermission.DeleteSubscription)]
        [AnnotateErrorCodes(typeof(DeleteSubscription))]
        public static async Task<bool> DeleteSubscription(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<DeleteSubscription.Input> input
            )
        {
            await mediator.Send(input.Value);

            return true;
        }

        [RequirePermission(OrganizationPermission.ManageOrganization)]
        [AnnotateErrorCodes(typeof(CreateBeneficiaryInOrganization))]
        public static Task<CreateBeneficiaryInOrganization.Payload> CreateBeneficiaryInOrganization(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<CreateBeneficiaryInOrganization.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(OrganizationPermission.ManageOrganization)]
        [AnnotateErrorCodes(typeof(ImportBeneficiariesListInOrganization))]
        public static Task<ImportBeneficiariesListInOrganization.Payload> ImportBeneficiariesListInOrganization(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<ImportBeneficiariesListInOrganization.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(OrganizationPermission.ManageOrganization)]
        [AnnotateErrorCodes(typeof(ImportOffPlatformBeneficiariesListInOrganization))]
        public static Task<ImportOffPlatformBeneficiariesListInOrganization.Payload> ImportOffPlatformBeneficiariesListInOrganization(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<ImportOffPlatformBeneficiariesListInOrganization.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(OrganizationPermission.ManageOrganization)]
        [AnnotateErrorCodes(typeof(RemoveBeneficiaryFromSubscription))]
        public static async Task<bool> RemoveBeneficiaryFromSubscription(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<RemoveBeneficiaryFromSubscription.Input> input)
        {
            await mediator.Send(input.Value);

            return true;
        }

        [RequirePermission(BeneficiaryPermission.ManageBeneficiary)]
        [AnnotateErrorCodes(typeof(EditBeneficiary))]
        public static Task<EditBeneficiary.Payload> EditBeneficiary(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<EditBeneficiary.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(BeneficiaryPermission.DeleteBeneficiary)]
        [AnnotateErrorCodes(typeof(DeleteBeneficiary))]
        public static async Task<bool> DeleteBeneficiary(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<DeleteBeneficiary.Input> input
            )
        {
            await mediator.Send(input.Value);

            return true;
        }

        [RequirePermission(ProjectPermission.CreateCard)]
        [AnnotateErrorCodes(typeof(CreateCards))]
        public static Task<CreateCards.Payload> CreateCards(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<CreateCards.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(BeneficiaryPermission.AssignCard)]
        [AnnotateErrorCodes(typeof(AssignCardToBeneficiary))]
        public static Task<AssignCardToBeneficiary.Payload> AssignCardToBeneficiary(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<AssignCardToBeneficiary.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(BeneficiaryPermission.AssignCard)]
        [AnnotateErrorCodes(typeof(TransfertCard))]
        public static Task<TransfertCard.Payload> TransfertCard(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<TransfertCard.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(BeneficiaryPermission.AssignCard)]
        [AnnotateErrorCodes(typeof(AssignUnassignedCardToBeneficiary))]
        public static Task<AssignUnassignedCardToBeneficiary.Payload> AssignUnassignedCardToBeneficiary(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<AssignUnassignedCardToBeneficiary.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(BeneficiaryPermission.AssignCard)]
        [AnnotateErrorCodes(typeof(UnassignCardFromBeneficiary))]
        public static Task<UnassignCardFromBeneficiary.Payload> UnassignCardFromBeneficiary(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<UnassignCardFromBeneficiary.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(MarketPermission.CreateTransaction)]
        [AnnotateErrorCodes(typeof(CreateTransaction))]
        public static Task<CreateTransaction.Payload> CreateTransaction(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<CreateTransaction.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(MarketPermission.RefundTransaction)]
        [AnnotateErrorCodes(typeof(RefundTransaction))]
        public static Task<RefundTransaction.Payload> RefundTransaction(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<RefundTransaction.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(BeneficiaryPermission.ManuallyAddingFund)]
        [AnnotateErrorCodes(typeof(CreateManuallyAddingFundTransaction))]
        public static Task<CreateManuallyAddingFundTransaction.Payload> CreateManuallyAddingFundTransaction(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<CreateManuallyAddingFundTransaction.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(ProjectPermission.AddLoyaltyFundToCard)]
        [AnnotateErrorCodes(typeof(AddLoyaltyFundToCard))]
        public static Task<AddLoyaltyFundToCard.Payload> AddLoyaltyFundToCard(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<AddLoyaltyFundToCard.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(ProjectPermission.ManageProject)]
        [AnnotateErrorCodes(typeof(AddBeneficiaryTypeInProject))]
        public static Task<AddBeneficiaryTypeInProject.Payload> AddBeneficiaryTypeInProject(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<AddBeneficiaryTypeInProject.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(BeneficiaryTypePermission.EditBeneficiaryType)]
        [AnnotateErrorCodes(typeof(EditBeneficiaryType))]
        public static Task<EditBeneficiaryType.Payload> EditBeneficiaryType(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<EditBeneficiaryType.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(BeneficiaryTypePermission.DeleteBeneficiaryType)]
        [AnnotateErrorCodes(typeof(DeleteBeneficiaryType))]
        public static async Task<bool> DeleteBeneficiaryType(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<DeleteBeneficiaryType.Input> input
            )
        {
            await mediator.Send(input.Value);

            return true;
        }

        [RequirePermission(GlobalPermission.ManageBudgetAllowance)]
        [AnnotateErrorCodes(typeof(CreateBudgetAllowance))]
        public static Task<CreateBudgetAllowance.Payload> CreateBudgetAllowance(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<CreateBudgetAllowance.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(GlobalPermission.ManageBudgetAllowance)]
        [AnnotateErrorCodes(typeof(EditBudgetAllowance))]
        public static Task<EditBudgetAllowance.Payload> EditBudgetAllowance(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<EditBudgetAllowance.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(GlobalPermission.ManageBudgetAllowance)]
        [AnnotateErrorCodes(typeof(DeleteBudgetAllowance))]
        public static async Task<bool> DeleteBudgetAllowance(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<DeleteBudgetAllowance.Input> input
            )
        {
            await mediator.Send(input.Value);

            return true;
        }

        [RequirePermission(OrganizationPermission.ManageOrganization)]
        [AnnotateErrorCodes(typeof(AssignBeneficiariesToSubscription))]
        public static Task<AssignBeneficiariesToSubscription.Payload> AssignBeneficiariesToSubscription(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<AssignBeneficiariesToSubscription.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(OrganizationPermission.ManageOrganization)]
        [AnnotateErrorCodes(typeof(AssignSubscriptionsToBeneficiary))]
        public static Task<AssignSubscriptionsToBeneficiary.Payload> AssignSubscriptionsToBeneficiary(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<AssignSubscriptionsToBeneficiary.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [AnnotateErrorCodes(typeof(ExampleFormError))]
        public static Task<ExampleFormError.Payload> ExampleFormError(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<ExampleFormError.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(GlobalPermission.ManageProductGroup)]
        [AnnotateErrorCodes(typeof(CreateProductGroup))]
        public static Task<CreateProductGroup.Payload> CreateProductGroup(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<CreateProductGroup.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(GlobalPermission.ManageProductGroup)]
        [AnnotateErrorCodes(typeof(EditProductGroup))]
        public static Task<EditProductGroup.Payload> EditProductGroup(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<EditProductGroup.Input> input)
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(GlobalPermission.ManageProductGroup)]
        [AnnotateErrorCodes(typeof(DeleteProductGroup))]
        public static async Task<bool> DeleteProductGroup(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<DeleteProductGroup.Input> input
            )
        {
            await mediator.Send(input.Value);

            return true;
        }

        [RequirePermission(CardPermission.EnableDisableCard)]
        [AnnotateErrorCodes(typeof(DisableCard))]
        public static Task<DisableCard.Payload> DisableCard(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<DisableCard.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(CardPermission.EnableDisableCard)]
        [AnnotateErrorCodes(typeof(EnableCard))]
        public static Task<EnableCard.Payload> EnableCard(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<EnableCard.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(CardPermission.EnableDisableCard)]
        [AnnotateErrorCodes(typeof(UnlockCard))]
        public static Task<UnlockCard.Payload> UnlockCard(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<UnlockCard.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(MarketGroupPermission.ManageMarketGroup)]
        [AnnotateErrorCodes(typeof(AddManagerToMarketGroup))]
        public static Task<AddManagerToMarketGroup.Payload> AddManagerToMarketGroup(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<AddManagerToMarketGroup.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(MarketGroupPermission.ManageMarketGroup)]
        [AnnotateErrorCodes(typeof(RemoveManagerFromMarketGroup))]
        public static Task<RemoveManagerFromMarketGroup.Payload> RemoveManagerFromMarketGroup(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<RemoveManagerFromMarketGroup.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(MarketGroupPermission.ArchiveMarketGroup)]
        [AnnotateErrorCodes(typeof(ArchiveMarketGroup))]
        public static async Task<bool> ArchiveMarketGroup(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<ArchiveMarketGroup.Input> input
            )
        {
            await mediator.Send(input.Value);

            return true;
        }

        [RequirePermission(ProjectPermission.ManageProject)]
        [AnnotateErrorCodes(typeof(CreateMarketGroup))]
        public static Task<CreateMarketGroup.Payload> CreateMarketGroup(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<CreateMarketGroup.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(MarketGroupPermission.DeleteMarketGroup)]
        [AnnotateErrorCodes(typeof(DeleteMarketGroup))]
        public static async Task<bool> DeleteMarketGroup(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<DeleteMarketGroup.Input> input
            )
        {
            await mediator.Send(input.Value);

            return true;
        }

        [RequirePermission(MarketGroupPermission.ManageMarketGroup)]
        [AnnotateErrorCodes(typeof(EditMarketGroup))]
        public static Task<EditMarketGroup.Payload> EditMarketGroup(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<EditMarketGroup.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(MarketGroupPermission.ManageMarketGroup)]
        [AnnotateErrorCodes(typeof(AddMarketToMarketGroup))]
        public static Task<AddMarketToMarketGroup.Payload> AddMarketToMarketGroup(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<AddMarketToMarketGroup.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        [RequirePermission(MarketGroupPermission.ManageMarketGroup)]
        [AnnotateErrorCodes(typeof(RemoveMarketFromMarketGroup))]
        public static Task<RemoveMarketFromMarketGroup.Payload> RemoveMarketFromMarketGroup(
            this GqlMutation _,
            [Inject] IMediator mediator,
            NonNull<RemoveMarketFromMarketGroup.Input> input
            )
        {
            return mediator.Send(input.Value);
        }

        public static Task<AdjustBeneficiarySubscription.Payload> AdjustBeneficiarySubscription(this GqlMutation _, [Inject] IMediator mediator, NonNull<AdjustBeneficiarySubscription.Input> input)
        {
            return mediator.Send(input.Value);
        }

        public static Task<AddMissingPayment.Payload> AddMissingPayment(this GqlMutation _, [Inject] IMediator mediator, NonNull<AddMissingPayment.Input> input)
        {
            return mediator.Send(input.Value);
        }

        public static Task<AddMissingPayments.Payload> AddMissingPayments(this GqlMutation _, [Inject] IMediator mediator, NonNull<AddMissingPayments.Input> input)
        {
            return mediator.Send(input.Value);
        }
    }
}