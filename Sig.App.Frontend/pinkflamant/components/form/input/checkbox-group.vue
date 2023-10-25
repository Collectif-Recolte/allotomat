<template>
  <FormFieldset
    :id="id"
    :legend="label"
    :description="description"
    :has-error-state="hasErrorState"
    :errors="errors"
    :is-filter="isFilter">
    <PfFormInputCheckbox
      v-for="(option, index) in options"
      :id="option.value"
      :key="index"
      :label="option.label"
      :description="option.description"
      :col-span-class="colSpanClass"
      :errors="errors"
      :disabled="disabled"
      :checked="isChecked(option.value)"
      :is-filter="isFilter"
      @input="(e) => updateCheckbox(option.value, e)" />
  </FormFieldset>
</template>

<script>
import { commonFieldProps } from "../field/index";
import FormFieldset from "../fieldset";

export default {
  components: {
    FormFieldset
  },
  props: {
    ...commonFieldProps,
    value: {
      type: Array,
      default() {
        return [];
      }
    },
    options: {
      type: Array,
      required: true,
      default() {
        return null;
      }
    },
    isFilter: Boolean
  },
  emits: ["input"],
  computed: {
    hasErrorState() {
      return this.errors && this.errors.length > 0;
    }
  },
  methods: {
    updateCheckbox(value, isChecked) {
      this.$emit("input", { value, isChecked });
    },
    isChecked(value) {
      return this.value.indexOf(value) !== -1;
    }
  }
};
</script>
