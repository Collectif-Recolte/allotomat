<i18n>
{
	"en": {
		"delete-market-success-notification": "The market {marketName} has been successfully deleted.",
		"delete-text-error": "The text must match the name of the market",
		"delete-text-label": "Type the name of the market to confirm",
		"description": "Warning ! The deletion of <strong>{marketName}</strong> cannot be undone. If you continue, the market will be permanently deleted along with all the elements it contains. Card payments can no longer be traced. However, the funds of the cards will remain unchanged.",
		"title": "Delete - {marketName}"
	},
	"fr": {
		"delete-market-success-notification": "Le commerce {marketName} a été supprimé avec succès.",
		"delete-text-error": "Le texte doit correspondre au nom du commerce",
		"delete-text-label": "Taper le nom du commerce pour confirmer",
		"description": "Avertissement ! La suppression de <strong>{marketName}</strong> ne peut pas être annulée. Si vous continuez, le commerce sera supprimé ainsi que tous les éléments qu'il contient de façon définitive. Les paiements sur les cartes ne pourront plus être retracés. Par contre, les fonds des cartes vont rester inchangés.",
		"title": "Supprimer - {marketName}"
	}
}
</i18n>

<template>
  <UiDialogDeleteModal
    :return-route="{ name: URL_MARKET_ADMIN }"
    :title="t('title', { marketName: getMarketName() })"
    :description="t('description', { marketName: getMarketName() })"
    :validation-text="getMarketName()"
    :delete-text-label="t('delete-text-label')"
    :delete-text-error="t('delete-text-error')"
    @onDelete="deleteMarket" />
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_MARKET_ADMIN } from "@/lib/consts/urls";

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

const { mutate: deleteMarketMutation } = useMutation(
  gql`
    mutation DeleteMarket($input: DeleteMarketInput!) {
      deleteMarket(input: $input)
    }
  `
);

function getMarketName() {
  return market.value ? market.value.name : "";
}

async function deleteMarket() {
  await deleteMarketMutation({
    input: {
      marketId: route.params.marketId
    }
  });

  addSuccess(t("delete-market-success-notification", { marketName: market.value.name }));
  router.push({ name: URL_MARKET_ADMIN });
}
</script>
