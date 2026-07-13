<i18n>
{
  "en": {
    "kiosk-title": "Self-service kiosk",
    "kiosk-help": "Open this link on the kiosk browser. Scan the QR code or copy the link. Enter the password on first use. Regenerate the link if it is compromised.",
    "enable-kiosk": "Enable kiosk",
    "copy-link": "Copy link",
    "regenerate-link": "Regenerate link",
    "disable-kiosk": "Disable kiosk",
    "show-qr": "Show QR code",
    "qr-title": "Kiosk access QR code",
    "qr-help": "Scan this code to open the kiosk URL.",
    "password-label": "Kiosk password",
    "show-password": "Show",
    "hide-password": "Hide",
    "copy-success": "Kiosk link copied to clipboard.",
    "copy-error": "Unable to copy the kiosk link to the clipboard.",
    "enable-success": "Kiosk mode enabled.",
    "regenerate-success": "Kiosk link and password regenerated.",
    "disable-success": "Kiosk mode disabled.",
    "regenerate-confirm": "Regenerating the link will invalidate the previous kiosk URL, reset the password, and disconnect authorized devices. Continue?",
    "disable-confirm": "Disabling the kiosk will invalidate the kiosk URL and disconnect authorized devices. Continue?"
  },
  "fr": {
    "kiosk-title": "Kiosque libre-service",
    "kiosk-help": "Ouvrez ce lien dans le navigateur du kiosque. Scannez le code QR ou copiez le lien. Saisissez le mot de passe lors de la première utilisation. Régénérez le lien s'il est compromis.",
    "enable-kiosk": "Activer le kiosque",
    "copy-link": "Copier le lien",
    "regenerate-link": "Régénérer le lien",
    "disable-kiosk": "Désactiver le kiosque",
    "show-qr": "Afficher le code QR",
    "qr-title": "Code QR d'accès au kiosque",
    "qr-help": "Scannez ce code pour ouvrir l'URL du kiosque.",
    "password-label": "Mot de passe du kiosque",
    "show-password": "Afficher",
    "hide-password": "Masquer",
    "copy-success": "Lien du kiosque copié dans le presse-papiers.",
    "copy-error": "Impossible de copier le lien du kiosque dans le presse-papiers.",
    "enable-success": "Mode kiosque activé.",
    "regenerate-success": "Lien et mot de passe du kiosque régénérés.",
    "disable-success": "Mode kiosque désactivé.",
    "regenerate-confirm": "La régénération invalidera l'ancien lien, réinitialisera le mot de passe et déconnectera les appareils autorisés. Continuer?",
    "disable-confirm": "La désactivation invalidera le lien du kiosque et déconnectera les appareils autorisés. Continuer?"
  }
}
</i18n>

<template>
  <div class="pt-4 border-t border-grey-300 space-y-3">
    <h4 class="text-p2 font-semibold uppercase text-primary-700">{{ t("kiosk-title") }}</h4>
    <p class="text-p3 text-primary-900">{{ t("kiosk-help") }}</p>
    <PfButtonAction
      v-if="!isKioskEnabled"
      btn-style="outline"
      :label="t('enable-kiosk')"
      :is-disabled="processing"
      @click="enableKiosk" />
    <template v-else>
      <a
        v-if="kioskLink"
        :href="kioskLink"
        target="_blank"
        rel="noopener noreferrer"
        class="text-p2 text-primary-700 font-medium break-all hover:underline focus:underline">
        {{ kioskLink }}
      </a>
      <div v-if="kioskPassword" class="flex flex-wrap items-center gap-6">
        <span class="flex gap-2">
          <span class="text-p3 font-semibold text-primary-900">{{ t("password-label") }}:</span>
          <span class="text-p2 font-mono tracking-widest">{{ showPassword ? kioskPassword : "••••••••" }}</span>
        </span>
        <PfButtonAction
          btn-style="link"
          size="sm"
          :label="showPassword ? t('hide-password') : t('show-password')"
          @click="showPassword = !showPassword" />
      </div>
      <div class="flex flex-wrap gap-2">
        <PfButtonAction btn-style="secondary" :label="t('copy-link')" :disabled="!kioskLink" @click="copyLink" />
        <PfButtonAction btn-style="outline" :label="t('show-qr')" :disabled="!kioskLink" @click="openQrModal" />
        <PfButtonAction btn-style="outline" :label="t('regenerate-link')" :is-disabled="processing" @click="regenerateLink" />
        <PfButtonAction btn-style="outline" :label="t('disable-kiosk')" :is-disabled="processing" @click="disableKiosk" />
      </div>
    </template>
    <UiDialogModal v-if="showQrModal" :title="t('qr-title')" :has-footer="true" @onClose="showQrModal = false">
      <p class="text-p3 text-primary-900 mb-4">{{ t("qr-help") }}</p>
      <QrCodePreview v-if="kioskLink" :qr-code="kioskLink" />
    </UiDialogModal>
  </div>
