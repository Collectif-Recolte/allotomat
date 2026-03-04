<i18n>
  {
    "en": {
      "add-merchant": "Add a merchant",
      "market-already-in-organization": "The market is already part of the group",
      "no-associated-merchant": "No associated merchant",
      "title": "Merchants - {organizationName}",
      "selected-market": "Selected market"
    },
    "fr": {
      "add-merchant": "Ajouter un commerce",
      "market-already-in-organization": "Le commerce fait déjà parti du groupe",
      "no-associated-merchant": "Aucun commerce associé",
      "title": "Commerces - {organizationName}",
      "selected-market": "Commerce sélectionné"
    }
  }
</i18n>

<template>
  <UiDialogModal
    :return-route="{ name: URL_ORGANIZATION_ADMIN }"
    :title="t('title', { organizationName: getOrganizationName() })">
    <template v-if="organization">
      <div v-if="organization.markets.length === 0" class="text-red-500">
        <p class="text-sm">{{ t("no-associated-merchant") }}</p>
      </div>

      <MarketTable
        v-else
        :markets="organization.markets"
        can-delete
        :url-name-market-delete="URL_REMOVE_MERCHANTS_FROM_ORGANIZATION"
        order-by-market-name />

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

import { URL_ORGANIZATION_ADMIN, URL_REMOVE_MERCHANTS_FROM_ORGANIZATION } from "@/lib/consts/urls";
import { useGraphQLErrorMessages } from "@/lib/helpers/error-handler";

import MarketTable from "@/components/market/market-table";

useGraphQLErrorMessages({
  MARKET_ALREADY_IN_ORGANIZATION: () => {
    return t("market-already-in-organization");
  }
});

const { t } = useI18n();
const route = useRoute();

const addMerchantDisplayed = ref(false);

const { result: resultOrganization, refetch } = useQuery(
  gql`
    query Organization($id: ID!) {
      organization(id: $id) {
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
    id: route.params.organizationId
  }
);
const organization = useResult(resultOrganization);

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
  return data.markets.map((x) => ({ label: x.name, value: x.id })).sort((a, b) => a.label.localeCompare(b.label));
});

const { mutate: addMarketToOrganization } = useMutation(
  gql`
    mutation AddMarketToOrganization($input: AddMarketToOrganizationInput!) {
      addMarketToOrganization(input: $input) {
        organization {
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
  return marketOptions.value.filter((x) => !organization.value.markets.some((y) => y.id === x.value));
});

function getOrganizationName() {
  return organization.value ? organization.value.name : "";
}

function showAddMerchant() {
  addMerchantDisplayed.value = true;
}

async function saveMerchant(market) {
  await addMarketToOrganization({
    input: {
      organizationId: route.params.organizationId,
      marketId: market
    }
  });

  refetch();

  addMerchantDisplayed.value = false;
}
</script>
