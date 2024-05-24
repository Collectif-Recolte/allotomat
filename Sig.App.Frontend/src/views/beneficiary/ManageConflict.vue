<i18n>
{
	"en": {
		"title": "Fix payment conflict(s)",
    "cancel": "Cancel",
    "confirm": "Confirm",
    "adjustment-amount-positif": "The amount <b>{amount}</b> will go back to the envelope",
    "adjustment-amount-negatif": "The amount <b>{amount}</b> will go out of the envelope",
    "confirm-message": "The adjustments have been made for the participant {beneficiary}"
	},
	"fr": {
		"title": "Résoudre les conflits de paiement(s)",
    "cancel": "Annuler",
    "confirm": "Confirmer",
    "adjustment-amount-positif": "→ L'enveloppe va recevoir un montant de <b>{amount}</b> suite à l'ajustement.",
    "adjustment-amount-negatif": "→ Un montant de <b>{amount}</b> va être retiré de l'enveloppe suite à l'ajustement.",
    "confirm-message": "Les ajustements ont été effectués pour le participant {beneficiary}"
	}
}
</i18n>

<template>
  <UiDialogModal
    v-if="!loading"
    v-slot="{ closeModal }"
    :return-route="{
      name: URL_BENEFICIARY_ADMIN
    }"
    :has-footer="false"
    :title="t('title')">
    <Form v-slot="{ isSubmitting }" @submit="onSubmit">
      <PfForm
        can-cancel
        :submit-label="t('confirm')"
        :cancel-label="t('cancel')"
        :processing="isSubmitting"
        :disable-submit="subscriptionChecked.length === 0"
        @cancel="closeModal">
        <SubscriptionConflict
          v-if="beneficiarySubscriptionsWithConflict.length > 0"
          id="subscriptionConflict"
          :value="subscriptionChecked"
          :options="beneficiarySubscriptionsWithConflict"
          @input="onSubscriptionConflictChecked"
          @checkAll="onSubscriptionConflictCheckAll" />
        <template #footer>
          <div class="pt-5">
            <div class="flex gap-x-6 items-center justify-end">
              <PfButtonAction btn-style="link" :label="t('cancel')" @click="closeModal" />
              <PfButtonAction :is-disabled="subscriptionChecked.length === 0" :label="t('confirm')" class="px-8" type="submit" />
            </div>
          </div>
        </template>
      </PfForm>
    </Form>
  </UiDialogModal>
</template>

<script setup>
import { useI18n } from "vue-i18n";
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useRoute, useRouter } from "vue-router";
import { ref, computed } from "vue";

import { URL_BENEFICIARY_ADMIN } from "@/lib/consts/urls";

import { useNotificationsStore } from "@/lib/store/notifications";

import { dateUtc } from "@/lib/helpers/date";
import { getMoneyFormat } from "@/lib/helpers/money";

import SubscriptionConflict from "@/components/beneficiaries/subscription-conflict";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { addSuccess } = useNotificationsStore();

const subscriptionChecked = ref([]);

const { result: resultBeneficiary } = useQuery(
  gql`
    query Beneficiary($id: ID!) {
      beneficiary(id: $id) {
        id
        firstname
        lastname
        organization {
          id
          budgetAllowances {
            id
            availableFund
            subscription {
              id
            }
          }
        }
        card {
          id
          addingFundTransactions {
            id
            ... on SubscriptionAddingFundTransactionGraphType {
              subscription {
                id
                subscription {
                  id
                }
              }
            }
          }
        }
        ... on BeneficiaryGraphType {
          beneficiaryType {
            id
            name
          }
          beneficiarySubscriptions {
            beneficiaryType {
              id
              name
            }
            subscription {
              id
              name
              endDate
              maxNumberOfPayments
              paymentRemaining
              types {
                id
                amount
                productGroup {
                  id
                  name
                  color
                  orderOfAppearance
                }
                beneficiaryType {
                  id
                }
              }
            }
          }
        }
      }
    }
  `,
  {
    id: route.params.beneficiaryId
  }
);
const beneficiary = useResult(resultBeneficiary);

const { mutate: adjustBeneficiarySubscription } = useMutation(
  gql`
    mutation AdjustBeneficiarySubscription($input: AdjustBeneficiarySubscriptionInput!) {
      adjustBeneficiarySubscription(input: $input) {
        beneficiary {
          id
          organization {
            id
            budgetAllowances {
              id
              availableFund
            }
          }
          ... on BeneficiaryGraphType {
            beneficiarySubscriptions {
              beneficiaryType {
                id
              }
            }
          }
        }
      }
    }
  `
);

