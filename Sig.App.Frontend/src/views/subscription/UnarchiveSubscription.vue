<i18n>
{
	"en": {
		"unarchive-subscription-success-notification": "The subscription {subscriptionName} has been successfully unarchived.",
		"unarchive-text-error": "The text must match the name of the subscription",
		"unarchive-subscription-success-notification-text-label": "Type the name of the subscription to confirm",
		"description": "Warning ! If you continue, the subscription will be unarchived and it will be possible to assign it to a participant. However, the funds of the cards will remain unchanged.",
    "title": "Unarchive - {subscriptionName}",
    "unarchive-btn": "Unarchive"
	},
	"fr": {
		"unarchive-subscription-success-notification": "L'abonnement {subscriptionName} a été désarchivé avec succès.",
		"unarchive-text-error": "Le texte doit correspondre au nom de l'abonnement",
		"unarchive-subscription-success-notification-text-label": "Taper le nom de l'abonnement pour confirmer",
		"description": "Avertissement ! Si vous continuez, l'abonnement sera désarchivé et il pourra être assigné à un participant. Par contre, les fonds des cartes vont rester inchangés.",
		"title": "Désarchiver - {subscriptionName}",
    "unarchive-btn": "Désarchiver"
	}
}
</i18n>

<template>
  <UiDialogDeleteModal
    :return-route="{ name: URL_SUBSCRIPTION_ADMIN }"
    :title="t('title', { subscriptionName: getSubscriptionName() })"
    :description="t('description', { subscriptionName: getSubscriptionName() })"
    :validation-text="getSubscriptionName()"
    :delete-text-label="t('unarchive-subscription-success-notification-text-label')"
    :delete-text-error="t('unarchive-text-error')"
    :delete-button-label="t('unarchive-btn')"
    @onDelete="unarchiveSubscription" />
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { subscriptionName } from "@/lib/helpers/subscription";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_SUBSCRIPTION_ADMIN } from "@/lib/consts/urls";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { addSuccess } = useNotificationsStore();

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

const { mutate: unarchiveSubscriptionMutation } = useMutation(
  gql`
    mutation UnarchiveSubscription($input: UnarchiveSubscriptionInput!) {
      unarchiveSubscription(input: $input) {
        subscription {
          id
          isArchived
        }
      }
    }
  `
);

function getSubscriptionName() {
  return subscription.value ? subscriptionName(subscription.value) : "";
}

async function unarchiveSubscription() {
  await unarchiveSubscriptionMutation({
    input: {
      subscriptionId: route.params.subscriptionId
    }
  });

  addSuccess(t("unarchive-subscription-success-notification", { subscriptionName: subscriptionName(subscription.value) }));
  router.push({ name: URL_SUBSCRIPTION_ADMIN });
}
</script>
