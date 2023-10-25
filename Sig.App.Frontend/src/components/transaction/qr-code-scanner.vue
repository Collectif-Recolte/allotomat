<i18n>
{
	"en": {
		"validation-in-progress": "Validation in progress",
    "camera-options": "Change camera",
    "cancel": "Cancel"
	},
	"fr": {
		"validation-in-progress": "Validation en cours",
    "camera-options": "Changer de caméra",
    "cancel": "Annuler"
	}
}
</i18n>

<template>
  <div class="w-sm max-w-full mb-9 relative">
    <div class="relative overflow-hidden rounded-2xl w-sm max-w-full shadow-2xl">
      <video ref="qrCodeVideo"></video>
    </div>
    <div class="text-center relative mt-6">
      <PfButtonAction
        class="mx-auto max-w-40 xs:max-w-none"
        :label="cancelLabel || t('cancel')"
        btn-style="link"
        @click="$emit('cancel')" />
      <div class="absolute -translate-y-1/2 top-1/2 right-0">
        <PfButtonAction
          :screen-reader-addon="t('camera-options')"
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

import { CARD_NOT_FOUND } from "@/lib/consts/qr-code-error";
import ICON_CAMERA_LENSE_SIDE from "@/lib/icons/camera-lense-side.json";

const { t } = useI18n();
const router = useRouter();

const props = defineProps({
  errorUrlConst: {
    type: String,
    required: true
  },
  cancelLabel: {
    type: String,
    default: ""
  }
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
    processing.value = true;
    const decryptResult = await QRCodeService.decrypt(result.data);

    if (decryptResult === "") {
      emit("triggerError");
      router.push({ name: props.errorUrlConst, query: { error: CARD_NOT_FOUND } });
    }

    emit("checkQRCode", decryptResult);
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
