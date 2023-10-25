<template>
  <label
    :for="inputId"
    class="font-semibold transition-colors duration-200 ease-in-out"
    :class="[
      isFilter ? 'text-xs relative top-px lg:top-[2px]' : 'text-sm',
      {
        'text-red-600 dark:text-white': hasErrorState,
        'text-grey-500 dark:text-grey-200': disabled,
        'text-primary-900 dark:text-white': !hasErrorState && !disabled && !isFilter,
        'text-primary-700': !hasErrorState && !disabled && isFilter
      },
      afterLabel ? 'flex justify-between pb-2' : 'block'
    ]">
    <!-- eslint-disable-next-line vue/no-v-html -->
    <slot><span v-html="label" /></slot>
    <slot name="afterLabel">
      <span class="font-normal">{{ afterLabel }}</span>
    </slot>
  </label>
</template>

<script>
export default {
  props: {
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
    isFilter: Boolean
  }
};
</script>
