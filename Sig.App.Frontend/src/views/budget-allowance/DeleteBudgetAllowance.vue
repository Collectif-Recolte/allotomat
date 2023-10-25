<i18n>
{
	"en": {
		"delete-budget-allowance-success-notification": "Envelope for organization {organizationName} has been successfully deleted.",
		"delete-text-error": "The text must match the name of the organization",
		"delete-text-label": "Type the name of the organization to confirm",
		"description": "Warning ! Deleting the envelope cannot be undone. If you continue, the envelope will be deleted.",
		"title": "Remove envelope for organization {organizationName}"
	},
	"fr": {
		"delete-budget-allowance-success-notification": "L'enveloppe pour l'organisme {organizationName} a été supprimée avec succès.",
		"delete-text-error": "Le texte doit correspondre au nom de l'organisme",
		"delete-text-label": "Taper le nom de l'organisme pour confirmer",
		"description": "Avertissement ! La suppression de l'enveloppe ne peut pas être annulée. Si vous continuez, l'enveloppe sera supprimée.",
		"title": "Supprimer l'enveloppe pour l'organisme {organizationName}"
	}
}
</i18n>

<template>
  <UiDialogDeleteModal
    :return-route="{ name: URL_SUBSCRIPTION_MANAGE_BUDGET_ALLOWANCE, params: { subscriptionId: route.params.subscriptionId } }"
    :title="t('title', { organizationName: getOrganizationName() })"
    :description="t('description')"
    :validation-text="getOrganizationName()"
    :delete-text-label="t('delete-text-label')"
    :delete-text-error="t('delete-text-error')"
    @onDelete="deleteBudgetAllowance" />
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_SUBSCRIPTION_MANAGE_BUDGET_ALLOWANCE } from "@/lib/consts/urls";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { addSuccess } = useNotificationsStore();

const { result: resultBudgetAllowance } = useQuery(
  gql`
    query BudgetAllowance($id: ID!) {
      budgetAllowance(id: $id) {
        id
        originalFund
        organization {
          id
          name
        }
      }
    }
  `,
  {
    id: route.params.budgetId
  }
);
const budgetAllowance = useResult(resultBudgetAllowance);

const { mutate: deleteBudgetAllowanceMutation } = useMutation(
  gql`
    mutation DeleteBudgetAllowance($input: DeleteBudgetAllowanceInput!) {
      deleteBudgetAllowance(input: $input)
    }
  `
);

function getOrganizationName() {
  return budgetAllowance.value ? budgetAllowance.value.organization.name : "";
}

async function deleteBudgetAllowance() {
  await deleteBudgetAllowanceMutation({
    input: {
      budgetAllowanceId: route.params.budgetId
    }
  });

  addSuccess(t("delete-budget-allowance-success-notification", { organizationName: budgetAllowance.value.organization.name }));
  router.push({ name: URL_SUBSCRIPTION_MANAGE_BUDGET_ALLOWANCE, params: { subscriptionId: route.params.subscriptionId } });
}
</script>