const beneficiarySubscriptionsWithConflict = computed(() => {
  var subscriptions = [];
  if (beneficiary.value && beneficiary.value.beneficiarySubscriptions) {
    for (var i = 0; i < beneficiary.value.beneficiarySubscriptions.length; i++) {
      var beneficiarySubscription = beneficiary.value.beneficiarySubscriptions[i];
      if (
        dateUtc(beneficiarySubscription.subscription.endDate) < new Date() ||
        beneficiarySubscription.beneficiaryType.id === beneficiary.value.beneficiaryType.id
      ) {
        continue;
      }

      const previousPaymentAmount = getAmountPayment(
        beneficiarySubscription.subscription,
        beneficiarySubscription.beneficiaryType.id
      );
      const newPaymentAmount = getAmountPayment(beneficiarySubscription.subscription, beneficiary.value.beneficiaryType.id);
      const paymentReceived = getSubscriptionPaymentReceivedCount(beneficiarySubscription.subscription.id);
      const paymentRemaining = beneficiarySubscription.subscription.paymentRemaining;

      const numberOfPaymentToReceive = Math.min(
        beneficiarySubscription.subscription.maxNumberOfPayments !== null
          ? beneficiarySubscription.subscription.maxNumberOfPayments - paymentReceived
          : paymentRemaining,
        paymentRemaining
      );

      var description = "";
      if (previousPaymentAmount > newPaymentAmount) {
        description = t("adjustment-amount-positif", {
          amount: getMoneyFormat((previousPaymentAmount - newPaymentAmount) * numberOfPaymentToReceive)
        });
      } else {
        description = t("adjustment-amount-negatif", {
          amount: getMoneyFormat((newPaymentAmount - previousPaymentAmount) * numberOfPaymentToReceive)
        });
      }

      const budgetAllowanceSubscription = beneficiary.value.organization.budgetAllowances.find(
        (budgetAllowance) => budgetAllowance.subscription.id === beneficiarySubscription.subscription.id
      );

      subscriptions.push({
        label: beneficiarySubscription.subscription.name,
        value: beneficiarySubscription.subscription.id,
        previousPaymentAmount,
        newPaymentAmount,
        description,
        numberOfPaymentToReceive,
        budgetAllowanceAvailableFund: budgetAllowanceSubscription.availableFund,
        disabled:
          budgetAllowanceSubscription.availableFund + (previousPaymentAmount - newPaymentAmount) * numberOfPaymentToReceive <= 0,
        previousCategoryName: beneficiarySubscription.beneficiaryType.name,
        previousCategoryAmount: previousPaymentAmount,
        newCategoryName: beneficiary.value.beneficiaryType.name,
        newCategoryAmount: newPaymentAmount
      });
    }
  }

  return subscriptions;
});

function getAmountPayment(subscription, beneficiaryTypeId) {
  var amount = 0;
  for (var i = 0; i < subscription.types.length; i++) {
    var type = subscription.types[i];
    if (type.beneficiaryType.id === beneficiaryTypeId) {
      amount += type.amount;
    }
  }
  return amount;
}

function getSubscriptionPaymentReceivedCount(subscriptionId) {
  var paymentReceivedCount = 0;

  if (beneficiary.value && beneficiary.value.card && beneficiary.value.card.addingFundTransactions) {
    paymentReceivedCount = beneficiary.value.card.addingFundTransactions.reduce((count, transaction) => {
      if (
        transaction.subscription !== null &&
        transaction.subscription !== undefined &&
        transaction.subscription.subscription.id === subscriptionId
      ) {
        return count + 1;
      }
      return count;
    }, 0);
  }

  return paymentReceivedCount;
}

function onSubscriptionConflictChecked(input) {
  if (input.isChecked) {
    subscriptionChecked.value.push(input.value);
  } else {
    subscriptionChecked.value = subscriptionChecked.value.filter((id) => id !== input.value);
  }
}

function onSubscriptionConflictCheckAll(checked) {
  if (checked) {
    subscriptionChecked.value = beneficiarySubscriptionsWithConflict.value
      .filter((subscription) => !subscription.disabled)
      .map((subscription) => subscription.value);
  } else {
    subscriptionChecked.value = [];
  }
}

async function onSubmit() {
  await adjustBeneficiarySubscription({
    input: {
      beneficiaryId: route.params.beneficiaryId,
      subscriptionIds: subscriptionChecked.value
    }
  });
  addSuccess(t("confirm-message", { beneficiary: `${beneficiary.value.firstname} ${beneficiary.value.lastname}` }));
  router.push({ name: URL_BENEFICIARY_ADMIN });
}
</script>
