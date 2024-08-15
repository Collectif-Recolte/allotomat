<i18n>
  {
    "en": {
      "add-missed-payment-description": "Select missed payments to add",
      "budget-allowance-not-enought": "<b>The budget allowance is not sufficient to cover the additional payment</b>",
      "close":"Close",
      "budget-allowance-needed": "<b>{amountByPayment} $ required for assignment</b>",
      "submit": "Add missed payments",
      "add-missed-payment-success-notification": "The missed payments have been successfully added.",
      "assign-card-description": "This card <b>has no funds on it</b>, and will remain empty until the next payment date."
    },
    "fr": {
      "add-missed-payment-description": "Sélectionnez les paiements manqués à ajouter",
      "budget-allowance-not-enought": "<b>L'enveloppe budgétaire n'est pas suffisante pour couvrir le versement supplémentaire</b>",
      "close":"Fermer",
      "budget-allowance-needed": "<b>{amountByPayment} $ requis pour l'attribution</b>",
      "submit": "Ajouter des paiements manqués",
      "add-missed-payment-success-notification": "Les paiements manqués ont été ajoutés avec succès.",
      "assign-card-description": "Cette carte <b>n'a pas de fonds</b> et restera vide jusqu'à la prochaine date de paiement."
    }
  }
  </i18n>

<template>
  <p>{{ t("add-missed-payment-description") }}</p>
  <Form v-slot="{ isSubmitting }" @submit="onSubmit">
    <PfForm :processing="isSubmitting" :disable-submit="subscriptionChecked.length === 0" @cancel="closeModal">
      <PfFormFieldset :id="props.id" :name="props.id" :has-error-state="props.hasErrorState" :errors="props.errors">
        <div v-for="(option, index) in subscriptionsWithMissedPayment" :key="index">
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
            <PfButtonAction :is-disabled="subscriptionChecked.length === 0" :label="t('submit')" class="px-8" type="submit" />
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

const { mutate: addMissingPayments } = useMutation(
  gql`
    mutation AddMissingPayments($input: AddMissingPaymentsInput!) {
      addMissingPayments(input: $input) {
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

const subscriptionsWithMissedPayment = computed(() => {
  return props.subscriptions
    .filter((x) => {
      return x.hasMissedPayment;
    })
    .map((x) => {
      return {
        id: x.subscription.id,
        name: x.subscription.name,
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
  await addMissingPayments({
    input: {
      beneficiaryId: props.beneficiary.id,
      subscriptions: subscriptionChecked.value
    }
  });
  router.push({ name: URL_BENEFICIARY_ADMIN });
  addSuccess(t("add-missed-payment-success-notification"));
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
