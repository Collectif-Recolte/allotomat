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
      <div class="flex flex-col gap-y-6">
        <div v-if="marketGroup.markets.length === 0" class="text-red-500">
          <p class="text-sm">{{ t("no-associated-merchant") }}</p>
        </div>

        <MarketTable v-else :markets="marketGroup.markets" :url-name-market-delete="URL_REMOVE_MERCHANTS_FROM_MARKET_GROUP" />

        <PfButtonAction btn-style="dash" has-icon-left type="button" :label="t('add-merchant')" @click="showAddMerchant" />
      </div>
      <RouterView />
    </template>
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";

import {
  URL_MARKET_GROUPS_OVERVIEW,
  URL_REMOVE_MERCHANTS_FROM_MARKET_GROUP,
  URL_ADD_MERCHANTS_FROM_MARKET_GROUP
} from "@/lib/consts/urls";
import { useGraphQLErrorMessages } from "@/lib/helpers/error-handler";

import MarketTable from "@/components/market/market-table";

useGraphQLErrorMessages({
  MARKET_ALREADY_IN_MARKET_GROUP: () => {
    return t("market-already-in-market-group");
  }
});

const { t } = useI18n();
const route = useRoute();
const router = useRouter();

const { result: resultMarketGroup } = useQuery(
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

function getMarketGroupName() {
  return marketGroup.value ? marketGroup.value.name : "";
}

function showAddMerchant() {
  router.push({ name: URL_ADD_MERCHANTS_FROM_MARKET_GROUP });
}
</script>
