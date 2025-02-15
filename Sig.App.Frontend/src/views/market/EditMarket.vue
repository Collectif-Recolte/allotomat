<i18n>
{
	"en": {
		"edit-market": "Edit",
		"edit-market-success-notification": "Editing market {marketName} was successful.",
		"title": "Edit a market"
	},
	"fr": {
		"edit-market": "Modifier",
		"edit-market-success-notification": "L’édition du commerce {marketName} a été un succès.",
		"title": "Modifier un commerce"
	}
}
</i18n>

<template>
  <UiDialogModal v-slot="{ closeModal }" :return-route="returnRoute()" :title="t('title')" :has-footer="false">
    <MarketForm
      v-if="market"
      :submit-btn="t('edit-market')"
      :market-name="market.name"
      @closeModal="closeModal"
      @submit="onSubmit" />
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { useRouter, useRoute } from "vue-router";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_MARKET_ADMIN, URL_MARKET_OVERVIEW_EDIT, URL_MARKET_OVERVIEW } from "@/lib/consts/urls";

import MarketForm from "@/views/market/_Form.vue";

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
const { addSuccess } = useNotificationsStore();

const { result } = useQuery(
  gql`
    query Market($id: ID!) {
      market(id: $id) {
        id
        name
      }
    }
  `,
  {
    id: route.params.marketId
  }
);
let market = useResult(result);

const { mutate: editMarket } = useMutation(
  gql`
    mutation EditMarket($input: EditMarketInput!) {
      editMarket(input: $input) {
        market {
          id
          name
        }
      }
    }
  `
);

async function onSubmit(values) {
  let input = {
    marketId: route.params.marketId,
    name: { value: values.marketName }
  };

  await editMarket({ input });
  router.push(returnRoute());
  addSuccess(t("edit-market-success-notification", { marketName: values.marketName }));
}

function returnRoute() {
  if (route.name === URL_MARKET_OVERVIEW_EDIT) return { name: URL_MARKET_OVERVIEW };
  else return { name: URL_MARKET_ADMIN };
}
</script>
