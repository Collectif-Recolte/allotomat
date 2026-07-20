<template>
  <ul v-if="programNames.length > 0" class="list-none p-0 m-0 text-center">
    <li
      v-for="programName in programNames"
      :key="programName"
      class="font-bold text-primary-700 leading-snug"
      :class="sizeClass">
      {{ programName }}
    </li>
  </ul>
</template>

<script setup>
import { computed, defineProps } from "vue";

import { useKioskToken } from "@/lib/composables/use-kiosk-token";

const props = defineProps({
  size: {
    type: String,
    default: "md",
    validator: (value) => ["sm", "md"].includes(value)
  }
});

const { programNames } = useKioskToken();

const sizeClass = computed(() => (props.size === "sm" ? "text-h3 sm:text-h2" : "text-h2 sm:text-d3"));
</script>
