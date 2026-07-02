<i18n>
{
  "en": {
    "card-cant-be-use-in-market": "The participant cannot make a purchase in this market.",
    "card-deactivated": "The card is deactivated.",
    "card-not-found": "QR code does not equate to a known card.",
    "card-cant-be-use-in-cash-register": "The participant cannot make a purchase at this cash register.",
    "title": "Error during payment",
    "back": "Back to kiosk home"
  },
  "fr": {
    "card-cant-be-use-in-market": "Le·a participant·e ne peut pas faire d'achat dans ce commerce.",
    "card-deactivated": "La carte est désactivée.",
    "card-not-found": "Le code QR n'équivaut pas à une carte connue.",
    "card-cant-be-use-in-cash-register": "Le·a participant·e ne peut pas faire d'achat avec cette caisse.",
    "title": "Erreur lors du paiement",
    "back": "Retour à l'accueil du kiosque"
  }
}
</i18n>

<template>
  <div class="flex flex-col items-center justify-center flex-1 p-8 gap-6 w-full max-w-2xl md:max-w-4xl mx-auto text-center">
    <h1 class="text-h2 font-semibold">{{ t("title") }}</h1>
    <p class="text-h4">{{ error }}</p>
    <PfButtonAction btn-style="secondary" size="lg" :label="t('back')" @click="goHome" />
  </div>
</template>

<script setup>
import { computed } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useI18n } from "vue-i18n";

import { useKioskToken } from "@/lib/composables/use-kiosk-token";
import { URL_KIOSK_HOME } from "@/lib/consts/urls";
import {
  CARD_CANT_BE_USED_IN_MARKET,
  CARD_NOT_FOUND,
  CARD_DEACTIVATED,
  CARD_CANT_BE_USED_WITH_CASH_REGISTER
} from "@/lib/consts/qr-code-error";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { kioskRoute } = useKioskToken();

const error = computed(() => {
  switch (route.query.error) {
    case CARD_CANT_BE_USED_IN_MARKET:
      return t("card-cant-be-use-in-market");
    case CARD_NOT_FOUND:
      return t("card-not-found");
    case CARD_DEACTIVATED:
      return t("card-deactivated");
    case CARD_CANT_BE_USED_WITH_CASH_REGISTER:
      return t("card-cant-be-use-in-cash-register");
    default:
      return t("card-not-found");
  }
});

function goHome() {
  router.push(kioskRoute(URL_KIOSK_HOME));
}
</script>
