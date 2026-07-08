<i18n>
{
  "en": {
    "quit": "Quit",
    "logo": "Tomat logo",
    "kiosk-mode-label": "Self-service kiosk",
    "invalid-link": "This link is invalid or has expired. Contact the merchant for a new kiosk link.",
    "market-disabled": "This merchant is temporarily unavailable."
  },
  "fr": {
    "quit": "Quitter",
    "logo": "Logo de Tomat",
    "kiosk-mode-label": "Kiosque libre-service",
    "invalid-link": "Ce lien est invalide ou a expiré. Communiquez avec le commerce pour obtenir un nouveau lien kiosque.",
    "market-disabled": "Ce commerce est temporairement indisponible."
  }
}
</i18n>

<template>
  <div class="kiosk-mode min-h-[100dvh] flex flex-col bg-primary-100">
    <header class="dark sticky top-0 z-30 bg-primary-900 px-4 sm:px-6 py-3 flex items-center justify-between gap-4 shrink-0">
      <div class="flex items-center gap-5 min-w-0">
        <img class="h-10 shrink-0" :src="require('@/assets/logo/logo-white.svg')" :alt="t('logo')" />
        <span class="hidden sm:block w-px h-8 bg-white/30 shrink-0" aria-hidden="true" />
        <div class="hidden sm:flex items-center gap-1 text-white min-w-0">
          <PfIcon :icon="ICON_QRCODE" size="lg" class="shrink-0" aria-hidden="true" />
          <span class="text-h4 font-medium truncate">{{ t("kiosk-mode-label") }}</span>
        </div>
      </div>
      <div class="flex items-center gap-3 shrink-0">
        <PfButtonAction
          v-if="showCancel"
          btn-style="secondary"
          class="rounded-full"
          size="lg"
          has-icon-left
          :icon="ICON_LOGOUT"
          :label="t('quit')"
          @click="handleCancel" />
        <KioskLangSwitch />
      </div>
    </header>
    <KioskIdlePrompt v-if="showIdlePrompt" :warning-seconds-remaining="warningSecondsRemaining" @dismiss="dismissIdlePrompt" />
    <main class="flex-1 flex flex-col min-h-0">
      <Loading :loading="kioskLoading || shellLoading" is-full-page>
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
import { useKioskIdleTimeout } from "@/lib/composables/use-kiosk-idle-timeout";
import { provideKioskShell } from "@/lib/composables/use-kiosk-shell";
import { useKioskToken } from "@/lib/composables/use-kiosk-token";
import { URL_KIOSK_HOME } from "@/lib/consts/urls";

import KioskIdlePrompt from "@/components/kiosk/kiosk-idle-prompt";

import ICON_LOGOUT from "@/lib/icons/logout.json";
import ICON_QRCODE from "@/lib/icons/qrcode.json";

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
  authError,
  kioskRoute
} = useKioskToken();

const { loading: shellLoading, showCancel, onCancel, idleTimeoutMode } = provideKioskShell();

const showAuthGate = computed(() => !kioskLoading.value && tokenFound.value && isValid.value && !isAuthenticated.value);

const idleTimeoutPaused = computed(
  () =>
    kioskLoading.value ||
    shellLoading.value ||
    showAuthGate.value ||
    route.name === URL_KIOSK_HOME ||
    (!kioskLoading.value && tokenFound.value && marketIsDisabled.value) ||
    (!kioskLoading.value && (!tokenFound.value || !isValid.value))
);

const effectiveIdleTimeoutMode = computed(() => {
  if (idleTimeoutPaused.value) {
    return "disabled";
  }
  return idleTimeoutMode.value;
});

function returnToKioskHome() {
  router.replace(kioskRoute(URL_KIOSK_HOME));
}

const { showIdlePrompt, warningSecondsRemaining, dismissIdlePrompt } = useKioskIdleTimeout({
  mode: effectiveIdleTimeoutMode,
  paused: idleTimeoutPaused,
  onReturnHome: returnToKioskHome
});

function handleCancel() {
  onCancel.value?.();
}

function handleInvalidSession() {
  clearKioskSession(slug.value);
  router.replace({ name: URL_KIOSK_HOME, params: { token: slug.value } });
}

useKioskSessionValidation(authToken, handleInvalidSession);
</script>

<style>
.kiosk-mode {
  /* Header: py-3 (1.5rem) + lg button min-h-12 (3rem) */
  --kiosk-top-bar-height: 4.5rem;
  --kiosk-content-min-height: calc(100dvh - var(--kiosk-top-bar-height) * 2);
}
</style>
