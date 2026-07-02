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
    class="flex flex-1 flex-col items-center justify-center min-h-0 p-4 sm:p-8 w-full">
    <h1 class="font-semibold text-h2 sm:text-h1 text-primary-900 mb-6 text-center">{{ t("title") }}</h1>
    <QRCodeScanner
      kiosk-mode
      :error-url-const="URL_KIOSK_TRANSACTION_ERROR"
      @triggerError="activeStep = CHECK_CARD_STEPS_SCAN"
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
import { useRouter, useRoute } from "vue-router";
import { useI18n } from "vue-i18n";
import { useApolloClient } from "@vue/apollo-composable";

import { usePageTitle } from "@/lib/helpers/page-title";
import { useKioskShellState } from "@/lib/composables/use-kiosk-shell";
import { useKioskToken } from "@/lib/composables/use-kiosk-token";
import { URL_KIOSK_HOME, URL_KIOSK_TRANSACTION, URL_KIOSK_TRANSACTION_ERROR } from "@/lib/consts/urls";
import { CHECK_CARD_STEPS_SCAN, CHECK_CARD_STEPS_COMPLETE } from "@/lib/consts/enums";
import {
  CARD_CANT_BE_USED_IN_MARKET,
  CARD_NOT_FOUND,
  CARD_DEACTIVATED,
  CARD_CANT_BE_USED_WITH_CASH_REGISTER,
  KIOSK_ACCESS_INVALID
} from "@/lib/consts/qr-code-error";

import QRCodeScanner from "@/components/transaction/qr-code-scanner.vue";
import KioskBalance from "@/views/kiosk/KioskBalance";

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
const { resolveClient } = useApolloClient();
const client = resolveClient();

usePageTitle(t("title"));

const { authToken, kioskRoute, handleKioskAuthError } = useKioskToken();

const activeStep = ref(CHECK_CARD_STEPS_SCAN);
const cardId = ref("");
const loading = ref(false);

function routeToKioskError(error) {
  router.push({
    name: URL_KIOSK_TRANSACTION_ERROR,
    params: { token: route.params.token },
    query: { error }
  });
}

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
      }
    });

    if (result.data.verifyCardCanBeUsedInKiosk) {
      cardId.value = id;
      activeStep.value = CHECK_CARD_STEPS_COMPLETE;
    }
  } catch (exception) {
    const message = exception.message || "";
    if (message.indexOf(KIOSK_ACCESS_INVALID) !== -1) {
      handleKioskAuthError();
      return;
    }
    if (message.indexOf(CARD_CANT_BE_USED_WITH_CASH_REGISTER) !== -1) {
      routeToKioskError(CARD_CANT_BE_USED_WITH_CASH_REGISTER);
    } else if (message.indexOf(CARD_CANT_BE_USED_IN_MARKET) !== -1) {
      routeToKioskError(CARD_CANT_BE_USED_IN_MARKET);
    } else if (message.indexOf(CARD_NOT_FOUND) !== -1) {
      routeToKioskError(CARD_NOT_FOUND);
    } else if (message.indexOf(CARD_DEACTIVATED) !== -1) {
      routeToKioskError(CARD_DEACTIVATED);
    }
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
