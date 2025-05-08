<i18n>
{
	"en": {
    "date-separator": " to ",
		"title": "Configure envelopes",
    "subscription-name": "Subscription period",
    "add-budget-allowance": "Add budget allowance",
    "close-modal": "Close",
    "edit-budget-allowance-success-notification": "The envelope was saved successfully.",
    "add-budget-allowance-success-notification": "Adding an envelope with the amount of {amount} for the subscription period {subscriptionName} was successful.",
    "cant-remove-budget-allowance-with-participant-error": "Deleting an envelope is impossible for envelopes that already have an assigned participant list."
	},
	"fr": {
    "date-separator": " au ",
		"title": "Configurer les enveloppes",
    "subscription-name": "Période d'abonnement",
    "add-budget-allowance": "Ajouter une enveloppe",
    "close-modal": "Terminer",
    "edit-budget-allowance-success-notification": "L’enveloppe a été sauvegardée avec succès.",
    "add-budget-allowance-success-notification": "L’ajout d’une enveloppe d’un montant de {amount} pour la période d’abonnement {subscriptionName} a été un succès.",
    "cant-remove-budget-allowance-with-participant-error": "La suppression d’une enveloppe est impossible pour les enveloppes qui possèdent déjà une liste de participant-e-s attitrée."
	}
}
</i18n>

<template>
  <div>
    <UiDialogModal :return-route="{ name: URL_SUBSCRIPTION_ADMIN }" :title="t('title')" :close-label="t('close-modal')">
      <p class="text-p2 font-semibold mb-2">{{ t("subscription-name") }}</p>
      <p class="text-p2 pb-6 mb-6 border-b border-grey-200">{{ getSubscriptionName() }}</p>
      <template v-for="budgetAllowance in budgetAllowances" :key="budgetAllowance.id">
        <BudgetAllowanceItem
          class="mb-4"
          :budget-allowance="budgetAllowance"
          :organizations="organizations"
          :available-organizations="availableOrganizations"
          @delete="deleteBudgetAllowance"
          @save="saveBudgetAllowance" />
      </template>
      <div v-if="availableOrganizations.length > 0 && canAddBudgetAllowance" class="mt-6 pb-6 border-b border-grey-200">
        <PfButtonAction
          type="button"
          btn-style="dash"
          class="w-full"
          has-icon-left
          :icon="ICON_PLUS"
          :label="t('add-budget-allowance')"
          @click="addBudgetAllowance" />
      </div>
    </UiDialogModal>
  </div>
</template>

<script setup>
import gql from "graphql-tag";
import { ref, computed } from "vue";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";

import { subscriptionName } from "@/lib/helpers/subscription";
import { formatDate, dateUtc, textualFormat } from "@/lib/helpers/date";
import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_SUBSCRIPTION_DELETE_BUDGET_ALLOWANCE, URL_SUBSCRIPTION_ADMIN } from "@/lib/consts/urls";
import ICON_PLUS from "@/lib/icons/plus.json";

import BudgetAllowanceItem from "@/components/budget-allowance/item";

import { getMoneyFormat } from "@/lib/helpers/money";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { addSuccess, addError } = useNotificationsStore();

const budgetAllowances = ref([]);

const { mutate: createBudgetAllowanceMutation } = useMutation(
  gql`
    mutation CreateBudgetAllowance($input: CreateBudgetAllowanceInput!) {
      createBudgetAllowance(input: $input) {
        budgetAllowance {
          id
          availableFund
          originalFund
          organization {
            id
          }
        }
      }
    }
  `
);

const { mutate: editBudgetAllowanceMutation } = useMutation(
  gql`
    mutation EditBudgetAllowance($input: EditBudgetAllowanceInput!) {
      editBudgetAllowance(input: $input) {
        budgetAllowance {
          id
          availableFund
          originalFund
          organization {
            id
          }
        }
      }
    }
  `
);

const { result: resultSubscription, refetch } = useQuery(
  gql`
    query Subscription($id: ID!) {
      subscription(id: $id) {
        id
        name
        startDate
        endDate
        budgetAllowances {
          id
          availableFund
          originalFund
          organization {
            id
          }
        }
      }
    }
  `,
  {
    id: route.params.subscriptionId
  }
);
const subscription = useResult(resultSubscription, null, (data) => {
  budgetAllowances.value = budgetAllowances.value = data.subscription.budgetAllowances.map((x) => ({
    id: x.id,
    availableFund: x.availableFund,
    originalFund: x.originalFund,
    organization: x.organization.id
  }));
  return data.subscription;
});

const { result: resultOrganizations } = useQuery(
  gql`
    query Organizations {
      organizations {
        id
        name
      }
    }
  `
);
const organizations = useResult(resultOrganizations, null, (data) => {
  return data.organizations.map((x) => ({ label: x.name, value: x.id }));
});

function getSubscriptionName() {
  if (!subscription.value) {
    return "";
  }

  return `${subscriptionName(subscription.value)} (${formatDate(dateUtc(subscription.value.startDate), textualFormat)}${t(
    "date-separator"
  )}${formatDate(dateUtc(subscription.value.endDate), textualFormat)})`;
}

const availableOrganizations = computed(() => {
  if (!organizations.value || !subscription.value) {
    return [];
  }

  return organizations.value.filter((x) => budgetAllowances.value.find((y) => y.organization === x.value) === undefined);
});

const canAddBudgetAllowance = computed(() => {
  return !budgetAllowances.value.some((x) => x.isNew);
});

async function saveBudgetAllowance(input) {
  if (input.budgetAllowanceId === null) {
    await createBudgetAllowanceMutation({
      input: { amount: input.originalFund, organizationId: input.organization, subscriptionId: route.params.subscriptionId }
    });
    addSuccess(
      t("add-budget-allowance-success-notification", {
        subscriptionName: subscriptionName(subscription.value),
        amount: getMoneyFormat(input.originalFund)
      })
    );
    refetch();
  } else {
    await editBudgetAllowanceMutation({
      input: { amount: input.originalFund, budgetAllowanceId: input.budgetAllowanceId }
    });
    addSuccess(t("edit-budget-allowance-success-notification"));
  }
}

function deleteBudgetAllowance(budgetAllowance) {
  if (budgetAllowance.isNew) {
    budgetAllowances.value.splice(budgetAllowances.value.indexOf(budgetAllowance), 1);
  } else {
    if (budgetAllowance.originalFund !== budgetAllowance.availableFund) {
      addError(t("cant-remove-budget-allowance-with-participant-error"));
    } else {
      router.push({
        name: URL_SUBSCRIPTION_DELETE_BUDGET_ALLOWANCE,
        params: { subscriptionId: route.params.subscriptionId, budgetId: budgetAllowance.id }
      });
    }
  }
}

function addBudgetAllowance() {
  budgetAllowances.value.push({
    id: (Math.random() + 1).toString(36).substring(7),
    availableFund: null,
    originalFund: null,
    organization: null,
    isNew: true
  });
}
</script>
