<i18n>
  {
    "en": {
      "title": "Scan a card",
      
    },
    "fr": {
      "title": "Scanner une carte"
    }
  }
  </i18n>

<template>
  <div class="text-center min-h-app p-8 flex flex-col justify-center items-center">
    <QRCodeScanner
      :error-url-const="URL_TRANSACTION_ERROR"
      @triggerError="isScanning = false"
      @checkQRCode="checkQRCode"
      @cancel="$emit('onUpdateStep', TRANSACTION_STEPS_START, {})" />
  </div>
</template>

<script setup>
import gql from "graphql-tag";
import { defineEmits } from "vue";
import { useI18n } from "vue-i18n";
import { usePageTitle } from "@/lib/helpers/page-title";
import { useQuery, useResult, useApolloClient } from "@vue/apollo-composable";
import { useRouter } from "vue-router";

import QRCodeScanner from "@/components/transaction/qr-code-scanner.vue";

import { URL_TRANSACTION, URL_TRANSACTION_ERROR } from "@/lib/consts/urls";
import { TRANSACTION_STEPS_START, TRANSACTION_STEPS_ADD } from "@/lib/consts/enums";
import { CARD_CANT_BE_USED_IN_MARKET, CARD_NOT_FOUND, CARD_DEACTIVATED } from "@/lib/consts/qr-code-error";

const audio = new Audio(require("@/assets/audio/scan.mp3"));
const { t } = useI18n();
const router = useRouter();
const { resolveClient } = useApolloClient();
const client = resolveClient();

usePageTitle(t("title"));

const emit = defineEmits(["onUpdateStep"]);

const { result } = useQuery(
  gql`
    query Markets {
      markets {
        id
        name
      }
    }
  `
);
const myMarket = useResult(result, null, (data) => data.markets[0]);

async function checkQRCode(result) {
  let canBeUsedInMarket = null;
  try {
    canBeUsedInMarket = await client.query({
      query: gql`
        query VerifyCardCanBeUsedInMarket($cardId: ID!, $marketId: ID!) {
          verifyCardCanBeUsedInMarket(cardId: $cardId, marketId: $marketId)
        }
      `,
      variables: {
        cardId: result,
        marketId: myMarket.value.id
      }
    });

    if (canBeUsedInMarket.data.verifyCardCanBeUsedInMarket) {
      audio.play();
      setTimeout(() => emit("onUpdateStep", TRANSACTION_STEPS_ADD, { cardId: result }), 200);
    }
  } catch (exception) {
    emit("onUpdateStep", TRANSACTION_STEPS_START, {});
    if (exception.message.indexOf(CARD_CANT_BE_USED_IN_MARKET) !== -1) {
      router.push({ name: URL_TRANSACTION_ERROR, query: { error: CARD_CANT_BE_USED_IN_MARKET, returnRoute: URL_TRANSACTION } });
    } else if (exception.message.indexOf(CARD_NOT_FOUND) !== -1) {
      router.push({ name: URL_TRANSACTION_ERROR, query: { error: CARD_NOT_FOUND, returnRoute: URL_TRANSACTION } });
    } else if (exception.message.indexOf(CARD_DEACTIVATED) !== -1) {
      router.push({ name: URL_TRANSACTION_ERROR, query: { error: CARD_DEACTIVATED, returnRoute: URL_TRANSACTION } });
    }
  }
}
</script>
