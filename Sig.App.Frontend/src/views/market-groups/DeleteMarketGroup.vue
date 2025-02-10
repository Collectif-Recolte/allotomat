<i18n>
{
	"en": {
		"delete-market-group-success-notification": "The market group {marketGroupName} has been successfully deleted.",
		"delete-text-error": "The text must match the name of the market group",
		"delete-text-label": "Type the name of the market group to confirm",
		"description": "Warning ! The deletion of <strong>{marketGroupName}</strong> cannot be undone. If you continue, the market group will be permanently deleted along with all the elements it contains. Card payments can no longer be traced. However, the funds of the cards will remain unchanged.",
		"title": "Delete - {marketGroupName}"
	},
	"fr": {
		"delete-market-group-success-notification": "Le groupe de commerce {marketGroupName} a été supprimé avec succès.",
		"delete-text-error": "Le texte doit correspondre au nom du groupe de commerce",
		"delete-text-label": "Taper le nom du groupe de commerce pour confirmer",
		"description": "Avertissement ! La suppression de <strong>{marketGroupName}</strong> ne peut pas être annulée. Si vous continuez, le groupe de commerce sera supprimé ainsi que tous les éléments qu'il contient de façon définitive. Les paiements sur les cartes ne pourront plus être retracés. Par contre, les fonds des cartes vont rester inchangés.",
		"title": "Supprimer - {marketGroupName}"
	}
}
</i18n>

<template>
  <UiDialogDeleteModal
    :return-route="{ name: URL_MARKET_GROUPS_OVERVIEW }"
    :title="t('title', { marketGroupName: getMarketGroupName() })"
    :description="t('description', { URL_MARKET_GROUPS_OVERVIEW: getMarketGroupName() })"
    :validation-text="getMarketGroupName()"
    :delete-text-label="t('delete-text-label')"
    :delete-text-error="t('delete-text-error')"
    @onDelete="deleteMarketGroup" />
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

const { mutate: deleteMarketGroupMutation } = useMutation(
  gql`
    mutation DeleteMarketGroup($input: DeleteMarketGroupInput!) {
      deleteMarketGroup(input: $input)
    }
  `
);

function getMarketGroupName() {
  return marketGroup.value ? marketGroup.value.name : "";
}

async function deleteMarketGroup() {
  await deleteMarketGroupMutation({
    input: {
      marketGroupId: route.params.marketGroupId
    }
  });

  addSuccess(t("delete-market-group-success-notification", { marketGroupName: marketGroup.value.name }));
  router.push({ name: URL_MARKET_GROUPS_OVERVIEW });
}
</script>
