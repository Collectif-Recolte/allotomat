<i18n>
{
	"en": {
		"description": "Warning ! The withdrawal of <strong>{beneficiaryName}</strong> in subscription <strong>{subscriptionName}</strong> cannot be undone. If you continue, <strong>{beneficiaryName}</strong> will no longer receive a transfer for subscription <strong>{subscriptionName}</strong>.",
		"remove-beneficiary-from-subscription-success-notification": "{beneficiaryName} has been successfully remove from subscription {subscriptionName}.",
		"remove-text-error": "Text must match subscription name",
		"remove-text-label": "Type the subscription name to confirm",
		"title": "Unsubscribe {subscriptionName} - {beneficiaryName}"
	},
	"fr": {
		"description": "Avertissement ! Le retrait de <strong>{beneficiaryName}</strong> dans l'abonnement <strong>{subscriptionName}</strong> ne peut pas être annulé. Si vous continuez, <strong>{beneficiaryName}</strong> ne recevra plus de virement pour l'abonnement <strong>{subscriptionName}</strong>.",
		"remove-beneficiary-from-subscription-success-notification": "{beneficiaryName} a été retiré-e avec succès de l'abonnement {subscriptionName}.",
		"remove-text-error": "Le texte doit correspondre au nom de l'abonnement",
		"remove-text-label": "Taper le nom de l'abonnement",
		"title": "Retirer l'abonnement {subscriptionName} - {beneficiaryName}"
	}
}
</i18n>

<template>
  <UiDialogDeleteModal
    :return-route="{
      name: URL_BENEFICIARY_ADMIN
    }"
    :title="t('title', { beneficiaryName: getBeneficiaryName(), subscriptionName: getSubscriptionName() })"
    :description="t('description', { beneficiaryName: getBeneficiaryName(), subscriptionName: getSubscriptionName() })"
    :validation-text="getSubscriptionName()"
    :delete-text-label="t('remove-text-label')"
    :delete-text-error="t('remove-text-error')"
    @onDelete="removeBeneficiaryFromSubscription" />
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_BENEFICIARY_ADMIN } from "@/lib/consts/urls";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { addSuccess } = useNotificationsStore();

const { result: resultBeneficiary } = useQuery(
  gql`
    query Beneficiary($id: ID!) {
      beneficiary(id: $id) {
        id
        firstname
        lastname
      }
    }
  `,
  {
    id: route.params.beneficiaryId
  }
);
const beneficiary = useResult(resultBeneficiary);

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

const { mutate: removeBeneficiaryFromSubscriptionMutation } = useMutation(
  gql`
    mutation RemoveBeneficiaryFromSubscription($input: RemoveBeneficiaryFromSubscriptionInput!) {
      removeBeneficiaryFromSubscription(input: $input)
    }
  `
);

function getBeneficiaryName() {
  return beneficiary.value ? `${beneficiary.value.firstname} ${beneficiary.value.lastname}` : "";
}

function getSubscriptionName() {
  return subscription.value ? subscription.value.name : "";
}

async function removeBeneficiaryFromSubscription() {
  await removeBeneficiaryFromSubscriptionMutation({
    input: {
      beneficiaryId: route.params.beneficiaryId,
      subscriptionId: route.params.subscriptionId
    }
  });

  addSuccess(
    t("remove-beneficiary-from-subscription-success-notification", {
      beneficiaryName: `${beneficiary.value.firstname} ${beneficiary.value.lastname}`,
      subscriptionName: subscription.value.name
    })
  );
  router.push({
    name: URL_BENEFICIARY_ADMIN
  });
}
</script>
