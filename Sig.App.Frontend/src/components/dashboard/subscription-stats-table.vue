<i18n>
  {
    "en": {
      "subscription-name": "Subscription",
      "subscription-start-date": "Start date",
      "subscription-end-date": "End date",
      "subscription-expiration": "Expiration",
      "subscription-total-budget-allowance" : "Total budget allowance",
      "subscription-available-fund": "Available fund",
      "subscription-amount-allocated": "Allocated amount ",
      "subscription-totals": "Total"
    },
    "fr": {
      "subscription-name": "Abonnement",
      "subscription-start-date": "Date de début",
      "subscription-end-date": "Date de fin",
      "subscription-expiration": "Expiration",
      "subscription-total-budget-allowance" : "Budget total",
      "subscription-available-fund": "Fonds disponibles",
      "subscription-amount-allocated": "Montant alloué",
      "subscription-totals": "Total"
    }
  }
</i18n>

<template>
  <UiTable v-if="props.subscriptionStats" :items="props.subscriptionStats" :cols="cols" :footers="footers">
    <template #default="slotProps">
      <td>
        {{ getSubscriptionName(slotProps.item) }}
      </td>
      <td class="text-right">
        {{ getSubscriptionStartDate(slotProps.item) }}
      </td>
      <td class="text-right">
        {{ getSubscriptionEndDate(slotProps.item) }}
      </td>
      <td class="text-right">
        {{ getSubscriptionExpiration(slotProps.item) }}
      </td>
      <td class="text-right">
        {{ getSubscriptionBudgetAllowance(slotProps.item) }}
      </td>
      <td class="text-right">
        {{ getSubscritionAvailableFund(slotProps.item) }}
      </td>
      <td class="text-right">
        {{ getSubscriptionAmountAllocated(slotProps.item) }}
      </td>
    </template>
  </UiTable>
</template>

<script setup>
import { defineProps, computed } from "vue";
import { useI18n } from "vue-i18n";

import { getMoneyFormat } from "@/lib/helpers/money";
import { formatDate, textualFormat } from "@/lib/helpers/date";

const { t } = useI18n();

const props = defineProps({
  subscriptionStats: { type: Array, required: true }
});

const cols = computed(() => {
  const cols = [];

  cols.push({ label: t("subscription-name") });
  cols.push({ label: t("subscription-start-date"), isRight: true });
  cols.push({ label: t("subscription-end-date"), isRight: true });
  cols.push({ label: t("subscription-expiration"), isRight: true });
  cols.push({ label: t("subscription-total-budget-allowance"), isRight: true });
  cols.push({ label: t("subscription-available-fund"), isRight: true });
  cols.push({ label: t("subscription-amount-allocated"), isRight: true });
  return cols;
});

const footers = computed(() => {
  const footers = [];

  footers.push({ value: t("subscription-totals") });
  footers.push({ value: "", isRight: true }); // Empty cell subscription-start-date
  footers.push({ value: "", isRight: true }); // Empty cell subscription-end-date
  footers.push({ value: "", isRight: true }); // Empty cell subscription-expiration
  footers.push({ value: getSubscriptionBudgetAllowanceTotal(), isRight: true });
  footers.push({ value: getSubscritionAvailableFundTotal(), isRight: true });
  footers.push({ value: getSubscriptionAmountAllocatedTotal(), isRight: true });

  return footers;
});

function getSubscriptionName(subscriptionStat) {
  return subscriptionStat.name;
}

function getSubscriptionStartDate(subscriptionStat) {
  return formatDate(new Date(subscriptionStat.startDate), textualFormat);
}

function getSubscriptionEndDate(subscriptionStat) {
  return formatDate(new Date(subscriptionStat.endDate), textualFormat);
}

function getSubscriptionExpiration(subscriptionStat) {
  return subscriptionStat.expiration;
}

function getSubscriptionBudgetAllowance(subscriptionStat) {
  return getMoneyFormat(subscriptionStat.originalFund);
}

function getSubscriptionBudgetAllowanceTotal() {
  let amounts = 0;

  props.subscriptionStats.forEach((x) => {
    amounts += x.originalFund;
  });

  return getMoneyFormat(amounts);
}

function getSubscritionAvailableFund(subscriptionStat) {
  return getMoneyFormat(subscriptionStat.availableFund);
}

function getSubscritionAvailableFundTotal() {
  let amounts = 0;

  props.subscriptionStats.forEach((x) => {
    amounts += x.availableFund;
  });

  return getMoneyFormat(amounts);
}

function getSubscriptionAmountAllocated(subscriptionStat) {
  return getMoneyFormat(subscriptionStat.amountAllocated);
}

function getSubscriptionAmountAllocatedTotal() {
  let amounts = 0;

  props.subscriptionStats.forEach((x) => {
    amounts += x.amountAllocated;
  });

  return getMoneyFormat(amounts);
}
</script>
