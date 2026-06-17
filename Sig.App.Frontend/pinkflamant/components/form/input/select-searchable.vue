<i18n>
{
  "en": {
    "clear-search": "Clear search",
    "toggle-options": "Show or hide options"
  },
  "fr": {
    "clear-search": "Effacer la recherche",
    "toggle-options": "Afficher ou masquer les choix"
  }
}
</i18n>

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
    <Combobox
      v-slot="{ open: isOpen }"
      :model-value="internalValue"
      :nullable="nullable"
      :disabled="disabled"
      :name="name"
      @update:model-value="onSelect">
      <ComboboxOpenWatcher :open="isOpen" @close="resetQuery" />
      <div class="relative">
        <PfIcon
          class="absolute top-1/2 -translate-y-1/2 left-3 flex items-center text-grey-500 pointer-events-none z-10"
          :icon="leadingIcon"
          size="lg" />
        <ComboboxInput
          :id="id"
          autocomplete="off"
          :required="required"
          :disabled="disabled"
          :placeholder="placeholder"
          :display-value="displayValue"
          class="pf-select text-[18px] min-h-11 shadow-sm block w-full rounded-md transition-colors duration-200 ease-in-out disabled:bg-grey-100 disabled:text-grey-700 px-10 pr-10"
          :class="inputClass(hasErrorState)"
          :aria-label="hasHiddenLabel ? label : null"
          :aria-invalid="hasErrorState"
          :aria-errormessage="hasErrorState ? `${id}-error` : null"
          :aria-describedby="description ? `${id}-description` : null"
          @change="onQueryChange"
          @keydown="onInputKeydown" />
        <div class="absolute inset-y-0 right-0 pr-3 flex items-center">
          <button
            v-if="query"
            type="button"
            class="text-grey-500 hover:text-primary-700 focus:text-primary-700"
            :aria-label="t('clear-search')"
            @mousedown.prevent
            @click.stop="clearQuery">
            <PfIcon :icon="clearIcon" size="xs" aria-hidden="true" />
          </button>
          <ComboboxButton
            v-else
            class="text-primary-700 focus:outline-none disabled:opacity-50"
            :aria-label="t('toggle-options')">
            <svg
              class="h-5 w-5 transition-transform duration-200"
              :class="{ 'rotate-180': isOpen }"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
              aria-hidden="true">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
            </svg>
          </ComboboxButton>
        </div>
        <ComboboxOptions
          class="absolute z-50 mt-1 w-full bg-white shadow-lg max-h-60 rounded-md py-1 text-base ring-1 ring-grey-50 overflow-auto outline-1">
          <ComboboxOption
            v-for="option in filteredOptions"
            :key="option.value"
            v-slot="{ active }"
            :value="option.value"
            :disabled="!!option.isDisabled"
            as="template">
            <li
              :class="[
                'cursor-pointer select-none relative py-2 pl-3 pr-9 transition-colors duration-200 ease-in-out',
                active ? 'bg-secondary-500' : 'text-primary-900 hover:bg-grey-100'
              ]"
              @mousedown="markUserSelection">
              <span class="block truncate">{{ option.label }}</span>
            </li>
          </ComboboxOption>
          <li
            v-if="filteredOptions.length === 0 && query"
            class="cursor-default select-none relative py-2 pl-3 pr-9 text-grey-500">
            <span class="block truncate">{{ noResultsFound }}</span>
          </li>
        </ComboboxOptions>
      </div>
    </Combobox>
  </FormField>
</template>

<script>
import { useI18n } from "vue-i18n";
import { Combobox, ComboboxButton, ComboboxInput, ComboboxOption, ComboboxOptions } from "@headlessui/vue";

import FormField, { commonFieldProps } from "../field/index";
import ComboboxOpenWatcher from "./combobox-open-watcher.vue";
import ICON_SEARCH from "../../../icons/search.json";
import ICON_CLOSE from "../../../icons/close.json";
import { filterOptions } from "../../../lib/filter-options";

export default {
  components: {
    FormField,
    Combobox,
    ComboboxButton,
    ComboboxInput,
    ComboboxOption,
    ComboboxOptions,
    ComboboxOpenWatcher
  },
  props: {
    ...commonFieldProps,
    value: {
      type: [String, null],
      default: ""
    },
    modelValue: {
      type: [String, null],
      default: undefined
    },
    placeholder: {
      type: String,
      default: ""
    },
    noResultsFound: {
      type: String,
      default: ""
    },
    options: {
      type: Array,
      default() {
        return [];
      }
    },
    nullable: {
      type: Boolean,
      default: true
    }
  },
  emits: ["input", "update:modelValue"],
  setup() {
    const { t } = useI18n();
    return { t };
  },
  data() {
    return {
      query: "",
      userInitiatedSelection: false,
      leadingIcon: ICON_SEARCH,
      clearIcon: ICON_CLOSE
    };
  },
  computed: {
    internalValue() {
      const raw = this.modelValue !== undefined ? this.modelValue : this.value;
      if (raw === "" || raw === undefined) {
        return null;
      }
      return raw;
    },
    filteredOptions() {
      return filterOptions(this.options, this.query);
    }
  },
  methods: {
    displayValue(value) {
      if (value === null || value === undefined || value === "") {
        return "";
      }
      const option = this.options.find((item) => item.value === value);
      return option ? option.label : "";
    },
    inputClass(hasErrorState) {
      return hasErrorState
        ? "border-3 text-red-600 border-red-600 placeholder-red-300 focus:ring-red-600 focus:border-red-600"
        : "text-primary-900 border-primary-500 focus:ring-secondary-500 focus:border-secondary-500 placeholder-grey-500";
    },
    onQueryChange(event) {
      this.query = event.target.value;
    },
    markUserSelection() {
      this.userInitiatedSelection = true;
    },
    onInputKeydown(event) {
      if (event.key === "Enter") {
        this.userInitiatedSelection = true;
      }
    },
    onSelect(value) {
      if (!this.userInitiatedSelection) {
        return;
      }
      this.userInitiatedSelection = false;
      this.resetQuery();
      const emitted = value ?? "";
      this.$emit("input", emitted);
      this.$emit("update:modelValue", value);
    },
    resetQuery() {
      this.query = "";
    },
    clearQuery() {
      this.resetQuery();
    }
  }
};
</script>
