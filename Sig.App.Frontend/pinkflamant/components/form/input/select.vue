<template>
  <FormField
    :id="id"
    v-slot="{ hasErrorState }"
    :label="label"
    :description="description"
    :col-span-class="colSpanClass"
    :errors="errors"
    :disabled="disabled"
    :has-hidden-label="hasHiddenLabel"
    :required="required">
    <select
      :id="id"
      :value="value"
      :name="name"
      :autocomplete="autocomplete"
      :required="required"
      :disabled="disabled"
      class="pf-select text-[18px] min-h-11 shadow-sm block w-full rounded-md transition-colors duration-200 ease-in-out disabled:bg-grey-100 disabled:text-grey-700"
      :class="
        hasErrorState
          ? 'border-3 text-red-600 border-red-600 placeholder-red-300 focus:ring-red-600 focus:border-red-600'
          : 'text-primary-900 border-primary-500 focus:ring-secondary-500 focus:border-secondary-500 placeholder-grey-500'
      "
      :aria-label="hasHiddenLabel ? label : null"
      :aria-invalid="hasErrorState"
      :aria-errormessage="hasErrorState ? `${id}-error` : null"
      :aria-describedby="description ? `${id}-description` : null"
      @input="$emit('input', $event.target.value)">
      <option v-if="placeholder" value="" disabled selected hidden>
        {{ placeholder }}
      </option>
      <option v-for="option in options" :key="option" :value="option.value" :disabled="!!option.isDisabled">
        {{ option.label }}
      </option>
    </select>
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
    value: {
      type: [String],
      default: ""
    },
    placeholder: {
      type: String,
      default: ""
    },
    autocomplete: {
      type: String,
      default: ""
    },
    options: {
      type: Array,
      default() {
        return [];
      }
    }
  },
  emits: ["input"]
};
</script>
