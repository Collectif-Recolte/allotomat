<template>
  <div
    :aria-label="hasAriaLabel ? label : null"
    class="relative rounded-full bg-white border border-primary-400"
    :class="props.size === 'lg' ? 'h-[40px] w-[40px]' : 'h-[28px] w-[28px]'">
    <div
      class="rounded-full absolute top-1/2 left-1/2 -translate-y-1/2 -translate-x-1/2"
      :class="[props.color, props.size === 'lg' ? 'h-[32px] w-[32px]' : 'h-[20px] w-[20px]']"></div>
  </div>
</template>

<script setup>
import { defineProps } from "vue";

const props = defineProps({
  label: {
    type: String,
    default: null
  },
  size: {
    type: String,
    default: "md"
  },
  color: {
    type: String,
    default: "bg-white"
  },
  hasAriaLabel: {
    type: Boolean,
    default: true
  }
});
</script>
