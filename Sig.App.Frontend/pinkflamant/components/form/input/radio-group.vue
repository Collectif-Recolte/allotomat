<template>
  <FormFieldset :id="id" :legend="label" :description="description" :has-error-state="hasErrorState" :errors="errors">
    <FormField
      v-for="(option, index) in options"
      :id="option.id"
      :key="index"
      :label="option.label"
      :description="option.description"
      :col-span-class="colSpanClass"
      :errors="errors"
      :disabled="disabled"
      is-radio>
      <input
        :id="option.id"
        v-model="choice"
        type="radio"
        :name="name"
        :value="option.value"
        :checked="choice === option.value"
        :disabled="disabled"
        class="h-4 w-4 disabled:text-grey-500"
        :class="
          hasErrorState
            ? 'text-red-600 border-red-600 focus:ring-red-900 focus:border-red-600'
            : 'text-primary-700 border-grey-300 focus:ring-yellow-500 focus:border-yellow-500'
        "
        :aria-describedby="option.description ? `${option.id}-description` : null"
        @input="$emit('input', $event.target.value)" />
    </FormField>
  </FormFieldset>
</template>

<script>
import FormField, { commonFieldProps } from "../field/index";
import FormFieldset from "../fieldset";

export default {
  components: {
    FormField,
    FormFieldset
  },
  props: {
    ...commonFieldProps,
    value: {
      type: [String, Number],
      default: ""
    },
    options: {
      type: Array,
      required: true,
      default() {
        return null;
      }
    }
  },
  emits: ["input"],
  data() {
    return {
      // Necessary to avoid loop in validation provider
      choice: ""
    };
  },
  computed: {
    hasErrorState() {
      return this.errors && this.errors.length > 0;
    }
  },
  mounted() {
    this.choice = this.value;
  }
};
</script>
