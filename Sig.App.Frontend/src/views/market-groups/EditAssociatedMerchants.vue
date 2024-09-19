<i18n>
  {
    "en": {
      "add-merchant": "Add a merchant",
      "market-already-in-market-group": "The market is already part of the market group",
      "no-associated-merchant": "No associated merchant",
      "title": "Merchants - {marketGroupName}",
      "selected-market": "Selected market"
    },
    "fr": {
      "add-merchant": "Ajouter un commerce",
      "market-already-in-market-group": "Le commerce fait déjà parti du groupe de commerce",
      "no-associated-merchant": "Aucun commerce associé",
      "title": "Commerces - {marketGroupName}",
      "selected-market": "Commerce sélectionné"
    }
  }
</i18n>

<template>
  <UiDialogModal
    :return-route="{ name: URL_MARKET_GROUPS_OVERVIEW }"
    :title="t('title', { marketGroupName: getMarketGroupName() })">
    <template v-if="marketGroup">
      <div v-if="marketGroup.markets.length === 0" class="text-red-500">
        <p class="text-sm">{{ t("no-associated-merchant") }}</p>
      </div>

      <MarketTable v-else :markets="marketGroup.markets" :url-name-market-delete="URL_REMOVE_MERCHANTS_FROM_MARKET_GROUP" />

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

import { URL_MARKET_GROUPS_OVERVIEW, URL_REMOVE_MERCHANTS_FROM_MARKET_GROUP } from "@/lib/consts/urls";
import { useGraphQLErrorMessages } from "@/lib/helpers/error-handler";

import MarketTable from "@/components/market/market-table";

useGraphQLErrorMessages({
  MARKET_ALREADY_IN_MARKET_GROUP: () => {
    return t("market-already-in-market-group");
  }
});

const { t } = useI18n();
const route = useRoute();

const addMerchantDisplayed = ref(false);

const { result: resultMarketGroup, refetch } = useQuery(
  gql`
    query MarketGroup($id: ID!) {
      marketGroup(id: $id) {
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
    id: route.params.marketGroupId
  }
);
const marketGroup = useResult(resultMarketGroup);

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

const { mutate: addMarketToMarketGroup } = useMutation(
  gql`
    mutation AddMarketToMarketGroup($input: AddMarketToMarketGroupInput!) {
      addMarketToMarketGroup(input: $input) {
        marketGroup {
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
  return marketOptions.value.filter((x) => !marketGroup.value.markets.some((y) => y.id === x.value));
});

function getMarketGroupName() {
  return marketGroup.value ? marketGroup.value.name : "";
}

function showAddMerchant() {
  addMerchantDisplayed.value = true;
}

async function saveMerchant(market) {
  await addMarketToMarketGroup({
    input: {
      marketGroupId: route.params.marketGroupId,
      marketId: market
    }
  });

  refetch();

  addMerchantDisplayed.value = false;
}
</script>
