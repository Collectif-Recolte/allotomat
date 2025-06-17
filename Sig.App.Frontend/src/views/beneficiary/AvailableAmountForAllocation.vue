<i18n>
{
	"en": {
		"title": "Details of budget allowances"
	},
	"fr": {
		"title": "Détails des enveloppes"
	}
}
</i18n>

<template>
  <UiDialogModal :return-route="{ name: URL_BENEFICIARY_ADMIN }" :title="t('title')">
    <BudgetAllowanceTable :budget-allowances="budgetAllowances" />
  </UiDialogModal>
</template>

<script setup>
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { useQuery, useResult } from "@vue/apollo-composable";
import gql from "graphql-tag";

import { URL_BENEFICIARY_ADMIN } from "@/lib/consts/urls";

import { useOrganizationStore } from "@/lib/store/organization";

import UiDialogModal from "@/components/ui/dialog/modal.vue";
import BudgetAllowanceTable from "@/components/budget-allowance/budget-allowance-table.vue";

const { t } = useI18n();
const { currentOrganization } = useOrganizationStore();

const { result } = useQuery(
  gql`
    query Organization($id: ID!) {
      organization(id: $id) {
        id
        budgetAllowances {
          id
          availableFund
          originalFund
          organization {
            id
            name
          }
        }
      }
    }
  `,
  {
    id: currentOrganization
  }
);
const organization = useResult(result);

const budgetAllowances = computed(() => {
  if (!organization.value) return [];
  return organization.value.budgetAllowances;
});
</script>
