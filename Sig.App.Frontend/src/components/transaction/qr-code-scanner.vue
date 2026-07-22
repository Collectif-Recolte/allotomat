<i18n>
{
	"en": {
		"validation-in-progress": "Validation in progress",
    "flip-camera": "Flip the camera",
    "scan-instruction": "Bring your card close to the reader to scan the QR code in the center of the screen.",
    "scan-privacy": "These images are only used to identify your card. They are not transmitted or shared with anyone.",
    "cancel": "Cancel",
    "scan-error-title": "Scan error",
    "scan-error-message": "The QR code was not recognized."
	},
	"fr": {
		"validation-in-progress": "Validation en cours",
    "flip-camera": "Retourner la caméra",
    "scan-instruction": "Approchez votre carte du lecteur pour scanner le code QR au centre de l'écran.",
    "scan-privacy": "Ces images servent uniquement à identifier votre carte. Elles ne sont ni transmises ni partagées avec personne.",
    "cancel": "Annuler",
    "scan-error-title": "Erreur de scan",
    "scan-error-message": "Le code QR n'a pas été reconnu."
	}
}
</i18n>

<template>
  <div :class="props.kioskMode ? 'w-full max-w-lg mx-auto mb-6' : 'w-sm max-w-full mb-9 relative'">
    <div
      class="relative overflow-hidden rounded-2xl shadow-2xl"
      :class="props.kioskMode ? 'w-full aspect-[16/10] max-w-md mx-auto' : 'w-sm max-w-full'">
      <video ref="qrCodeVideo" class="w-full h-full object-cover"></video>
    </div>
    <div v-if="props.kioskMode" class="text-center text-h3 my-8 px-4 mx-auto">
      <template v-if="scanErrorVisible">
        <p class="font-bold text-red-500 mb-1">{{ t("scan-error-title") }}</p>
        <p class="text-red-500 mb-0">{{ t("scan-error-message") }}</p>
      </template>
      <template v-else>
        <p class="text-primary-700 mb-2">{{ t("scan-instruction") }}</p>
        <p class="text-d7 text-grey-600 mb-0">{{ t("scan-privacy") }}</p>
      </template>
    </div>
    <div
      v-if="props.kioskMode"
      class="flex flex-col sm:flex-row gap-3 justify-center items-stretch sm:items-center max-w-lg mx-auto">
      <PfButtonAction
        :label="t('flip-camera')"
        :icon="ICON_CAMERA_LENSE_SIDE"
        has-icon-left
        size="lg"
        btn-style="white"
        @click="changeCamera()" />
      <PfButtonAction
        :label="cancelLabel || t('cancel')"
        :icon="ICON_CLOSE"
        has-icon-left
        size="lg"
        btn-style="white"
        @click="$emit('cancel')" />
    </div>
    <div v-else class="text-center relative mt-6">
      <PfButtonAction
        class="mx-auto max-w-40 xs:max-w-none"
        :label="cancelLabel || t('cancel')"
        btn-style="link"
        @click="$emit('cancel')" />
      <div class="absolute -translate-y-1/2 top-1/2 right-0">
        <PfButtonAction
          :screen-reader-addon="t('flip-camera')"
          :icon="ICON_CAMERA_LENSE_SIDE"
          is-icon-only
          icon-size="lg"
          btn-type="button"
          btn-style="outline"
          @click="changeCamera()" />
      </div>
    </div>
  </div>
  <PfSpinner v-if="processing" class="mr-3" is-small>{{ t("validation-in-progress") }}</PfSpinner>
</template>

<script setup>
import { useI18n } from "vue-i18n";
import { ref, onMounted, onUnmounted, defineEmits, defineProps, defineExpose } from "vue";
import QrScanner from "qr-scanner";
import { useRouter } from "vue-router";

import QRCodeService from "@/lib/services/qr-code";
import { useKioskResetIdle } from "@/lib/composables/use-kiosk-shell";

import { CARD_NOT_FOUND } from "@/lib/consts/qr-code-error";
import ICON_CAMERA_LENSE_SIDE from "@/lib/icons/camera-lense-side.json";
import ICON_CLOSE from "@/lib/icons/close.json";

const { t } = useI18n();
const router = useRouter();
const resetKioskIdle = useKioskResetIdle();

const props = defineProps({
  errorUrlConst: {
    type: String,
    default: ""
  },
  cancelLabel: {
    type: String,
    default: ""
  },
  kioskMode: Boolean
});

const emit = defineEmits(["triggerError", "checkQRCode", "cancel"]);

const KIOSK_SCAN_ERROR_TIMEOUT_MS = 5_000;

const listCameras = ref([]);
const qrCodeVideo = ref(null);
const currentCameraMode = ref("environment");
const scanErrorVisible = ref(false);

let qrScanner = null;
let scanErrorTimeout = null;
const processing = ref(false);

onMounted(async () => {
  qrScanner = new QrScanner(qrCodeVideo.value, decryptQRCode, {
    highlightScanRegion: true,
    highlightCodeOutline: true,
    preferredCamera: "environment"
  });
  listCameras.value = await QrScanner.listCameras(true);
  qrScanner.start();
});

onUnmounted(() => {
  if (scanErrorTimeout !== null) {
    clearTimeout(scanErrorTimeout);
    scanErrorTimeout = null;
  }
  qrScanner.stop();
  qrScanner.destroy();
  qrScanner = null;
});

function showKioskScanError() {
  scanErrorVisible.value = true;
  processing.value = false;

  if (scanErrorTimeout !== null) {
    clearTimeout(scanErrorTimeout);
  }

  scanErrorTimeout = setTimeout(() => {
    scanErrorVisible.value = false;
    scanErrorTimeout = null;
  }, KIOSK_SCAN_ERROR_TIMEOUT_MS);

  qrScanner?.start();
}

defineExpose({ showScanError: showKioskScanError });

async function decryptQRCode(result) {
  if (processing.value) {
    return;
  }

  if (props.kioskMode) {
    resetKioskIdle();
  }

  processing.value = true;
  const decryptResult = await QRCodeService.decrypt(result.data);

  if (decryptResult === "" || decryptResult === null) {
    emit("triggerError");
    if (props.kioskMode) {
      showKioskScanError();
      return;
    }
    if (props.errorUrlConst) {
      router.push({ name: props.errorUrlConst, query: { error: CARD_NOT_FOUND } });
    }
    qrScanner?.stop();
    processing.value = false;
    return;
  }

  emit("checkQRCode", decryptResult);

  if (props.kioskMode) {
    return;
  }

  qrScanner?.stop();
  processing.value = false;
}

async function changeCamera() {
  if (currentCameraMode.value === "environment") {
    currentCameraMode.value = "user";
  } else {
    currentCameraMode.value = "environment";
  }
  qrScanner.setCamera(currentCameraMode.value);
}
</script>
