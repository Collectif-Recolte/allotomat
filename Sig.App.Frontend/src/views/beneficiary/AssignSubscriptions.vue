<i18n>
{
	"en": {
		"assign-subscriptions": "Assign subscriptions",
		"title-edit": "Assign subscriptions",
    "title-confirm": "Do we confirm the selection?",
    "warning-message": "The amounts will be distributed in order according to the selected filters.",
    "warning-message-random": "The amounts will be distributed randomly according to the selected filters.",
    "select-subscription": "Susbscription selection",
    "choose-subscription": "Choose a subscription",
    "allowance": "Amount allocated to the assignation",
    "available-amount": "Remaining budget allowance",
    "no-available-subscription": "There are no subscriptions left to assign for your selection.",
    "subscription-count": "{count} participant will be assigned the subscription {subscriptionName}. | {count} participant will be assigned the subscription {subscriptionName}. | {count} participants will be assigned the subscription {subscriptionName}.",
    "usage-amount": "will be allocated.",
    "remaining-amount": "The remaining amount will be:",
    "edit": "Cancel",
    "submit": "Yes, assign subscriptions",
    "success-assign-beneficiaries-to-subscription": "The subscription \"{subscriptionName}\" was successfully assigned to {assignedBeneficiariesCount} participants out of the {totalBeneficiariesCount} selected.",
    "attribution-method": "Distribution of amounts",
    "random-attribution": "Random distribution",
    "in-order": "In order (top to bottom)",
    "random": "Random",
	},
	"fr": {
		"assign-subscriptions": "Attribuer des abonnements",
		"title-edit": "Attribuer des abonnements",
    "title-confirm": "On confirme la sélection ?",
    "warning-message": "Les montants seront distribués dans l’ordre selon les filtres sélectionnés.",
    "warning-message-random": "Les montants seront distribués aléatoirement selon les filtres sélectionnés.",
    "select-subscription": "Sélection de l'abonnement",
    "choose-subscription": "Choisir un abonnement",
    "allowance": "Montant alloué à l'attribution",
    "available-amount": "Enveloppe restante",
    "no-available-subscription": "Il ne reste aucun abonnement à assigner pour votre sélection.",
    "subscription-count": "{count} participant-e se verra attribuer l’abonnement {subscriptionName}. | {count} participant-e se verra attribuer l’abonnement {subscriptionName}. | {count} participant-es se verront attribuer l’abonnement {subscriptionName}.",
    "usage-amount": "seront alloués.",
    "remaining-amount": "L'enveloppe restante sera de:",
    "edit": "Annuler",
    "submit": "Oui, attribuer les abonnements",
    "success-assign-beneficiaries-to-subscription": "L'abonnement «{subscriptionName}» a été assigné avec succès à {assignedBeneficiariesCount} participant-e-s sur les {totalBeneficiariesCount} sélectionnés.",
    "attribution-method": "Distribution des montants",
    "random-attribution": "Distribution aléatoire",
    "in-order": "Dans l'ordre (de haut en bas)",
    "random": "Aléatoire",
  }
}
</i18n>

