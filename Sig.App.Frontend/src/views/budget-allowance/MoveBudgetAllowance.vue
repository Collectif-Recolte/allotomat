<i18n>
{
	"en": {
		"delete-budget-allowance-success-notification": "Envelope for group {organizationName} has been successfully deleted.",
		"description": "Warning ! The transfer of funds from the envelope cannot be undone. If you continue, the funds will be transferred to another envelope.",
		"title": "Transfer funds to another envelope",
    "cancel-btn": "Cancel",
    "transfer-funds-btn": "Transfer funds",
    "amount-label": "Amount",
    "amount-placeholder": "Enter the amount",
    "budget-allowance-label": "Envelope",
    "move-budget-allowance-success-notification": "Funds have been transferred successfully."
	},
	"fr": {
		"delete-budget-allowance-success-notification": "L'enveloppe pour le groupe {organizationName} a été supprimée avec succès.",
		"description": "Avertissement ! Le transfert des fonds de l'enveloppe ne peut pas être annulée. Si vous continuez, les fonds seront transférés vers une autre enveloppe.",
		"title": "Transférer des fonds vers une autre enveloppe",
    "cancel-btn": "Annuler",
    "transfer-funds-btn": "Transférer les fonds",
    "amount-label": "Montant",
    "amount-placeholder": "Entrez le montant",
    "budget-allowance-label": "Enveloppe",
    "move-budget-allowance-success-notification": "Les fonds ont été transférés avec succès."
	}
}
</i18n>

<template>
  <UiDialogModal
    :return-route="{ name: URL_SUBSCRIPTION_MANAGE_BUDGET_ALLOWANCE, params: { subscriptionId: route.params.subscriptionId } }"
    :title="t('title', { organizationName: getOrganizationName() })"
    hide-main-btn>
    <template #default>
      <p class="text-sm text-gray-500">{{ t("description") }}</p>
      <Form
        v-if="budgetAllowance"
        v-slot="{ isSubmitting, errors: formErrors }"
        :validation-schema="validationSchema"
        :initial-values="initialValues"
        @submit="onSubmit"
        @cancel="closeModal">
        <PfForm
          can-cancel
          has-footer
          :disable-submit="Object.keys(formErrors).length > 0"
          :submit-label="t('transfer-funds-btn')"
          :cancel-label="t('cancel-btn')"
          :processing="isSubmitting"
          :warning-message="t('description')">
          <PfFormSection>
            <Field v-slot="{ field: inputField, errors: fieldErrors }" name="budgetAllowanceId">
              <PfFormInputSelect
                id="budgetAllowanceId"
                v-bind="inputField"
                :label="t('budget-allowance-label')"
                :options="availableBudgetAllowances"
                col-span-class="sm:col-span-6"
                :errors="fieldErrors"
                required />
            </Field>
            <Field v-slot="{ field, errors: fieldErrors }" name="amount">
              <PfFormInputText
                id="amount"
                required
                v-bind="field"
                :label="t('amount-label')"
                :placeholder="t('amount-placeholder')"
                :errors="fieldErrors"
                col-span-class="sm:col-span-6" />
            </Field>
          </PfFormSection>
          <template #footer>
            <div class="pt-5">
              <div class="flex gap-x-6 items-center justify-end">
                <PfButtonAction :label="t('cancel-btn')" btn-style="link" @click="closeModal" />
                <PfButtonAction
                  type="submit"
                  :disabled="Object.keys(formErrors).length > 0"
                  :label="t('transfer-funds-btn')"
                  @click="submit" />
              </div>
            </div>
          </template>
        </PfForm>
      </Form>
    </template>
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { computed } from "vue";
import { string, object, number } from "yup";

import { URL_SUBSCRIPTION_MANAGE_BUDGET_ALLOWANCE } from "@/lib/consts/urls";

import { useNotificationsStore } from "@/lib/store/notifications";
import { getMoneyFormat } from "@/lib/helpers/money";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { addSuccess } = useNotificationsStore();

const initialValues = {
  amount: 0,
  budgetAllowanceId: ""
};

const { mutate: moveBudgetAllowanceMutation } = useMutation(
  gql`
    mutation MoveBudgetAllowance($input: MoveBudgetAllowanceInput!) {
      moveBudgetAllowance(input: $input) {
        initialBudgetAllowance {
          id
          availableFund
          originalFund
        }
        targetBudgetAllowance {
          id
          availableFund
          originalFund
        }
      }
    }
  `
);

const { result: resultBudgetAllowance } = useQuery(
  gql`
    query BudgetAllowance($id: ID!) {
      budgetAllowance(id: $id) {
        id
        availableFund
        organization {
          id
        }
      }
    }
  `,
  {
    id: route.params.budgetId
  }
);

const budgetAllowance = useResult(resultBudgetAllowance, null, (data) => {
  initialValues.amount = data.budgetAllowance.availableFund;
  return data.budgetAllowance;
});

const { result } = useQuery(
  gql`
    query Projects {
      projects {
        id
        subscriptions {
          id
          name
          isArchived
          budgetAllowances {
            id
            availableFund
            organization {
              id
              name
            }
            subscription {
              id
              name
            }
          }
        }
      }
    }
  `
);
const projects = useResult(result);

const validationSchema = computed(() =>
  object({
    amount: number().label(t("amount-label")).required().min(0.01),
    budgetAllowanceId: string().label(t("budget-allowance-label")).required()
  })
);

const availableBudgetAllowances = computed(() => {
  if (!projects.value) return [];
  return projects.value[0].subscriptions
    .filter((subscription) => subscription.id !== route.params.subscriptionId && !subscription.isArchived)
    .map((subscription) => subscription.budgetAllowances)
    .flat()
    .map((x) => ({
      value: x.id,
      label: x.subscription.name + " - " + x.organization.name + " - " + getMoneyFormat(x.availableFund)
    }));
});

function getOrganizationName() {
  return budgetAllowance.value ? budgetAllowance.value.organization.name : "";
}

function closeModal() {
  router.push({ name: URL_SUBSCRIPTION_MANAGE_BUDGET_ALLOWANCE, params: { subscriptionId: route.params.subscriptionId } });
}

async function onSubmit(values) {
  await moveBudgetAllowanceMutation({
    input: {
      initialBudgetAllowanceId: budgetAllowance.value.id,
      targetBudgetAllowanceId: values.budgetAllowanceId,
      amount: parseFloat(values.amount)
    }
  });
  addSuccess(t("move-budget-allowance-success-notification"));
  closeModal();
}
</script>
