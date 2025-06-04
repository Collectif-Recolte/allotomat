<i18n>
  {
    "en": {
      "add-market": "Add Market",
      "title": "Markets",
      "empty-list": "No markets are linked to this program.",
      "market-count": "{count} market | {count} markets",
      "search-placeholder": "Search by market name, email address, person name",
      "reset-search": "Reset search",
      "market-groups": "Market Groups"
    },
    "fr": {
      "add-market": "Ajouter un commerce",
      "title": "Commerces",
      "empty-list": "Aucun commerce n'est lié à ce programme.",
      "market-count": "{count} commerce | {count} commerces",
      "search-placeholder": "Chercher par nom du commerce, addresse courriel, nom de la personne",
      "reset-search": "Réinitialiser la recherche",
      "market-groups": "Groupes de commerce"
    }
  }
</i18n>

<template>
  <RouterView v-slot="{ Component }">
    <AppShell :title="t('title')" :loading="loading || loadingMarketGroups">
      <div v-if="markets">
        <UiTableHeader :title="t('market-count', { count: markets.length })">
          <template #right>
            <div class="flex items-center gap-x-4">
              <UiFilter
                v-model="searchInput"
                has-search
                :has-filters="userType === USER_TYPE_PROJECTMANAGER"
                :has-active-filters="hasActiveFilters"
                :active-filters-count="activeFiltersCount"
                :placeholder="t('search-placeholder')"
                @resetFilters="resetSearch"
                @search="onSearch">
                <PfFormInputCheckboxGroup
                  v-if="availableMarketGroups.length > 0"
                  id="market-groups"
                  is-filter
                  :value="marketGroups"
                  :label="t('market-groups')"
                  :options="availableMarketGroups"
                  @input="onMarketGroupsChecked" />
              </UiFilter>
              <PfButtonLink
                v-if="userType === USER_TYPE_PROJECTMANAGER"
                tag="RouterLink"
                :to="addMarketRoute"
                :label="t('add-market')" />
            </div>
          </template>
        </UiTableHeader>
        <MarketTable
          v-if="markets.length > 0"
          can-edit
          :markets="markets"
          :url-name-market-archive="URL_MARKET_OVERVIEW_ARCHIVE"
          :url-name-market-edit="URL_MARKET_OVERVIEW_EDIT"
          :url-name-market-manage-managers="URL_MARKET_OVERVIEW_MANAGE_MANAGERS" />
        <UiEmptyPage v-else-if="!loading">
          <UiCta
            :description="t('empty-list')"
            :primary-btn-label="t('reset-search')"
            primary-btn-is-action
            @onPrimaryBtnClick="resetSearch"></UiCta>
        </UiEmptyPage>
        <UiPagination
          v-if="marketsPagination && marketsPagination.totalPages > 1"
          v-model:page="page"
          :total-pages="marketsPagination.totalPages">
        </UiPagination>
      </div>
      <Component :is="Component" />
    </AppShell>
  </RouterView>
</template>

<script setup>
import gql from "graphql-tag";
import { computed, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useQuery, useResult } from "@vue/apollo-composable";
import { onBeforeRouteUpdate } from "vue-router";
import { storeToRefs } from "pinia";

import { useAuthStore } from "@/lib/store/auth";
import { usePageTitle } from "@/lib/helpers/page-title";
import {
  URL_MARKET_OVERVIEW,
  URL_MARKET_OVERVIEW_SELECT,
  URL_MARKET_OVERVIEW_ARCHIVE,
  URL_MARKET_OVERVIEW_EDIT,
  URL_MARKET_OVERVIEW_MANAGE_MANAGERS
} from "@/lib/consts/urls";

import { USER_TYPE_PROJECTMANAGER, USER_TYPE_MARKETGROUPMANAGER } from "@/lib/consts/enums";

import MarketTable from "@/components/market/market-table";

const { t } = useI18n();
const { userType } = storeToRefs(useAuthStore());

usePageTitle(t("title"));

const page = ref(1);
const searchInput = ref("");
const searchText = ref("");
const marketGroups = ref([]);

const {
  result: resultMarketGroups,
  loading: loadingMarketGroups,
  refetch: refetchMarketGroups
} = useQuery(
  gql`
    query MarketGroups($page: Int!, $searchText: String) {
      marketGroups {
        id
        marketsSearch(page: $page, limit: 30, searchText: $searchText) {
          pageNumber
          pageSize
          totalCount
          totalPages
          items {
            id
            name
            isArchived
            isDisabled
          }
        }
      }
    }
  `,
  marketsVariables,
  () => ({
    enabled: userType.value === USER_TYPE_MARKETGROUPMANAGER
  })
);

const {
  result: resultProjects,
  loading,
  refetch
} = useQuery(
  gql`
    query Projects($page: Int!, $searchText: String, $marketGroups: [ID!]) {
      projects {
        id
        marketsSearch(page: $page, limit: 30, searchText: $searchText, marketGroups: $marketGroups) {
          pageNumber
          pageSize
          totalCount
          totalPages
          items {
            id
            name
            isArchived
            isDisabled
          }
        }
        marketGroups {
          id
          name
        }
      }
    }
  `,
  marketsVariables,
  () => ({
    enabled: userType.value === USER_TYPE_PROJECTMANAGER
  })
);

function marketsVariables() {
  return {
    page: page.value,
    searchText: searchText.value,
    marketGroups: marketGroups.value
  };
}

const marketGroup = useResult(resultMarketGroups, null, (data) => data.marketGroups[0]);
const project = useResult(resultProjects, null, (data) => data.projects[0]);
const availableMarketGroups = useResult(resultProjects, [], (data) =>
  data.projects[0]?.marketGroups.map((x) => ({ label: x.name, value: x.id }))
);

const addMarketRoute = { name: URL_MARKET_OVERVIEW_SELECT };

const markets = computed(() => {
  if (userType.value === USER_TYPE_MARKETGROUPMANAGER) {
    return marketGroup.value?.marketsSearch.items;
  } else {
    return project.value?.marketsSearch.items;
  }
});
const marketsPagination = computed(() => {
  if (userType.value === USER_TYPE_MARKETGROUPMANAGER) {
    return marketGroup.value?.marketsSearch;
  } else {
    return project.value?.marketsSearch;
  }
});

function onSearch() {
  page.value = 1;
  searchText.value = searchInput.value;
}

function resetSearch() {
  page.value = 1;
  searchText.value = "";
  searchInput.value = "";
  marketGroups.value = [];
}

function onMarketGroupsChecked(input) {
  if (input.isChecked) {
    marketGroups.value.push(input.value);
  } else {
    marketGroups.value = marketGroups.value.filter((x) => x !== input.value);
  }
}

const hasActiveFilters = computed(() => {
  return marketGroups.value.length > 0 || searchText.value !== "";
});

const activeFiltersCount = computed(() => {
  return marketGroups.value.length;
});

onBeforeRouteUpdate((to) => {
  if (to.name === URL_MARKET_OVERVIEW) {
    refetch();
    refetchMarketGroups();
  }
});
</script>
