<template>
  <div
    :class="hasErrorState ? 'text-red-600 font-bold' : 'text-primary-700'"
    class="absolute inset-y-0 right-0 pr-3 flex items-center pointer-events-none">
    <!-- eslint-disable-next-line @intlify/vue-i18n/no-raw-text -->
    <span class="text-[18px]">$</span>
  </div>
</template>

<script setup>
import { defineProps, computed } from "vue";

const props = defineProps({
  errors: {
    type: Array,
    default: null
  }
});

const hasErrorState = computed(() => props.errors && props.errors.length > 0);
</script>
