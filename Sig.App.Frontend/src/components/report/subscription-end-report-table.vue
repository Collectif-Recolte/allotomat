<i18n>
  {
    "en": {
      "organization-name": "Group",
      "subscription-name": "Subscription",
      "total-purchases": "Number of purchases",
      "cards-with-funds": "Cards that received funds",
      "cards-used-for-purchases": "Cards used for purchases",
      "merchants-with-purchases": "Merchants with purchases",
      "total-funds-loaded": "Total funds loaded",
      "total-purchase-value": "Total purchase value",
      "total-expired-amount": "Total expired amount",
      "subscription-totals": "Total"
    },
    "fr": {
      "organization-name": "Groupe",
      "subscription-name": "Abonnement",
      "total-purchases": "Nombre d’achats",
      "cards-with-funds": "Cartes qui ont reçu des fonds",
      "cards-used-for-purchases": "Cartes utilisées pour les achats",
      "merchants-with-purchases": "Marchands avec achats",
      "total-funds-loaded": "Total des fonds chargés",
      "total-purchase-value": "Valeur totale des achats",
      "total-expired-amount": "Montant total expiré",
      "subscription-totals": "Total"
    }
  }
  </i18n>

<template>
  <UiTable :items="props.organizations" :cols="cols" :footers="footers">
    <template #default="slotProps">
      <td>
        <div class="inline-flex items-center">
          {{ slotProps.item.organization.name }}
        </div>
      </td>
      <td>
        <div class="inline-flex items-center">
          {{ slotProps.item.subscription.name }}
        </div>
      </td>
      <td class="text-right">
        <div class="inline-flex items-center">
          {{ slotProps.item.totalPurchases }}
        </div>
      </td>
      <td class="text-right">
        <div class="inline-flex items-center">
          {{ slotProps.item.cardsWithFunds }}
        </div>
      </td>
      <td class="text-right">
        <div class="inline-flex items-center">
          {{ slotProps.item.cardsUsedForPurchases }}
        </div>
      </td>
      <td class="text-right">
        <div class="inline-flex items-center">
          {{ slotProps.item.merchantsWithPurchases }}
        </div>
      </td>
      <td class="text-right">
        <div class="inline-flex items-center">
          {{ getMoneyFormat(slotProps.item.totalFundsLoaded) }}
        </div>
      </td>
      <td class="text-right">
        <div class="inline-flex items-center">
          {{ getMoneyFormat(slotProps.item.totalPurchaseValue) }}
        </div>
      </td>
      <td class="text-right">
        <div class="inline-flex items-center">
          {{ getMoneyFormat(slotProps.item.totalExpiredAmount) }}
        </div>
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
  organizations: { type: Array, required: true }
});

const cols = computed(() => [
  { label: t("organization-name") },
  { label: t("subscription-name") },
  {
    label: t("total-purchases"),
    isRight: true
  },
  {
    label: t("cards-with-funds"),
    isRight: true
  },
  {
    label: t("cards-used-for-purchases"),
    isRight: true
  },
  {
    label: t("merchants-with-purchases"),
    isRight: true
  },
  {
    label: t("total-funds-loaded"),
    isRight: true
  },
  {
    label: t("total-purchase-value"),
    isRight: true
  },
  {
    label: t("total-expired-amount"),
    isRight: true
  }
]);

const footers = computed(() => {
  const footers = [];

  footers.push({ value: t("subscription-totals") });
  footers.push({ value: "" }); // Empty cell subscription-name
  footers.push({ value: getSubscriptionPurchasesTotal(), isRight: true });
  footers.push({ value: getSubscriptionCardsWithFundsTotal(), isRight: true });
  footers.push({ value: getSubscriptionCardsUsedForPurchasesTotal(), isRight: true });
  footers.push({ value: getSubscriptionMerchantsWithPurchasesTotal(), isRight: true });
  footers.push({ value: getSubscriptionTotalFundsLoadedTotal(), isRight: true });
  footers.push({ value: getSubscriptionTotalPurchaseValueTotal(), isRight: true });
  footers.push({ value: getSubscriptionTotalExpiredAmountTotal(), isRight: true });

  return footers;
});

function getSubscriptionPurchasesTotal() {
  return props.organizations.reduce((total, organization) => total + organization.totalPurchases, 0);
}

function getSubscriptionCardsWithFundsTotal() {
  return props.organizations.reduce((total, organization) => total + organization.cardsWithFunds, 0);
}

function getSubscriptionCardsUsedForPurchasesTotal() {
  return props.organizations.reduce((total, organization) => total + organization.cardsUsedForPurchases, 0);
}

function getSubscriptionMerchantsWithPurchasesTotal() {
  return props.organizations.reduce((total, organization) => total + organization.merchantsWithPurchases, 0);
}

function getSubscriptionTotalFundsLoadedTotal() {
  return getMoneyFormat(props.organizations.reduce((total, organization) => total + organization.totalFundsLoaded, 0));
}

function getSubscriptionTotalPurchaseValueTotal() {
  return getMoneyFormat(props.organizations.reduce((total, organization) => total + organization.totalPurchaseValue, 0));
}

function getSubscriptionTotalExpiredAmountTotal() {
  return getMoneyFormat(props.organizations.reduce((total, organization) => total + organization.totalExpiredAmount, 0));
}
</script>
