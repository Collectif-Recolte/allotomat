<template>
  <div class="flex flex-1 flex-col items-center justify-center min-h-0 p-4 sm:p-8 w-full">
    <h1 v-if="props.heading" class="font-semibold text-h2 sm:text-h1 text-primary-900 mb-6 text-center">{{ props.heading }}</h1>
    <QRCodeScanner
      kiosk-mode
      :error-url-const="URL_KIOSK_TRANSACTION_ERROR"
      @checkQRCode="checkQRCode"
      @cancel="emit('cancel')" />
  </div>
</template>

<script setup>
import gql from "graphql-tag";
import { defineEmits, defineProps } from "vue";
import { useApolloClient } from "@vue/apollo-composable";
import { useRoute, useRouter } from "vue-router";

import QRCodeScanner from "@/components/transaction/qr-code-scanner.vue";
import { URL_KIOSK_TRANSACTION_ERROR } from "@/lib/consts/urls";
import {
  CARD_CANT_BE_USED_IN_MARKET,
  CARD_NOT_FOUND,
  CARD_DEACTIVATED,
  CARD_CANT_BE_USED_WITH_CASH_REGISTER,
  KIOSK_ACCESS_INVALID
} from "@/lib/consts/qr-code-error";

const audio = new Audio(require("@/assets/audio/scan.mp3"));
const router = useRouter();
const route = useRoute();
const { resolveClient } = useApolloClient();
const client = resolveClient();

const props = defineProps({
  kioskToken: { type: String, required: true },
  heading: { type: String, default: "" }
});

const emit = defineEmits(["scanned", "cancel", "authError"]);

function routeToKioskError(error) {
  router.push({
    name: URL_KIOSK_TRANSACTION_ERROR,
    params: { token: route.params.token },
    query: { error }
  });
}

async function checkQRCode(cardId) {
  try {
    const result = await client.query({
      query: gql`
        query VerifyCardCanBeUsedInKiosk($kioskToken: String!, $cardId: ID!) {
          verifyCardCanBeUsedInKiosk(kioskToken: $kioskToken, cardId: $cardId)
        }
      `,
      variables: {
        kioskToken: props.kioskToken,
        cardId
      }
    });

    if (result.data.verifyCardCanBeUsedInKiosk) {
      audio.play();
      setTimeout(() => emit("scanned", cardId), 200);
    }
  } catch (exception) {
    const message = exception.message || "";
    if (message.indexOf(KIOSK_ACCESS_INVALID) !== -1) {
      emit("authError");
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
    } else {
      routeToKioskError(CARD_NOT_FOUND);
    }
  }
}
</script>
