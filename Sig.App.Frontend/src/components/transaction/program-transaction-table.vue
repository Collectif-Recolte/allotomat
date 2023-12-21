<i18n>
  {
    "en": {
      "transaction-date-hour": "Date and hour",
      "transaction-beneficiary-name": "Participants",
      "transaction-operation": "Operation",
      "transaction-amount": "Amount",
      "transaction-refund": "Refund",
      "operation-transaction-expire-fund-title": "Expire fund",
      "operation-transaction-loyalty-adding-fund-title": "Adding fund",
      "operation-transaction-loyalty-adding-fund-desc": "Gift card",
      "operation-transaction-manually-adding-fund-title": "Adding fund",
      "operation-transaction-manually-adding-fund-desc": "Manually",
      "operation-transaction-off-platform-adding-fund-title": "Adding fund",
      "operation-transaction-off-platform-adding-fund-desc": "Off platform user",
      "operation-transaction-payment-title": "Payment",
      "operation-transaction-payment-desc": "By: {x}",
      "operation-transaction-refund-budget-allowance-from-no-card-when-adding-fund-title": "Refund budget allowance from no card when adding fund",
      "operation-transaction-refund-budget-allowance-from-no-card-when-adding-fund-desc": "Refund budget allowance from no card when adding fund",
      "operation-transaction-refund-budget-allowance-from-removed-beneficiary-from-subscription-title": "Refund budget allowance from removed beneficiary from subscription",
      "operation-transaction-refund-budget-allowance-from-removed-beneficiary-from-subscription-desc": "Refund budget allowance from removed beneficiary from subscription",
      "operation-transaction-refund-budget-allowance-from-unassigned-card-title": "Refund budget allowance from unassigned card",
      "operation-transaction-refund-budget-allowance-from-unassigned-card-desc": "Refund budget allowance from unassigned card",
      "operation-transaction-refund-payment-title": "Refund payment",
      "operation-transaction-refund-payment-desc": "By: {x}",
      "operation-transaction-subscription-adding-fund-title": "Adding fund",
      "operation-transaction-subscription-adding-fund-desc": "Subscription",
      "operation-transaction-transfer-fund-title": "Transfer fund"
    },
    "fr": {
      "transaction-date-hour": "Date et heure",
      "transaction-beneficiary-name":"Participant-e-s",
      "transaction-operation": "Opération",
      "transaction-amount": "Montant",
      "transaction-refund": "Remboursement",
      "operation-transaction-expire-fund-title": "Expiration de fonds",
      "operation-transaction-loyalty-adding-fund-title": "Ajout de fonds",
      "operation-transaction-loyalty-adding-fund-desc": "Carte-cadeau",
      "operation-transaction-manually-adding-fund-title": "Ajout de fonds",
      "operation-transaction-manually-adding-fund-desc": "Manuellement",
      "operation-transaction-off-platform-adding-fund-title": "Ajout de fonds",
      "operation-transaction-off-platform-adding-fund-desc": "Utilisateur hors plateforme",
      "operation-transaction-payment-title": "Paiement",
      "operation-transaction-payment-desc": "Par: {x}",
      "operation-transaction-refund-budget-allowance-from-no-card-when-adding-fund-title": "Remboursement",
      "operation-transaction-refund-budget-allowance-from-no-card-when-adding-fund-desc": "Allocation budgétaire sans carte lors d'un ajout de fonds",
      "operation-transaction-refund-budget-allowance-from-removed-beneficiary-from-subscription-title": "Remboursement",
      "operation-transaction-refund-budget-allowance-from-removed-beneficiary-from-subscription-desc": "Allocation budgétaire d'un participant supprimé d'un abonnement",
      "operation-transaction-refund-budget-allowance-from-unassigned-card-title": "Remboursement",
      "operation-transaction-refund-budget-allowance-from-unassigned-card-desc": "Allocation budgétaire d'une carte non assignée",
      "operation-transaction-refund-payment-title": "Remboursement",
      "operation-transaction-refund-payment-desc": "Par: {x}",
      "operation-transaction-subscription-adding-fund-title": "Ajout de fonds",
      "operation-transaction-subscription-adding-fund-desc": "D'un abonnement",
      "operation-transaction-transfer-fund-title": "Transfert de fonds"
    }
  }
  </i18n>

<template>
  <UiTable :items="props.transactions" :cols="cols" has-bottom-padding>
    <template #default="slotProps">
      <td>
        {{ getTransactionDate(slotProps.item) }}
      </td>
      <td>
        {{ getBeneficiaryName(slotProps.item) }}
      </td>
      <td>
        <b>{{ getOperationName(slotProps.item) }}</b>
        <p>{{ getOperationDetail(slotProps.item) }}</p>
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

import ICON_RESET from "@/lib/icons/reset.json";

import { URL_TRANSACTION_ADMIN_REFUND } from "@/lib/consts/urls";

import { getMoneyFormat } from "@/lib/helpers/money";
import { formatDate, textualWithTimeFormat } from "@/lib/helpers/date";

