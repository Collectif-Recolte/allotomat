<i18n>
{
  "en": {
    "title": "Self-service kiosk",
    "make-purchase": "Make a purchase",
    "check-balance": "Check my card balance"
  },
  "fr": {
    "title": "Kiosque libre-service",
    "make-purchase": "Faire un achat",
    "check-balance": "Vérifier le solde de ma carte"
  }
}
</i18n>

<template>
  <div class="flex flex-col items-center justify-center flex-1 px-6 py-12 gap-8 max-w-lg mx-auto w-full">
    <h1 class="text-h1 font-semibold text-primary-900 text-center">{{ t("title") }}</h1>
    <PfButtonAction
      class="w-full"
      size="lg"
      btn-style="primary"
      :label="t('make-purchase')"
      @click="router.push(kioskRoute(URL_KIOSK_TRANSACTION))" />
    <PfButtonAction
      class="w-full"
      size="lg"
      btn-style="secondary"
      :label="t('check-balance')"
      @click="router.push(kioskRoute(URL_KIOSK_CHECK))" />
  </div>
</template>

<script setup>
import { useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import { usePageTitle } from "@/lib/helpers/page-title";
import { useKioskToken } from "@/lib/composables/use-kiosk-token";
import { URL_KIOSK_TRANSACTION, URL_KIOSK_CHECK } from "@/lib/consts/urls";

const { t } = useI18n();
const router = useRouter();
usePageTitle(t("title"));

const { kioskRoute } = useKioskToken();
</script>
