<i18n>
{
  "en": {
    "title": "Change maximum number of payments",
    "select-subscription-label": "Subscription",
    "max-number-of-payments-label": "New maximum number of payments",
    "cancel": "Cancel",
    "submit": "Save changes",
    "additional-cost": "{amount} will be deducted from the budget allowance",
    "budget-allowance-available": "Remaining budget allowance after change: {amount}",
    "success-notification": "The maximum number of payments has been updated successfully.",
    "must-be-greater-than-current": "Must be greater than the current maximum ({current})",
    "must-be-a-number": "Must be a valid number"
  },
  "fr": {
    "title": "Modifier le nombre maximum de paiements",
    "select-subscription-label": "Abonnement",
    "max-number-of-payments-label": "Nouveau nombre maximum de paiements",
    "cancel": "Annuler",
    "submit": "Enregistrer les modifications",
    "additional-cost": "{amount} seront déduits de l'enveloppe",
    "budget-allowance-available": "Enveloppe restante après modification : {amount}",
    "success-notification": "Le nombre maximum de paiements a été mis à jour avec succès.",
    "must-be-greater-than-current": "Doit être supérieur au maximum actuel ({current})",
    "must-be-a-number": "Doit être un nombre valide"
  }
}
</i18n>

<template>
  <UiDialogModal v-if="!loading" v-slot="{ closeModal }" :title="t('title')" :has-footer="false"
    :return-route="{ name: URL_BENEFICIARY_ADMIN }">
    <Form v-slot="{ isSubmitting, errors: formErrors, setFieldValue }" :validation-schema="validationSchema"
      @submit="onSubmit">
      <PfForm has-footer can-cancel
        :disable-submit="Object.keys(formErrors).length > 0 || budgetAllowanceAvailableAfterChange < 0"
        :submit-label="t('submit')" :cancel-label="t('cancel')" :processing="isSubmitting" @cancel="closeModal">
        <PfFormSection>
          <Field v-slot="{ field, errors: fieldErrors }" name="subscription">
            <PfFormInputSelect id="subscription" v-bind="field" :label="t('select-subscription-label')"
              :options="subscriptionOptions" :errors="fieldErrors"
              @input="(e) => onSubscriptionSelected(e, setFieldValue)" />
          </Field>
          <Field v-slot="{ field, errors: fieldErrors }" name="maxNumberOfPayments">
            <PfFormInputText id="maxNumberOfPayments" v-bind="field" input-type="number"
              :label="t('max-number-of-payments-label')" :disabled="selectedSubscription === ''" :errors="fieldErrors"
              @input="onMaxPaymentsInput" />
          </Field>
          <div v-if="selectedSubscription !== ''">
            <h2 class="my-2">{{ t("additional-cost", { amount: additionalCostMoneyFormat }) }}</h2>
            <p class="my-0" :class="{ 'text-red-500 font-bold': budgetAllowanceAvailableAfterChange < 0 }">
              {{ t("budget-allowance-available", { amount: budgetAllowanceAvailableAfterChangeMoneyFormat }) }}
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
import { object, string, number } from "yup";
import { Form, Field } from "vee-validate";

import { getMoneyFormat } from "@/lib/helpers/money";
import { subscriptionName } from "@/lib/helpers/subscription";
import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_BENEFICIARY_ADMIN } from "@/lib/consts/urls";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { addSuccess } = useNotificationsStore();

const selectedSubscription = ref("");
const newMaxNumberOfPayments = ref(0);

const validationSchema = computed(() => {
  const currentMax = selectedSubscriptionData.value?.currentMax ?? 0;
  return object({
    subscription: string().label(t("select-subscription-label")).required(),
    maxNumberOfPayments: number()
      .typeError(t("must-be-a-number"))
      .label(t("max-number-of-payments-label"))
      .required()
      .integer()
      .min(currentMax + 1, t("must-be-greater-than-current", { current: currentMax }))
  });
});

