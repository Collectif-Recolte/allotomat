<i18n>
  {
    "en": {
      "budget-allowance-name": "Budget allowance",
      "transaction-date-hour": "Date and time",
      "operation-name": "Operation",
      "amount": "Amount",
      "delete-budget-allowance-transaction": "Delete budget allowance transaction",
      "edit-budget-allowance-transaction": "Edit budget allowance transaction",
      "move-budget-allowance-transaction": "Move budget allowance transaction",
      "create-budget-allowance-transaction": "Create budget allowance transaction",
      "target-budget-allowance-name": "Target budget allowance name"
    },
    "fr": {
      "budget-allowance-name": "Enveloppe",
      "transaction-date-hour": "Date et heure",
      "operation-name": "Opération",
      "amount": "Montant",
      "delete-budget-allowance-transaction": "Suppression d'une enveloppe",
      "edit-budget-allowance-transaction": "Modification d'une enveloppe",
      "move-budget-allowance-transaction": "Transfert d'une enveloppe",
      "create-budget-allowance-transaction": "Création d'une enveloppe",
      "target-budget-allowance-name": "Enveloppe cible"
    }
  }
  </i18n>

<template>
  <UiTable :items="props.budgetAllowanceLogs" :cols="cols">
    <template #default="slotProps">
      <td>
        {{ getBudgetAllowanceLogDate(slotProps.item) }}
      </td>
      <td>
        <div>
          {{ getBudgetAllowanceName(slotProps.item) }}
        </div>
      </td>
      <td>
        <div>
          {{ getTargetBudgetAllowanceName(slotProps.item) }}
        </div>
      </td>
      <td>
        <div>
          <b>{{ getBudgetAllowanceLogDescription(slotProps.item) }}</b>
        </div>
      </td>
      <td>
        <div>
          {{ getBudgetAllowanceLogAmount(slotProps.item) }}
        </div>
      </td>
    </template>
  </UiTable>
</template>

<script setup>
import { defineProps, computed } from "vue";
import { useI18n } from "vue-i18n";

import { formatDate, textualWithTimeFormat } from "@/lib/helpers/date";
import { getMoneyFormat } from "@/lib/helpers/money";

const { t } = useI18n();

const props = defineProps({
  budgetAllowanceLogs: { type: Array, required: true }
});

function getBudgetAllowanceLogDate(budgetAllowanceLog) {
  return formatDate(new Date(budgetAllowanceLog.createdAt), textualWithTimeFormat);
}

function getBudgetAllowanceName(budgetAllowanceLog) {
  return budgetAllowanceLog.subscriptionName + " - " + budgetAllowanceLog.organizationName;
}

function getTargetBudgetAllowanceName(budgetAllowanceLog) {
  if (budgetAllowanceLog.discriminator === "MOVE_BUDGET_ALLOWANCE_LOG") {
    return budgetAllowanceLog.targetSubscriptionName + " - " + budgetAllowanceLog.targetOrganizationName;
  }
  return "-";
}

function getBudgetAllowanceLogDescription(budgetAllowanceLog) {
  switch (budgetAllowanceLog.discriminator) {
    case "DELETE_BUDGET_ALLOWANCE_LOG":
      return t("delete-budget-allowance-transaction");
    case "EDIT_BUDGET_ALLOWANCE_LOG":
      return t("edit-budget-allowance-transaction");
    case "MOVE_BUDGET_ALLOWANCE_LOG":
      return t("move-budget-allowance-transaction");
    case "CREATE_BUDGET_ALLOWANCE_LOG":
      return t("create-budget-allowance-transaction");
  }
}

function getBudgetAllowanceLogAmount(budgetAllowanceLog) {
  var amount = budgetAllowanceLog.amount;
  
  const invertedTypes = ["DELETE_BUDGET_ALLOWANCE_LOG", "MOVE_BUDGET_ALLOWANCE_LOG"];
  if (invertedTypes.includes(budgetAllowanceLog.discriminator)) {
    amount *= -1;
  }
  
  return amount > 0
    ? `+${getMoneyFormat(amount)}`
    : `${getMoneyFormat(amount)}`;
}

const cols = computed(() => [
  { label: t("transaction-date-hour") },
  { label: t("budget-allowance-name") },
  { label: t("target-budget-allowance-name") },
  { label: t("operation-name") },
  { label: t("amount") }
]);
</script>
