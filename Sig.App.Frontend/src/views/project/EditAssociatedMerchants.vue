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
      <div class="flex flex-col gap-y-6">
        <div v-if="project.markets.length === 0" class="text-red-500">
          <p class="text-sm">{{ t("no-associated-merchant") }}</p>
        </div>

        <MarketTable v-else :markets="project.markets" :url-name-market-delete="URL_REMOVE_MERCHANTS_FROM_PROJECT" can-delete order-by-market-name />

        <PfButtonAction btn-style="dash" has-icon-left type="button" :label="t('add-merchant')" @click="showAddMerchant" />
        <RouterView />
      </div>
    </template>
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";

import { URL_PROJECT_ADMIN, URL_ADD_MERCHANTS_FROM_PROJECT, URL_REMOVE_MERCHANTS_FROM_PROJECT } from "@/lib/consts/urls";
import { useGraphQLErrorMessages } from "@/lib/helpers/error-handler";

import MarketTable from "@/components/market/market-table";

useGraphQLErrorMessages({
  MARKET_ALREADY_IN_PROJECT: () => {
    return t("market-already-in-project");
  }
});

const { t } = useI18n();
const route = useRoute();
const router = useRouter();

const { result: resultProject } = useQuery(
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

function getProjectName() {
  return project.value ? project.value.name : "";
}

function showAddMerchant() {
  router.push({ name: URL_ADD_MERCHANTS_FROM_PROJECT });
}
</script>
