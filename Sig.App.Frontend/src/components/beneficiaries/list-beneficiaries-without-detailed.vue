<i18n>
  {
    "en": {
      "assign-subscription-btn": "Assign subscriptions",
    },
    "fr": {
      "assign-subscription-btn": "Attribuer des abonnements",
    }
  }
</i18n>

<template>
  <BeneficiaryTable
    v-if="!props.administrationSubscriptionsOffPlatform"
    :beneficiaries="props.beneficiariesPagination.items"
    :beneficiaries-are-anonymous="props.beneficiariesAreAnonymous"
    show-associated-card>
    <template #floatingActions>
      <PfButtonLink
        tag="routerLink"
        :to="{ name: URL_BENEFICIARY_ASSIGN_SUBSCRIPTIONS, query: props.filteredQuery }"
        btn-style="secondary"
        class="rounded-full">
        <span class="inline-flex items-center">
          {{ t("assign-subscription-btn") }}
          <span class="bg-primary-700 w-6 h-6 flex items-center justify-center rounded-full text-p3 leading-none ml-2 -mr-2">{{
            props.beneficiariesPagination.totalCount
          }}</span>
        </span>
      </PfButtonLink>
    </template>
  </BeneficiaryTable>
  <OffPlatformBeneficiaryTable
    v-else
    :beneficiaries="props.beneficiariesPagination.items"
    :beneficiaries-are-anonymous="props.beneficiariesAreAnonymous"
    :product-groups="props.productGroups"
    show-associated-card />
</template>

<script setup>
import { defineProps } from "vue";
import { useI18n } from "vue-i18n";

import { URL_BENEFICIARY_ASSIGN_SUBSCRIPTIONS } from "@/lib/consts/urls";

import BeneficiaryTable from "@/components/beneficiaries/beneficiary-table";
import OffPlatformBeneficiaryTable from "@/components/beneficiaries/off-platform-beneficiary-table";

const { t } = useI18n();

const props = defineProps({
  productGroups: {
    type: Array,
    required: true
  },
  administrationSubscriptionsOffPlatform: {
    type: Boolean,
    required: true
  },
  beneficiariesPagination: {
    type: Object,
    required: true
  },
  beneficiariesAreAnonymous: {
    type: Boolean,
    default: false
  },
  filteredQuery: {
    type: Object,
    required: true
  }
});
</script>
