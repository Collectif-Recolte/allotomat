<i18n>
  {
    "en": {
      "title": "Create a transaction"
    },
    "fr": {
      "title": "Créer une transaction"
    }
  }
  </i18n>

<template>
  <RouterView v-slot="{ Component }">
    <AppShell :class="appShellClass" no-padding :is-dark="isDark" :loading="loading">
      <NewScan v-if="activeStep === TRANSACTION_STEPS_START" @onUpdateStep="updateStep" />
      <ScanQRCode v-else-if="activeStep === TRANSACTION_STEPS_SCAN" @onUpdateStep="updateStep" />
      <MarketManuallyEnterCardNumber
        v-else-if="activeStep === TRANSACTION_STEPS_MANUALLY_ENTER_CARD_NUMBER"
        @onUpdateStep="updateStep" />
      <AddTransaction
        v-else-if="activeStep === TRANSACTION_STEPS_ADD"
        :card-id="cardId"
        @onUpdateStep="updateStep"
        @onUpdateLoadingState="updateLoadingState" />
      <CompleteTransaction
        v-else-if="activeStep === TRANSACTION_STEPS_COMPLETE"
        :transaction-id="transactionId"
        @onUpdateStep="updateStep"
        @onUpdateLoadingState="updateLoadingState" />
      <Component :is="Component" />
    </AppShell>
  </RouterView>
</template>

<script setup>
import { ref, computed } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute } from "vue-router";

import { usePageTitle } from "@/lib/helpers/page-title";
import {
  TRANSACTION_STEPS_START,
  TRANSACTION_STEPS_SCAN,
  TRANSACTION_STEPS_ADD,
  TRANSACTION_STEPS_COMPLETE,
  TRANSACTION_STEPS_MANUALLY_ENTER_CARD_NUMBER,
  TRANSACTION_FINISH
} from "@/lib/consts/enums";

import ScanQRCode from "@/views/transaction/ScanQRCode";
import MarketManuallyEnterCardNumber from "@/views/transaction/MarketManuallyEnterCardNumber";
import AddTransaction from "@/views/transaction/Add";
import CompleteTransaction from "@/views/transaction/Complete";
import NewScan from "@/views/transaction/NewScan";

const route = useRoute();
const { t } = useI18n();

usePageTitle(t("title"));

const activeStep = ref(route.query.isScan ? TRANSACTION_STEPS_SCAN : TRANSACTION_STEPS_START);
const cardId = ref("");
const transactionId = ref("");
const loading = ref(false);

const isDark = computed(() => {
  if (activeStep.value === TRANSACTION_STEPS_ADD || activeStep.value === TRANSACTION_STEPS_COMPLETE) return true;
  return false;
});

const appShellClass = computed(() => {
  if (isDark.value) return "bg-primary-700";
  return "bg-primary-100 md:bg-white";
});

const updateStep = (stepName, values) => {
  activeStep.value = stepName;
  if (values.cardId) cardId.value = values.cardId;
  if (values.transactionId) transactionId.value = values.transactionId;
  if (stepName === TRANSACTION_STEPS_COMPLETE) loading.value = false;
  if (stepName === TRANSACTION_FINISH) {
    activeStep.value = TRANSACTION_STEPS_START;
    cardId.value = "";
    transactionId.value = "";
    loading.value = false;
  }
};

const updateLoadingState = (state) => {
  loading.value = state;
};
</script>
