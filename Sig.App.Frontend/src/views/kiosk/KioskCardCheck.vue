<i18n>
{
  "en": {
    "title": "Check card balance",
    "restart-scan": "Scan a card",
    "camera-access": "You must have access to a camera.",
    "invalid-link": "This link is invalid or has expired."
  },
  "fr": {
    "title": "Vérifier le solde d'une carte",
    "restart-scan": "Scanner une carte",
    "camera-access": "Vous devez avoir accès à une caméra.",
    "invalid-link": "Ce lien est invalide ou a expiré."
  }
}
</i18n>

<template>
  <KioskShell :loading="loading || kioskLoading" :show-cancel="activeStep !== CHECK_CARD_STEPS_COMPLETE" @cancel="goHome">
    <div v-if="!kioskLoading && !isValid" class="flex flex-1 items-center justify-center p-8 text-h3 text-center">
      {{ t("invalid-link") }}
    </div>
    <template v-else-if="isValid">
      <div v-if="activeStep === CHECK_CARD_STEPS_START" class="flex justify-center py-8 lg:py-16 px-4">
        <UiCta
          class="w-full max-w-lg"
          :img-src="require('@/assets/img/scan-marchand.jpg')"
          :primary-btn-label="t('restart-scan')"
          :description="t('camera-access')"
          primary-btn-is-action
          @onPrimaryBtnClick="activeStep = CHECK_CARD_STEPS_SCAN" />
      </div>
      <div v-else-if="activeStep === CHECK_CARD_STEPS_SCAN" class="text-center flex flex-col justify-center items-center p-8">
        <QRCodeScanner
          kiosk-mode
          :error-url-const="URL_CARD_ERROR"
          @triggerError="activeStep = CHECK_CARD_STEPS_START"
          @checkQRCode="checkQRCode"
          @cancel="activeStep = CHECK_CARD_STEPS_START" />
      </div>
      <Balance
        v-else-if="activeStep === CHECK_CARD_STEPS_COMPLETE"
        :card-id="cardId"
        hide-transaction-list
        is-kiosk
        @finished="goHome"
        @onUpdateLoadingState="loading = $event" />
    </template>
  </KioskShell>
</template>

<script setup>
import gql from "graphql-tag";
import { ref } from "vue";
import { useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import { useApolloClient } from "@vue/apollo-composable";

import { usePageTitle } from "@/lib/helpers/page-title";
import { useKioskToken } from "@/lib/composables/use-kiosk-token";
import { URL_KIOSK_HOME, URL_CARD_ERROR } from "@/lib/consts/urls";
import { CHECK_CARD_STEPS_START, CHECK_CARD_STEPS_SCAN, CHECK_CARD_STEPS_COMPLETE } from "@/lib/consts/enums";
import { CARD_NOT_FOUND } from "@/lib/consts/qr-code-error";

import KioskShell from "@/components/app/kiosk-shell";
import QRCodeScanner from "@/components/transaction/qr-code-scanner.vue";
import Balance from "@/views/card/Balance";

const { t } = useI18n();
const router = useRouter();
const { resolveClient } = useApolloClient();
const client = resolveClient();

usePageTitle(t("title"));

const { loading: kioskLoading, isValid, kioskRoute } = useKioskToken();

const activeStep = ref(CHECK_CARD_STEPS_START);
const cardId = ref("");
const loading = ref(false);

async function checkQRCode(id) {
  const result = await client.query({
    query: gql`
      query Card($id: ID!) {
        card(id: $id) {
          id
        }
      }
    `,
    variables: { id }
  });

  if (!result.data.card) {
    activeStep.value = CHECK_CARD_STEPS_START;
    router.push({ name: URL_CARD_ERROR, query: { error: CARD_NOT_FOUND, returnRoute: URL_KIOSK_HOME } });
    return;
  }

  cardId.value = result.data.card.id;
  activeStep.value = CHECK_CARD_STEPS_COMPLETE;
}

function goHome() {
  router.push(kioskRoute(URL_KIOSK_HOME));
}
</script>
