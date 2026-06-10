<i18n>
{
  "en": {
    "kiosk-title": "Self-service kiosk",
    "kiosk-help": "Open this link on the kiosk browser. Do not share it publicly. Regenerate the link if it is compromised.",
    "enable-kiosk": "Enable kiosk",
    "copy-link": "Copy link",
    "regenerate-link": "Regenerate link",
    "disable-kiosk": "Disable kiosk",
    "copy-success": "Kiosk link copied to clipboard.",
    "enable-success": "Kiosk mode enabled.",
    "regenerate-success": "Kiosk link regenerated.",
    "disable-success": "Kiosk mode disabled.",
    "regenerate-confirm": "Regenerating the link will invalidate the previous kiosk URL. Continue?",
    "disable-confirm": "Disabling the kiosk will invalidate the kiosk URL. Continue?"
  },
  "fr": {
    "kiosk-title": "Kiosque libre-service",
    "kiosk-help": "Ouvrez ce lien dans le navigateur du kiosque. Ne le partagez pas publiquement. Régénérez le lien s'il est compromis.",
    "enable-kiosk": "Activer le kiosque",
    "copy-link": "Copier le lien",
    "regenerate-link": "Régénérer le lien",
    "disable-kiosk": "Désactiver le kiosque",
    "copy-success": "Lien du kiosque copié dans le presse-papiers.",
    "enable-success": "Mode kiosque activé.",
    "regenerate-success": "Lien du kiosque régénéré.",
    "disable-success": "Mode kiosque désactivé.",
    "regenerate-confirm": "La régénération invalidera l'ancien lien du kiosque. Continuer?",
    "disable-confirm": "La désactivation invalidera le lien du kiosque. Continuer?"
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
      :processing="processing"
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
      <div class="flex flex-wrap gap-2">
        <PfButtonAction btn-style="secondary" :label="t('copy-link')" :disabled="!kioskLink" @click="copyLink" />
        <PfButtonAction btn-style="outline" :label="t('regenerate-link')" :processing="processing" @click="regenerateLink" />
        <PfButtonAction btn-style="outline" :label="t('disable-kiosk')" :processing="processing" @click="disableKiosk" />
      </div>
    </template>
  </div>
</template>

<script setup>
import gql from "graphql-tag";
import { computed, defineProps, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useMutation } from "@vue/apollo-composable";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_KIOSK_HOME } from "@/lib/consts/urls";

const { t } = useI18n();
const router = useRouter();
const { addSuccess } = useNotificationsStore();
const processing = ref(false);

const props = defineProps({
  cashRegister: { type: Object, required: true }
});

const isKioskEnabled = ref(!!props.cashRegister.isKioskEnabled);
const kioskAccessToken = ref(props.cashRegister.kioskAccessToken || "");

watch(
  () => props.cashRegister,
  (cashRegister) => {
    isKioskEnabled.value = !!cashRegister.isKioskEnabled;
    kioskAccessToken.value = cashRegister.kioskAccessToken || "";
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
  await navigator.clipboard.writeText(kioskLink.value);
  addSuccess(t("copy-success"));
}
</script>
