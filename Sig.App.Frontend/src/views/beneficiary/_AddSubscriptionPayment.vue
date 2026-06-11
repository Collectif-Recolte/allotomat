<i18n>
  {
    "en": {
      "add-subscription-payment-description": "Select subscription payments to add per rules",
      "no-subscription-payment-description": "No subscription payments are available per rules. It is possible to manually add funds to the cards if required.",
      "budget-allowance-not-enought": "<b>The budget envelope is not sufficient to cover the additional payment</b>",
      "close":"Close",
      "budget-allowance-needed": "<b>{amountByPayment} $ required for assignment</b>",
      "submit": "Add subscription payments",
      "add-subscription-payment-success-notification": "The subscription payments have been successfully added.",
      "assign-card-description": "This card <b>has no funds on it</b>, and will remain empty until the next payment date."
    },
    "fr": {
      "add-subscription-payment-description": "Sélectionnez les versements à ajouter selon les règles de l'abonnement",
      "no-subscription-payment-description": "Aucun versement n'est possible selon les règles de l'abonnement. Il est possible d'ajouter manuellement des fonds sur les cartes si nécessaire.",
      "budget-allowance-not-enought": "<b>L'enveloppe budgétaire n'est pas suffisante pour couvrir le versement supplémentaire</b>",
      "close":"Fermer",
      "budget-allowance-needed": "<b>{amountByPayment} $ requis pour l'attribution</b>",
      "submit": "Ajouter les versements",
      "add-subscription-payment-success-notification": "Les versements selon les règles de l'abonnement ont été ajoutés avec succès.",
      "assign-card-description": "Cette carte <b>n'a pas de fonds</b> et restera vide jusqu'à la prochaine date de paiement."
    }
  }
  </i18n>

<template>
  <p v-if="subscriptionsWithAvailablePayment.length > 0">{{ t("add-subscription-payment-description") }}</p>
  <p v-else>{{ t("no-subscription-payment-description") }}</p>
  <Form v-slot="{ isSubmitting }" @submit="onSubmit">
    <PfForm :processing="isSubmitting" :disable-submit="subscriptionChecked.length === 0" @cancel="closeModal">
      <PfFormFieldset :id="props.id" :name="props.id" :has-error-state="props.hasErrorState" :errors="props.errors">
        <div v-for="(option, index) in subscriptionsWithAvailablePayment" :key="index">
          <PfFormInputCheckbox
            :value="isChecked(option.id)"
            :label="option.name"
            :checked="isChecked(option.id)"
            :disabled="!isBudgetAllowanceIsEnough(option)"
            @input="(e) => updateCheckbox(option.id, e)">
            <template #description>
              <!-- eslint-disable vue/no-v-html @intlify/vue-i18n/no-v-html -->
              <p
                v-if="!isBudgetAllowanceIsEnough(option)"
                class="mb-2 text-p2 leading-none text-red-500"
                v-html="t('budget-allowance-not-enought')"></p>
              <!-- eslint-disable vue/no-v-html @intlify/vue-i18n/no-v-html -->
              <p v-else class="mb-2 text-p2 leading-none" v-html="getBudgetAllowanceNeeded(option)"></p>
            </template>
          </PfFormInputCheckbox>
        </div>
      </PfFormFieldset>
      <template #footer>
        <div class="pt-5">
          <!-- eslint-disable vue/no-v-html @intlify/vue-i18n/no-v-html -->
          <p v-html="t('assign-card-description')"></p>
          <div class="flex gap-x-6 items-center justify-end">
            <PfButtonAction btn-style="link" :label="t('close')" @click="closeModal" />
            <PfButtonAction
              v-if="subscriptionsWithAvailablePayment.length > 0"
              :is-disabled="subscriptionChecked.length === 0"
              :label="t('submit')"
              class="px-8"
              type="submit" />
          </div>
        </div>
      </template>
    </PfForm>
  </Form>
</template>

<script setup>
import { defineProps } from "vue";
import { useI18n } from "vue-i18n";
import { ref, computed, defineEmits } from "vue";
import gql from "graphql-tag";
import { useRouter } from "vue-router";
import { useMutation } from "@vue/apollo-composable";
import { subscriptionName } from "@/lib/helpers/subscription";

import { useNotificationsStore } from "@/lib/store/notifications";

import { URL_BENEFICIARY_ADMIN } from "@/lib/consts/urls";

const { t } = useI18n();
const emit = defineEmits(["closeModal", "submit"]);
const router = useRouter();
const { addSuccess } = useNotificationsStore();

const props = defineProps({
  subscriptions: {
    type: Array,
    required: true,
    default() {
      return [];
    }
  },
  beneficiary: {
    type: Object,
    required: true
  }
});

const subscriptionChecked = ref([]);

const { mutate: addSubscriptionPayments } = useMutation(
  gql`
    mutation AddSubscriptionPayments($input: AddSubscriptionPaymentsInput!) {
      addSubscriptionPayments(input: $input) {
        beneficiary {
          id
          firstname
          lastname
          ... on BeneficiaryGraphType {
            beneficiarySubscriptions {
              canAddSubscriptionPayment
              paymentReceived
            }
          }
        }
      }
    }
  `
);

const subscriptionsWithAvailablePayment = computed(() => {
  return props.subscriptions
    .filter((x) => {
      return x.canAddSubscriptionPayment;
    })
    .map((x) => {
      return {
        id: x.subscription.id,
        name: subscriptionName(x.subscription),
        budgetAllowance: x.subscription.budgetAllowances.filter(
          (y) => y.organization.id === props.beneficiary.organization.id
        )[0],
        types: x.subscription.types
      };
    });
});

const isBudgetAllowanceIsEnough = (option) => {
  const amountByPayment = option.types
    .filter((x) => x.beneficiaryType.id === props.beneficiary.beneficiaryType.id)
    .reduce((acc, x) => acc + x.amount, 0);
  return amountByPayment <= option.budgetAllowance.availableFund;
};

const getBudgetAllowanceNeeded = (option) => {
  const amountByPayment = option.types
    .filter((x) => x.beneficiaryType.id === props.beneficiary.beneficiaryType.id)
    .reduce((acc, x) => acc + x.amount, 0);

  return t("budget-allowance-needed", { amountByPayment });
};

function closeModal() {
  emit("closeModal");
}

async function onSubmit() {
  await addSubscriptionPayments({
    input: {
      beneficiaryId: props.beneficiary.id,
      subscriptions: subscriptionChecked.value
    }
  });
  router.push({ name: URL_BENEFICIARY_ADMIN });
  addSuccess(t("add-subscription-payment-success-notification"));
}

function updateCheckbox(value, isChecked) {
  if (isChecked) {
    subscriptionChecked.value.push(value);
  } else {
    subscriptionChecked.value = subscriptionChecked.value.filter((id) => id !== value);
  }
}

function isChecked(value) {
  return subscriptionChecked.value.indexOf(value) !== -1;
}
</script>
