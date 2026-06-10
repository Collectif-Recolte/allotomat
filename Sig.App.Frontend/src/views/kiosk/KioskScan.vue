<template>
  <div class="text-center min-h-app p-8 flex flex-col justify-center items-center">
    <h1 v-if="props.heading" class="font-semibold text-h2 text-primary-900 mb-6">{{ props.heading }}</h1>
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
import { useRouter } from "vue-router";

import QRCodeScanner from "@/components/transaction/qr-code-scanner.vue";
import { URL_KIOSK_TRANSACTION_ERROR } from "@/lib/consts/urls";
import {
  CARD_CANT_BE_USED_IN_MARKET,
  CARD_NOT_FOUND,
  CARD_DEACTIVATED,
  CARD_CANT_BE_USED_WITH_CASH_REGISTER
} from "@/lib/consts/qr-code-error";

const audio = new Audio(require("@/assets/audio/scan.mp3"));
const router = useRouter();
const { resolveClient } = useApolloClient();
const client = resolveClient();

const props = defineProps({
  kioskToken: { type: String, required: true },
  transactionRouteName: { type: String, required: true },
  heading: { type: String, default: "" }
});

const emit = defineEmits(["scanned", "cancel"]);

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
    if (message.indexOf(CARD_CANT_BE_USED_WITH_CASH_REGISTER) !== -1) {
      router.push({ name: URL_KIOSK_TRANSACTION_ERROR, params: { token: props.kioskToken }, query: { error: CARD_CANT_BE_USED_WITH_CASH_REGISTER } });
    } else if (message.indexOf(CARD_CANT_BE_USED_IN_MARKET) !== -1) {
      router.push({ name: URL_KIOSK_TRANSACTION_ERROR, params: { token: props.kioskToken }, query: { error: CARD_CANT_BE_USED_IN_MARKET } });
    } else if (message.indexOf(CARD_NOT_FOUND) !== -1) {
      router.push({ name: URL_KIOSK_TRANSACTION_ERROR, params: { token: props.kioskToken }, query: { error: CARD_NOT_FOUND } });
    } else if (message.indexOf(CARD_DEACTIVATED) !== -1) {
      router.push({ name: URL_KIOSK_TRANSACTION_ERROR, params: { token: props.kioskToken }, query: { error: CARD_DEACTIVATED } });
    } else {
      emit("cancel");
    }
  }
}
</script>
