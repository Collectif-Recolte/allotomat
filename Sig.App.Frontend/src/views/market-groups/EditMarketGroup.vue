<i18n>
{
	"en": {
		"edit-market-group": "Edit",
		"edit-market-group-success-notification": "Editing market group {marketGroupName} was successful.",
		"title": "Edit a market group"
	},
	"fr": {
		"edit-market-group": "Modifier",
		"edit-market-group-success-notification": "L’édition du groupe de commerce {marketGroupName} a été un succès.",
		"title": "Modifier un groupe de commerce"
	}
}
</i18n>

<template>
  <UiDialogModal
    v-slot="{ closeModal }"
    :return-route="{ name: URL_MARKET_GROUPS_OVERVIEW }"
    :title="t('title')"
    :has-footer="false">
    <MarketGroupForm
      v-if="marketGroup"
      :submit-btn="t('edit-market-group')"
      :market-group-name="marketGroup.name"
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
import { URL_MARKET_GROUPS_OVERVIEW } from "@/lib/consts/urls";

import MarketGroupForm from "@/views/market-groups/_Form.vue";

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
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
let marketGroup = useResult(result);

const { mutate: editMarketGroup } = useMutation(
  gql`
    mutation EditMarketGroup($input: EditMarketGroupInput!) {
      editMarketGroup(input: $input) {
        marketGroup {
          id
          name
        }
      }
    }
  `
);

async function onSubmit(values) {
  let input = {
    marketGroupId: route.params.marketGroupId,
    name: { value: values.marketGroupName }
  };

  await editMarketGroup({ input });
  router.push({ name: URL_MARKET_GROUPS_OVERVIEW });
  addSuccess(t("edit-market-group-success-notification", { marketGroupName: values.marketGroupName }));
}
</script>
