<i18n>
{
	"en": {
		"archive-market-group-success-notification": "The market group {marketGroupName} has been successfully archived.",
		"archive-text-error": "The text must match the name of the market group",
		"archive-text-label": "Type the name of the market group to confirm",
		"description": "Warning ! The archive of <strong>{marketGroupName}</strong> cannot be undone. If you continue, the market group will be permanently archived along with all the elements it contains. However, the funds of the cards will remain unchanged.",
		"title": "Archive - {marketGroupName}",
    "archive-btn-label": "Archive"
	},
	"fr": {
		"archive-market-group-success-notification": "Le groupe de commerce {marketGroupName} a été archivé avec succès.",
		"archive-text-error": "Le texte doit correspondre au nom du groupe de commerce",
		"archive-text-label": "Taper le nom du groupe de commerce pour confirmer",
		"description": "Avertissement ! L'archivage de <strong>{marketGroupName}</strong> ne peut pas être annulé. Si vous continuez, le groupe de commerce sera archivé ainsi que tous les éléments qu'il contient de façon définitive. Par contre, les fonds des cartes vont rester inchangés.",
		"title": "Archiver - {marketGroupName}",
    "archive-btn-label": "Archiver"
	}
}
</i18n>

<template>
  <UiDialogDeleteModal
    :return-route="{ name: URL_MARKET_GROUPS_OVERVIEW }"
    :title="t('title', { marketGroupName: getMarketGroupName() })"
    :description="t('description', { marketGroupName: getMarketGroupName() })"
    :validation-text="getMarketGroupName()"
    :delete-text-label="t('archive-text-label')"
    :delete-text-error="t('archive-text-error')"
    :delete-button-label="t('archive-btn-label')"
    @onDelete="archiveMarketGroup" />
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_MARKET_GROUPS_OVERVIEW } from "@/lib/consts/urls";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { addSuccess } = useNotificationsStore();

const { result } = useQuery(
  gql`
    query MarketGroup($id: ID!) {
      marketGroup(id: $id) {
        id
        name
      }
    }
  `,
  {
    id: route.params.marketGroupId
  }
);
const marketGroup = useResult(result);

const { mutate: archiveMarketGroupMutation } = useMutation(
  gql`
    mutation ArchiveMarketGroup($input: ArchiveMarketGroupInput!) {
      archiveMarketGroup(input: $input)
    }
  `
);

function getMarketGroupName() {
  return marketGroup.value ? marketGroup.value.name : "";
}

async function archiveMarketGroup() {
  await archiveMarketGroupMutation({
    input: {
      marketGroupId: route.params.marketGroupId
    }
  });

  addSuccess(t("archive-market-group-success-notification", { marketGroupName: marketGroup.value.name }));
  router.push({ name: URL_MARKET_GROUPS_OVERVIEW });
}
</script>
