<i18n>
  {
    "en": {
      "title-new-transaction": "New transaction",
      "title-new-transaction-organization": "New transaction - {beneficiaryName}",
      "title-add-transaction": "Transaction"
    },
    "fr": {
      "title-new-transaction": "Nouvelle transaction",
      "title-new-transaction-organization": "Nouvelle transaction - {beneficiaryName}",
      "title-add-transaction": "Transaction"
    }
  }
  </i18n>

<template>
  <UiDialogModal
    v-slot="{ closeModal }"
    :title="title"
    :has-title="hasTitle"
    :has-footer="false"
    :return-route="{ name: userType === USER_TYPE_ORGANIZATIONMANAGER ? URL_BENEFICIARY_ADMIN : URL_TRANSACTION_ADMIN }">
    <template v-if="activeStep === TRANSACTION_STEPS_MANUALLY_ENTER_CARD_NUMBER">
      <SelectMarket v-if="userType === USER_TYPE_ORGANIZATIONMANAGER" @onUpdateStep="updateStep" @onCloseModal="closeModal" />
      <ManuallyEnterCardNumber
        v-else-if="userType === USER_TYPE_PROJECTMANAGER"
        @onUpdateStep="updateStep"
        @onCloseModal="closeModal" />
    </template>
    <AddTransaction
      v-else-if="activeStep === TRANSACTION_STEPS_ADD"
      :market-id="marketId"
      :card-id="cardId"
      @onUpdateStep="updateStep"
      @onCloseModal="closeModal" />
    <CompleteTransaction
      v-else-if="activeStep === TRANSACTION_STEPS_COMPLETE"
      :transaction-id="transactionId"
      @onUpdateStep="updateStep" />
  </UiDialogModal>
</template>

<script setup>
import { ref, computed, onMounted } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter, useRoute } from "vue-router";
import { useQuery } from "@vue/apollo-composable";
import { storeToRefs } from "pinia";
import gql from "graphql-tag";

import { usePageTitle } from "@/lib/helpers/page-title";
import { useAuthStore } from "@/lib/store/auth";

import {
  TRANSACTION_STEPS_ADD,
  TRANSACTION_STEPS_COMPLETE,
  TRANSACTION_STEPS_MANUALLY_ENTER_CARD_NUMBER,
  TRANSACTION_FINISH
} from "@/lib/consts/enums";
import { URL_TRANSACTION_ADMIN, URL_BENEFICIARY_ADMIN } from "@/lib/consts/urls";
import { USER_TYPE_ORGANIZATIONMANAGER, USER_TYPE_PROJECTMANAGER } from "@/lib/consts/enums";

import SelectMarket from "@/views/transaction/SelectMarket";
import ManuallyEnterCardNumber from "@/views/transaction/ManuallyEnterCardNumber";
import AddTransaction from "@/components/transaction/add-transaction";
import CompleteTransaction from "@/components/transaction/complete-transaction";

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
const { userType } = storeToRefs(useAuthStore());

onMounted(loadBeneficiary);

async function loadBeneficiary() {
  if (userType.value === USER_TYPE_ORGANIZATIONMANAGER) {
    const { onResult } = useQuery(
      gql`
        query Beneficiary($id: ID!) {
          beneficiary(id: $id) {
            id
            firstname
            lastname
            id1
            card {
              id
              cardNumber
            }
          }
        }
      `,
      {
        id: route.params.beneficiaryId
      }
    );
    onResult(async (queryResult) => {
      beneficiary.value = queryResult.data.beneficiary;
      cardId.value = queryResult.data.beneficiary.card.id;
      cardNumber.value = queryResult.data.beneficiary.card.cardNumber.replaceAll("-", " ");
    });
  }
}

const activeStep = ref(TRANSACTION_STEPS_MANUALLY_ENTER_CARD_NUMBER);
const cardId = ref("");
const cardNumber = ref("");
const transactionId = ref("");
const marketId = ref("");
const beneficiary = ref(null);

const title = computed(() => {
  if (activeStep.value === TRANSACTION_STEPS_MANUALLY_ENTER_CARD_NUMBER) {
    if (userType.value === USER_TYPE_ORGANIZATIONMANAGER && beneficiary.value !== null) {
      return t("title-new-transaction-organization", {
        beneficiaryName: `${beneficiary.value.firstname} ${beneficiary.value.lastname}`
      });
    } else {
      return t("title-new-transaction");
    }
  }
  if (activeStep.value === TRANSACTION_STEPS_ADD) return t("title-add-transaction");
  return "";
});

const hasTitle = computed(() => {
  return activeStep.value !== TRANSACTION_STEPS_COMPLETE;
});

usePageTitle(title);

const updateStep = (currentStep, values) => {
  switch (currentStep) {
    case TRANSACTION_STEPS_ADD:
      marketId.value = values.marketId;
      if (values.cardNumber !== undefined) {
        cardNumber.value = values.cardNumber.replaceAll("-", " ");
      }
      if (values.cardId !== undefined) {
        cardId.value = values.cardId;
      }
      activeStep.value = TRANSACTION_STEPS_ADD;
      break;
    case TRANSACTION_STEPS_COMPLETE:
      transactionId.value = values.transactionId;
      activeStep.value = TRANSACTION_STEPS_COMPLETE;
      break;
    case TRANSACTION_FINISH:
      router.push({
        name: userType.value === USER_TYPE_ORGANIZATIONMANAGER ? URL_BENEFICIARY_ADMIN : URL_TRANSACTION_ADMIN
      });
      break;
  }
};
</script>
