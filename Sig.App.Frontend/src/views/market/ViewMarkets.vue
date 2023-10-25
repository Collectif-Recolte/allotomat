<i18n>
  {
    "en": {
      "title": "Markets",
      "empty-list": "No markets are linked to this program.",
      "market-count": "{count} market | {count} markets"
    },
    "fr": {
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
        <UiTableHeader :title="t('market-count', { count: markets.length })" />
        <MarketTable :markets="markets" />
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

import { usePageTitle } from "@/lib/helpers/page-title";

import MarketTable from "@/components/market/market-table";

const { t } = useI18n();

usePageTitle(t("title"));

const { result: resultProjects, loading } = useQuery(
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

const markets = computed(() => project.value?.markets);
</script>
