<i18n>
  {
    "en": {
      "transaction-date-hour": "Date and hour",
      "transaction-card-no": "Card number",
      "transaction-amount": "Amount"
    },
    "fr": {
      "transaction-date-hour": "Date et heure",
      "transaction-card-no": "No de carte",
      "transaction-amount": "Montant"
    }
  }
  </i18n>

<template>
  <UiTable :items="props.transactions" :cols="cols">
    <template #default="slotProps">
      <td>
        {{ getTransactionDate(slotProps.item) }}
      </td>
      <td>
        {{ getCardNo(slotProps.item) }}
      </td>
      <td>
        {{ getTransactionAmount(slotProps.item) }}
      </td>
    </template>
  </UiTable>
</template>

<script setup>
import { defineProps, computed } from "vue";
import { useI18n } from "vue-i18n";

import { getMoneyFormat } from "@/lib/helpers/money";
import { formatDate, textualWithTimeFormat } from "@/lib/helpers/date";

const { t } = useI18n();

const props = defineProps({
  transactions: { type: Array, required: true }
});

const cols = computed(() => [
  {
    label: t("transaction-date-hour")
  },
  {
    label: t("transaction-card-no")
  },
  {
    label: t("transaction-amount"),
    isRight: true
  }
]);

function getTransactionDate(transaction) {
  return formatDate(new Date(transaction.createdAt), textualWithTimeFormat);
}

function getCardNo(transaction) {
  return `${transaction.card?.programCardId}`;
}

function getTransactionAmount(transaction) {
  return getMoneyFormat(parseFloat(transaction.amount));
}
</script>
