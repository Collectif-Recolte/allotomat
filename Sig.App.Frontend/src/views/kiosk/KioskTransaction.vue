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
    class="flex flex-col justify-center min-h-[var(--kiosk-content-min-height)] px-6 xs:px-8 sm:px-12 py-12 w-full max-w-5xl mx-auto">
    <div v-if="!isConfirmStep" class="flex flex-col xs:flex-row xs:items-center xs:justify-between gap-y-4 gap-x-8 mb-6 sm:mb-8">
      <h1 class="font-bold text-d2 text-primary-700 shrink-0 my-0 flex flex-wrap items-baseline gap-4 leading-none">
        {{ t("purchase-title") }}
        <template v-if="cardProgramCardId !== ''">
          <span class="text-d7 text-grey-500 font-normal relative -top-1">
            <!-- eslint-disable-next-line @intlify/vue-i18n/no-raw-text -->
            <span class="mr-1">|</span>
            {{ t("card-number", { cardProgramCardId }) }}
          </span>
        </template>
      </h1>
      <!-- eslint-disable-next-line vue/no-v-html @intlify/vue-i18n/no-v-html -->
      <p class="text-d6 text-primary-700 xs:text-right xs:max-w-md leading-snug mb-0" v-html="t('instruction')" />
    </div>
    <div class="bg-white rounded-4xl shadow-lg">
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
    class="flex flex-1 flex-col items-center justify-center px-6 xs:px-8 sm:px-12 py-16 w-full">
    <div class="bg-white rounded-4xl shadow-lg p-8 pt-0 xs:p-12 xs:pt-0 w-full max-w-2xl relative">
      <UiIconComplete />
      <CompleteTransaction :transaction-id="transactionId" is-kiosk @finished="goHome" />
    </div>
  </div>
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
  onCancel: goHome,
  idleTimeoutMode: () => (activeStep.value === KIOSK_STEP_COMPLETE ? "purchase-complete" : "idle")
});
</script>
