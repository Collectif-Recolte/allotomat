<i18n>
  {
    "en": {
      "title": "Check card balance"
    },
    "fr": {
      "title": "Vérifier le solde d'une carte"
    }
  }
  </i18n>

<template>
  <div class="text-center flex flex-col justify-center items-center">
    <QRCodeScanner
      :error-url-const="URL_CARD_ERROR"
      @triggerError="emit('onUpdateStep', CHECK_CARD_STEPS_START)"
      @checkQRCode="checkQRCode"
      @cancel="emit('onUpdateStep', CHECK_CARD_STEPS_START)" />
  </div>
</template>

<script setup>
import gql from "graphql-tag";
import { useRouter } from "vue-router";
import { useApolloClient } from "@vue/apollo-composable";
import { defineEmits } from "vue";
import { useI18n } from "vue-i18n";
import { usePageTitle } from "@/lib/helpers/page-title";

import { URL_CARD_ERROR, URL_CARD_CHECK } from "@/lib/consts/urls";
import { CHECK_CARD_STEPS_START, CHECK_CARD_STEPS_COMPLETE } from "@/lib/consts/enums";
import { CARD_NOT_FOUND } from "@/lib/consts/qr-code-error";

import QRCodeScanner from "@/components/transaction/qr-code-scanner.vue";

const { t } = useI18n();
const router = useRouter();
const { resolveClient } = useApolloClient();
const client = resolveClient();

usePageTitle(t("title"));

const emit = defineEmits(["onUpdateStep"]);

async function checkQRCode(cardId) {
  const result = await client.query({
    query: gql`
      query Card($id: ID!) {
        card(id: $id) {
          id
        }
      }
    `,
    variables: {
      id: cardId
    }
  });

  const card = result.data.card;

  if (!card) {
    emit("onUpdateStep", CHECK_CARD_STEPS_START);
    router.push({ name: URL_CARD_ERROR, query: { error: CARD_NOT_FOUND, returnRoute: URL_CARD_CHECK } });
    return;
  }

  emit("onUpdateStep", CHECK_CARD_STEPS_COMPLETE, card.id);
}
</script>
