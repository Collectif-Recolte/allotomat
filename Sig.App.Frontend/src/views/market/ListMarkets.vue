<i18n>
  {
    "en": {
      "add-market": "Add",
      "title": "Market management",
      "empty-list": "No markets have been created yet.",
      "create-market": "Create a market",
      "market-count": "{count} market | {count} markets"
    },
    "fr": {
      "add-market": "Ajouter",
      "title": "Gestion des commerces",
      "empty-list": "Aucun commerce n'a encore été créé.",
      "create-market": "Créer un commerce",
      "market-count": "{count} commerce | {count} commerces"
    }
  }
</i18n>

<template>
  <RouterView v-slot="{ Component }">
    <AppShell :title="t('title')" :loading="loading">
      <div v-if="markets">
        <template v-if="markets.length > 0">
          <UiTableHeader
            :title="t('market-count', { count: markets.length })"
            :cta-label="t('add-market')"
            :cta-route="addMarketRoute" />
          <MarketTable
            can-edit
            :markets="markets"
            :url-name-market-archive="URL_MARKET_ARCHIVE"
            :url-name-market-edit="URL_MARKET_EDIT"
            :url-name-market-manage-managers="URL_MARKET_MANAGE_MANAGERS" />
        </template>

        <UiEmptyPage v-else>
          <UiCta :description="t('empty-list')" :primary-btn-label="t('create-market')" :primary-btn-route="addMarketRoute" />
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

import {
  URL_MARKET_ADD,
  URL_MARKET_ADMIN,
  URL_MARKET_ARCHIVE,
  URL_MARKET_EDIT,
  URL_MARKET_MANAGE_MANAGERS
} from "@/lib/consts/urls";
import { usePageTitle } from "@/lib/helpers/page-title";

import MarketTable from "@/components/market/market-table";

const { t } = useI18n();

usePageTitle(t("title"));

const { result, loading, refetch } = useQuery(
  gql`
    query Markets {
      markets {
        id
        name
      }
    }
  `
);
const markets = useResult(result);

const addMarketRoute = { name: URL_MARKET_ADD };

onBeforeRouteUpdate((to) => {
  if (to.name === URL_MARKET_ADMIN) {
    refetch();
  }
});
</script>
