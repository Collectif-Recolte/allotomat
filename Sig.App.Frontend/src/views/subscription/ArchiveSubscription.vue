<i18n>
{
	"en": {
		"archive-subscription-success-notification": "The subscription {subscriptionName} has been successfully archived.",
		"archive-text-error": "The text must match the name of the subscription",
		"archive-subscription-success-notification-text-label": "Type the name of the subscription to confirm",
		"description": "Warning ! If you continue, the subscription will be archived and it will be impossible to assign it to a participant. However, the funds of the cards will remain unchanged.",
    "title": "Archive - {subscriptionName}",
    "archive-btn": "Archive"
	},
	"fr": {
		"archive-subscription-success-notification": "L'abonnement {subscriptionName} a été archivé avec succès.",
		"archive-text-error": "Le texte doit correspondre au nom de l'abonnement",
		"archive-subscription-success-notification-text-label": "Taper le nom de l'abonnement pour confirmer",
		"description": "Avertissement ! Si vous continuez, l'abonnement sera archivé et il ne pourra plus être assigné à un participant. Par contre, les fonds des cartes vont rester inchangés.",
		"title": "Archiver - {subscriptionName}",
    "archive-btn": "Archiver"
	}
}
</i18n>

<template>
  <UiDialogDeleteModal
    :return-route="{ name: URL_SUBSCRIPTION_ADMIN }"
    :title="t('title', { subscriptionName: getSubscriptionName() })"
    :description="t('description', { subscriptionName: getSubscriptionName() })"
    :validation-text="getSubscriptionName()"
    :delete-text-label="t('archive-subscription-success-notification-text-label')"
    :delete-text-error="t('archive-text-error')"
    :delete-button-label="t('archive-btn')"
    @onDelete="archiveSubscription" />
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

const { mutate: archiveSubscriptionMutation } = useMutation(
  gql`
    mutation ArchiveSubscription($input: ArchiveSubscriptionInput!) {
      archiveSubscription(input: $input) {
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

async function archiveSubscription() {
  await archiveSubscriptionMutation({
    input: {
      subscriptionId: route.params.subscriptionId
    }
  });

  addSuccess(t("archive-subscription-success-notification", { subscriptionName: subscriptionName(subscription.value) }));
  router.push({ name: URL_SUBSCRIPTION_ADMIN });
}
</script>
