<i18n>
  {
    "en": {
      "title-new-transaction": "New Transaction"
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
    :return-route="{ name: URL_CARDS_MANAGE_GIFT_CARDS }">
    <ManuallyEnterCardNumber
      v-if="activeStep === TRANSACTION_STEPS_MANUALLY_ENTER_CARD_NUMBER"
      :card-number="cardNumber"
      @onUpdateStep="updateStep"
      @onCloseModal="closeModal" />
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
import { URL_CARDS_MANAGE_GIFT_CARDS } from "@/lib/consts/urls";
import { USER_TYPE_PROJECTMANAGER } from "@/lib/consts/enums";

import ManuallyEnterCardNumber from "@/views/transaction/ManuallyEnterCardNumber";
import AddTransaction from "@/components/transaction/add-transaction";
import CompleteTransaction from "@/components/transaction/complete-transaction";

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
const { userType } = storeToRefs(useAuthStore());
const { currentMarket, changeCurrentMarket } = useMarketStore();

onMounted(loadMarket);

function loadMarket() {
  if (userType.value === USER_TYPE_PROJECTMANAGER) {
    marketId.value = currentMarket;
  }
}

const { onResult: onResultCard } = useQuery(
  gql`
    query Card($id: ID!) {
      card(id: $id) {
        id
        cardNumber
      }
    }
  `,
  () => ({ id: route.params.cardId })
);
onResultCard((result) => {
  const card = result?.data?.card;
  if (card?.cardNumber) {
    cardNumber.value = card.cardNumber.replaceAll("-", " ");
  }
});

const activeStep = ref(TRANSACTION_STEPS_MANUALLY_ENTER_CARD_NUMBER);
const cardId = ref("");
const cardNumber = ref("");
const transactionId = ref("");
const marketId = ref("");
const cashRegisterId = ref("");

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
        name: URL_CARDS_MANAGE_GIFT_CARDS
      });
      break;
  }
};
</script>
