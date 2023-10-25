<template>
  <FormField
    :id="id"
    v-slot="{ hasErrorState }"
    :label="label"
    :description="description"
    :col-span-class="colSpanClass"
    :errors="errors"
    :disabled="disabled"
    :is-filter="isFilter"
    is-checkbox>
    <input
      :id="id"
      :name="name"
      :checked="checked"
      :value="value"
      :required="required"
      :disabled="disabled"
      type="checkbox"
      class="h-4 w-4 rounded disabled:text-grey-500 disabled:bg-grey-100"
      :class="
        hasErrorState
          ? 'text-red-600 border-red-600 focus:ring-red-900 focus:border-red-600'
          : 'text-primary-700 border-grey-300 focus:ring-yellow-500 focus:border-yellow-500'
      "
      :aria-invalid="hasErrorState"
      :aria-errormessage="hasErrorState ? `${id}-error` : null"
      :aria-describedby="description ? `${id}-description` : null"
      @input="$emit('input', $event.target.checked)" />
  </FormField>
</template>

<script>
import FormField, { commonFieldProps } from "../field/index";

export default {
  components: {
    FormField
  },
  props: {
    ...commonFieldProps,
    checked: Boolean,
    value: {
      type: [Boolean, String],
      default: false
    },
    isFilter: Boolean
  },
  emits: ["input"]
};
</script>
