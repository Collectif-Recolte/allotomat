<i18n>
  {
    "en": {
      "add-market": "Add",
      "title": "Market management",
      "empty-list": "No markets have been created yet.",
      "create-market": "Create a market",
      "market-count": "{count} market | {count} markets",
      "search-placeholder": "Search by market name, email address, person name"
    },
    "fr": {
      "add-market": "Ajouter",
      "title": "Gestion des commerces",
      "empty-list": "Aucun commerce n'a encore été créé.",
      "create-market": "Créer un commerce",
      "market-count": "{count} commerce | {count} commerces",
      "search-placeholder": "Chercher par nom du commerce, addresse courriel, nom de la personne"
    }
  }
</i18n>

<template>
  <RouterView v-slot="{ Component }">
    <AppShell :title="t('title')" :loading="loading">
      <div v-if="marketsPagination?.items">
        <template v-if="marketsPagination?.items.length > 0">
          <UiTableHeader :title="t('market-count', { count: marketsPagination.items.length })">
            <template #right>
              <div class="flex items-center gap-x-4">
                <UiFilter
                  v-model="searchInput"
                  has-search
                  :placeholder="t('search-placeholder')"
                  @resetFilters="resetSearch"
                  @search="onSearch">
                </UiFilter>
                <PfButtonLink tag="RouterLink" :to="addMarketRoute" :label="t('add-market')" />
              </div>
            </template>
          </UiTableHeader>
          <MarketTable
            can-edit
            :markets="marketsPagination.items"
            :url-name-market-archive="URL_MARKET_ARCHIVE"
            :url-name-market-delete="URL_MARKET_DELETE"
            :url-name-market-edit="URL_MARKET_EDIT"
            :url-name-market-manage-managers="URL_MARKET_MANAGE_MANAGERS" />
          <UiPagination
            v-if="marketsPagination && marketsPagination.totalPages > 1"
            v-model:page="page"
            :total-pages="marketsPagination.totalPages">
          </UiPagination>
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
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import { onBeforeRouteUpdate } from "vue-router";
import { useQuery, useResult } from "@vue/apollo-composable";

import {
  URL_MARKET_ADD,
  URL_MARKET_ADMIN,
  URL_MARKET_ARCHIVE,
  URL_MARKET_DELETE,
  URL_MARKET_EDIT,
  URL_MARKET_MANAGE_MANAGERS
} from "@/lib/consts/urls";
import { usePageTitle } from "@/lib/helpers/page-title";

import MarketTable from "@/components/market/market-table";

const { t } = useI18n();

const page = ref(1);
const searchInput = ref("");
const searchText = ref("");

usePageTitle(t("title"));

const { result, loading, refetch } = useQuery(
  gql`
    query AllMarketsSearch($page: Int!, $searchText: String!) {
      allMarketsSearch(page: $page, limit: 30, searchText: $searchText) {
        pageNumber
        pageSize
        totalCount
        totalPages
        items {
          id
          name
          isArchived
        }
      }
    }
  `,
  marketsVariables
);

function marketsVariables() {
  return {
    page: page.value,
    searchText: searchText.value
  };
}

const marketsPagination = useResult(result, null, (data) => data.allMarketsSearch);

const addMarketRoute = { name: URL_MARKET_ADD };

function onSearch() {
  page.value = 1;
  searchText.value = searchInput.value;
}

function resetSearch() {
  page.value = 1;
  searchText.value = "";
  searchInput.value = "";
}

onBeforeRouteUpdate((to) => {
  if (to.name === URL_MARKET_ADMIN) {
    refetch();
  }
});
</script>
