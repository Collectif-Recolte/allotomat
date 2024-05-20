<i18n>
  {
    "en": {
      "organization-name": "Group",
      "organization-total-active-subscriptions-envelopes": "Budget allowance (before allocation)",
      "organization-total-allocated-on-cards": "Total allocated on cards",
      "organization-remaining-per-envelope": "Remaining per envelope",
      "organization-balance-on-cards": "Balance on cards",
      "organization-card-spending-amounts": "Card spending",
      "organization-expired-amounts": "Expired amounts",
      "organization-totals": "Total"
    },
    "fr": {
      "organization-name": "Groupe",
      "organization-total-active-subscriptions-envelopes": "Enveloppe (avant allocation)",
      "organization-total-allocated-on-cards": "Total alloué sur les cartes",
      "organization-remaining-per-envelope":"Restants par enveloppe",
      "organization-balance-on-cards": "Solde sur les cartes",
      "organization-card-spending-amounts": "Dépenses des cartes",
      "organization-expired-amounts": "Montants expirés",
      "organization-totals": "Total"
    }
  }
</i18n>

<template>
  <UiTable v-if="props.organizationsStats" :items="props.organizationsStats" :cols="cols" :footers="footers">
    <template #default="slotProps">
      <td>
        {{ getOrganizationName(slotProps.item) }}
      </td>
      <td class="text-right">
        {{ getOrganizationTotalActiveSubscriptionEnvelopes(slotProps.item) }}
      </td>
      <td class="text-right">
        {{ getOrganizationTotalAllocatedOnCards(slotProps.item) }}
      </td>
      <td class="text-right">
        {{ getOrganizationRemainingPerEnvelope(slotProps.item) }}
      </td>
      <td class="text-right">
        {{ getOrganizationBalanceOnCards(slotProps.item) }}
      </td>
      <td class="text-right">
        {{ getOrganizationCardSpendingAmounts(slotProps.item) }}
      </td>
      <td class="text-right">
        {{ getOrganizationExpiredAmounts(slotProps.item) }}
      </td>
    </template>
  </UiTable>
</template>

<script setup>
import { defineProps, computed } from "vue";
import { useI18n } from "vue-i18n";

import { getMoneyFormat } from "@/lib/helpers/money";

const { t } = useI18n();

const props = defineProps({
  organizationsStats: { type: Array, required: true }
});

const cols = computed(() => {
  const cols = [];

  cols.push({ label: t("organization-name") });
  cols.push({ label: t("organization-total-active-subscriptions-envelopes"), isRight: true });
  cols.push({ label: t("organization-total-allocated-on-cards"), isRight: true });
  cols.push({ label: t("organization-remaining-per-envelope"), isRight: true });
  cols.push({ label: t("organization-balance-on-cards"), isRight: true });
  cols.push({ label: t("organization-card-spending-amounts"), isRight: true });
  cols.push({ label: t("organization-expired-amounts"), isRight: true });
  return cols;
});

const footers = computed(() => {
  const footers = [];

  footers.push({ value: t("organization-totals") });
  footers.push({ value: getOrganizationTotalActiveSubscriptionEnvelopesTotal(), isRight: true });
  footers.push({ value: getOrganizationTotalAllocatedOnCardsTotal(), isRight: true });
  footers.push({ value: getOrganizationRemainingPerEnvelopeTotal(), isRight: true });
  footers.push({ value: getOrganizationBalanceOnCardsTotal(), isRight: true });
  footers.push({ value: getOrganizationCardSpendingAmountsTotal(), isRight: true });
  footers.push({ value: getOrganizationExpiredAmountsTotal(), isRight: true });

  return footers;
});

function getOrganizationName(organizationStats) {
  return organizationStats.organization ? organizationStats.organization.name : "";
}

function getOrganizationTotalActiveSubscriptionEnvelopes(organizationStats) {
  return getMoneyFormat(organizationStats.totalActiveSubscriptionsEnvelopes);
}

function getOrganizationTotalActiveSubscriptionEnvelopesTotal() {
  let amounts = 0;

  props.organizationsStats.forEach((x) => {
    amounts += x.totalActiveSubscriptionsEnvelopes;
  });

  return getMoneyFormat(amounts);
}

function getOrganizationTotalAllocatedOnCards(organizationStats) {
  return getMoneyFormat(organizationStats.totalAllocatedOnCards);
}

function getOrganizationTotalAllocatedOnCardsTotal() {
  let amounts = 0;

  props.organizationsStats.forEach((x) => {
    amounts += x.totalAllocatedOnCards;
  });

  return getMoneyFormat(amounts);
}

function getOrganizationRemainingPerEnvelope(organizationStats) {
  return getMoneyFormat(organizationStats.remainingPerEnvelope);
}

function getOrganizationRemainingPerEnvelopeTotal() {
  let amounts = 0;

  props.organizationsStats.forEach((x) => {
    amounts += x.remainingPerEnvelope;
  });

  return getMoneyFormat(amounts);
}

function getOrganizationBalanceOnCards(organizationStats) {
  return getMoneyFormat(organizationStats.balanceOnCards);
}

function getOrganizationBalanceOnCardsTotal() {
  let amounts = 0;

  props.organizationsStats.forEach((x) => {
    amounts += x.balanceOnCards;
  });

  return getMoneyFormat(amounts);
}

function getOrganizationCardSpendingAmounts(organizationStats) {
  return getMoneyFormat(organizationStats.cardSpendingAmounts);
}

function getOrganizationCardSpendingAmountsTotal() {
  let amounts = 0;

  props.organizationsStats.forEach((x) => {
    amounts += x.cardSpendingAmounts;
  });

  return getMoneyFormat(amounts);
}

function getOrganizationExpiredAmounts(organizationStats) {
  return getMoneyFormat(organizationStats.expiredAmounts);
}

function getOrganizationExpiredAmountsTotal() {
  let amounts = 0;

  props.organizationsStats.forEach((x) => {
    amounts += x.expiredAmounts;
  });

  return getMoneyFormat(amounts);
}
</script>
