<i18n>
  {
    "en": {
      "transaction-date-hour": "Date and time",
      "transaction-beneficiary-name": "Participant",
      "transaction-operation": "Operation",
      "transaction-amount": "Amount",
      "transaction-refund": "Refund",
      "operation-transaction-payment-title": "Purchase - {subscriptionName}",
      "operation-transaction-payment-desc": "By: {x}",
      "operation-transaction-refund-payment-title": "Purchase refund",
      "operation-transaction-refund-payment-desc": "By: {x}",
      "operation-transaction-manually-adding-fund-title": "Transfer",
      "operation-transaction-manually-adding-fund-desc": "Manual",
      "operation-transaction-subscription-adding-fund-title": "Transfer",
      "operation-transaction-subscription-adding-fund-desc": "Subscription",
      "operation-transaction-loyalty-adding-fund-title": "Transfer",
      "operation-transaction-loyalty-adding-fund-desc": "Gift card created",
      "operation-transaction-loyalty-editing-fund-title": "Transfer",
      "operation-transaction-loyalty-editing-fund-desc": "Gift funds adjusted",
      "operation-transaction-off-platform-adding-fund-title": "Transfer",
      "operation-transaction-off-platform-adding-fund-desc": "Off-platform",
      "operation-transaction-expire-fund-title": "Expiry",
      "operation-transaction-refund-budget-allowance-from-no-card-when-adding-fund-title": "Envelope refund",
      "operation-transaction-refund-budget-allowance-from-no-card-when-adding-fund-desc": "Participant without a card",
      "operation-transaction-refund-budget-allowance-from-removed-beneficiary-from-subscription-title": "Envelope refund",
      "operation-transaction-refund-budget-allowance-from-removed-beneficiary-from-subscription-desc": "Subscription unassigned",
      "operation-transaction-refund-budget-allowance-from-unassigned-card-title": "Envelope refund",
      "operation-transaction-refund-budget-allowance-from-unassigned-card-desc": "Card unassigned",
      "operation-transaction-allocate-budget-allowance-from-subscription-assignment-title": "Envelope withdrawal",
      "operation-transaction-allocate-budget-allowance-from-subscription-assignment-desc": "Subscription assigned",
      "operation-transaction-transfer-fund-title": "Card replacement",
      "beneficiary-id1": "ID 1:",
      "beneficiary-id2": "ID 2:",
    },
    "fr": {
      "transaction-date-hour": "Date et heure",
      "transaction-beneficiary-name":"Participant·e",
      "transaction-operation": "Opération",
      "transaction-amount": "Montant",
      "transaction-refund": "Remboursement",
      "operation-transaction-payment-title": "Achat - {subscriptionName}",
      "operation-transaction-payment-desc": "Par: {x}",
      "operation-transaction-refund-payment-title": "Remboursement d'achat",
      "operation-transaction-refund-payment-desc": "Par: {x}",
      "operation-transaction-manually-adding-fund-title": "Versement",
      "operation-transaction-manually-adding-fund-desc": "Sur mesure",
      "operation-transaction-subscription-adding-fund-title": "Versement",
      "operation-transaction-subscription-adding-fund-desc": "Abonnement",
      "operation-transaction-loyalty-adding-fund-title": "Versement",
      "operation-transaction-loyalty-adding-fund-desc": "Création de carte-cadeau",
      "operation-transaction-loyalty-editing-fund-title": "Versement",
      "operation-transaction-loyalty-editing-fund-desc": "Ajustement de fonds cadeaux",
      "operation-transaction-off-platform-adding-fund-title": "Versement",
      "operation-transaction-off-platform-adding-fund-desc": "Hors plateforme",
      "operation-transaction-expire-fund-title": "Expiration",
      "operation-transaction-refund-budget-allowance-from-no-card-when-adding-fund-title": "Remboursement d'enveloppe",
      "operation-transaction-refund-budget-allowance-from-no-card-when-adding-fund-desc": "Participant·e sans carte",
      "operation-transaction-refund-budget-allowance-from-removed-beneficiary-from-subscription-title": "Remboursement d'enveloppe",
      "operation-transaction-refund-budget-allowance-from-removed-beneficiary-from-subscription-desc": "Désattribution d'abonnement",
      "operation-transaction-refund-budget-allowance-from-unassigned-card-title": "Remboursement d'enveloppe",
      "operation-transaction-refund-budget-allowance-from-unassigned-card-desc": "Désassignation de carte",
      "operation-transaction-allocate-budget-allowance-from-subscription-assignment-title": "Retrait d'enveloppe",
      "operation-transaction-allocate-budget-allowance-from-subscription-assignment-desc": "Attribution d'abonnement",
      "operation-transaction-transfer-fund-title": "Remplacement de carte",
      "beneficiary-id1": "ID 1 :",
      "beneficiary-id2": "ID 2 :",
    }
  }
  </i18n>

