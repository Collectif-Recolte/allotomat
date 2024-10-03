<i18n>
{
	"en": {
		"archive-market-success-notification": "The market {marketName} has been successfully archived.",
		"archive-text-error": "The text must match the name of the market",
		"archive-text-label": "Type the name of the market to confirm",
		"description": "Warning ! The archive of <strong>{marketName}</strong> cannot be undone. If you continue, the market will be permanently archived along with all the elements it contains. However, the funds of the cards will remain unchanged.",
		"title": "Archive - {marketName}",
    "archive-btn-label": "Archive"
	},
	"fr": {
		"archive-market-success-notification": "Le commerce {marketName} a été archivé avec succès.",
		"archive-text-error": "Le texte doit correspondre au nom du commerce",
		"archive-text-label": "Taper le nom du commerce pour confirmer",
		"description": "Avertissement ! L'archivage de <strong>{marketName}</strong> ne peut pas être annulé. Si vous continuez, le commerce sera archivé ainsi que tous les éléments qu'il contient de façon définitive. Par contre, les fonds des cartes vont rester inchangés.",
		"title": "Archiver - {marketName}",
    "archive-btn-label": "Archiver"
	}
}
</i18n>

<template>
  <UiDialogDeleteModal
    :return-route="returnRoute()"
    :title="t('title', { marketName: getMarketName() })"
    :description="t('description', { marketName: getMarketName() })"
    :validation-text="getMarketName()"
    :delete-text-label="t('archive-text-label')"
    :delete-text-error="t('archive-text-error')"
    :delete-button-label="t('archive-btn-label')"
    @onDelete="archiveMarket" />
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_MARKET_ADMIN, URL_MARKET_OVERVIEW_ARCHIVE, URL_MARKET_OVERVIEW } from "@/lib/consts/urls";

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

const { mutate: archiveMarketMutation } = useMutation(
  gql`
    mutation ArchiveMarket($input: ArchiveMarketInput!) {
      archiveMarket(input: $input)
    }
  `
);

function getMarketName() {
  return market.value ? market.value.name : "";
}

async function archiveMarket() {
  await archiveMarketMutation({
    input: {
      marketId: route.params.marketId
    }
  });

  addSuccess(t("archive-market-success-notification", { marketName: market.value.name }));
  router.push(returnRoute());
}

function returnRoute() {
  if (route.name === URL_MARKET_OVERVIEW_ARCHIVE) return { name: URL_MARKET_OVERVIEW };
  else return { name: URL_MARKET_ADMIN };
}
</script>
