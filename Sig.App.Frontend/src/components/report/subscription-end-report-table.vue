<i18n>
  {
    "en": {
      "organization-name": "Group",
      "subscription-name": "Subscription",
      "total-purchases": "Total purchases",
      "cards-with-funds": "Cards with funds",
      "cards-used-for-purchases": "Cards used for purchases",
      "merchants-with-purchases": "Merchants with purchases",
      "total-funds-loaded": "Total funds loaded",
      "total-purchase-value": "Total purchase value",
      "total-expired-amount": "Total expired amount"
    },
    "fr": {
      "organization-name": "Groupe",
      "subscription-name": "Abonnement",
      "total-purchases": "Total des achats",
      "cards-with-funds": "Cartes avec fonds",
      "cards-used-for-purchases": "Cartes utilisées pour les achats",
      "merchants-with-purchases": "Marchands avec achats",
      "total-funds-loaded": "Total des fonds chargés",
      "total-purchase-value": "Valeur totale des achats",
      "total-expired-amount": "Montant total expiré"
    }
  }
  </i18n>

<template>
  <UiTable :items="props.organizations" :cols="cols">
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
</script>
