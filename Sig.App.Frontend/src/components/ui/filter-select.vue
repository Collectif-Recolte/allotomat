<i18n>
  {
    "en": {
      "filter": "Filter",
      "sort": "Sort order",
      "reset-filters": "Reset"
    },
    "fr": {
      "filter": "Filtrer",
      "sort": "Ordre de tri",
      "reset-filters": "Réinitialiser"
    }
  }
</i18n>

<template>
  <div class="relative inline-block group pf-transition-visibility pf-transition-visibility--focus-only">
    <button class="pf-button pf-button--outline px-3 min-h-11">
      {{ props.label }}
      <span
        v-if="props.activeFiltersCount > 0"
        class="bg-tertiary-500 w-6 h-6 flex items-center justify-center rounded-full text-p3 leading-none ml-4 text-white">
        {{ props.activeFiltersCount }}
      </span>
      <PfIcon :icon="ICON_ARROW_BOTTOM" size="xxs" :class="props.activeFiltersCount > 0 ? 'ml-2' : 'ml-12'" />
    </button>
    <div
      class="text-left absolute z-50 bottom-1 right-0 translate-y-full min-w-64 max-h-60 overflow-auto bg-white rounded-md border rounded-tr-none border-primary-700 px-4 py-3 transition ease-in-out duration-300 pf-transition-visibility__content h-remove-margin">
      <slot></slot>
    </div>
  </div>
</template>

<script setup>
import { defineProps } from "vue";

import ICON_ARROW_BOTTOM from "@/lib/icons/arrow-bottom.json";

const props = defineProps({
  label: {
    type: String,
    default: ""
  },
  activeFiltersCount: {
    type: Number,
    default: 0,
    validator(value) {
      return value >= 0;
    }
  }
});
</script>
