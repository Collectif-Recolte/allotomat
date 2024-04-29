<template>
  <!-- Field container -->
  <div class="pf-form-field" :class="[colSpanClass, { 'pf-form-field--hidden-label': hasHiddenLabel }]">
    <!-- Labels for text-like inputs -->
    <FormFieldLabel
      v-if="!isRadioOrCheck && !hasHiddenLabel"
      class="mb-1"
      :input-id="id"
      :label="label"
      :after-label="afterLabel"
      :has-error-state="hasErrorState"
      :disabled="disabled" />

    <div :class="{ 'relative flex items-start': isRadioOrCheck }">
      <!-- Input -->
      <div :class="{ 'flex items-center h-5': isRadioOrCheck }">
        <slot :hasErrorState="hasErrorState"></slot>
      </div>

      <!-- Labels for radio or checkbox -->
      <div v-if="isRadioOrCheck && !hasHiddenLabel" class="ml-3 text-sm h-remove-margin">
        <FormFieldLabel
          :input-id="id"
          :label="label"
          :has-error-state="hasErrorState"
          :disabled="disabled"
          :is-filter="isFilter" />
        <div v-if="description || $slots.description" class="mt-1" :class="hasErrorState ? 'text-red-500' : 'text-grey-500'">
          <slot name="description">
            <p v-if="description" :id="`${id}-description`" class="mb-0 text-p4 leading-none">
              {{ description }}
            </p>
          </slot>
        </div>
      </div>
    </div>

    <!-- Field footer -->
    <template v-if="!isRadioOrCheck">
      <div
        v-if="description || $slots.description"
        class="mt-2"
        :class="hasErrorState ? 'text-red-600 dark:text-white' : 'text-grey-500'">
        <slot name="description">
          <p v-if="description" :id="`${id}-description`" class="mb-0 text-p4 leading-none">
            {{ description }}
          </p>
        </slot>
      </div>
    </template>
    <template v-if="!isRadio">
      <transition name="fade">
        <div v-if="hasErrorState || $slots.feedback" class="mt-2 text-red-600 dark:text-white">
          <slot name="feedback">
            <p v-if="hasErrorState" :id="`${id}-error`" class="mb-0 text-sm flex items-start gap-x-1.5">
              <Icon :icon="ICON_WARNING" size="md" />
              <span>{{ errors[0] }}</span>
            </p>
          </slot>
        </div>
      </transition>
    </template>
  </div>
</template>

<script>
import Icon from "../../icon";
import FormFieldLabel from "./label";
import ICON_WARNING from "../../../icons/exclamation-circle-fill.json";

export const commonFieldProps = {
  colSpanClass: {
    type: String,
    default: ""
  },
  id: {
    type: String,
    default: ""
  },
  name: {
    type: String,
    default: ""
  },
  label: {
    type: String,
    default: "",
    required: true
  },
  afterLabel: {
    type: String,
    default: ""
  },
  description: {
    type: String,
    default: ""
  },
  errors: {
    type: Array,
    default() {
      return [];
    }
  },
  required: Boolean,
  disabled: Boolean,
  isFilter: Boolean,
  hasHiddenLabel: Boolean
};

export default {
  components: {
    FormFieldLabel,
    Icon
  },
  props: {
    ...commonFieldProps,
    isRadio: Boolean,
    isCheckbox: Boolean,
    hasHiddenLabel: Boolean
  },
  data() {
    return {
      ICON_WARNING
    };
  },
  computed: {
    hasErrorState() {
      return this.errors && this.errors.length > 0;
    },
    isRadioOrCheck() {
      return this.isRadio || this.isCheckbox;
    }
  }
};
</script>
