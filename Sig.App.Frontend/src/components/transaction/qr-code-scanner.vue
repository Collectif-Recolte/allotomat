<i18n>
{
	"en": {
		"validation-in-progress": "Validation in progress",
    "flip-camera": "Flip the camera",
    "scan-instruction": "Bring your card close to the reader to scan the QR code in the center of the screen.",
    "cancel": "Cancel"
	},
	"fr": {
		"validation-in-progress": "Validation en cours",
    "flip-camera": "Retourner la caméra",
    "scan-instruction": "Approchez votre carte du lecteur pour scanner le code QR au centre de l'écran.",
    "cancel": "Annuler"
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
    <p v-if="props.kioskMode" class="text-center text-h3 text-primary-700 my-8">
      {{ t("scan-instruction") }}
    </p>
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
import { ref, onMounted, onUnmounted, defineEmits, defineProps } from "vue";
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
    required: true
  },
  cancelLabel: {
    type: String,
    default: ""
  },
  kioskMode: Boolean
});

const emit = defineEmits(["triggerError", "checkQRCode", "cancel"]);

const listCameras = ref([]);
const qrCodeVideo = ref(null);
const currentCameraMode = ref("environment");

let qrScanner = null;
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
  qrScanner.stop();
  qrScanner.destroy();
  qrScanner = null;
});

async function decryptQRCode(result) {
  if (!processing.value) {
    if (props.kioskMode) {
      resetKioskIdle();
    }
    processing.value = true;
    const decryptResult = await QRCodeService.decrypt(result.data);

    if (decryptResult === "" || decryptResult === null) {
      emit("triggerError");
      if (props.errorUrlConst) {
        router.push({ name: props.errorUrlConst, query: { error: CARD_NOT_FOUND } });
      }
    } else {
      emit("checkQRCode", decryptResult);
    }

    qrScanner.stop();
    processing.value = false;
  }
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
