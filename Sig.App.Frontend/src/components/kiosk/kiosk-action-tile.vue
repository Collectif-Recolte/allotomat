<template>
  <button
    type="button"
    class="flex flex-col items-center justify-center gap-4 rounded-2xl p-6 sm:p-8 min-h-[180px] sm:min-h-[220px] w-full text-white font-semibold text-h4 sm:text-h3 transition-opacity hover:opacity-95 active:opacity-90"
    :class="props.variant === 'balance' ? 'bg-secondary-800' : 'bg-primary-900'"
    @click="emit('click')">
    <PfIcon :icon="props.icon" size="2xl" class="text-white" aria-hidden="true" />
    <span class="text-center leading-tight">{{ props.label }}</span>
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
