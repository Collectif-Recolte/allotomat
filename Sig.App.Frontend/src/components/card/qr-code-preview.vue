<template>
  <div>
    <div ref="qrCodeImage" class="qrcode-container my-6"></div>
  </div>
</template>

<script setup>
import QrCreator from "qr-creator";
import { ref, onMounted, defineProps } from "vue";

const qrCodeImage = ref(null);

const props = defineProps({
  qrCode: {
    type: String,
    default: ""
  }
});

onMounted(async () => {
  QrCreator.render(
    {
      text: props.qrCode,
      radius: 0, // 0.0 to 0.5
      ecLevel: "H", // L, M, Q, H
      fill: "#1c5857", // foreground color
      background: null, // color or null for transparent
      size: 404 // in pixels
    },
    qrCodeImage.value
  );
});
</script>

<style lang="postcss">
.qrcode-container {
  canvas {
    @apply mx-auto max-w-full;
  }
}
</style>
