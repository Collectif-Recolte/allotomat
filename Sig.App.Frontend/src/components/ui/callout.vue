<template>
  <div role="alert" class="rounded-md border-l-4 p-4" :class="[theme.bg, theme.border]">
    <div class="flex">
      <div class="flex-shrink-0 flex items-center">
        <PfIcon class="shrink-0" :class="theme.icon" size="md" :icon="icon" aria-hidden="true" />
      </div>
      <div class="ml-3 text-sm mb-0" :class="theme.text">
        <slot>
          <p v-if="message" class="mb-0" :class="{ 'callout-message-html': allowHtml }">
            <template v-if="allowHtml">
              <!-- eslint-disable-next-line vue/no-v-html -->
              <span v-html="message" />
            </template>
            <template v-else>{{ message }}</template>
          </p>
        </slot>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, defineProps } from "vue";

import { CALLOUT_SUCCESS, CALLOUT_INFO, CALLOUT_WARNING, CALLOUT_ERROR } from "@/lib/consts/callout";
import EXCLAMATION_ICON from "@/lib/icons/exclamation-circle-fill.json";
import ICON_INFO from "@/lib/icons/info.json";
import ICON_SHIELD_CHECK from "@/lib/icons/shield-check.json";

const props = defineProps({
  variant: {
    type: String,
    default: CALLOUT_INFO,
    validator: (v) => [CALLOUT_SUCCESS, CALLOUT_INFO, CALLOUT_WARNING, CALLOUT_ERROR].includes(v)
  },
  message: {
    type: String,
    default: ""
  },
  allowHtml: {
    type: Boolean,
    default: false
  }
});

const theme = computed(() => {
  const themes = {
    [CALLOUT_SUCCESS]: {
      bg: "bg-green-50 dark:bg-grey-800",
      border: "border-green-500 dark:border-green-500",
      text: "text-green-800 dark:text-green-300",
      icon: "text-green-500 dark:text-green-400"
    },
    [CALLOUT_INFO]: {
      bg: "bg-primary-50 dark:bg-grey-800",
      border: "border-primary-500 dark:border-primary-500",
      text: "text-primary-800 dark:text-primary-300",
      icon: "text-primary-500 dark:text-primary-400"
    },
    [CALLOUT_WARNING]: {
      bg: "bg-yellow-50 dark:bg-grey-800",
      border: "border-yellow-500 dark:border-yellow-500",
      text: "text-yellow-800 dark:text-yellow-300",
      icon: "text-yellow-500 dark:text-yellow-400"
    },
    [CALLOUT_ERROR]: {
      bg: "bg-red-50 dark:bg-grey-800",
      border: "border-red-500 dark:border-red-500",
      text: "text-red-800 dark:text-red-300",
      icon: "text-red-500 dark:text-red-400"
    }
  };
  return themes[props.variant] ?? themes[CALLOUT_INFO];
});

const icon = computed(() => {
  switch (props.variant) {
    case CALLOUT_SUCCESS:
      return ICON_SHIELD_CHECK;
    case CALLOUT_INFO:
      return ICON_INFO;
    case CALLOUT_WARNING:
    case CALLOUT_ERROR:
    default:
      return EXCLAMATION_ICON;
  }
});
</script>

<style scoped>
.callout-message-html :deep(b) {
  font-weight: 600;
}
</style>
