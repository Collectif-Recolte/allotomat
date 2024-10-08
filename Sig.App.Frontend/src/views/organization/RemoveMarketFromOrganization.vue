<i18n>
{
	"en": {
		"delete-market-success-notification": "The market {marketName} has been successfully deleted.",
		"delete-text-error": "The text must match the name of the market",
		"delete-text-label": "Type the name of the market to confirm",
		"description": "Warning ! The withdrawal of the market <strong>{marketName}</strong> cannot be undone. If you continue, the market will be removed from the group permanently.",
		"title": "Remove - {marketName}"
	},
	"fr": {
		"delete-market-success-notification": "Le commerce {marketName} a été retiré avec succès.",
		"delete-text-error": "Le texte doit correspondre au nom du commerce",
		"delete-text-label": "Taper le nom du commerce pour confirmer",
		"description": "Avertissement ! Le retrait du commerce <strong>{marketName}</strong> ne peut pas être annulé. Si vous continuez, le commerce sera retiré du groupe de façon définitive.",
		"title": "Retirer - {marketName}"
	}
}
</i18n>

<template>
  <UiDialogDeleteModal
    :return-route="{ name: URL_ORGANIZATION_MANAGE_MERCHANTS }"
    :title="t('title', { marketName: getMarketName() })"
    :description="t('description', { marketName: getMarketName() })"
    :validation-text="getMarketName()"
    :delete-text-label="t('delete-text-label')"
    :delete-text-error="t('delete-text-error')"
    @onDelete="removeMarket" />
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_ORGANIZATION_MANAGE_MERCHANTS } from "@/lib/consts/urls";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { addSuccess } = useNotificationsStore();

const { result: resultMarket } = useQuery(
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
const market = useResult(resultMarket);

const { mutate: removeMarketFromOrganization } = useMutation(
  gql`
    mutation RemoveMarketFromOrganization($input: RemoveMarketFromOrganizationInput!) {
      removeMarketFromOrganization(input: $input) {
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

function getMarketName() {
  return market.value ? market.value.name : "";
}

async function removeMarket() {
  await removeMarketFromOrganization({
    input: {
      organizationId: route.params.organizationId,
      marketId: route.params.marketId
    }
  });

  addSuccess(t("delete-market-success-notification", { marketName: market.value.name }));
  router.push({ name: URL_ORGANIZATION_MANAGE_MERCHANTS });
}
</script>
