<i18n>
  {
    "en": {
      "add-market-group": "Add",
      "title": "Manage Market Groups",
      "empty-list": "No market groups have been created yet.",
      "create-market-group": "Create a market group",
      "market-group-count": "{count} market group | {count} market groups"
    },
    "fr": {
      "add-market-group": "Ajouter",
      "title": "Gestion des groupes de commerces",
      "empty-list": "Aucun groupe de commerce n'a encore été créé.",
      "create-market-group": "Créer un groupe de commerces",
      "market-group-count": "{count} groupe de commerces | {count} groupes de commerces"
    }
  }
</i18n>

<template>
  <RouterView v-slot="{ Component }">
    <AppShell :title="t('title')" :loading="loading || projectsLoading">
      <div v-if="marketGroups && projects && projects.length > 0">
        <template v-if="marketGroups.length > 0">
          <UiTableHeader
            :title="t('market-group-count', { count: marketGroups.length })"
            :cta-label="t('add-market-group')"
            :cta-route="addMarketGroupRoute" />
          <MarketGroupTable can-edit :market-groups="marketGroups" :url-name-market-group-archive="URL_MARKET_GROUP_ARCHIVE" />
        </template>

        <UiEmptyPage v-else>
          <UiCta
            :description="t('empty-list')"
            :primary-btn-label="t('create-market-group')"
            :primary-btn-route="addMarketGroupRoute" />
        </UiEmptyPage>
      </div>

      <Component :is="Component" />
    </AppShell>
  </RouterView>
</template>

<script setup>
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { onBeforeRouteUpdate } from "vue-router";
import { useQuery, useResult } from "@vue/apollo-composable";
import { computed } from "vue";

import { URL_MARKET_GROUP_ADD, URL_MARKET_GROUPS_OVERVIEW, URL_MARKET_GROUP_ARCHIVE } from "@/lib/consts/urls";
import { usePageTitle } from "@/lib/helpers/page-title";

import MarketGroupTable from "@/components/market-groups/market-group-table";

const { t } = useI18n();

usePageTitle(t("title"));

const { result, loading, refetch } = useQuery(
  gql`
    query MarketGroups {
      marketGroups {
        id
        name
        isArchived
      }
    }
  `
);
const marketGroups = useResult(result);

const { result: resultProjects, loading: projectsLoading } = useQuery(
  gql`
    query Projects {
      projects {
        id
        name
      }
    }
  `
);
const projects = useResult(resultProjects);

const addMarketGroupRoute = computed(() => {
  return {
    name: URL_MARKET_GROUP_ADD,
    query: { projectId: projects.value[0].id }
  };
});

onBeforeRouteUpdate((to) => {
  if (to.name === URL_MARKET_GROUPS_OVERVIEW) {
    refetch();
  }
});
</script>
