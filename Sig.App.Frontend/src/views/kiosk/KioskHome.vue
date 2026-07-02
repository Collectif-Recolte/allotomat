<i18n>
{
  "en": {
    "welcome": "Welcome!",
    "subtitle": "Use your <strong>Proximity Card</strong> to make a purchase or check your balance.",
    "make-purchase": "Make a purchase",
    "check-balance": "Check my card balance"
  },
  "fr": {
    "welcome": "Bienvenue!",
    "subtitle": "Utilisez votre <strong>Carte Proximité</strong> pour faire un achat ou vérifier votre solde.",
    "make-purchase": "Faire un achat",
    "check-balance": "Vérifier le solde de ma carte"
  }
}
</i18n>

<template>
  <div class="flex flex-col items-center justify-center flex-1 px-4 sm:px-8 py-10 sm:py-16 w-full max-w-5xl mx-auto">
    <h1 class="text-h1 sm:text-d3 font-semibold text-primary-900 text-center mb-4">{{ t("welcome") }}</h1>
    <!-- eslint-disable-next-line vue/no-v-html @intlify/vue-i18n/no-v-html -->
    <p class="text-h4 sm:text-h3 text-primary-900 text-center mb-10 sm:mb-14 max-w-2xl" v-html="t('subtitle')" />

    <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 sm:gap-6 w-full max-w-3xl">
      <KioskActionTile
        variant="purchase"
        :label="t('make-purchase')"
        :icon="ICON_SHOPPING_CART"
        @click="router.push(kioskRoute(URL_KIOSK_TRANSACTION))" />
      <KioskActionTile
        variant="balance"
        :label="t('check-balance')"
        :icon="ICON_CREDIT_CARD"
        @click="router.push(kioskRoute(URL_KIOSK_CHECK))" />
    </div>
  </div>
</template>

<script setup>
import { useRouter } from "vue-router";
import { useI18n } from "vue-i18n";

import KioskActionTile from "@/components/kiosk/kiosk-action-tile";
import { usePageTitle } from "@/lib/helpers/page-title";
import { useKioskToken } from "@/lib/composables/use-kiosk-token";
import { URL_KIOSK_TRANSACTION, URL_KIOSK_CHECK } from "@/lib/consts/urls";

import ICON_CREDIT_CARD from "@/lib/icons/credit-card.json";
import ICON_SHOPPING_CART from "@/lib/icons/shopping-cart.json";

const { t } = useI18n();
const router = useRouter();
usePageTitle(t("welcome"));

const { kioskRoute } = useKioskToken();
</script>
