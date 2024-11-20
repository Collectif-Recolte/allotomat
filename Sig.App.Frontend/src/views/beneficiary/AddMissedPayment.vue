<i18n>
{
	"en": {
		"title": "Add a missed payment",
		"select-subscription-label": "Subscription",
		"cancel": "Cancel",
		"add-missed-payment": "Add missed payment",
		"subscription-label": "{name} - expires on {date}",
    "subscription-desc": "This will transfer the appropriate funds to the card according to the rules of the subscription. If the subscription has a maximum number of payments, this will count towards that maximum.",
    "amount-allocated": "{amount} will be allocated",
    "budget-allowance-available": "Remaining budget allowance after allocation: {amount}",
    "add-payment": "Add payment",
    "manually-add-missed-payment-success-notification": "The missed payment has been successfully added for {name} for an amount of {amount}."
	},
	"fr": {
		"title": "Versement d’un paiement manqué",
		"select-subscription-label": "Période d'abonnement",
		"cancel": "Annuler",
		"add-missed-payment": "Ajouter un paiement manqué",
		"subscription-label": "{name} - expire le {date}",
    "subscription-desc": "Cela transférera les fonds appropriés sur la carte conformément aux règles de l'abonnement. Si l'abonnement a un nombre maximum de paiements, celui-ci sera pris en compte dans le calcul de ce maximum.",
    "amount-allocated": "{amount} seront alloués",
    "budget-allowance-available": "Enveloppe restante après attribution: {amount}",
    "add-payment": "Ajouter le versement",
    "manually-add-missed-payment-success-notification": "Le paiement manqué a été ajouté avec succès pour {name} pour un montant de {amount}."
	}
}
</i18n>

<template>
  <UiDialogModal
    v-if="!loading"
    v-slot="{ closeModal }"
    :title="t('title')"
    :has-footer="false"
    :return-route="{ name: URL_BENEFICIARY_ADMIN }">
    <Form v-slot="{ isSubmitting, errors: formErrors }" :validation-schema="validationSchema" @submit="onSubmit">
      <PfForm
        has-footer
        can-cancel
        :disable-submit="
          Object.keys(formErrors).length > 0 || (selectedSubscription !== '' && budgetAllowanceAvailableAfterAllocation < 0)
        "
        :submit-label="t('add-payment')"
        :cancel-label="t('cancel')"
        :processing="isSubmitting"
        @cancel="closeModal">
        <PfFormSection>
          <Field v-slot="{ field, errors: fieldErrors }" name="subscription">
            <PfFormInputSelect
              id="subscription"
              v-bind="field"
              :label="t('select-subscription-label')"
              :options="subscriptionOptions"
              :description="t('subscription-desc')"
              :errors="fieldErrors"
              @input="onSubscriptionSelected" />
          </Field>
          <div v-if="selectedSubscription" class="flex flex-col">
            <h2 class="my-2">{{ t("amount-allocated", { amount: amountThatWillBeAllocatedMoneyFormat }) }}</h2>
            <p class="my-0" :class="{ 'text-red-500 font-bold': budgetAllowanceAvailableAfterAllocation < 0 }">
              {{ t("budget-allowance-available", { amount: budgetAllowanceAvailableAfterAllocationMoneyFormat }) }}
            </p>
          </div>
        </PfFormSection>
      </PfForm>
    </Form>
  </UiDialogModal>
</template>

<script setup>
import { ref, computed } from "vue";
import { useI18n } from "vue-i18n";
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useRoute, useRouter } from "vue-router";
import { object, string, lazy } from "yup";

import { formatDate, dateUtc, textualFormat } from "@/lib/helpers/date";
import { getMoneyFormat } from "@/lib/helpers/money";

import { useNotificationsStore } from "@/lib/store/notifications";

import { URL_BENEFICIARY_ADMIN } from "@/lib/consts/urls";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { addSuccess } = useNotificationsStore();

const selectedSubscription = ref("");

const validationSchema = computed(() =>
  object({
    subscription: lazy(() => {
      return string().label(t("select-subscription-label")).required();
    })
  })
);

