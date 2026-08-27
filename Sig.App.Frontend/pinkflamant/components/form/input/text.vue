<i18n>
{
  "en": {
    "show-password": "Show password",
    "hide-password": "Hide password"
  },
  "fr": {
    "show-password": "Afficher le mot de passe",
    "hide-password": "Masquer le mot de passe"
  }
}
</i18n>

<template>
  <FormField
    :id="id"
    v-slot="{ hasErrorState }"
    :label="label"
    :after-label="afterLabel"
    :description="description"
    :col-span-class="colSpanClass"
    :errors="errors"
    :disabled="disabled"
    :has-hidden-label="hasHiddenLabel"
    :required="required"
    :is-large="isLarge">
    <div class="flex rounded-md shadow-sm relative">
      <span
        v-if="addOn || $slots.addOn"
        class="inline-flex items-center px-3 rounded-l-md border border-r-0 border-grey-300 bg-grey-100 text-grey-600 sm:text-p3">
        <slot name="add-on">{{ addOn }}</slot>
      </span>

      <div
        v-else-if="hasLeadingIcon"
        class="absolute inset-y-0 left-0 pl-3 flex items-center text-primary-700 pointer-events-none">
        <slot name="leadingIcon">
          <PfIcon class="h-5 w-5" :icon="leadingIcon" aria-hidden="true" />
        </slot>
      </div>

      <input
        :id="id"
        :value="value"
        :type="resolvedInputType"
        :inputmode="inputMode"
        :name="name"
        :autocomplete="autocomplete"
        :placeholder="placeholder"
        :required="required"
        :disabled="disabled"
        :max="max"
        :min="min"
        :readonly="isDatepicker || isReadOnly"
        class="text-[18px] flex-1 block w-full min-w-0 min-h-11 transition-colors duration-200 ease-in-out disabled:bg-grey-100 disabled:text-grey-700 read-only:bg-grey-50"
        :class="[
          addOn ? 'rounded-none rounded-r-md' : 'rounded-md',
          hasLeadingIcon ? 'pl-10' : 'pl-3',
          hasTrailingIcon ? 'pr-10' : 'pr-3',
          isDatepicker && !disabled ? 'cursor-pointer' : '',
          hasErrorState
            ? 'text-red-600 border-3 border-red-600 placeholder-red-300 focus:ring-red-600 focus:border-red-600'
            : 'text-primary-900 border-primary-500 focus:ring-secondary-500 focus:border-secondary-500 placeholder-grey-500 read-only:text-grey-700 read-only:focus:border-primary-500 read-only:focus:ring-primary-500'
        ]"
        :aria-invalid="hasErrorState"
        :aria-errormessage="hasErrorState ? `${id}-error` : null"
        :aria-describedby="description ? `${id}-description` : null"
        @input="$emit('input', $event.target.value)"
        @keypress="$emit('keypress', $event)" />
      <slot name="trailingIcon">
        <div
          v-if="showPasswordToggle"
          class="absolute inset-y-0 right-0 pr-3 flex items-center"
          :class="hasErrorState ? 'text-red-600' : 'text-primary-700'">
          <button
            type="button"
            class="flex items-center justify-center p-1 rounded focus:outline-none focus:ring-2 focus:ring-secondary-500 disabled:opacity-50"
            :aria-label="passwordToggleLabel"
            :aria-pressed="isPasswordVisible"
            :disabled="disabled"
            @click="isPasswordVisible = !isPasswordVisible">
            <PfIcon class="h-5 w-5" :icon="passwordToggleIcon" aria-hidden="true" />
          </button>
        </div>
        <div
          v-else-if="trailingIcon"
          class="absolute inset-y-0 right-0 pr-3 flex items-center pointer-events-none"
          :class="hasErrorState ? 'text-red-600' : 'text-primary-700'">
          <PfIcon class="h-5 w-5" :icon="trailingIcon" aria-hidden="true" />
        </div>
      </slot>
    </div>
  </FormField>
</template>

<script>
import { useI18n } from "vue-i18n";

import FormField, { commonFieldProps } from "../field/index";
import ICON_EYE from "@/lib/icons/eye.json";
import ICON_EYE_OFF from "@/lib/icons/eye-off.json";

export default {
  components: {
    FormField
  },
  props: {
    ...commonFieldProps,
    value: {
      type: [String, Number],
      default: ""
    },
    placeholder: {
      type: String,
      default: ""
    },
    autocomplete: {
      type: String,
      default: "on"
    },
    addOn: {
      type: String,
      default: ""
    },
    inputType: {
      type: String,
      default: "text"
    },
    inputMode: {
      type: String,
      default: "text"
    },
    leadingIcon: {
      type: Object,
      default() {
        return null;
      }
    },
    trailingIcon: {
      type: Object,
      default() {
        return null;
      }
    },
    isStrokeIcon: Boolean,
    isDatepicker: Boolean,
    isReadOnly: Boolean,
    min: {
      type: Number,
      default: null
    },
    max: {
      type: Number,
      default: null
    },
    isLarge: Boolean
  },
  emits: ["input", "keypress"],
  setup() {
    const { t } = useI18n();
    return { t };
  },
  data() {
    return {
      isPasswordVisible: false
    };
  },
  computed: {
    hasLeadingIcon() {
      return this.leadingIcon || this.$slots.leadingIcon;
    },
    showPasswordToggle() {
      return this.inputType === "password" && !this.trailingIcon && !this.$slots.trailingIcon;
    },
    hasTrailingIcon() {
      return this.trailingIcon || this.$slots.trailingIcon || this.showPasswordToggle;
    },
    resolvedInputType() {
      if (this.inputType === "password" && this.isPasswordVisible) {
        return "text";
      }
      return this.inputType;
    },
    passwordToggleIcon() {
      return this.isPasswordVisible ? ICON_EYE_OFF : ICON_EYE;
    },
    passwordToggleLabel() {
      return this.isPasswordVisible ? this.t("hide-password") : this.t("show-password");
    }
  }
};
</script>
