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
    <div class="relative">
      <!-- Input de recherche -->
      <input
        :id="id"
        ref="searchInput"
        :value="searchValue"
        :name="name"
        autocomplete="off"
        :required="required"
        :disabled="disabled"
        :placeholder="placeholder"
        class="pf-select text-[18px] min-h-11 shadow-sm block w-full rounded-md transition-colors duration-200 ease-in-out disabled:bg-grey-100 disabled:text-grey-700 pr-10"
        :class="
          hasErrorState
            ? 'border-3 text-red-600 border-red-600 placeholder-red-300 focus:ring-red-600 focus:border-red-600'
            : 'text-primary-900 border-primary-500 focus:ring-secondary-500 focus:border-secondary-500 placeholder-grey-500'
        "
        :aria-label="hasHiddenLabel ? label : null"
        :aria-invalid="hasErrorState"
        :aria-errormessage="hasErrorState ? `${id}-error` : null"
        :aria-describedby="description ? `${id}-description` : null"
        :aria-expanded="isOpen"
        :aria-haspopup="true"
        role="combobox"
        @input="onSearchInput"
        @focus="onFocus"
        @blur="onBlur"
        @keydown="onKeydown" />

      <!-- Icône de dropdown -->
      <div class="absolute inset-y-0 right-0 pr-3 flex items-center pointer-events-none">
        <svg
          class="h-5 w-5 text-primary-700 transition-transform duration-200"
          :class="{ 'rotate-180': isOpen }"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 9l-7 7-7-7" />
        </svg>
      </div>

      <!-- Liste déroulante -->
      <div
        v-if="isOpen && filteredOptions.length > 0"
        class="absolute z-50 mt-1 w-full bg-white shadow-lg max-h-60 rounded-md py-1 text-base ring-1 ring-black ring-opacity-5 overflow-auto outline-1"
        role="listbox"
        @mousedown.stop>
        <div
          v-for="(option, index) in filteredOptions"
          :key="option.value"
          :class="[
            'cursor-pointer select-none relative py-2 pl-3 pr-9',
            index === highlightedIndex ? 'bg-secondary-500 text-white' : 'text-primary-900 hover:bg-grey-100'
          ]"
          role="option"
          :aria-selected="index === highlightedIndex"
          @mousedown.prevent="selectOption(option)"
          @mouseenter="highlightedIndex = index">
          <span class="block truncate" :class="{ 'font-semibold': index === highlightedIndex }">
            {{ option.label }}
          </span>
        </div>
      </div>

      <!-- Message quand aucun résultat -->
      <div
        v-if="isOpen && filteredOptions.length === 0 && searchValue"
        class="absolute z-50 mt-1 w-full bg-white shadow-lg rounded-md py-1 text-base ring-1 ring-black ring-opacity-5"
        @mousedown.stop>
        <div class="cursor-pointer select-none relative py-2 pl-3 pr-9 text-grey-500">
          <span class="block truncate">{{ noResultsFound }}</span>
        </div>
      </div>
    </div>
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
    noResultsFound: {
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
  emits: ["input"],
  data() {
    return {
      searchValue: "",
      isOpen: false,
      highlightedIndex: -1,
      blurTimeout: null
    };
  },
  computed: {
    filteredOptions() {
      if (!this.searchValue) {
        return this.options;
      }
      const searchTerm = this.searchValue.toLowerCase();
      return this.options.filter((option) => option.label.toLowerCase().includes(searchTerm) && !option.isDisabled);
    },
    selectedOption() {
      return this.options.find((option) => option.value === this.value);
    }
  },
  watch: {
    value(newValue) {
      if (newValue && this.selectedOption) {
        this.searchValue = this.selectedOption.label;
      } else if (!newValue) {
        this.searchValue = "";
      }
    }
  },
  mounted() {
    if (this.value && this.selectedOption) {
      this.searchValue = this.selectedOption.label;
    }
  },
  beforeUnmount() {
    if (this.blurTimeout) {
      clearTimeout(this.blurTimeout);
    }
  },
  methods: {
    onSearchInput(event) {
      this.searchValue = event.target.value;
      this.isOpen = true;
      this.highlightedIndex = -1;

      // Si la recherche ne correspond à aucune option, émettre une valeur vide
      if (!this.filteredOptions.length) {
        this.$emit("input", "");
      }
    },
    onFocus() {
      this.isOpen = true;
      this.highlightedIndex = -1;
    },
    onBlur() {
      // Délai pour permettre la sélection d'une option avec la souris
      this.blurTimeout = setTimeout(() => {
        this.isOpen = false;
        this.highlightedIndex = -1;

        // Si aucune option n'est sélectionnée, réinitialiser la recherche
        if (!this.selectedOption || this.searchValue !== this.selectedOption.label) {
          if (this.selectedOption) {
            this.searchValue = this.selectedOption.label;
          } else if (this.filteredOptions.length === 1) {
            this.selectOption(this.filteredOptions[0]);
          } else {
            this.searchValue = "";
            this.$emit("input", "");
          }
        }
      }, 150);
    },
    onKeydown(event) {
      if (!this.isOpen) {
        if (event.key === "ArrowDown" || event.key === "Enter" || event.key === " ") {
          event.preventDefault();
          this.isOpen = true;
        }
        return;
      }

      switch (event.key) {
        case "ArrowDown":
          event.preventDefault();
          this.highlightedIndex = Math.min(this.highlightedIndex + 1, this.filteredOptions.length - 1);
          break;
        case "ArrowUp":
          event.preventDefault();
          this.highlightedIndex = Math.max(this.highlightedIndex - 1, -1);
          break;
        case "Enter":
          event.preventDefault();
          if (this.highlightedIndex >= 0 && this.filteredOptions[this.highlightedIndex]) {
            this.selectOption(this.filteredOptions[this.highlightedIndex]);
          }
          break;
        case "Escape":
          event.preventDefault();
          this.isOpen = false;
          this.highlightedIndex = -1;
          this.$refs.searchInput.blur();
          break;
      }
    },
    selectOption(option) {
      this.searchValue = option.label;
      this.isOpen = false;
      this.highlightedIndex = -1;
      this.$emit("input", option.value);

      // Focus sur l'input après sélection
      this.$nextTick(() => {
        this.$refs.searchInput.focus();
      });
    }
  }
};
</script>
