<i18n>
{
  "en": {
    "title": "Make a purchase",
    "purchase-title": "Make a purchase",
    "card-number": "Card #{cardProgramCardId}",
    "instruction": "☝️ Enter the amount to debit from your <strong>Proximity Card</strong> for each product category."
  },
  "fr": {
    "title": "Faire un achat",
    "purchase-title": "Faire un achat",
    "card-number": "Carte #{cardProgramCardId}",
    "instruction": "☝️ Entrez le montant à débiter de votre <strong>Carte Proximité</strong> pour chaque catégorie de produits."
  }
}
</i18n>

<template>
  <KioskScan
    v-if="activeStep === KIOSK_STEP_SCANNING"
    :kiosk-token="authToken"
    :heading="t('title')"
    @scanned="onScanned"
    @cancel="goHome"
    @auth-error="handleKioskAuthError" />
  <div
    v-else-if="activeStep === KIOSK_STEP_ADD"
    class="flex flex-1 flex-col min-h-0 px-4 sm:px-8 py-6 w-full max-w-2xl md:max-w-4xl lg:max-w-5xl mx-auto">
    <div v-if="!isConfirmStep" class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-4 sm:mb-6">
      <h1 class="font-semibold text-h2 sm:text-h1 text-primary-900 shrink-0">
        {{ t("purchase-title") }}
        <template v-if="cardProgramCardId !== ''">
          <!-- eslint-disable-next-line @intlify/vue-i18n/no-raw-text -->
          <span class="font-normal text-h2 sm:text-h1 text-grey-500"> | {{ t("card-number", { cardProgramCardId }) }}</span>
        </template>
      </h1>
      <!-- eslint-disable-next-line vue/no-v-html @intlify/vue-i18n/no-v-html -->
      <p class="text-h4 sm:text-h3 text-primary-900 sm:text-right sm:max-w-lg leading-snug" v-html="t('instruction')" />
    </div>
    <div class="bg-white rounded-2xl shadow-sm p-4 xs:p-6 flex flex-col flex-1 min-h-0">
      <KioskAddTransaction
        :card-id="cardId"
        :kiosk-token="authToken"
        @onUpdateStep="onUpdateStep"
        @onUpdateLoadingState="loading = $event"
        @onCloseModal="goHome"
        @kiosk-auth-error="handleKioskAuthError"
        @card-loaded="cardProgramCardId = $event"
        @step-change="isConfirmStep = $event === 1" />
    </div>
  </div>
  <div
    v-else-if="activeStep === KIOSK_STEP_COMPLETE"
    class="flex flex-1 flex-col items-center justify-center px-4 sm:px-8 pt-12 sm:pt-16 pb-8 w-full">
    <div class="bg-white rounded-2xl shadow-sm px-4 xs:px-6 pt-12 sm:pt-14 pb-4 xs:pb-6 w-full max-w-2xl md:max-w-4xl lg:max-w-5xl relative">
      <UiIconComplete />
      <CompleteTransaction :transaction-id="transactionId" is-kiosk @finished="goHome" />
    </div>
  </div>
  <RouterView />
</template>

<script setup>
import { ref } from "vue";
import { useRouter } from "vue-router";
import { useI18n } from "vue-i18n";

import { usePageTitle } from "@/lib/helpers/page-title";
import { useKioskShellState } from "@/lib/composables/use-kiosk-shell";
import { useKioskToken } from "@/lib/composables/use-kiosk-token";
import { URL_KIOSK_HOME } from "@/lib/consts/urls";
import { TRANSACTION_STEPS_COMPLETE } from "@/lib/consts/enums";

import KioskScan from "@/views/kiosk/KioskScan";
import KioskAddTransaction from "@/components/kiosk/kiosk-add-transaction";
import CompleteTransaction from "@/components/transaction/complete-transaction";

const KIOSK_STEP_SCANNING = "scanning";
const KIOSK_STEP_ADD = "add";
const KIOSK_STEP_COMPLETE = "complete";

const { t } = useI18n();
const router = useRouter();
usePageTitle(t("title"));

const { authToken, kioskRoute, handleKioskAuthError } = useKioskToken();

const activeStep = ref(KIOSK_STEP_SCANNING);
const cardId = ref("");
const cardProgramCardId = ref("");
const isConfirmStep = ref(false);
const transactionId = ref("");
const loading = ref(false);

function onScanned(id) {
  cardId.value = id;
  cardProgramCardId.value = "";
  isConfirmStep.value = false;
  activeStep.value = KIOSK_STEP_ADD;
}

function onUpdateStep(stepName, values) {
  if (stepName === TRANSACTION_STEPS_COMPLETE && values.transactionId) {
    transactionId.value = values.transactionId;
    activeStep.value = KIOSK_STEP_COMPLETE;
    loading.value = false;
  }
}

function goHome() {
  router.push(kioskRoute(URL_KIOSK_HOME));
}

useKioskShellState({
  loading: () => loading.value,
  showCancel: () => activeStep.value !== KIOSK_STEP_COMPLETE,
  onCancel: goHome
});
</script>
