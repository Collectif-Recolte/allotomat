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
  <div class="flex flex-col relative mb-6">
    <BeneficiaryWithDetail
      v-for="beneficiary in props.beneficiariesPagination.items"
      :key="beneficiary.id"
      :beneficiary="beneficiary"
      :beneficiaries-are-anonymous="props.beneficiariesAreAnonymous" />
    <div
      v-if="!administrationSubscriptionsOffPlatform"
      class="sticky bottom-4 ml-auto before:block before:absolute before:pointer-events-none before:w-[calc(100%+50px)] before:h-[calc(100%+50px)] before:-translate-y-1/2 before:right-0 before:top-1/2 before:bg-gradient-radial before:bg-white/70 before:blur-lg before:rounded-full">
      <PfButtonLink
        tag="routerLink"
        :to="{ name: URL_BENEFICIARY_ASSIGN_SUBSCRIPTIONS, query: props.filteredQuery }"
        btn-style="secondary"
        class="rounded-full"
        :is-disabled="props.isAllGroupSelected">
        <span class="inline-flex items-center">
          {{ t("assign-subscription-btn") }}
          <span class="bg-primary-700 w-6 h-6 flex items-center justify-center rounded-full text-p3 leading-none ml-2 -mr-2">{{
            props.beneficiariesPagination.totalCount
          }}</span>
        </span>
      </PfButtonLink>
    </div>
  </div>
</template>

<script setup>
import { defineProps } from "vue";
import { useI18n } from "vue-i18n";

import { URL_BENEFICIARY_ASSIGN_SUBSCRIPTIONS } from "@/lib/consts/urls";

import BeneficiaryWithDetail from "@/components/beneficiaries/beneficiary-with-detail";

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
  },
  isAllGroupSelected: {
    type: Boolean,
    default: false
  }
});
</script>
