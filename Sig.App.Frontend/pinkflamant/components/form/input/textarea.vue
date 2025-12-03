<template>
  <FormField
    :id="id"
    v-slot="{ hasErrorState }"
    :label="label"
    :description="description"
    :col-span-class="colSpanClass"
    :errors="errors"
    :disabled="disabled"
    :required="required">
    <textarea
      :id="id"
      :name="name"
      :value="modelValue"
      :placeholder="placeholder"
      :required="required"
      :disabled="disabled"
      :rows="rows"
      class="text-[18px] min-h-11 shadow-sm block w-full border rounded-md transition-colors duration-200 ease-in-out disabled:bg-grey-100 disabled:text-grey-700"
      :class="
        hasErrorState
          ? 'border-3 text-red-600 border-red-600 placeholder-red-300 focus:ring-red-600 focus:border-red-600'
          : 'text-primary-900 border-primary-500 focus:ring-secondary-500 focus:border-secondary-500 placeholder-grey-500'
      "
      :aria-invalid="hasErrorState"
      :aria-errormessage="hasErrorState ? `${id}-error` : null"
      :aria-describedby="description ? `${id}-description` : null"
      @input="$emit('update:modelValue', $event.target.value)"
      @blur="$emit('blur', $event)"></textarea>
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
    modelValue: {
      type: [String, Number],
      default: ""
    },
    placeholder: {
      type: String,
      default: ""
    },
    rows: {
      type: Number,
      default: 3
    }
  },
  emits: ["update:modelValue", "blur"]
};
</script>
