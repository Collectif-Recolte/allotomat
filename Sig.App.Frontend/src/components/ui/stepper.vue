<i18n>
  {
    "en": {
      "step": "Step"
    },
    "fr": {
      "step": "Étape"
    }
  }
  </i18n>

<template>
  <div>
    <!-- eslint-disable-next-line -->
    <p class="text-p2 mb-2 font-semibold">{{ t("step") }} {{ stepNumber }} : {{ props.stepLabel }}</p>
    <div class="rounded-full bg-primary-100 overflow-hidden w-full">
      <div
        class="h-2 bg-yellow-500 rounded-full transition-width duration-300 ease-in-out"
        :style="{ width: progressPercent }"></div>
    </div>
  </div>
</template>

<script setup>
import { defineProps, computed } from "vue";
import { useI18n } from "vue-i18n";

const { t } = useI18n();

const props = defineProps({
  stepLabel: { type: String, default: "" },
  stepCount: { type: Number, default: 1 },
  stepNumber: { type: Number, default: 1 }
});

const progressPercent = computed(() => `${(props.stepNumber / props.stepCount) * 100}%`);
</script>
