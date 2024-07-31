<i18n>
  {
    "en": {
      "beneficiary-count-label": "Number of participants",
      "manage-beneficiary-btn": "Manage participants",
      "unspend-loyalty-fund-label": "Unspent amount on gift cards",
      "total-active-subscriptions-envelopes-label": "Budget allowance total (active subscriptions)",
      "organization-list-stats": "By group",
      "empty-list": "No group is associated with the program.",
      "subscription-filter": "Subscriptions"
    },
    "fr": {
      "beneficiary-count-label": "Nombre de participants",
      "manage-beneficiary-btn": "Gérer les participants",
      "unspend-loyalty-fund-label": "Montant non-dépensé sur les cartes-cadeaux",
      "total-active-subscriptions-envelopes-label": "Total des enveloppes (abonnements actifs)",
      "organization-list-stats": "Par groupe",
      "empty-list": "Aucun groupe n'est associé au programme.",
      "subscription-filter": "Abonnements"
    }
  }
  </i18n>

<template>
  <Loading :loading="loading" is-full-height>
    <div class="flex flex-col gap-6 sm:flex-row mt-4 mb-12">
      <UiStat
        class="sm:w-1/3"
        :stat="project?.projectStats.beneficiaryCount"
        :label="t('beneficiary-count-label')"
        :secondary-btn-route="{ name: URL_BENEFICIARY_ADMIN }"
        :secondary-btn-label="t('manage-beneficiary-btn')" />
      <UiStat
        class="sm:w-1/3"
        :stat="getMoneyFormat(project?.projectStats.unspentLoyaltyFund)"
        :label="t('unspend-loyalty-fund-label')" />
      <UiStat
        class="sm:w-1/3"
        :stat="getMoneyFormat(project?.projectStats.totalActiveSubscriptionsEnvelopes)"
        :label="t('total-active-subscriptions-envelopes-label')" />
    </div>
    <div v-if="organizationsStats">
      <UiTableHeader :title="t('organization-list-stats')">
        <template #right>
          <UiFilter
            has-filters
            :has-active-filters="hasActiveFilters"
            :active-filters-count="activeFiltersCount"
            @resetFilters="onResetFilters">
            <PfFormInputCheckboxGroup
              v-if="availableSubscriptions.length > 0"
              class="mt-3"
              is-filter
              :value="selectedSubscriptions"
              :label="t('subscription-filter')"
              :options="availableSubscriptions"
              @input="onSubscriptionsChecked" />
          </UiFilter>
        </template>
      </UiTableHeader>
      <template v-if="organizationsStats.length > 0">
        <OrganizationStatsTable :organizations-stats="organizationsStats" />
      </template>
      <div v-else class="flex items-center justify-center my-8 lg:my-16">
        <UiCta :img-src="require('@/assets/img/organismes.jpg')" :description="t('empty-list')" />
      </div>
    </div>
  </Loading>
</template>

<script setup>
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { useQuery } from "@vue/apollo-composable";
import { ref, computed, watch } from "vue";

import { URL_BENEFICIARY_ADMIN } from "@/lib/consts/urls";

import { getMoneyFormat } from "@/lib/helpers/money";

import Loading from "@/components/app/loading";
import OrganizationStatsTable from "@/components/dashboard/organization-stats-table.vue";

const { t } = useI18n();

const selectedSubscriptions = ref([]);

const organizationsStats = ref([]);
const project = ref(null);
const availableSubscriptions = ref([]);

const { result: resultProjects, loading } = useQuery(
  gql`
    query Projects($subscriptions: [ID!]) {
      projects {
        id
        subscriptions {
          id
          name
        }
        projectStats {
          beneficiaryCount
          unspentLoyaltyFund
          totalActiveSubscriptionsEnvelopes
        }
        organizationsStats(subscriptions: $subscriptions) {
          balanceOnCards
          cardSpendingAmounts
          expiredAmounts
          organization {
            id
            name
          }
          remainingPerEnvelope
          totalActiveSubscriptionsEnvelopes
          totalAllocatedOnCards
        }
      }
    }
  `,
  projectsStatsVariables
);

watch(resultProjects, (value) => {
  if (value === undefined) return;

  project.value = value.projects[0];
  organizationsStats.value = value.projects[0].organizationsStats;
  availableSubscriptions.value = value.projects[0].subscriptions.map((subscription) => {
    return {
      value: subscription.id,
      label: subscription.name
    };
  });
});

const hasActiveFilters = computed(() => {
  return selectedSubscriptions.value.length > 0;
});

const activeFiltersCount = computed(() => {
  return selectedSubscriptions.value.length;
});

function onResetFilters() {
  selectedSubscriptions.value = [];
}

function projectsStatsVariables() {
  return {
    subscriptions: selectedSubscriptions.value
  };
}

function onSubscriptionsChecked(input) {
  if (input.isChecked) {
    selectedSubscriptions.value.push(input.value);
  } else {
    selectedSubscriptions.value = selectedSubscriptions.value.filter((x) => x !== input.value);
  }
}
</script>
