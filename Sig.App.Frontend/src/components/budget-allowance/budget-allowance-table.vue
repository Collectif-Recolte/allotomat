<i18n>
  {
      "en": {
          "organization-name": "Group name",
					"budget-allowance-original-fund": "Original fund",
					"budget-allowance-available-fund": "Available fund",
      },
      "fr": {
          "organization-name": "Nom du groupe",
					"budget-allowance-original-fund": "Fonds original",
					"budget-allowance-available-fund": "Fonds disponible"
      }
  }
</i18n>

<template>
  <UiTable v-if="props.budgetAllowances" :items="props.budgetAllowances" :cols="cols">
    <template #default="slotProps">
      <td class="py-3">
        {{ slotProps.item.organization.name }}
      </td>
      <td class="py-3">
        {{ getMoneyFormat(slotProps.item.originalFund) }}
      </td>
      <td class="py-3">
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
    { label: t("organization-name") },
    { label: t("budget-allowance-original-fund") },
    { label: t("budget-allowance-available-fund") }
  ];
});
</script>
