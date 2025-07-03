<i18n>
  {
      "en": {
          "subscription-name": "Subscription",
					"budget-allowance-original-fund": "Original fund",
					"budget-allowance-available-fund": "Available fund",
      },
      "fr": {
          "subscription-name": "Abonnement",
					"budget-allowance-original-fund": "Fonds original",
					"budget-allowance-available-fund": "Fonds disponible"
      }
  }
</i18n>

<template>
  <UiTable v-if="props.budgetAllowances" :items="props.budgetAllowances" :cols="cols">
    <template #default="slotProps">
      <td class="py-3">
        {{ slotProps.item.subscription.name }}
      </td>
      <td class="py-3 text-right">
        {{ getMoneyFormat(slotProps.item.originalFund) }}
      </td>
      <td class="py-3 text-right">
        {{ getMoneyFormat(slotProps.item.availableFund) }}
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
  budgetAllowances: { type: Object, default: null }
});

const cols = computed(() => {
  return [
    { label: t("subscription-name") },
    { label: t("budget-allowance-original-fund"), isRight: true },
    { label: t("budget-allowance-available-fund"), isRight: true }
  ];
});
</script>
