<i18n>
{
  "en": {
    "title": "Check my card balance"
  },
  "fr": {
    "title": "Vérifier le solde de ma carte"
  }
}
</i18n>

<template>
  <div
    v-if="activeStep === CHECK_CARD_STEPS_SCAN"
    class="flex flex-col items-center justify-center min-h-[var(--kiosk-content-min-height)] px-8 sm:px-12 py-12 w-full">
    <h1 class="font-bold text-d2 text-primary-700 mb-6 mt-0 text-center">{{ t("title") }}</h1>
    <QRCodeScanner
      ref="scannerRef"
      kiosk-mode
      :error-url-const="URL_KIOSK_TRANSACTION_ERROR"
      @checkQRCode="checkQRCode"
      @cancel="goHome" />
  </div>
  <KioskBalance
    v-else-if="activeStep === CHECK_CARD_STEPS_COMPLETE"
    :card-id="cardId"
    :kiosk-token="authToken"
    @finished="goHome"
    @startPurchase="goToPurchase"
    @onUpdateLoadingState="loading = $event"
    @kiosk-auth-error="handleKioskAuthError" />
</template>

<script setup>
import gql from "graphql-tag";
import { ref } from "vue";
import { useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import { useApolloClient } from "@vue/apollo-composable";

import { usePageTitle } from "@/lib/helpers/page-title";
import { useKioskShellState } from "@/lib/composables/use-kiosk-shell";
import { useKioskToken } from "@/lib/composables/use-kiosk-token";
import { URL_KIOSK_HOME, URL_KIOSK_TRANSACTION, URL_KIOSK_TRANSACTION_ERROR } from "@/lib/consts/urls";
import { CHECK_CARD_STEPS_SCAN, CHECK_CARD_STEPS_COMPLETE } from "@/lib/consts/enums";
import {
  KIOSK_ACCESS_INVALID
} from "@/lib/consts/qr-code-error";

import QRCodeScanner from "@/components/transaction/qr-code-scanner.vue";
import KioskBalance from "@/views/kiosk/KioskBalance";

const { t } = useI18n();
const router = useRouter();
const { resolveClient } = useApolloClient();
const client = resolveClient();

usePageTitle(t("title"));

const { authToken, kioskRoute, handleKioskAuthError } = useKioskToken();

const activeStep = ref(CHECK_CARD_STEPS_SCAN);
const cardId = ref("");
const loading = ref(false);
const scannerRef = ref(null);

async function checkQRCode(id) {
  try {
    const result = await client.query({
      query: gql`
        query VerifyCardCanBeUsedInKiosk($kioskToken: String!, $cardId: ID!) {
          verifyCardCanBeUsedInKiosk(kioskToken: $kioskToken, cardId: $cardId)
        }
      `,
      variables: {
        kioskToken: authToken.value,
        cardId: id
      },
      fetchPolicy: "network-only"
    });

    if (result.data.verifyCardCanBeUsedInKiosk) {
      cardId.value = id;
      activeStep.value = CHECK_CARD_STEPS_COMPLETE;
      return;
    }

    scannerRef.value?.showScanError();
  } catch (exception) {
    const message = exception.message || "";
    if (message.indexOf(KIOSK_ACCESS_INVALID) !== -1) {
      handleKioskAuthError();
      return;
    }

    scannerRef.value?.showScanError();
  }
}

function goHome() {
  router.push(kioskRoute(URL_KIOSK_HOME));
}

function goToPurchase() {
  router.push(kioskRoute(URL_KIOSK_TRANSACTION));
}

useKioskShellState({
  loading: () => loading.value,
  showCancel: () => activeStep.value !== CHECK_CARD_STEPS_COMPLETE,
  onCancel: goHome
});
</script>
