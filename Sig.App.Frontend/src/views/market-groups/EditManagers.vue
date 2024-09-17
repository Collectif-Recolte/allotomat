<i18n>
{
	"en": {
		"add-market-group-manager-success-notification": "The addition of the market group manager was a success. The manager will receive an email for the creation of his account in the next few minutes.",
		"remove-manager-success-notification": "The manager {email} has been successfully removed.",
		"title": "Managers - {marketGroupName}"
	},
	"fr": {
		"add-market-group-manager-success-notification": "L’ajout du gestionnaire du groupe de commerce a été un succès. Le gestionnaire va recevoir un courriel pour la création de son compte dans les prochaines minutes.",
		"remove-manager-success-notification": "Le gestionnaire {email} a été enlevé avec succès.",
		"title": "Gestionnaires - {marketGroupName}"
	}
}
</i18n>

<template>
  <ManageManagersModal
    ref="manageManagersModal"
    :return-route="{ name: URL_MARKET_GROUPS_OVERVIEW }"
    :managers="managers"
    :title="t('title', { marketGroupName: getMarketGroupName() })"
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
import { URL_MARKET_GROUPS_OVERVIEW } from "@/lib/consts/urls";

import ManageManagersModal from "@/components/managers/manage-managers-modal.vue";

const { t } = useI18n();
const route = useRoute();
const { addSuccess } = useNotificationsStore();
const manageManagersModal = ref(null);

const { result, refetch } = useQuery(
  gql`
    query MarketGroup($id: ID!) {
      marketGroup(id: $id) {
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
    id: route.params.marketGroupId
  }
);
const marketGroup = useResult(result);

const { mutate: removeManagerFromMarketGroup } = useMutation(
  gql`
    mutation RemoveManagerFromMarketGroup($input: RemoveManagerFromMarketGroupInput!) {
      removeManagerFromMarketGroup(input: $input) {
        marketGroup {
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

const { mutate: addManagerToMarketGroup } = useMutation(
  gql`
    mutation AddManagerToMarketGroup($input: AddManagerToMarketGroupInput!) {
      addManagerToMarketGroup(input: $input) {
        managers {
          id
          email
        }
      }
    }
  `
);

function getMarketGroupName() {
  return marketGroup.value ? marketGroup.value.name : "";
}

const managers = computed(() => {
  return marketGroup.value ? marketGroup.value.managers : [];
});

async function removeManager(manager) {
  let input = { marketGroupId: marketGroup.value.id, managerId: manager.id };

  await removeManagerFromMarketGroup({ input });
  addSuccess(t("remove-manager-success-notification", { email: manager.email }));
  refetch();
}

async function addManager(email) {
  await addManagerToMarketGroup({
    input: {
      marketGroupId: marketGroup.value.id,
      managerEmails: [email]
    }
  });
  addSuccess(t("add-market-group-manager-success-notification"));
  refetch();
  manageManagersModal.value.hideAddManagerForm();
}
</script>
