<i18n>
  {
    "en": {
      "title-new-transaction": "New transaction",
      "title-add-transaction": "Transaction"
    },
    "fr": {
      "title-new-transaction": "Nouvelle transaction",
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
    :return-route="{ name: URL_TRANSACTION_ADMIN }">
    <ManuallyEnterCardNumber
      v-if="activeStep === TRANSACTION_STEPS_MANUALLY_ENTER_CARD_NUMBER"
      @onUpdateStep="updateStep"
      @onCloseModal="closeModal" />
    <AddTransaction
      v-else-if="activeStep === TRANSACTION_STEPS_ADD"
      :marketId="marketId"
      :cardId="cardId"
      @onUpdateStep="updateStep"
      @onCloseModal="closeModal" />
    <CompleteTransaction
      v-else-if="activeStep === TRANSACTION_STEPS_COMPLETE"
      :transactionId="transactionId"
      @onUpdateStep="updateStep" />
  </UiDialogModal>
</template>

<script setup>
import { ref, computed } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";

import { usePageTitle } from "@/lib/helpers/page-title";

import {
  TRANSACTION_STEPS_ADD,
  TRANSACTION_STEPS_COMPLETE,
  TRANSACTION_STEPS_MANUALLY_ENTER_CARD_NUMBER,
  TRANSACTION_FINISH
} from "@/lib/consts/enums";
import { URL_TRANSACTION_ADMIN } from "@/lib/consts/urls";

import ManuallyEnterCardNumber from "@/views/transaction/ManuallyEnterCardNumber";
import AddTransaction from "@/components/transaction/add-transaction";
import CompleteTransaction from "@/components/transaction/complete-transaction";

const { t } = useI18n();
const router = useRouter();

const activeStep = ref(TRANSACTION_STEPS_MANUALLY_ENTER_CARD_NUMBER);
const cardId = ref("");
const cardNumber = ref("");
const transactionId = ref("");
const marketId = ref("");

usePageTitle(title);

const updateStep = (currentStep, values) => {
  switch (currentStep) {
    case TRANSACTION_STEPS_ADD:
      marketId.value = values.marketId;
      cardNumber.value = values.cardNumber;
      cardId.value = values.cardId;
      activeStep.value = TRANSACTION_STEPS_ADD;
      break;
    case TRANSACTION_STEPS_COMPLETE:
      transactionId.value = values.transactionId;
      activeStep.value = TRANSACTION_STEPS_COMPLETE;
      break;
    case TRANSACTION_FINISH:
      router.push({
        name: URL_TRANSACTION_ADMIN
      });
      break;
  }
};

const title = computed(() => {
  if (activeStep.value === TRANSACTION_STEPS_MANUALLY_ENTER_CARD_NUMBER) return t("title-new-transaction");
  if (activeStep.value === TRANSACTION_STEPS_ADD) return t("title-add-transaction");
  return "";
});

const hasTitle = computed(() => {
  return activeStep.value !== TRANSACTION_STEPS_COMPLETE;
});
</script>