<template>
  <UiTable :items="props.transactions" :cols="cols" has-bottom-padding>
    <template #default="slotProps">
      <td>
        {{ getTransactionDate(slotProps.item) }}
      </td>
      <td v-if="!props.beneficiariesAreAnonymous">
        <b>{{ getBeneficiaryName(slotProps.item) }}</b>
        <p class="mb-0">{{ t("beneficiary-id1") }} {{ getBeneficiaryId1(slotProps.item) }}</p>
        <p class="mb-0">{{ t("beneficiary-id2") }} {{ getBeneficiaryId2(slotProps.item) }}</p>
      </td>
      <td>
        <b>{{ getOperationName(slotProps.item) }}</b>
        <p class="mb-0">{{ getOperationDetail(slotProps.item) }}</p>
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

import { URL_TRANSACTION_ADMIN_REFUND } from "@/lib/consts/urls";

import { useAuthStore } from "@/lib/store/auth";

import { getMoneyFormat } from "@/lib/helpers/money";
import { formatDate, textualWithTimeFormat } from "@/lib/helpers/date";

import {
  EXPIRE_FUND_TRANSACTION_LOG,
  LOYALTY_ADDING_FUND_TRANSACTION_LOG,
  LOYALTY_EDIT_FUND_TRANSACTION_LOG,
  MANUALLY_ADDING_FUND_TRANSACTION_LOG,
  OFF_PLATFORM_ADDING_FUND_TRANSACTION_LOG,
  PAYMENT_TRANSACTION_LOG,
  REFUND_BUDGET_ALLOWANCE_FROM_NO_CARD_WHEN_ADDING_FUND_TRANSACTION_LOG,
  REFUND_BUDGET_ALLOWANCE_FROM_REMOVED_BENEFICIARY_FROM_SUBSCRIPTION_TRANSACTION_LOG,
  REFUND_BUDGET_ALLOWANCE_FROM_UNASSIGNED_CARD_TRANSACTION_LOG,
  REFUND_PAYMENT_TRANSACTION_LOG,
  SUBSCRIPTION_ADDING_FUND_TRANSACTION_LOG,
  TRANSFER_FUND_TRANSACTION_LOG,
  ADDING_FUND_TRANSACTION_STATUS_ACTIVED
} from "@/lib/consts/enums";

import { GLOBAL_REFUND_TRANSACTION } from "@/lib/consts/permissions";

const { t } = useI18n();
const { getGlobalPermissions } = storeToRefs(useAuthStore());

const props = defineProps({
  transactions: { type: Array, required: true },
  beneficiariesAreAnonymous: {
    type: Boolean,
    default: false
  }
});

const cols = computed(() => {
  if (props.beneficiariesAreAnonymous) {
    return [
      {
        label: t("transaction-date-hour")
      },
      {
        label: t("transaction-operation")
      },
      {
        label: t("transaction-amount")
      },
      { label: "" }
    ];
  }
  return [
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
  ];
});

const canRefundTransaction = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_REFUND_TRANSACTION);
});

function getTransactionDate(transaction) {
  return formatDate(new Date(transaction.createdAt), textualWithTimeFormat);
}

function getBeneficiaryName(transaction) {
  if (transaction.beneficiaryFirstname !== null && transaction.beneficiaryLastname !== null) {
    return `${transaction.beneficiaryFirstname} ${transaction.beneficiaryLastname}`;
  }
  return `-`;
}

function getBeneficiaryId1(transaction) {
  return transaction.beneficiaryID1 !== null ? transaction.beneficiaryID1 : "-";
}

function getBeneficiaryId2(transaction) {
  return transaction.beneficiaryID2 !== null ? transaction.beneficiaryID2 : "-";
}

