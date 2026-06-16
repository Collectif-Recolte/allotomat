<i18n>
{
  "en": {
    "cancel": "Cancel",
    "logo": "Tomat logo",
    "invalid-link": "This link is invalid or has expired. Contact the merchant for a new kiosk link.",
    "market-disabled": "This merchant is temporarily unavailable."
  },
  "fr": {
    "cancel": "Annuler",
    "logo": "Logo de Tomat",
    "invalid-link": "Ce lien est invalide ou a expiré. Communiquez avec le commerce pour obtenir un nouveau lien kiosque.",
    "market-disabled": "Ce commerce est temporairement indisponible."
  }
}
</i18n>

<template>
  <div class="kiosk-mode min-h-[100dvh] flex flex-col bg-primary-100">
    <header class="sticky top-0 z-30 bg-primary-700 px-4 py-3 flex items-center justify-between gap-4">
      <img class="h-8" :src="require('@/assets/logo/logo-white.svg')" :alt="t('logo')" />
      <div class="flex items-center gap-3">
        <PfButtonAction v-if="showCancel" btn-style="secondary" size="lg" :label="t('cancel')" @click="handleCancel" />
        <KioskLangSwitch />
      </div>
    </header>
    <main class="flex-1 flex flex-col">
      <Loading :loading="kioskLoading || shellLoading" is-full-height>
        <KioskAuthGate v-if="showAuthGate" :login="login" :login-loading="loginLoading" :auth-error="authError" />
        <p
          v-else-if="!kioskLoading && tokenFound && marketIsDisabled"
          class="flex flex-1 items-center justify-center p-8 text-h3 text-center text-primary-900">
          {{ t("market-disabled") }}
        </p>
        <p
          v-else-if="!kioskLoading && (!tokenFound || !isValid)"
          class="flex flex-1 items-center justify-center p-8 text-h3 text-center text-primary-900">
          {{ t("invalid-link") }}
        </p>
        <RouterView v-else />
      </Loading>
    </main>
  </div>
</template>

<script setup>
import { computed } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useI18n } from "vue-i18n";

import Loading from "@/components/app/loading";
import KioskAuthGate from "@/components/app/kiosk-auth-gate";
import KioskLangSwitch from "@/components/app/kiosk-lang-switch";
import { clearKioskSession, useKioskSession } from "@/lib/composables/use-kiosk-session";
import { useKioskSessionValidation } from "@/lib/composables/use-kiosk-session-validation";
import { provideKioskShell } from "@/lib/composables/use-kiosk-shell";
import { useKioskToken } from "@/lib/composables/use-kiosk-token";
import { URL_KIOSK_HOME } from "@/lib/consts/urls";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const slug = computed(() => route.params.token);
const session = useKioskSession(slug);
const authToken = computed(() => session.value?.accessToken ?? "");

const {
  loading: kioskLoading,
  isValid,
  marketIsDisabled,
  tokenFound,
  isAuthenticated,
  login,
  loginLoading,
  authError
} = useKioskToken();

const { loading: shellLoading, showCancel, onCancel } = provideKioskShell();

const showAuthGate = computed(() => !kioskLoading.value && tokenFound.value && isValid.value && !isAuthenticated.value);

function handleCancel() {
  onCancel.value?.();
}

function handleInvalidSession() {
  clearKioskSession(slug.value);
  router.replace({ name: URL_KIOSK_HOME, params: { token: slug.value } });
}

useKioskSessionValidation(authToken, handleInvalidSession);
</script>