const { result: resultBeneficiary } = useQuery(
  gql`
    query Beneficiary($id: ID!) {
      beneficiary(id: $id) {
        id
        ... on BeneficiaryGraphType {
          beneficiaryType {
            id
          }
          beneficiarySubscriptions {
            hasMissedPayment
            paymentReceived
            paymentRemaining
            missedPaymentCount
            maxNumberOfPayments
            subscription {
              id
              name
              budgetAllowancesTotal
              fundsExpirationDate
              isFundsAccumulable
              maxNumberOfPayments
              types {
                id
                amount
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

const beneficiary = useResult(resultBeneficiary, null, (data) => data.beneficiary);

const subscriptionOptions = useResult(resultBeneficiary, null, (data) => {
  return data.beneficiary.beneficiarySubscriptions
    .filter(
      (x) =>
        (x.hasMissedPayment && dateUtc(x.subscription.fundsExpirationDate) > Date.now()) ||
        x.subscription.fundsExpirationDate === null
    )
    .map((x) => {
      let label = "";

      if (x.subscription.fundsExpirationDate !== null) {
        label = t("subscription-label", {
          name: x.subscription.name,
          date: formatDate(dateUtc(x.subscription.fundsExpirationDate), textualFormat)
        });
      } else {
        label = x.subscription.name;
      }

      return {
        label: label,
        value: x.subscription.id,
        types: x.subscription.types,
        budgetAllowance: x.subscription.budgetAllowancesTotal,
        isBudgetAllowanceAlreadyAllocated: x.maxNumberOfPayments - x.paymentReceived <= x.paymentRemaining
      };
    })
    .reduce(function (a, b) {
      return a.concat(b);
    }, []);
});

const { mutate: addMissingPayment } = useMutation(
  gql`
    mutation AddMissingPayment($input: AddMissingPaymentInput!) {
      addMissingPayment(input: $input) {
        beneficiary {
          id
          firstname
          lastname
          ... on BeneficiaryGraphType {
            beneficiarySubscriptions {
              hasMissedPayment
              paymentReceived
            }
          }
        }
      }
    }
  `
);

async function onSubmit({ subscription }) {
  var result = await addMissingPayment({
    input: {
      beneficiaryId: route.params.beneficiaryId,
      subscriptionId: subscription
    }
  });
  router.push({ name: URL_BENEFICIARY_ADMIN });
  addSuccess(
    t("manually-add-missed-payment-success-notification", {
      name: `${result.data.addMissingPayment.beneficiary.firstname} ${result.data.addMissingPayment.beneficiary.lastname}`,
      amount: amountThatWillBeAllocatedMoneyFormat.value
    })
  );
}

function onSubscriptionSelected(e) {
  selectedSubscription.value = e;
}

const budgetAllowanceAvailableAfterAllocation = computed(() => {
  if (selectedSubscription.value === null) return 0;
  var selectedSubscriptionData = subscriptionOptions.value.find((x) => x.value === selectedSubscription.value);

  if (selectedSubscriptionData.isBudgetAllowanceAlreadyAllocated) {
    return selectedSubscriptionData.budgetAllowance;
  }

  return selectedSubscriptionData.budgetAllowance - amountThatWillBeAllocated.value;
});

const amountThatWillBeAllocatedMoneyFormat = computed(() => {
  let amount = amountThatWillBeAllocated.value;
  return amount !== 0 ? getMoneyFormat(amount) : "-";
});

const budgetAllowanceAvailableAfterAllocationMoneyFormat = computed(() => {
  var amount = budgetAllowanceAvailableAfterAllocation.value;
  return amount !== 0 ? getMoneyFormat(amount) : "-";
});

const amountThatWillBeAllocated = computed(() => {
  if (selectedSubscription.value === null) return 0;

  var selectedSubscriptionData = subscriptionOptions.value.find((x) => x.value === selectedSubscription.value);

  var amount = selectedSubscriptionData.types
    .filter((x) => beneficiary.value.beneficiaryType.id === x.beneficiaryType.id)
    .reduce((accumulator, type) => accumulator + type.amount, 0);

  return amount;
});
</script>
