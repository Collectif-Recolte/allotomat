<i18n>
{
  "en": {
    "welcome": "Welcome!",
    "subtitle": "Use your <strong>card</strong> to make a purchase or check your balance.",
    "make-purchase": "Make a purchase",
    "check-balance": "Check my card balance"
  },
  "fr": {
    "welcome": "Bienvenue!",
    "subtitle": "Utilisez votre <strong>carte</strong> pour faire un achat ou vérifier votre solde.",
    "make-purchase": "Faire un achat",
    "check-balance": "Vérifier le solde de ma carte"
  }
}
</i18n>

<template>
  <div
    class="flex flex-col items-center justify-center min-h-[var(--kiosk-content-min-height)] px-8 py-12 sm:px-12 w-full max-w-5xl mx-auto">
    <h1 class="text-h1 sm:text-d1 font-bold text-primary-700 text-center mb-4 mt-0">{{ t("welcome") }}</h1>
    <KioskProgramNames class="mb-4" />
    <!-- eslint-disable-next-line vue/no-v-html @intlify/vue-i18n/no-v-html -->
    <p class="text-h4 sm:text-h3 text-primary-700 text-center mb-8 sm:mb-12 max-w-sm" v-html="t('subtitle')" />

    <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 sm:gap-6 w-full max-w-lg">
      <KioskActionTile
        variant="purchase"
        :label="t('make-purchase')"
        :icon="ICON_KIOSK_SHOPPING_CART"
        @click="router.push(kioskRoute(URL_KIOSK_TRANSACTION))" />
      <KioskActionTile
        variant="balance"
        :label="t('check-balance')"
        :icon="ICON_PURCHASE_WALLET"
        @click="router.push(kioskRoute(URL_KIOSK_CHECK))" />
    </div>
  </div>
</template>

<script setup>
import { useRouter } from "vue-router";
import { useI18n } from "vue-i18n";

import KioskActionTile from "@/components/kiosk/kiosk-action-tile";
import KioskProgramNames from "@/components/kiosk/kiosk-program-names";
import { usePageTitle } from "@/lib/helpers/page-title";
import { useKioskShellState } from "@/lib/composables/use-kiosk-shell";
import { useKioskToken } from "@/lib/composables/use-kiosk-token";
import { URL_KIOSK_TRANSACTION, URL_KIOSK_CHECK } from "@/lib/consts/urls";

import ICON_KIOSK_SHOPPING_CART from "@/lib/icons/kiosk-shopping-cart.json";
import ICON_PURCHASE_WALLET from "@/lib/icons/purchase-wallet.json";

const { t } = useI18n();
const router = useRouter();
usePageTitle(t("welcome"));

const { kioskRoute } = useKioskToken();

useKioskShellState({ idleTimeoutMode: "disabled" });
</script>
