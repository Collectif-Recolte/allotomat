<i18n>
  {
    "en": {
      "add-subscription": "Add",
      "title": "Subscription management",
      "subscription-count": "{count} subscription | {count} subscriptions",
      "empty-list": "There is no subscription for this program",
      "create-subscription": "Create a subscription"
    },
    "fr": {
      "add-subscription": "Ajouter",
      "title": "Gestion des abonnements",
      "subscription-count": "{count} abonnement | {count} abonnements",
      "empty-list": "Il n'existe aucun abonnement pour ce programme.",
      "create-subscription": "Créer un abonnement"
    }
  }
</i18n>

<template>
  <RouterView v-slot="{ Component }">
    <AppShell :title="t('title')" :loading="loading">
      <div v-if="projects && projects.length > 0">
        <template v-if="showSubscriptionList">
          <UiTableHeader :title="subscriptionCount" :cta-label="t('add-subscription')" :cta-route="addSubscriptionRoute" />
          <SubscriptionTable
            can-edit
            show-subscription-period
            show-budget-allowance-total
            :subscriptions="subscriptionsOrderByDate" />
        </template>

        <UiEmptyPage v-else>
          <UiCta
            :img-src="require('@/assets/img/abonnements.jpg')"
            :description="t('empty-list')"
            :primary-btn-label="t('create-subscription')"
            :primary-btn-route="addSubscriptionRoute" />
        </UiEmptyPage>
      </div>

      <Component :is="Component" />
    </AppShell>
  </RouterView>
</template>

<script setup>
import { computed } from "vue";
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { onBeforeRouteUpdate } from "vue-router";
import { useQuery, useResult } from "@vue/apollo-composable";

import { usePageTitle } from "@/lib/helpers/page-title";

import { URL_SUBSCRIPTION_ADD, URL_SUBSCRIPTION_ADMIN } from "@/lib/consts/urls";

import SubscriptionTable from "@/components/subscription/subscription-table";

const { t } = useI18n();

usePageTitle(t("title"));

const { result, loading, refetch } = useQuery(
  gql`
    query Projects {
      projects {
        id
        subscriptions {
          id
          name
          startDate
          endDate
          fundsExpirationDate
          isFundsAccumulable
          isArchived
          budgetAllowances {
            id
            originalFund
          }
        }
      }
    }
  `
);
const projects = useResult(result);

const showSubscriptionList = computed(() => projects.value[0].subscriptions.length > 0);

const addSubscriptionRoute = computed(() => {
  return { name: URL_SUBSCRIPTION_ADD, query: { projectId: projects.value[0].id } };
});

const subscriptionCount = computed(() => t("subscription-count", { count: projects.value[0].subscriptions.length }));

const subscriptionsOrderByDate = computed(() => {
  if (projects.value.length === 0) {
    return [];
  }
  let subscriptions = [...projects.value[0].subscriptions];

  return subscriptions.sort((a, b) => {
    if (a.isArchived && !b.isArchived) return 1;
    if (!a.isArchived && b.isArchived) return -1;
    const dateA = a.isFundsAccumulable ? new Date(a.fundsExpirationDate) : new Date(a.endDate);
    const dateB = b.isFundsAccumulable ? new Date(b.fundsExpirationDate) : new Date(b.endDate);
    return dateB - dateA;
  });
});

onBeforeRouteUpdate((to) => {
  if (to.name === URL_SUBSCRIPTION_ADMIN) {
    refetch();
  }
});
</script>
