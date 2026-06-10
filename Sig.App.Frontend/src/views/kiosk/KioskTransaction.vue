<i18n>
{
  "en": {
    "title": "Make a purchase",
    "invalid-link": "This link is invalid or has expired."
  },
  "fr": {
    "title": "Faire un achat",
    "invalid-link": "Ce lien est invalide ou a expiré."
  }
}
</i18n>

<template>
  <KioskShell :loading="loading || kioskLoading" :show-cancel="activeStep !== KIOSK_STEP_COMPLETE" @cancel="goHome">
    <div v-if="!kioskLoading && !isValid" class="flex flex-1 items-center justify-center p-8 text-h3 text-center">
      {{ t("invalid-link") }}
    </div>
    <template v-else-if="isValid">
      <KioskScan
        v-if="activeStep === KIOSK_STEP_SCANNING"
        :kiosk-token="token"
        :heading="t('title')"
        transaction-route-name="kiosk-transaction-url"
        @scanned="onScanned"
        @cancel="goHome" />
      <div v-else-if="activeStep === KIOSK_STEP_ADD" class="py-5 px-4 xs:px-8">
        <div class="bg-white rounded-2xl pt-6 pb-3 px-3 xs:p-6 max-w-lg mx-auto">
          <h1 class="font-semibold mb-4 text-h2">{{ t("title") }}</h1>
          <AddTransaction
            :card-id="cardId"
            :kiosk-token="token"
            is-kiosk
            @onUpdateStep="onUpdateStep"
            @onUpdateLoadingState="loading = $event"
            @onCloseModal="goHome" />
        </div>
      </div>
      <div v-else-if="activeStep === KIOSK_STEP_COMPLETE" class="min-h-app px-4 py-10 flex items-center justify-center">
        <div class="bg-white rounded-2xl pt-6 pb-3 px-3 xs:p-6 w-full max-w-lg">
          <UiIconComplete />
          <CompleteTransaction :transaction-id="transactionId" is-kiosk @finished="goHome" />
        </div>
      </div>
    </template>
    <RouterView />
  </KioskShell>
</template>

<script setup>
import { ref } from "vue";
import { useRouter } from "vue-router";
import { useI18n } from "vue-i18n";

import { usePageTitle } from "@/lib/helpers/page-title";
import { useKioskToken } from "@/lib/composables/use-kiosk-token";
import { URL_KIOSK_HOME } from "@/lib/consts/urls";
import { TRANSACTION_STEPS_COMPLETE } from "@/lib/consts/enums";

import KioskShell from "@/components/app/kiosk-shell";
import KioskScan from "@/views/kiosk/KioskScan";
import AddTransaction from "@/components/transaction/add-transaction";
import CompleteTransaction from "@/components/transaction/complete-transaction";

const KIOSK_STEP_SCANNING = "scanning";
const KIOSK_STEP_ADD = "add";
const KIOSK_STEP_COMPLETE = "complete";

const { t } = useI18n();
const router = useRouter();
usePageTitle(t("title"));

const { token, loading: kioskLoading, isValid, kioskRoute } = useKioskToken();

const activeStep = ref(KIOSK_STEP_SCANNING);
const cardId = ref("");
const transactionId = ref("");
const loading = ref(false);

function onScanned(id) {
  cardId.value = id;
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
</script>