import {
  EXPIRE_FUND_TRANSACTION_LOG,
  LOYALTY_ADDING_FUND_TRANSACTION_LOG,
  MANUALLY_ADDING_FUND_TRANSACTION_LOG,
  OFF_PLATFORM_ADDING_FUND_TRANSACTION_LOG,
  PAYMENT_TRANSACTION_LOG,
  REFUND_BUDGET_ALLOWANCE_FROM_NO_CARD_WHEN_ADDING_FUND_TRANSACTION_LOG,
  REFUND_BUDGET_ALLOWANCE_FROM_REMOVED_BENEFICIARY_FROM_SUBSCRIPTION_TRANSACTION_LOG,
  REFUND_BUDGET_ALLOWANCE_FROM_UNASSIGNED_CARD_TRANSACTION_LOG,
  REFUND_PAYMENT_TRANSACTION_LOG,
  SUBSCRIPTION_ADDING_FUND_TRANSACTION_LOG,
  TRANSFER_FUND_TRANSACTION_LOG
} from "@/lib/consts/enums";

const { t } = useI18n();

const props = defineProps({
  transactions: { type: Array, required: true }
});

const cols = computed(() => [
  {
    label: t("transaction-date-hour")
  },
  {
    label: t("transaction-beneficiary-name")
  },
  {
    label: t("transaction-operation")
  },
  {
    label: t("transaction-amount")
  },
  { label: "" }
]);

function getTransactionDate(transaction) {
  return formatDate(new Date(transaction.createdAt), textualWithTimeFormat);
}

function getBeneficiaryName(transaction) {
  if (transaction.beneficiaryFirstname !== null && transaction.beneficiaryLastname !== null) {
    return `${transaction.beneficiaryFirstname} ${transaction.beneficiaryLastname}`;
  }
  return `-`;
}

function getOperationName(transaction) {
  switch (transaction.discriminator) {
    case EXPIRE_FUND_TRANSACTION_LOG:
      return t("operation-transaction-expire-fund-title");
    case LOYALTY_ADDING_FUND_TRANSACTION_LOG:
      return t("operation-transaction-loyalty-adding-fund-title");
    case MANUALLY_ADDING_FUND_TRANSACTION_LOG:
      return t("operation-transaction-manually-adding-fund-title");
    case OFF_PLATFORM_ADDING_FUND_TRANSACTION_LOG:
      return t("operation-transaction-off-platform-adding-fund-title");
    case PAYMENT_TRANSACTION_LOG:
      return t("operation-transaction-payment-title");
    case REFUND_BUDGET_ALLOWANCE_FROM_NO_CARD_WHEN_ADDING_FUND_TRANSACTION_LOG:
      return t("operation-transaction-refund-budget-allowance-from-no-card-when-adding-fund-title");
    case REFUND_BUDGET_ALLOWANCE_FROM_REMOVED_BENEFICIARY_FROM_SUBSCRIPTION_TRANSACTION_LOG:
      return t("operation-transaction-refund-budget-allowance-from-removed-beneficiary-from-subscription-title");
    case REFUND_BUDGET_ALLOWANCE_FROM_UNASSIGNED_CARD_TRANSACTION_LOG:
      return t("operation-transaction-refund-budget-allowance-from-unassigned-card-title");
    case REFUND_PAYMENT_TRANSACTION_LOG:
      return t("operation-transaction-refund-payment-title");
    case SUBSCRIPTION_ADDING_FUND_TRANSACTION_LOG:
      return t("operation-transaction-subscription-adding-fund-title");
    case TRANSFER_FUND_TRANSACTION_LOG:
      return t("operation-transaction-transfer-fund-title");
    default:
      return "";
  }
}

function getOperationDetail(transaction) {
  switch (transaction.discriminator) {
    case LOYALTY_ADDING_FUND_TRANSACTION_LOG:
      return t("operation-transaction-loyalty-adding-fund-desc");
    case MANUALLY_ADDING_FUND_TRANSACTION_LOG:
      return t("operation-transaction-manually-adding-fund-desc");
    case OFF_PLATFORM_ADDING_FUND_TRANSACTION_LOG:
      return t("operation-transaction-off-platform-adding-fund-desc");
    case PAYMENT_TRANSACTION_LOG:
      return t("operation-transaction-payment-desc", {
        x: transaction.initiatedByProject ? transaction.projectName : transaction.marketName
      });
    case REFUND_BUDGET_ALLOWANCE_FROM_NO_CARD_WHEN_ADDING_FUND_TRANSACTION_LOG:
      return t("operation-transaction-refund-budget-allowance-from-no-card-when-adding-fund-desc");
    case REFUND_BUDGET_ALLOWANCE_FROM_REMOVED_BENEFICIARY_FROM_SUBSCRIPTION_TRANSACTION_LOG:
      return t("operation-transaction-refund-budget-allowance-from-removed-beneficiary-from-subscription-desc");
    case REFUND_BUDGET_ALLOWANCE_FROM_UNASSIGNED_CARD_TRANSACTION_LOG:
      return t("operation-transaction-refund-budget-allowance-from-unassigned-card-desc");
    case REFUND_PAYMENT_TRANSACTION_LOG:
      return t("operation-transaction-refund-payment-desc", {
        x: transaction.initiatedByProject ? transaction.projectName : transaction.marketName
      });
    case SUBSCRIPTION_ADDING_FUND_TRANSACTION_LOG:
      return t("operation-transaction-subscription-adding-fund-desc");
    default:
      return "";
  }
}

function getTransactionAmount(transaction) {
  return getMoneyFormat(parseFloat(transaction.totalAmount));
}

function getBtnGroup(item) {
  if (item.discriminator !== PAYMENT_TRANSACTION_LOG) {
    return [];
  }
  return [
    {
      isExtra: true,
      icon: ICON_RESET,
      label: t("transaction-refund"),
      route: { name: URL_TRANSACTION_ADMIN_REFUND, params: { transactionId: item.transaction.id } }
    }
  ];
}
</script>
