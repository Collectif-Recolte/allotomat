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
  <div v-if="activeStep === CHECK_CARD_STEPS_SCAN" class="text-center flex flex-col justify-center items-center p-8">
    <h1 class="font-semibold text-h2 text-primary-900 mb-6">{{ t("title") }}</h1>
    <QRCodeScanner
      kiosk-mode
      :error-url-const="URL_CARD_ERROR"
      @triggerError="activeStep = CHECK_CARD_STEPS_SCAN"
      @checkQRCode="checkQRCode"
      @cancel="goHome" />
  </div>
  <Balance
    v-else-if="activeStep === CHECK_CARD_STEPS_COMPLETE"
    :card-id="cardId"
    :kiosk-token="authToken"
    hide-transaction-list
    is-kiosk
    @finished="goHome"
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
import { URL_KIOSK_HOME, URL_CARD_ERROR, URL_KIOSK_TRANSACTION_ERROR } from "@/lib/consts/urls";
import { CHECK_CARD_STEPS_SCAN, CHECK_CARD_STEPS_COMPLETE } from "@/lib/consts/enums";
import {
  CARD_CANT_BE_USED_IN_MARKET,
  CARD_NOT_FOUND,
  CARD_DEACTIVATED,
  CARD_CANT_BE_USED_WITH_CASH_REGISTER,
  KIOSK_ACCESS_INVALID
} from "@/lib/consts/qr-code-error";

import QRCodeScanner from "@/components/transaction/qr-code-scanner.vue";
import Balance from "@/views/card/Balance";

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
      router.push({
        name: URL_KIOSK_TRANSACTION_ERROR,
        params: { token: route.params.token },
        query: { error: CARD_CANT_BE_USED_WITH_CASH_REGISTER }
      });
    } else if (message.indexOf(CARD_CANT_BE_USED_IN_MARKET) !== -1) {
      router.push({
        name: URL_KIOSK_TRANSACTION_ERROR,
        params: { token: route.params.token },
        query: { error: CARD_CANT_BE_USED_IN_MARKET }
      });
    } else if (message.indexOf(CARD_NOT_FOUND) !== -1) {
      router.push({ name: URL_CARD_ERROR, query: { error: CARD_NOT_FOUND, returnRoute: URL_KIOSK_HOME } });
    } else if (message.indexOf(CARD_DEACTIVATED) !== -1) {
      router.push({
        name: URL_KIOSK_TRANSACTION_ERROR,
        params: { token: route.params.token },
        query: { error: CARD_DEACTIVATED }
      });
    }
  }
}

function goHome() {
  router.push(kioskRoute(URL_KIOSK_HOME));
}

useKioskShellState({
  loading: () => loading.value,
  showCancel: () => activeStep.value !== CHECK_CARD_STEPS_COMPLETE,
  onCancel: goHome
});
</script>