const { result: resultBeneficiary, loading } = useQuery(
  gql`
    query BeneficiaryForChangeMaxPayments($id: ID!) {
      beneficiary(id: $id) {
        id
        ... on BeneficiaryGraphType {
          beneficiaryType {
            id
          }
          organization {
            id
          }
          beneficiarySubscriptions {
            maxNumberOfPayments
            subscription {
              id
              name
              isArchived
              isSubscriptionPaymentBasedCardUsage
              types {
                id
                amount
                beneficiaryType {
                  id
                }
              }
              budgetAllowances {
                id
                availableFund
                organization {
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

const subscriptionOptions = useResult(resultBeneficiary, [], (data) => {
  return data.beneficiary.beneficiarySubscriptions
    .filter((x) => x.subscription.isSubscriptionPaymentBasedCardUsage && !x.subscription.isArchived)
    .map((x) => ({
      label: subscriptionName(x.subscription),
      value: x.subscription.id,
      currentMax: x.maxNumberOfPayments,
      types: x.subscription.types,
      budgetAllowance:
        x.subscription.budgetAllowances.find((b) => b.organization.id === data.beneficiary.organization.id)?.availableFund ?? 0
    }));
});

const selectedSubscriptionData = computed(() => {
  if (!selectedSubscription.value || !subscriptionOptions.value) return null;
  return subscriptionOptions.value.find((x) => x.value === selectedSubscription.value);
});

const { mutate: changeBeneficiarySubscriptionMaxNumberOfPaymentsMutation } = useMutation(
  gql`
    mutation ChangeBeneficiarySubscriptionMaxNumberOfPayments($input: ChangeBeneficiarySubscriptionMaxNumberOfPaymentsInput!) {
      changeBeneficiarySubscriptionMaxNumberOfPayments(input: $input) {
        beneficiary {
          id
        }
      }
    }
  `
);

async function onSubmit({ subscription, maxNumberOfPayments }) {
  await changeBeneficiarySubscriptionMaxNumberOfPaymentsMutation({
    input: {
      beneficiaryId: route.params.beneficiaryId,
      subscriptionId: subscription,
      maxNumberOfPayments: parseInt(maxNumberOfPayments, 10)
    }
  });
  addSuccess(t("success-notification"));
  router.push({ name: URL_BENEFICIARY_ADMIN });
}

function onSubscriptionSelected(value, setFieldValue) {
  selectedSubscription.value = value;
  const data = subscriptionOptions.value?.find((x) => x.value === value);
  if (data) {
    newMaxNumberOfPayments.value = data.currentMax;
    setFieldValue("maxNumberOfPayments", data.currentMax);
  }
}

function onMaxPaymentsInput(e) {
  const raw = e && e.target ? e.target.value : e;
  const parsed = parseInt(raw, 10);
  newMaxNumberOfPayments.value = isNaN(parsed) ? 0 : parsed;
}

const additionalCost = computed(() => {
  if (!selectedSubscriptionData.value || !beneficiary.value) return 0;
  const currentMax = selectedSubscriptionData.value.currentMax;
  const additional = Math.max(0, newMaxNumberOfPayments.value - currentMax);
  const amountPerPayment = selectedSubscriptionData.value.types
    .filter((type) => type.beneficiaryType.id === beneficiary.value.beneficiaryType.id)
    .reduce((sum, type) => sum + type.amount, 0);
  return additional * amountPerPayment;
});

const additionalCostMoneyFormat = computed(() => {
  return additionalCost.value > 0 ? getMoneyFormat(additionalCost.value) : "-";
});

const budgetAllowanceAvailableAfterChange = computed(() => {
  if (!selectedSubscriptionData.value) return 0;
  return selectedSubscriptionData.value.budgetAllowance - additionalCost.value;
});

const budgetAllowanceAvailableAfterChangeMoneyFormat = computed(() => {
  return getMoneyFormat(budgetAllowanceAvailableAfterChange.value);
});
</script>
