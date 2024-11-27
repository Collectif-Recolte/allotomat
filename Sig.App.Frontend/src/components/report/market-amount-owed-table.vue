<i18n>
  {
    "en": {
      "market-name": "Name",
      "market-amount-owed": "Amount owed",
      "cash-register-name": "Cash register"
    },
    "fr": {
      "market-name": "Nom",
      "market-amount-owed": "Montant dû",
      "cash-register-name": "Caisse"
    }
  }
  </i18n>

<template>
  <UiTable :items="props.markets" :cols="cols">
    <template #default="slotProps">
      <td>
        <div class="inline-flex items-center">
          {{ getMarketName(slotProps.item) }}
        </div>
      </td>
      <td>
        <div class="inline-flex items-center">
          {{ getCashRegisterName(slotProps.item) }}
        </div>
      </td>
      <td>
        <div class="inline-flex items-center">
          {{ getAmountOwed(slotProps.item) }}
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
  markets: { type: Array, required: true }
});

const cols = computed(() => [
  { label: t("market-name") },
  { label: t("cash-register-name") },
  {
    label: t("market-amount-owed"),
    isRight: true
  }
]);

function getMarketName(item) {
  return `${item.market.name}`;
}

function getCashRegisterName(item) {
  return `${item.cashRegister.name}`;
}

function getAmountOwed(item) {
  return getMoneyFormat(item.amount);
}
</script>
