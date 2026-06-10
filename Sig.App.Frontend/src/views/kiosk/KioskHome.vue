<i18n>
{
  "en": {
    "title": "Self-service kiosk",
    "make-purchase": "Make a purchase",
    "check-balance": "Check my card balance",
    "invalid-link": "This link is invalid or has expired. Contact the merchant for a new kiosk link.",
    "market-disabled": "This merchant is temporarily unavailable."
  },
  "fr": {
    "title": "Kiosque libre-service",
    "make-purchase": "Faire un achat",
    "check-balance": "Vérifier le solde de ma carte",
    "invalid-link": "Ce lien est invalide ou a expiré. Communiquez avec le commerce pour obtenir un nouveau lien kiosque.",
    "market-disabled": "Ce commerce est temporairement indisponible."
  }
}
</i18n>

<template>
  <KioskShell :loading="loading">
    <div v-if="!loading" class="flex flex-col items-center justify-center flex-1 px-6 py-12 gap-8 max-w-lg mx-auto w-full">
      <template v-if="tokenFound && isValid">
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
      </template>
      <p v-else-if="!loading && tokenFound && marketIsDisabled" class="text-h3 text-center text-primary-900">
        {{ t("market-disabled") }}
      </p>
      <p v-else-if="!loading" class="text-h3 text-center text-primary-900">{{ t("invalid-link") }}</p>
    </div>
  </KioskShell>
</template>

<script setup>
import { useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import { usePageTitle } from "@/lib/helpers/page-title";
import { useKioskToken } from "@/lib/composables/use-kiosk-token";
import { URL_KIOSK_TRANSACTION, URL_KIOSK_CHECK } from "@/lib/consts/urls";

import KioskShell from "@/components/app/kiosk-shell";

const { t } = useI18n();
const router = useRouter();
usePageTitle(t("title"));

const { loading, isValid, marketIsDisabled, tokenFound, kioskRoute } = useKioskToken();
</script>
