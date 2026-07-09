<template>
  <div class="flex flex-col items-center justify-center min-h-[var(--kiosk-content-min-height)] px-8 sm:px-12 py-12 w-full">
    <h1 v-if="props.heading" class="font-bold text-d2 text-primary-700 mb-6 text-center">{{ props.heading }}</h1>
    <QRCodeScanner
      ref="scannerRef"
      kiosk-mode
      :error-url-const="URL_KIOSK_TRANSACTION_ERROR"
      @checkQRCode="checkQRCode"
      @cancel="emit('cancel')" />
  </div>
</template>

<script setup>
import gql from "graphql-tag";
import { defineEmits, defineProps, ref } from "vue";
import { useApolloClient } from "@vue/apollo-composable";

import QRCodeScanner from "@/components/transaction/qr-code-scanner.vue";
import { URL_KIOSK_TRANSACTION_ERROR } from "@/lib/consts/urls";
import { KIOSK_ACCESS_INVALID } from "@/lib/consts/qr-code-error";

const audio = new Audio(require("@/assets/audio/scan.mp3"));
const { resolveClient } = useApolloClient();
const client = resolveClient();

const props = defineProps({
  kioskToken: { type: String, required: true },
  heading: { type: String, default: "" }
});

const emit = defineEmits(["scanned", "cancel", "authError"]);

const scannerRef = ref(null);

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
      return;
    }

    scannerRef.value?.showScanError();
  } catch (exception) {
    const message = exception.message || "";
    if (message.indexOf(KIOSK_ACCESS_INVALID) !== -1) {
      emit("authError");
      return;
    }

    scannerRef.value?.showScanError();
  }
}
</script>
