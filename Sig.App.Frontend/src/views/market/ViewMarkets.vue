<i18n>
  {
    "en": {
      "add-market": "Add Market",
      "title": "Markets",
      "empty-list": "No markets are linked to this program.",
      "market-count": "{count} market | {count} markets"
    },
    "fr": {
      "add-market": "Ajouter un commerce",
      "title": "Commerces",
      "empty-list": "Aucun commerce n'est lié à ce programme.",
      "market-count": "{count} commerce | {count} commerces"
    }
  }
</i18n>

<template>
  <RouterView v-slot="{ Component }">
    <AppShell :title="t('title')" :loading="loading">
      <div v-if="markets && markets.length > 0">
        <UiTableHeader
          :title="t('market-count', { count: markets.length })"
          :cta-label="t('add-market')"
          :cta-route="addMarketRoute" />
        <MarketTable
          can-edit
          :markets="markets"
          :url-name-market-archive="URL_MARKET_OVERVIEW_ARCHIVE"
          :url-name-market-edit="URL_MARKET_OVERVIEW_EDIT"
          :url-name-market-manage-managers="URL_MARKET_OVERVIEW_MANAGE_MANAGERS" />
      </div>
      <UiEmptyPage v-else-if="!loading">
        <UiCta :description="t('empty-list')" />
      </UiEmptyPage>

      <Component :is="Component" />
    </AppShell>
  </RouterView>
</template>

<script setup>
import gql from "graphql-tag";
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { useQuery, useResult } from "@vue/apollo-composable";
import { onBeforeRouteUpdate } from "vue-router";

import { usePageTitle } from "@/lib/helpers/page-title";
import {
  URL_MARKET_OVERVIEW,
  URL_MARKET_OVERVIEW_SELECT,
  URL_MARKET_OVERVIEW_ARCHIVE,
  URL_MARKET_OVERVIEW_EDIT,
  URL_MARKET_OVERVIEW_MANAGE_MANAGERS
} from "@/lib/consts/urls";

import MarketTable from "@/components/market/market-table";

const { t } = useI18n();

usePageTitle(t("title"));

const {
  result: resultProjects,
  loading,
  refetch
} = useQuery(
  gql`
    query Projects {
      projects {
        id
        markets {
          id
          name
        }
      }
    }
  `
);

const project = useResult(resultProjects, null, (data) => data.projects[0]);

const addMarketRoute = { name: URL_MARKET_OVERVIEW_SELECT };

const markets = computed(() => project.value?.markets);

onBeforeRouteUpdate((to) => {
  if (to.name === URL_MARKET_OVERVIEW) {
    refetch();
  }
});
</script>
