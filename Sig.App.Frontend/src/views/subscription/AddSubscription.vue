<i18n>
{
	"en": {
		"add-subscription": "Add subscription",
		"add-subscription-success-notification": "Adding subscription {subscriptionName} was successful. You can now assign the new subscription to participants.",
		"title": "Add a subscription",
    "confirm-title": "Subscription added!",
    "confirm-desc": "Do you want to configure the monetary envelopes available to organizations right away?",
    "configure-envelopes": "Configure envelopes",
    "later": "Later",
    "product-group-type-can-only-be-assign-once-by-beneficiary-type": "A product group cannot be linked to more than one type."
	},
	"fr": {
		"add-subscription": "Ajouter l'abonnement",
		"add-subscription-success-notification": "L’ajout de l’abonnement {subscriptionName} a été un succès. Vous pouvez maintenant assigner le nouvel abonnement à des participant-e-s.",
		"title": "Ajouter un abonnement",
    "confirm-title": "Abonnement ajouté!",
    "confirm-desc": "Désirez-vous configurer les enveloppes monétaires disponibles pour les organismes tout de suite ?",
    "configure-envelopes": "Configurer les enveloppes",
    "later": "Plus tard",
    "product-group-type-can-only-be-assign-once-by-beneficiary-type": "Un groupe de produit ne peut pas être liée à plus d'un type."

	}
}
</i18n>

<template>
  <UiDialogModal
    v-if="isInEdition"
    v-slot="{ closeModal }"
    :return-route="{ name: URL_SUBSCRIPTION_ADMIN }"
    :title="t('title')"
    :has-footer="false">
    <SubscriptionForm
      :submit-btn="t('add-subscription')"
      :project-id="route.query.projectId"
      @closeModal="closeModal"
      @submit="onSubmit" />
  </UiDialogModal>
  <UiDialogConfirmModal
    v-else
    :title="t('confirm-title')"
    :description="t('confirm-desc')"
    :confirm-button-label="t('configure-envelopes')"
    :cancel-button-label="t('later')"
    :return-route="{ name: URL_SUBSCRIPTION_ADMIN }"
    @confirm="onConfirm" />
</template>

<script setup>
import gql from "graphql-tag";
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { useMutation } from "@vue/apollo-composable";
import { formatDate, serverFormat } from "@/lib/helpers/date";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_SUBSCRIPTION_ADMIN, URL_SUBSCRIPTION_MANAGE_BUDGET_ALLOWANCE } from "@/lib/consts/urls";
import { useGraphQLErrorMessages } from "@/lib/helpers/error-handler";

import SubscriptionForm from "@/views/subscription/_Form.vue";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { addSuccess } = useNotificationsStore();

const isInEdition = ref(true);
const newSubscriptionId = ref(null);

useGraphQLErrorMessages({
  CANT_HAVE_MULTIPLE_BENEFICIARY_TYPE_AND_PRODUCT_GROUP_IN_SUBSCRIPTION: () => {
    return t("product-group-type-can-only-be-assign-once-by-beneficiary-type");
  }
});

const { mutate: createSubscriptionInProject } = useMutation(
  gql`
    mutation CreateSubscriptionInProject($input: CreateSubscriptionInProjectInput!) {
      createSubscriptionInProject(input: $input) {
        subscription {
          id
          name
          isFundsAccumulable
          monthlyPaymentMoment
          project {
            id
            name
          }
          startDate
          endDate
          fundsExpirationDate
          types {
            id
            amount
            productGroup {
              id
              name
              color
              orderOfAppearance
            }
          }
        }
      }
    }
  `
);

async function onSubmit({
  subscriptionName,
  startDate,
  endDate,
  monthlyPaymentMoment,
  isSubscriptionPaymentBasedCardUsage,
  maxNumberOfPayments,
  isFundsAccumulable,
  fundsExpirationDate,
  triggerFundExpiration,
  numberDaysUntilFundsExpire,
  productGroupSubscriptionTypes
}) {
  let types = [];
  for (var i = 0; i < productGroupSubscriptionTypes.length; i++) {
    let item = productGroupSubscriptionTypes[i];
    for (var j = 0; j < item.types.length; j++) {
      let type = item.types[j];
      types.push({
        amount: parseFloat(type.amount),
        beneficiaryTypeId: type.type,
        productGroupId: item.productGroupId
      });
    }
  }

  const result = await createSubscriptionInProject({
    input: {
      projectId: route.query.projectId,
      name: subscriptionName,
      monthlyPaymentMoment,
      startDate: formatDate(startDate, serverFormat),
      endDate: formatDate(endDate, serverFormat),
      isSubscriptionPaymentBasedCardUsage,
      maxNumberOfPayments:
        maxNumberOfPayments !== null && maxNumberOfPayments !== undefined ? { value: maxNumberOfPayments } : undefined,
      triggerFundExpiration,
      numberDaysUntilFundsExpire:
        numberDaysUntilFundsExpire !== null && numberDaysUntilFundsExpire !== undefined
          ? { value: numberDaysUntilFundsExpire }
          : undefined,
      types,
      fundsExpirationDate: isFundsAccumulable ? { value: formatDate(fundsExpirationDate, serverFormat) } : undefined,
      isFundsAccumulable
    }
  });
  isInEdition.value = false;
  newSubscriptionId.value = result.data.createSubscriptionInProject.subscription.id;
  addSuccess(t("add-subscription-success-notification", { subscriptionName }));
}

const onConfirm = () => {
  router.push({ name: URL_SUBSCRIPTION_MANAGE_BUDGET_ALLOWANCE, params: { subscriptionId: newSubscriptionId.value } });
};
</script>
