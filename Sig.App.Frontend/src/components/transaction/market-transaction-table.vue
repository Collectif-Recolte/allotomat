<i18n>
  {
    "en": {
      "transaction-date-hour": "Date and hour",
      "transaction-card-no": "Card number",
      "transaction-amount": "Amount",
      "transaction-refund": "Refund"
    },
    "fr": {
      "transaction-date-hour": "Date et heure",
      "transaction-card-no": "No de carte",
      "transaction-amount": "Montant",
      "transaction-refund": "Remboursement"
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
      <td>
        <UiButtonGroup :items="getBtnGroup(slotProps.item)" />
      </td>
    </template>
  </UiTable>
</template>

<script setup>
import { defineProps, computed } from "vue";
import { useI18n } from "vue-i18n";
import { storeToRefs } from "pinia";

import ICON_RESET from "@/lib/icons/reset.json";

import { URL_MARKET_TRANSACTION_REFUND, URL_MARKETGROUP_TRANSACTION_REFUND } from "@/lib/consts/urls";
import { USER_TYPE_MARKETGROUPMANAGER } from "@/lib/consts/enums";

import { useAuthStore } from "@/lib/store/auth";
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
    label: t("transaction-amount")
  },
  { label: "" }
]);

const { userType } = storeToRefs(useAuthStore());
const isMarketGroupManager = computed(() => {
  return userType.value === USER_TYPE_MARKETGROUPMANAGER;
});

function getTransactionDate(transaction) {
  return formatDate(new Date(transaction.createdAt), textualWithTimeFormat);
}

function getCardNo(transaction) {
  if (transaction.card === null) return "-";
  return `${transaction.card?.programCardId}`;
}

function getTransactionAmount(transaction) {
  if (transaction.__typename === "RefundTransactionGraphType") {
    return getMoneyFormat(parseFloat(-transaction.amount));
  }
  return getMoneyFormat(parseFloat(transaction.amount));
}

function getBtnGroup(transaction) {
  if (transaction.__typename === "RefundTransactionGraphType") {
    return [];
  }
  return [
    {
      isExtra: true,
      icon: ICON_RESET,
      label: t("transaction-refund"),
      route: {
        name: isMarketGroupManager.value ? URL_MARKETGROUP_TRANSACTION_REFUND : URL_MARKET_TRANSACTION_REFUND,
        params: { transactionId: transaction.id }
      }
    }
  ];
}
</script>
