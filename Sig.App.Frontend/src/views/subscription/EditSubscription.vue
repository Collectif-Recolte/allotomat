<i18n>
{
	"en": {
		"edit-subscription": "Edit",
		"edit-subscription-success-notification": "The subscription modification {subscriptionName} was successful.",
    "title": "Edit a subscription",
    "cant-edit-subscription-with-beneficiaries": "It is not possible to modify a subscription that is already assigned to participants",
    "beneficiary-type-can-only-be-assign-once": "A category cannot be linked to more than one type."
	},
	"fr": {
		"edit-subscription": "Modifier",
		"edit-subscription-success-notification": "La modification de l’abonnement {subscriptionName} a été un succès.",
    "title": "Modifier un abonnement",
    "cant-edit-subscription-with-beneficiaries": "Il n'est pas possible de modifier un abonnement qui est déjà attribué a des participants",
    "beneficiary-type-can-only-be-assign-once": "Une catégorie ne peut pas être liée à plus d'un type."
	}
}
</i18n>

<template>
  <UiDialogModal v-slot="{ closeModal }" :return-route="{ name: URL_SUBSCRIPTION_ADMIN }" :title="t('title')" :has-footer="false">
    <SubscriptionForm
      v-if="subscription"
      :project-id="subscription.project.id"
      :subscription-name="subscription.name"
      :monthly-payment-moment="subscription.monthlyPaymentMoment"
      :start-date="formattedDate(subscription.startDate)"
      :end-date="formattedDate(subscription.endDate)"
      :funds-expiration-date="formattedDate(subscription.fundsExpirationDate)"
      :is-funds-accumulable="subscription.isFundsAccumulable"
      :product-group-subscription-types="productGroupSubscriptionTypes"
      :submit-btn="t('edit-subscription')"
      @closeModal="closeModal"
      @submit="onSubmit" />
  </UiDialogModal>
</template>

<script setup>
import { computed } from "vue";
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { useRouter, useRoute } from "vue-router";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";

import { formatDate, serverFormat } from "@/lib/helpers/date";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_SUBSCRIPTION_ADMIN } from "@/lib/consts/urls";
import { useGraphQLErrorMessages } from "@/lib/helpers/error-handler";

import SubscriptionForm from "@/views/subscription/_Form.vue";

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
const { addSuccess } = useNotificationsStore();

useGraphQLErrorMessages({
  CANT_EDIT_SUBSCRIPTION_WITH_BENEFICIARIES: () => {
    return t("cant-edit-subscription-with-beneficiaries");
  },
  BENEFICIARY_TYPE_CAN_ONLY_BE_ASSIGN_ONCE: () => {
    return t("beneficiary-type-can-only-be-assign-once");
  }
});

const { result } = useQuery(
  gql`
    query Subscription($id: ID!) {
      subscription(id: $id) {
        id
        name
        startDate
        endDate
        isFundsAccumulable
        monthlyPaymentMoment
        fundsExpirationDate
        types {
          id
          productGroup {
            id
          }
          amount
          beneficiaryType {
            id
          }
        }
        project {
          id
        }
      }
    }
  `,
  {
    id: route.params.subscriptionId
  }
);
let subscription = useResult(result);

const { mutate: editSubscription } = useMutation(
  gql`
    mutation EditSubscription($input: EditSubscriptionInput!) {
      editSubscription(input: $input) {
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
            productGroup {
              id
            }
            amount
            beneficiaryType {
              id
            }
          }
        }
      }
    }
  `
);

const productGroupSubscriptionTypes = computed(() => {
  let results = [];

  for (var i = 0; i < subscription.value.types.length; i++) {
    let subscriptionType = subscription.value.types[i];
    let productGroup = results.find((x) => x.productGroupId === subscriptionType.productGroup.id);
    if (productGroup === undefined) {
      productGroup = { productGroupId: subscriptionType.productGroup.id, types: [] };
      results.push(productGroup);
    }
    productGroup.types.push({
      originalId: subscriptionType.id,
      amount: subscriptionType.amount,
      type: subscriptionType.beneficiaryType.id
    });
  }

  return results;
});

async function onSubmit({
  subscriptionName,
  monthlyPaymentMoment,
  fundsExpirationDate,
  startDate,
  endDate,
  productGroupSubscriptionTypes,
  isFundsAccumulable
}) {
  let types = [];
  for (var i = 0; i < productGroupSubscriptionTypes.length; i++) {
    let item = productGroupSubscriptionTypes[i];
    for (var j = 0; j < item.types.length; j++) {
      let type = item.types[j];
      types.push({
        amount: parseFloat(type.amount),
        originalId: type.originalId,
        beneficiaryTypeId: type.type,
        productGroupId: item.productGroupId
      });
    }
  }

  await editSubscription({
    input: {
      subscriptionId: route.params.subscriptionId,
      name: subscriptionName,
      monthlyPaymentMoment,
      startDate: formatDate(startDate, serverFormat),
      endDate: formatDate(endDate, serverFormat),
      isFundsAccumulable,
      types,
      fundsExpirationDate: isFundsAccumulable ? { value: formatDate(fundsExpirationDate, serverFormat) } : undefined
    }
  });
  router.push({ name: URL_SUBSCRIPTION_ADMIN });
  addSuccess(t("edit-subscription-success-notification", { subscriptionName }));
}

function formattedDate(date) {
  if (date === null) {
    return null;
  }
  let formattedDate = date.substring(0, 10).split("-");
  return new Date(formattedDate[0], formattedDate[1] - 1, formattedDate[2]);
}
</script>
