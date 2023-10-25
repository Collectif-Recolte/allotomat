<i18n>
{
	"en": {
		"delete-subscription-success-notification": "The subscription {subscriptionName} has been successfully deleted.",
		"delete-text-error": "The text must match the name of the subscription",
		"delete-text-label": "Type the name of the subscription to confirm",
		"description": "Warning ! The deletion of <strong>{subscriptionName}</strong> cannot be undone. If you continue, the subscription will be deleted along with all the elements it contains. Transfers on the cards can no longer be traced. However, the funds of the cards will remain unchanged.",
    "title": "Delete - {subscriptionName}",
    "cant-delete-subscription-with-beneficiaries": "It is not possible to delete a subscription that is already assigned to participants"
	},
	"fr": {
		"delete-subscription-success-notification": "L'abonnement {subscriptionName} a été supprimé avec succès.",
		"delete-text-error": "Le texte doit correspondre au nom de l'abonnement",
		"delete-text-label": "Taper le nom de l'abonnement pour confirmer",
		"description": "Avertissement ! La suppression de <strong>{subscriptionName}</strong> ne peut pas être annulée. Si vous continuez, l'abonnement sera supprimé ainsi que tous les éléments qu'il contient de façon définitive. Les transferts sur les cartes ne pourront plus être retracés. Par contre, les fonds des cartes vont rester inchangés.",
		"title": "Supprimer - {subscriptionName}",
    "cant-delete-subscription-with-beneficiaries": "Il n'est pas possible de supprimer un abonnement qui est déjà attribué a des participants"
	}
}
</i18n>

<template>
  <UiDialogDeleteModal
    :return-route="{ name: URL_SUBSCRIPTION_ADMIN }"
    :title="t('title', { subscriptionName: getSubscriptionName() })"
    :description="t('description', { subscriptionName: getSubscriptionName() })"
    :validation-text="getSubscriptionName()"
    :delete-text-label="t('delete-text-label')"
    :delete-text-error="t('delete-text-error')"
    @onDelete="deleteSubscription" />
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_SUBSCRIPTION_ADMIN } from "@/lib/consts/urls";
import { useGraphQLErrorMessages } from "@/lib/helpers/error-handler";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { addSuccess } = useNotificationsStore();

useGraphQLErrorMessages({
  CANT_DELETE_SUBSCRIPTION_WITH_BENEFICIARIES: () => {
    return t("cant-delete-subscription-with-beneficiaries");
  }
});

const { result: resultSubscription } = useQuery(
  gql`
    query Subscription($id: ID!) {
      subscription(id: $id) {
        id
        name
      }
    }
  `,
  {
    id: route.params.subscriptionId
  }
);
const subscription = useResult(resultSubscription);

const { mutate: deleteSubscriptionMutation } = useMutation(
  gql`
    mutation DeleteSubscription($input: DeleteSubscriptionInput!) {
      deleteSubscription(input: $input)
    }
  `
);

function getSubscriptionName() {
  return subscription.value ? subscription.value.name : "";
}

async function deleteSubscription() {
  await deleteSubscriptionMutation({
    input: {
      subscriptionId: route.params.subscriptionId
    }
  });

  addSuccess(t("delete-subscription-success-notification", { subscriptionName: subscription.value.name }));
  router.push({ name: URL_SUBSCRIPTION_ADMIN });
}
</script>
