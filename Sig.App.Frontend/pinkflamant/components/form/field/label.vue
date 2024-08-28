<i18n>
  {
    "en": {
      "mandatory-label": "* Mandatory field"
    },
    "fr": {
      "mandatory-label": "* Champ obligatoire"
    }
  }
  </i18n>

<template>
  <label
    :for="props.inputId"
    class="font-semibold transition-colors duration-200 ease-in-out"
    :class="[
      isFilter ? 'text-xs relative top-px lg:top-[2px]' : 'text-sm',
      {
        'text-red-600 dark:text-white': props.hasErrorState,
        'text-grey-500 dark:text-grey-200': props.disabled,
        'text-primary-900 dark:text-white': !props.hasErrorState && !props.disabled && !props.isFilter,
        'text-primary-700': !props.hasErrorState && !props.disabled && props.isFilter
      },
      afterLabel ? 'flex justify-between pb-2' : 'block'
    ]">
    <!-- eslint-disable-next-line vue/no-v-html -->
    <slot><span v-html="props.label" /></slot>
    <slot v-if="props.required" name="mandatoryLabel">
      <span class="font-normal text-red-500 pl-1">{{ t("mandatory-label") }}</span>
    </slot>
    <slot name="afterLabel">
      <span class="font-normal">{{ props.afterLabel }}</span>
    </slot>
  </label>
</template>

<script setup>
import { useI18n } from "vue-i18n";
import { defineProps } from "vue";

const { t } = useI18n();

const props = defineProps({
  label: {
    type: String,
    default: ""
  },
  afterLabel: {
    type: String,
    default: ""
  },
  inputId: {
    type: String,
    default: ""
  },
  hasErrorState: Boolean,
  disabled: Boolean,
  isFilter: Boolean,
  required: Boolean
});
</script>
