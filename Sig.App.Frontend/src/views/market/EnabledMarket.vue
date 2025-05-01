<i18n>
{
	"en": {
		"enabled-market-success-notification": "The market {marketName} has been successfully enabled.",
		"enabled-text-error": "The text must match the name of the market",
		"enabled-text-label": "Type the name of the market to confirm",
		"description": "Warning ! If you continue, the market will be enabled and the market will be able to process transactions.",
		"title": "Enabled - {marketName}",
    "enabled-btn-label": "Enabled"
	},
	"fr": {
		"enabled-market-success-notification": "Le commerce {marketName} a été activé avec succès.",
		"enabled-text-error": "Le texte doit correspondre au nom du commerce",
		"enabled-text-label": "Taper le nom du commerce pour confirmer",
		"description": "Avertissement ! Si vous continuez, le commerce sera activé et le commerce pourra traiter les transactions.",
		"title": "Réactiver - {marketName}",
    "enabled-btn-label": "Réactiver"
	}
}
</i18n>

<template>
  <UiDialogDeleteModal
    :return-route="returnRoute()"
    :title="t('title', { marketName: getMarketName() })"
    :description="t('description', { marketName: getMarketName() })"
    :validation-text="getMarketName()"
    :delete-text-label="t('enabled-text-label')"
    :delete-text-error="t('enabled-text-error')"
    :delete-button-label="t('enabled-btn-label')"
    @onDelete="enabledMarket" />
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_MARKET_ADMIN, URL_MARKET_OVERVIEW_ENABLED, URL_MARKET_OVERVIEW } from "@/lib/consts/urls";

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

const { mutate: enabledMarketMutation } = useMutation(
  gql`
    mutation EanabledMarket($input: EnabledMarketInput!) {
      enabledMarket(input: $input) {
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

async function enabledMarket() {
  await enabledMarketMutation({
    input: {
      marketId: route.params.marketId
    }
  });

  addSuccess(t("enabled-market-success-notification", { marketName: market.value.name }));
  router.push(returnRoute());
}

function returnRoute() {
  if (route.name === URL_MARKET_OVERVIEW_ENABLED) return { name: URL_MARKET_OVERVIEW };
  else return { name: URL_MARKET_ADMIN };
}
</script>
