<i18n>
  {
    "en": {
      "title-new-transaction": "New transaction"
    },
    "fr": {
      "title-new-transaction": "Nouvelle transaction"
    }
  }
  </i18n>

<template>
  <UiDialogModal
    v-slot="{ closeModal }"
    :title="t('title-new-transaction')"
    :has-title="hasTitle"
    :has-footer="false"
    :return-route="{ name: route.params.beneficiaryId !== undefined ? URL_BENEFICIARY_ADMIN : URL_TRANSACTION_ADMIN }">
    <template v-if="activeStep === TRANSACTION_STEPS_MANUALLY_ENTER_CARD_NUMBER">
      <SelectMarket v-if="beneficiary" :market-id="marketId" @onUpdateStep="updateStep" @onCloseModal="closeModal" />
      <ManuallyEnterCardNumber
        v-else-if="userType === USER_TYPE_PROJECTMANAGER && route.params.beneficiaryId === undefined"
        @onUpdateStep="updateStep"
        @onCloseModal="closeModal" />
    </template>
    <AddTransaction
      v-else-if="activeStep === TRANSACTION_STEPS_ADD"
      :market-id="marketId"
      :cash-register-id="cashRegisterId"
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
import { useMarketStore } from "@/lib/store/market";
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
const { currentMarket, changeCurrentMarket } = useMarketStore();

onMounted(loadBeneficiary);
onMounted(loadMarket);

function loadMarket() {
  if (userType.value === USER_TYPE_PROJECTMANAGER) {
    marketId.value = currentMarket;
  }
}

async function loadBeneficiary() {
  if (route.params.beneficiaryId !== undefined) {
    const { onResult } = useQuery(
      gql`
        query Beneficiary($id: ID!) {
          beneficiary(id: $id) {
            id
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
const cashRegisterId = ref("");
const beneficiary = ref(null);

const hasTitle = computed(() => {
  return activeStep.value !== TRANSACTION_STEPS_COMPLETE;
});

usePageTitle(t("title-new-transaction"));

const updateStep = (currentStep, values) => {
  switch (currentStep) {
    case TRANSACTION_STEPS_ADD:
      marketId.value = values.marketId;
      cashRegisterId.value = values.cashRegisterId;
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
      if (userType.value === USER_TYPE_PROJECTMANAGER) {
        changeCurrentMarket(marketId.value);
      }
      router.push({
        name:
          userType.value === USER_TYPE_ORGANIZATIONMANAGER || userType.value === USER_TYPE_PROJECTMANAGER
            ? URL_BENEFICIARY_ADMIN
            : URL_TRANSACTION_ADMIN
      });
      break;
  }
};
</script>
