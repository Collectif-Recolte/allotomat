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
      @update:modelValue="(isChecked) => updateCheckbox(option.value, isChecked)" />
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
    modelValue: {
      type: Array,
      default() {
        return [];
      }
    },
    options: {
      type: Array,
      required: true,
      default() {
        return [];
      }
    },
    isFilter: Boolean
  },
  emits: ["update:modelValue"],
  computed: {
    hasErrorState() {
      return this.errors && this.errors.length > 0;
    }
  },
  methods: {
    updateCheckbox(value, isChecked) {
      let newValue;
      if (isChecked) {
        // Ajouter la valeur si cochée
        newValue = [...this.modelValue, value];
      } else {
        // Retirer la valeur si décochée
        newValue = this.modelValue.filter((v) => v !== value);
      }
      this.$emit("update:modelValue", newValue);
    },
    isChecked(value) {
      return this.modelValue.indexOf(value) !== -1;
    }
  }
};
</script>