</template>

<script setup>
import gql from "graphql-tag";
import { computed, defineProps, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useMutation } from "@vue/apollo-composable";

import { copyTextToClipboard } from "@/lib/helpers/clipboard";
import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_KIOSK_HOME } from "@/lib/consts/urls";
import QrCodePreview from "@/components/card/qr-code-preview.vue";

const { t } = useI18n();
const router = useRouter();
const { addSuccess, addError } = useNotificationsStore();
const processing = ref(false);
const showPassword = ref(false);
const showQrModal = ref(false);

const props = defineProps({
  cashRegister: { type: Object, required: true }
});

const isKioskEnabled = ref(!!props.cashRegister.isKioskEnabled);
const kioskAccessToken = ref(props.cashRegister.kioskAccessToken || "");
const kioskPassword = ref(props.cashRegister.kioskPassword || "");

watch(
  () => props.cashRegister,
  (cashRegister) => {
    isKioskEnabled.value = !!cashRegister.isKioskEnabled;
    kioskAccessToken.value = cashRegister.kioskAccessToken || "";
    kioskPassword.value = cashRegister.kioskPassword || "";
  },
  { deep: true }
);

const kioskLink = computed(() => {
  if (!kioskAccessToken.value) return "";
  const resolved = router.resolve({
    name: URL_KIOSK_HOME,
    params: { token: kioskAccessToken.value }
  });
  return `${window.location.origin}${resolved.fullPath}`;
});

function applyMutationResult(cashRegister) {
  if (!cashRegister) return;
  isKioskEnabled.value = !!cashRegister.isKioskEnabled;
  kioskAccessToken.value = cashRegister.kioskAccessToken || "";
  kioskPassword.value = cashRegister.kioskPassword || "";
  showPassword.value = false;
}

const mutationOptions = {
  refetchQueries: ["Markets"],
  awaitRefetchQueries: true
};

const { mutate: enableCashRegisterKiosk } = useMutation(
  gql`
    mutation EnableCashRegisterKiosk($input: EnableCashRegisterKioskInput!) {
      enableCashRegisterKiosk(input: $input) {
        cashRegister {
          id
          isKioskEnabled
          kioskAccessToken
          kioskPassword
        }
      }
    }
  `,
  mutationOptions
);

const { mutate: regenerateCashRegisterKioskToken } = useMutation(
  gql`
    mutation RegenerateCashRegisterKioskToken($input: RegenerateCashRegisterKioskTokenInput!) {
      regenerateCashRegisterKioskToken(input: $input) {
        cashRegister {
          id
          isKioskEnabled
          kioskAccessToken
          kioskPassword
        }
      }
    }
  `,
  mutationOptions
);

const { mutate: disableCashRegisterKiosk } = useMutation(
  gql`
    mutation DisableCashRegisterKiosk($input: DisableCashRegisterKioskInput!) {
      disableCashRegisterKiosk(input: $input) {
        cashRegister {
          id
          isKioskEnabled
          kioskAccessToken
          kioskPassword
        }
      }
    }
  `,
  mutationOptions
);

async function enableKiosk() {
  processing.value = true;
  try {
    const result = await enableCashRegisterKiosk({ input: { cashRegisterId: props.cashRegister.id } });
    applyMutationResult(result?.data?.enableCashRegisterKiosk?.cashRegister);
    addSuccess(t("enable-success"));
  } finally {
    processing.value = false;
  }
}

async function regenerateLink() {
  if (!window.confirm(t("regenerate-confirm"))) return;
  processing.value = true;
  try {
    const result = await regenerateCashRegisterKioskToken({ input: { cashRegisterId: props.cashRegister.id } });
    applyMutationResult(result?.data?.regenerateCashRegisterKioskToken?.cashRegister);
    addSuccess(t("regenerate-success"));
  } finally {
    processing.value = false;
  }
}

async function disableKiosk() {
  if (!window.confirm(t("disable-confirm"))) return;
  processing.value = true;
  try {
    const result = await disableCashRegisterKiosk({ input: { cashRegisterId: props.cashRegister.id } });
    applyMutationResult(result?.data?.disableCashRegisterKiosk?.cashRegister);
    addSuccess(t("disable-success"));
  } finally {
    processing.value = false;
  }
}

async function copyLink() {
  if (!kioskLink.value) return;

  const copied = await copyTextToClipboard(kioskLink.value);
  if (copied) {
    addSuccess(t("copy-success"));
  } else {
    addError(t("copy-error"));
  }
}

function openQrModal() {
  showQrModal.value = true;
}
</script>