function getOperationName(transaction) {
  switch (transaction.discriminator) {
    case EXPIRE_FUND_TRANSACTION_LOG:
      return t("operation-transaction-expire-fund-title");
    case LOYALTY_ADDING_FUND_TRANSACTION_LOG:
      return t("operation-transaction-loyalty-adding-fund-title");
    case LOYALTY_EDIT_FUND_TRANSACTION_LOG:
      return t("operation-transaction-loyalty-editing-fund-title");
    case MANUALLY_ADDING_FUND_TRANSACTION_LOG:
      return t("operation-transaction-manually-adding-fund-title");
    case OFF_PLATFORM_ADDING_FUND_TRANSACTION_LOG:
      return t("operation-transaction-off-platform-adding-fund-title");
    case PAYMENT_TRANSACTION_LOG:
      return t("operation-transaction-payment-title", {
        subscriptionName:
          transaction.subscriptionName !== null
            ? transaction.subscriptionName
            : t("operation-transaction-loyalty-adding-fund-desc")
      });
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
    case LOYALTY_EDIT_FUND_TRANSACTION_LOG:
      return t("operation-transaction-loyalty-editing-fund-desc");
    case MANUALLY_ADDING_FUND_TRANSACTION_LOG:
      return t("operation-transaction-manually-adding-fund-desc");
    case OFF_PLATFORM_ADDING_FUND_TRANSACTION_LOG:
      return t("operation-transaction-off-platform-adding-fund-desc");
    case PAYMENT_TRANSACTION_LOG:
      return t("operation-transaction-payment-desc", {
        x: transaction.initiatedByProject
          ? transaction.projectName
          : transaction.initiatedByOrganization
          ? transaction.organizationName
          : transaction.marketName
      });
    case REFUND_BUDGET_ALLOWANCE_FROM_NO_CARD_WHEN_ADDING_FUND_TRANSACTION_LOG:
      return t("operation-transaction-refund-budget-allowance-from-no-card-when-adding-fund-desc");
    case REFUND_BUDGET_ALLOWANCE_FROM_REMOVED_BENEFICIARY_FROM_SUBSCRIPTION_TRANSACTION_LOG:
      return t("operation-transaction-refund-budget-allowance-from-removed-beneficiary-from-subscription-desc");
    case REFUND_BUDGET_ALLOWANCE_FROM_UNASSIGNED_CARD_TRANSACTION_LOG:
      return t("operation-transaction-refund-budget-allowance-from-unassigned-card-desc");
    case REFUND_PAYMENT_TRANSACTION_LOG:
      return t("operation-transaction-refund-payment-desc", {
        x: transaction.initiatedByProject
          ? transaction.projectName
          : transaction.initiatedByOrganization
          ? transaction.organizationName
          : transaction.marketName
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
  if (item.discriminator !== PAYMENT_TRANSACTION_LOG || !canRefundTransaction.value) {
    return [];
  }
  if (!hasAnyActiveSubscription(item)) return [];
  return [
    {
      isExtra: true,
      icon: ICON_RESET,
      label: t("transaction-refund"),
      route: { name: URL_TRANSACTION_ADMIN_REFUND, params: { transactionId: item.transaction.id } }
    }
  ];
}

function hasAnyActiveSubscription(item) {
  //  In this case, we don't know if the transaction have any active Subscription since it's the old way
  if (
    item.transaction.paymentTransactionAddingFundTransactions === null ||
    item.transaction.paymentTransactionAddingFundTransactions.length === 0
  )
    return true;

  for (const transaction of item.transaction.paymentTransactionAddingFundTransactions) {
    if (
      transaction.addingFundTransaction.status === ADDING_FUND_TRANSACTION_STATUS_ACTIVED &&
      transaction.amount > transaction.refundAmount &&
      (transaction.addingFundTransaction.expirationDate === null ||
        (transaction.addingFundTransaction.__typename === "SubscriptionAddingFundTransactionGraphType" &&
          item.subscriptionId === transaction.addingFundTransaction.subscription.subscription.id) ||
        (transaction.addingFundTransaction.__typename === "ManuallyAddingFundTransactionGraphType" &&
          item.subscriptionId === transaction.addingFundTransaction.subscription.id))
    )
      return true;
  }

  return false;
}
</script>
