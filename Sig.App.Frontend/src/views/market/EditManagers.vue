<i18n>
{
	"en": {
		"add-market-manager-success-notification": "The addition of the market manager was a success. The manager will receive an email for the creation of his account in the next few minutes.",
		"remove-manager-success-notification": "The manager {email} has been successfully removed.",
		"title": "Managers - {marketName}"
	},
	"fr": {
		"add-market-manager-success-notification": "L’ajout du gestionnaire du commerce a été un succès. Le gestionnaire va recevoir un courriel pour la création de son compte dans les prochaines minutes.",
		"remove-manager-success-notification": "Le gestionnaire {email} a été enlevé avec succès.",
		"title": "Gestionnaires - {marketName}"
	}
}
</i18n>

<template>
  <ManageManagersModal
    ref="manageManagersModal"
    :return-route="{ name: URL_MARKET_ADMIN }"
    :managers="managers"
    :title="t('title', { marketName: getMarketName() })"
    @removeManager="removeManager"
    @addManager="addManager" />
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { ref, computed } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute } from "vue-router";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_MARKET_ADMIN } from "@/lib/consts/urls";

import ManageManagersModal from "@/components/managers/manage-managers-modal.vue";

const { t } = useI18n();
const route = useRoute();
const { addSuccess } = useNotificationsStore();
const manageManagersModal = ref(null);

const { result, refetch } = useQuery(
  gql`
    query Market($id: ID!) {
      market(id: $id) {
        id
        name
        managers {
          id
          profile {
            id
            firstName
            lastName
          }
          email
        }
      }
    }
  `,
  {
    id: route.params.marketId
  }
);
const market = useResult(result);

const { mutate: removeManagerFromMarket } = useMutation(
  gql`
    mutation RemoveManagerFromMarket($input: RemoveManagerFromMarketInput!) {
      removeManagerFromMarket(input: $input) {
        market {
          id
          managers {
            id
            profile {
              id
              firstName
              lastName
            }
            email
          }
        }
      }
    }
  `
);

const { mutate: addManagerToMarket } = useMutation(
  gql`
    mutation AddManagerToMarket($input: AddManagerToMarketInput!) {
      addManagerToMarket(input: $input) {
        managers {
          id
          email
        }
      }
    }
  `
);

function getMarketName() {
  return market.value ? market.value.name : "";
}

const managers = computed(() => {
  return market.value ? market.value.managers : [];
});

async function removeManager(manager) {
  let input = { marketId: market.value.id, managerId: manager.id };

  await removeManagerFromMarket({ input });
  addSuccess(t("remove-manager-success-notification", { email: manager.email }));
  refetch();
}

async function addManager(email) {
  await addManagerToMarket({
    input: {
      marketId: market.value.id,
      managerEmails: [email]
    }
  });
  addSuccess(t("add-market-manager-success-notification"));
  refetch();
  manageManagersModal.value.hideAddManagerForm();
}
</script>