<template>
  <UiDialogModal
    v-if="subscriptions && currentStep === 0"
    v-slot="{ closeModal }"
    :title="t('title-edit')"
    :has-footer="subscriptionAvailable ? false : true"
    :return-route="{ name: URL_BENEFICIARY_ADMIN }">
    <Form
      v-if="subscriptionAvailable"
      v-slot="{ errors: formErrors, values }"
      :validation-schema="currentSchema"
      keep-values
      @submit="nextStep">
      <PfForm
        has-footer
        :disable-submit="!forecast || Object.keys(formErrors).length > 0"
        :submit-label="t('assign-subscriptions')"
        :warning-message="randomAttribution ? t('warning-message-random') : t('warning-message')"
        @cancel="closeModal">
        <PfFormSection is-grid>
          <Field v-slot="{ field: inputField, errors: fieldErrors }" name="subscription">
            <PfFormInputSelect
              id="subscription"
              v-bind="inputField"
              :placeholder="t('choose-subscription')"
              :label="t('select-subscription')"
              :options="availableSubscriptionSelectOptions"
              col-span-class="sm:col-span-8"
              :errors="fieldErrors"
              @change="onSubscriptionChange" />
          </Field>
          <div v-if="values.subscription" class="sm:col-span-4 sm:pl-4">
            <span class="text-sm block font-semibold mb-1">{{ t("available-amount") }}</span>
            <span v-if="availableBudgetAllowance" class="text-3xl font-bold text-primary-700">
              {{ getShortMoneyFormat(availableBudgetAllowance) }}
            </span>
          </div>
          <div class="sm:col-span-6" :class="values.subscription ? 'visible' : 'invisible'">
            <Field v-slot="{ field: inputField, errors: fieldErrors }" name="allowance">
              <PfFormInputText
                id="allowance"
                v-bind="inputField"
                :label="t('allowance')"
                :disabled="!values.subscription"
                :errors="fieldErrors"
                input-type="number"
                min="0"
                @change="onAllowanceChange">
                <template #trailingIcon>
                  <UiDollarSign :errors="fieldErrors" />
                </template>
              </PfFormInputText>
            </Field>
          </div>
        </PfFormSection>

        <PfFormSection>
          <div>
            <p class="font-semibold text-primary-900 mb-3 text-sm">{{ t("attribution-method") }}</p>
            <UiSwitch v-model="randomAttribution" :label="t('random-attribution')">
              <template #left>
                <span class="mr-2 text-p3 font-semibold">{{ t("in-order") }}</span>
              </template>
              <template #right>
                <span class="ml-2 text-p3 font-semibold">{{ t("random") }}</span>
              </template>
            </UiSwitch>
          </div>
        </PfFormSection>
      </PfForm>
    </Form>
    <p v-else class="text-red-600">{{ t("no-available-subscription") }}</p>
  </UiDialogModal>

  <UiDialogWarningModal
    v-else-if="subscriptions && currentStep === 1"
    :title="t('title-confirm')"
    :cancel-button-label="t('edit')"
    :confirm-button-label="t('submit')"
    @goBack="prevStep"
    @confirm="nextStep">
    <template #description>
      <div>
        <p class="text-h1 font-bold text-primary-900">
          {{
            t(
              "subscription-count",
              {
                count: forecast.beneficiariesWhoGetSubscriptions,
                subscriptionName: activeSubscription.label
              },
              forecast.beneficiariesWhoGetSubscriptions
            )
          }}
        </p>
        <p class="text-primary-700">
          <span class="font-bold">{{ getUsageBudgetAllowance() }} </span>
          {{ t("usage-amount") }}
        </p>
        <p class="text-primary-700">
          {{ t("remaining-amount") }}
          <span class="font-bold"> {{ getRemainingBudgetAllowance() }}</span>
        </p>
      </div>
    </template>
  </UiDialogWarningModal>
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { computed, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { string, object, number } from "yup";

import { useNotificationsStore } from "@/lib/store/notifications";
import { useOrganizationStore } from "@/lib/store/organization";
import { getShortMoneyFormat } from "@/lib/helpers/money";
import { URL_BENEFICIARY_ADMIN } from "@/lib/consts/urls";
import { WITHOUT_SUBSCRIPTION, SORT_DEFAULT, SORT_RANDOM } from "@/lib/consts/enums";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { addSuccess } = useNotificationsStore();
const { currentOrganization } = useOrganizationStore();

const randomAttribution = ref(false);
const currentStep = ref(0);
const activeSubscription = ref({
  id: "",
  label: ""
});
const activeAllowance = ref(0);
const organizationId = currentOrganization;

const { result: resultSubscriptions } = useQuery(
  gql`
    query Organization($id: ID!) {
      organization(id: $id) {
        id
        budgetAllowancesTotal
        project {
          id
          beneficiaryTypes {
            id
            name
          }
          subscriptions {
            id
            name
            getLastDateToAssignBeneficiary
            budgetAllowances {
              id
              availableFund
              originalFund
              organization {
                id
              }
            }
          }
        }
      }
    }
  `,
  {
    id: organizationId
  }
);

let subscriptions = useResult(resultSubscriptions, null, (data) => {
  return data.organization?.project?.subscriptions.filter(
    (x) =>
      x.budgetAllowances != null &&
      x.budgetAllowances.find((y) => y.organization.id == organizationId) != undefined &&
      new Date(x.getLastDateToAssignBeneficiary) > new Date()
  );
});

// Find list of subscriptions' filtered ids
const filteredSubscriptionIds = computed(() => {
  if (!route.query.subscriptions) return [];
  let subscriptions = route.query.subscriptions.split(",");
  let index = subscriptions.indexOf(WITHOUT_SUBSCRIPTION);
  if (index !== -1) {
    subscriptions.splice(index, 1);
  }
  return subscriptions;
});

// Check if withoutSubscription filter is active
const withoutSubscription = computed(() => {
  if (!route.query.subscriptions) return false;
  return route.query.subscriptions.includes(WITHOUT_SUBSCRIPTION);
});

// Find list of categories' filtered ids
const filteredCategoryIds = computed(() => {
  if (!route.query.beneficiaryTypes) return [];
  let categories = route.query.beneficiaryTypes.split(",");
  return categories;
});

// Find text for filter
const searchText = computed(() => {
  return route.query.text ? route.query.text : "";
});

// Build select input options array
const subscriptionSelectOptions = computed(() => subscriptions.value?.map((x) => ({ label: x.name, value: x.id })));

// Remove filtered subscriptions from the list of options
const availableSubscriptionSelectOptions = computed(() => {
  if (!filteredSubscriptionIds.value) return subscriptionSelectOptions.value;
  let availableSubscr = subscriptionSelectOptions.value;
  for (let filter of filteredSubscriptionIds.value) {
    if (availableSubscr) {
      const subset = availableSubscr.filter((x) => x.value !== filter);
      availableSubscr = subset;
    }
  }
  return availableSubscr;
});

// Helper variable for subscription available state
const subscriptionAvailable = computed(
  () => availableSubscriptionSelectOptions.value && availableSubscriptionSelectOptions.value?.length > 0
);

// Update active subscription if selection changes
function onSubscriptionChange(e) {
  activeSubscription.value = {
    id: e.target.value,
    label: subscriptionSelectOptions.value.find((x) => x.value === e.target.value).label
  };
}

// Update active allowance if amount changes
function onAllowanceChange(e) {
  activeAllowance.value = e.target.value;
}

// Call forecast to predict how many beneficiaries could benefit from the allowance attributed
const { result: resultForecast } = useQuery(
  gql`
    query ForecastAssignBeneficiariesToSubscription(
      $amount: Int!
      $organizationId: ID!
      $subscriptionId: ID!
      $withCategories: [ID!]
      $withoutSubscription: Boolean!
      $withSubscriptions: [ID!]
      $searchText: String!
    ) {
      forecastAssignBeneficiariesToSubscription(
        amount: $amount
        organizationId: $organizationId
        subscriptionId: $subscriptionId
        withCategories: $withCategories
        withoutSubscription: $withoutSubscription
        withSubscriptions: $withSubscriptions
        searchText: $searchText
      ) {
        beneficiariesWhoGetSubscriptions
        totalBeneficiaries
        availableBudgetAfter
        usageOfBudget
      }
    }
  `,
  forecastVariables
);

function forecastVariables() {
  return {
    amount: parseFloat(activeAllowance.value) ?? 0,
    organizationId: organizationId,
    subscriptionId: activeSubscription.value.id,
    withCategories: filteredCategoryIds.value,
    withSubscriptions: filteredSubscriptionIds.value,
    withoutSubscription: withoutSubscription.value,
    searchText: searchText.value
  };
}

let forecast = useResult(resultForecast, null, (data) => {
  return data.forecastAssignBeneficiariesToSubscription;
});

// Available fund for active subscription
const availableBudgetAllowance = computed(() => {
  if (!activeSubscription.value.id) return null;
  return subscriptions.value
    .find((x) => x.id === activeSubscription.value.id)
    .budgetAllowances.find((y) => y.organization.id === organizationId).availableFund;
});

// Remaining fund after attribution
function getRemainingBudgetAllowance() {
  if (!forecast.value) return;
  return getShortMoneyFormat(forecast.value.availableBudgetAfter);
}

function getUsageBudgetAllowance() {
  if (!forecast.value) return;
  return getShortMoneyFormat(forecast.value.usageOfBudget);
}

// Form validation & steps management
const validationSchemas = computed(() => {
  return [
    object({
      subscription: string().label(t("select-subscription")).required(),
      allowance: number().label(t("allowance")).required().min(0).max(availableBudgetAllowance.value)
    })
  ];
});

const currentSchema = computed(() => {
  return validationSchemas.value[currentStep.value];
});

function nextStep(values) {
  if (currentStep.value === 1) {
    onSubmit(values);
    return;
  }
  currentStep.value++;
}

function prevStep() {
  if (currentStep.value <= 0) {
    return;
  }

  currentStep.value--;
}

// Send data to backend
const { mutate: assignBeneficiariesToSubscription } = useMutation(
  gql`
    mutation AssignBeneficiariesToSubscription($input: AssignBeneficiariesToSubscriptionInput!) {
      assignBeneficiariesToSubscription(input: $input) {
        availableBudgetAfter
        beneficiariesWhoGetSubscriptions
        totalBeneficiaries
        organization {
          id
          budgetAllowancesTotal
        }
      }
    }
  `
);

async function onSubmit() {
  await assignBeneficiariesToSubscription({
    input: {
      amount: parseFloat(activeAllowance.value),
      organizationId: organizationId,
      subscriptionId: activeSubscription.value.id,
      withCategories: filteredCategoryIds.value,
      withSubscriptions: filteredSubscriptionIds.value,
      withoutSubscription: withoutSubscription.value,
      sort: randomAttribution.value ? SORT_RANDOM : SORT_DEFAULT,
      searchText: searchText.value
    }
  });
  router.push({ name: URL_BENEFICIARY_ADMIN });
  addSuccess(
    t("success-assign-beneficiaries-to-subscription", {
      assignedBeneficiariesCount: forecast.value.beneficiariesWhoGetSubscriptions,
      totalBeneficiariesCount: forecast.value.totalBeneficiaries,
      subscriptionName: activeSubscription.value.label
    })
  );
}
</script>
