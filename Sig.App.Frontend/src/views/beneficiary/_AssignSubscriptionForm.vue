<i18n>
  {
    "en": {
      "assign-subscription-description": "Even if this participant will not receive automated funds, they require a subscription in order for you to transfer any funds onto their card.",
      "assign-subscription-need-card": "This participant will not receive any funds unless a card is assigned to them.",
      "close":"Close"
    },
    "fr": {
      "assign-subscription-description": "Même si cette personne ne recevra pas de fonds automatisés, elle a besoin d'un abonnement pour pouvoir transférer des fonds sur sa carte.",
      "assign-subscription-need-card": "Cette personne ne recevra aucun fonds à moins qu'une carte ne lui soit attribuée.",
      "close":"Fermer",
    }
  }
  </i18n>

<template>
  <p>{{ t("assign-subscription-description") }}</p>
  <Form v-slot="{ isSubmitting }" @submit="onSubmit">
    <PfForm :processing="isSubmitting" :disable-submit="subscriptionChecked.length === 0" @cancel="closeModal">
      <AssignSubscription
        v-if="subscriptionsOrderByDate.length > 0"
        id="subscriptionConflict"
        :value="subscriptionChecked"
        :options="subscriptionsOrderByDate"
        :beneficiary="beneficiary"
        @input="onSubscriptionConflictChecked"
        @checkAll="onSubscriptionConflictCheckAll" />
      <template #footer>
        <div class="pt-5">
          <p>{{ t("assign-subscription-need-card") }}</p>
          <div class="flex gap-x-6 items-center justify-end">
            <PfButtonAction btn-style="link" :label="t('close')" @click="closeModal" />
            <PfButtonAction :is-disabled="subscriptionChecked.length === 0" :label="submitBtn" class="px-8" type="submit" />
            <PfButtonAction
              v-if="manageCards"
              :is-disabled="subscriptionChecked.length === 0"
              :label="nextStepBtn"
              class="px-8"
              type="submit"
              @click="onNextStepBtn" />
          </div>
        </div>
      </template>
    </PfForm>
  </Form>
</template>

<script setup>
import gql from "graphql-tag";
import { defineProps } from "vue";
import { useI18n } from "vue-i18n";
import { useQuery, useResult } from "@vue/apollo-composable";
import { ref, computed, defineEmits } from "vue";
import { storeToRefs } from "pinia";

import { useAuthStore } from "@/lib/store/auth";

import { GLOBAL_MANAGE_CARDS } from "@/lib/consts/permissions";

import AssignSubscription from "@/components/beneficiaries/assign-subscription";

const { t } = useI18n();
const emit = defineEmits(["submit", "closeModal", "nextStep"]);
const { getGlobalPermissions } = storeToRefs(useAuthStore());

const props = defineProps({
  beneficiaryId: {
    type: String,
    required: true
  },
  submitBtn: {
    type: String,
    required: true
  },
  nextStepBtn: {
    type: String,
    required: true
  },
  organizationId: {
    type: String,
    required: true
  }
});

const manageCards = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_CARDS);
});

const { result: resultBeneficiary } = useQuery(
  gql`
    query Beneficiary($id: ID!) {
      beneficiary(id: $id) {
        id
        organization {
          id
        }
        ... on BeneficiaryGraphType {
          beneficiaryType {
            id
          }
        }
      }
    }
  `,
  {
    id: props.beneficiaryId
  }
);
const beneficiary = useResult(resultBeneficiary);

const { result } = useQuery(
  gql`
    query Organization($id: ID!) {
      organization(id: $id) {
        project {
          id
          subscriptions {
            id
            name
            monthlyPaymentMoment
            startDate
            endDate
            maxNumberOfPayments
            isSubscriptionPaymentBasedCardUsage
            isFundsAccumulable
            fundsExpirationDate
            numberDaysUntilFundsExpire
            paymentRemaining
            totalPayment
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
  `,
  {
    id: props.organizationId
  }
);

const organization = useResult(result);
const subscriptionChecked = ref([]);
const isNextStepBtnClicked = ref(false);

const subscriptionsOrderByDate = computed(() => {
  if (organization.value === undefined || beneficiary.value === null) {
    return [];
  }
  let subscriptions = [...organization.value.project.subscriptions];

  subscriptions.sort((a, b) => {
    const dateA = a.isFundsAccumulable ? new Date(a.fundsExpirationDate) : new Date(a.endDate);
    const dateB = b.isFundsAccumulable ? new Date(b.fundsExpirationDate) : new Date(b.endDate);
    return dateB - dateA;
  });

  return subscriptions.map((subscription) => {
    return {
      id: subscription.id,
      name: subscription.name,
      monthlyPaymentMoment: subscription.monthlyPaymentMoment,
      startDate: subscription.startDate,
      endDate: subscription.endDate,
      maxNumberOfPayments: subscription.maxNumberOfPayments,
      isSubscriptionPaymentBasedCardUsage: subscription.isSubscriptionPaymentBasedCardUsage,
      isFundsAccumulable: subscription.isFundsAccumulable,
      fundsExpirationDate: subscription.fundsExpirationDate,
      numberDaysUntilFundsExpire: subscription.numberDaysUntilFundsExpire,
      dontHaveBudgetAllowance: !subscription.budgetAllowances.some((budgetAllowance) => {
        return budgetAllowance.organization.id === beneficiary.value.organization.id;
      }),
      dontHaveBeneficiaryType: !subscription.types.some((type) => {
        return type.beneficiaryType.id === beneficiary.value.beneficiaryType.id;
      }),
      paymentRemaining: subscription.paymentRemaining,
      totalPayment: subscription.totalPayment,
      types: subscription.types,
      budgetAllowances: subscription.budgetAllowances
    };
  });
});

function closeModal() {
  emit("closeModal");
}

async function onSubmit() {
  if (isNextStepBtnClicked.value) {
    emit("nextStep", subscriptionChecked.value);
  } else {
    emit("submit", subscriptionChecked.value);
  }
}

async function onNextStepBtn() {
  isNextStepBtnClicked.value = true;
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
    subscriptionChecked.value = subscriptionsOrderByDate.value
      .filter((subscription) => !subscription.dontHaveBudgetAllowance)
      .map((subscription) => subscription.id);
  } else {
    subscriptionChecked.value = [];
  }
}
</script>
