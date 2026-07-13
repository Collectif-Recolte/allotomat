<template>
  <button
    type="button"
    class="flex flex-col items-center justify-center gap-4 rounded-4xl p-6 sm:p-8 min-h-[220px] sm:min-h-[276px] w-full text-white font-bold text-h3 transition-opacity hover:opacity-95 active:opacity-90 shadow-[0_-6px_0_0_rgba(0,0,0,0.25)_inset]"
    :class="props.variant === 'balance' ? 'bg-secondary-800 shadow-secondary-900' : 'bg-primary-800 shadow-primary-900 '"
    @click="emit('click')">
    <PfIcon :icon="props.icon" custom-size-class="w-16 h-16" class="text-white" aria-hidden="true" />
    <span class="text-center leading-tight min-h-[60px] flex items-center">{{ props.label }}</span>
  </button>
</template>

<script setup>
import { defineEmits, defineProps } from "vue";

const props = defineProps({
  label: { type: String, required: true },
  icon: { type: Object, required: true },
  variant: {
    type: String,
    default: "purchase",
    validator: (value) => ["purchase", "balance"].includes(value)
  }
});

const emit = defineEmits(["click"]);
</script>
