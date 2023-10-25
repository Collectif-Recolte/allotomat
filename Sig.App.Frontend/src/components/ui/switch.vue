<template>
  <SwitchGroup as="div" class="flex items-center">
    <slot name="left">
      <PfIcon v-if="props.iconLeft" :icon="props.iconLeft" class="mr-2.5" :class="iconColorClass" aria-hidden="true" />
    </slot>
    <Switch
      :model-value="modelValue"
      class="bg-primary-700"
      :class="[
        'relative inline-flex flex-shrink-0 h-4 w-8 border-2 border-transparent rounded-full cursor-pointer transition-colors ease-in-out duration-200 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500'
      ]"
      @update:model-value="(v) => emit('update:modelValue', v)">
      <span
        aria-hidden="true"
        :class="[
          modelValue ? 'translate-x-4' : 'translate-x-0',
          'pointer-events-none inline-block h-3 w-3 rounded-full bg-white dark:bg-grey-800 shadow transform ring-0 transition ease-in-out duration-200'
        ]" />
    </Switch>
    <SwitchLabel v-if="props.label || $slots.rightLabel" as="span" class="flex items-center">
      <slot name="right">
        <PfIcon v-if="props.iconRight" class="ml-2.5" :class="iconColorClass" :icon="props.iconRight" aria-hidden="true" />
        <span v-if="props.label" class="ml-5" :class="{ 'sr-only': showIconOnly }">
          <span class="block text-sm font-medium text-grey-900 dark:text-grey-200">{{ props.label }}</span>
          <span v-if="props.description" class="block text-sm text-grey-500 dark:text-grey-400">{{ props.description }}</span>
        </span>
      </slot>
    </SwitchLabel>
  </SwitchGroup>
</template>

<script setup>
import { defineProps, defineEmits } from "vue";
import { Switch, SwitchGroup, SwitchLabel } from "@headlessui/vue";

const props = defineProps({
  label: {
    type: String,
    default: ""
  },
  description: {
    type: String,
    default: ""
  },
  iconLeft: {
    type: Object,
    default() {
      return null;
    }
  },
  iconRight: {
    type: Object,
    default() {
      return null;
    }
  },
  iconColorClass: {
    type: String,
    default: "text-grey-500"
  },
  showIconOnly: Boolean,
  modelValue: Boolean
});

const emit = defineEmits(["update:modelValue"]);
</script>
