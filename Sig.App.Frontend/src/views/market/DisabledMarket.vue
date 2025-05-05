<i18n>
  {
    "en": {
      "disabled-market-success-notification": "The market {marketName} has been successfully disabled.",
      "disabled-text-error": "The text must match the name of the market",
      "disabled-text-label": "Type the name of the market to confirm",
      "description": "Warning ! If you continue, the market will be disabled and the market will not be able to process transactions.",
      "title": "Disabled - {marketName}",
      "disabled-btn-label": "Disabled"
    },
    "fr": {
      "disabled-market-success-notification": "Le commerce {marketName} a été désactivé avec succès.",
      "disabled-text-error": "Le texte doit correspondre au nom du commerce",
      "disabled-text-label": "Taper le nom du commerce pour confirmer",
      "description": "Avertissement ! Si vous continuez, le commerce sera désactivé et le commerce ne pourra pas traiter les transactions.",
      "title": "Désactiver - {marketName}",
      "disabled-btn-label": "Désactiver"
    }
  }
  </i18n>

<template>
  <UiDialogDeleteModal
    :return-route="returnRoute()"
    :title="t('title', { marketName: getMarketName() })"
    :description="t('description', { marketName: getMarketName() })"
    :validation-text="getMarketName()"
    :delete-text-label="t('disabled-text-label')"
    :delete-text-error="t('disabled-text-error')"
    :delete-button-label="t('disabled-btn-label')"
    @onDelete="disabledMarket" />
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_MARKET_ADMIN, URL_MARKET_OVERVIEW_DISABLED, URL_MARKET_OVERVIEW } from "@/lib/consts/urls";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
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
const market = useResult(result);

const { mutate: disabledMarketMutation } = useMutation(
  gql`
    mutation DisabledMarket($input: DisabledMarketInput!) {
      disabledMarket(input: $input) {
        market {
          id
          isDisabled
        }
      }
    }
  `
);

function getMarketName() {
  return market.value ? market.value.name : "";
}

async function disabledMarket() {
  await disabledMarketMutation({
    input: {
      marketId: route.params.marketId
    }
  });

  addSuccess(t("disabled-market-success-notification", { marketName: market.value.name }));
  router.push(returnRoute());
}

function returnRoute() {
  if (route.name === URL_MARKET_OVERVIEW_DISABLED) return { name: URL_MARKET_OVERVIEW };
  else return { name: URL_MARKET_ADMIN };
}
</script>
