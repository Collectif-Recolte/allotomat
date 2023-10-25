<i18n>
  {
    "en": {
      "add-merchant": "Add a merchant",
      "market-already-in-project": "The market is already part of the program",
      "no-associated-merchant": "No associated merchant",
      "title": "Merchants - {projectName}",
      "selected-market": "Selected market"
    },
    "fr": {
      "add-merchant": "Ajouter un commerce",
      "market-already-in-project": "Le commerce fait déjà parti du programme",
      "no-associated-merchant": "Aucun commerce associé",
      "title": "Commerces - {projectName}",
      "selected-market": "Commerce sélectionné"
    }
  }
</i18n>

<template>
  <UiDialogModal :return-route="{ name: URL_PROJECT_ADMIN }" :title="t('title', { projectName: getProjectName() })">
    <template v-if="project">
      <div v-if="project.markets.length === 0" class="text-red-500">
        <p class="text-sm">{{ t("no-associated-merchant") }}</p>
      </div>

      <MarketTable v-else :markets="project.markets" :url-name-market-delete="URL_REMOVE_MERCHANTS_FROM_PROJECT" />

      <UiSelectAndAdd
        v-if="filteredMarketOptions.length > 0"
        :show-select="addMerchantDisplayed"
        :select-label="t('selected-market')"
        :add-label="t('add-merchant')"
        :options="filteredMarketOptions"
        @showSelect="showAddMerchant"
        @submit="saveMerchant" />
      <RouterView />
    </template>
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { ref, computed } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute } from "vue-router";

import { URL_PROJECT_ADMIN, URL_REMOVE_MERCHANTS_FROM_PROJECT } from "@/lib/consts/urls";
import { useGraphQLErrorMessages } from "@/lib/helpers/error-handler";

import MarketTable from "@/components/market/market-table";

useGraphQLErrorMessages({
  MARKET_ALREADY_IN_PROJECT: () => {
    return t("market-already-in-project");
  }
});

const { t } = useI18n();
const route = useRoute();

const addMerchantDisplayed = ref(false);

const { result: resultProject, refetch } = useQuery(
  gql`
    query Project($id: ID!) {
      project(id: $id) {
        id
        name
        markets {
          id
          name
        }
      }
    }
  `,
  {
    id: route.params.projectId
  }
);
const project = useResult(resultProject);

const { result: resultMarkets } = useQuery(
  gql`
    query Markets {
      markets {
        id
        name
      }
    }
  `
);

const marketOptions = useResult(resultMarkets, null, (data) => {
  return data.markets.map((x) => ({ label: x.name, value: x.id }));
});

const { mutate: addMarketToProject } = useMutation(
  gql`
    mutation AddMarketToProject($input: AddMarketToProjectInput!) {
      addMarketToProject(input: $input) {
        project {
          id
          name
          markets {
            id
            name
          }
        }
      }
    }
  `
);

const filteredMarketOptions = computed(() => {
  if (!marketOptions.value) return [];
  return marketOptions.value.filter((x) => !project.value.markets.some((y) => y.id === x.value));
});

function getProjectName() {
  return project.value ? project.value.name : "";
}

function showAddMerchant() {
  addMerchantDisplayed.value = true;
}

async function saveMerchant(market) {
  await addMarketToProject({
    input: {
      projectId: route.params.projectId,
      marketId: market
    }
  });

  refetch();

  addMerchantDisplayed.value = false;
}
</script>
