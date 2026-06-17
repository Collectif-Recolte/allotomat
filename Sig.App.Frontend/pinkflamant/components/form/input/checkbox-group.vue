<i18n>
{
  "en": {
    "search-placeholder": "Search...",
    "no-results-found": "No results found",
    "clear-search": "Clear search"
  },
  "fr": {
    "search-placeholder": "Chercher...",
    "no-results-found": "Aucun résultat trouvé",
    "clear-search": "Effacer la recherche"
  }
}
</i18n>

<template>
  <FormFieldset
    :id="id"
    :legend="label"
    :description="description"
    :has-error-state="hasErrorState"
    :errors="errors"
    :is-filter="isFilter"
    :has-hidden-label="hasHiddenLabel">
    <div v-if="searchable" class="sticky top-0 z-10 bg-white pb-2">
      <div class="relative">
        <PfIcon
          class="absolute top-1/2 -translate-y-1/2 left-3 flex items-center text-grey-500 pointer-events-none"
          :icon="leadingIcon"
          size="lg" />
        <input
          :id="`${id}-search`"
          ref="searchInput"
          v-model="searchValue"
          type="text"
          autocomplete="off"
          :placeholder="effectiveSearchPlaceholder"
          class="pf-select text-[18px] min-h-11 shadow-sm block w-full rounded-md transition-colors duration-200 ease-in-out text-primary-900 border-primary-500 focus:ring-secondary-500 focus:border-secondary-500 placeholder-grey-500 px-10"
          :aria-label="effectiveSearchPlaceholder"
          data-filter-search
          @mousedown.stop
          @click.stop
          @keydown.stop />
        <button
          v-if="searchValue"
          type="button"
          class="absolute inset-y-0 right-0 pr-3 flex items-center text-grey-500 hover:text-primary-700 focus:text-primary-700"
          :aria-label="t('clear-search')"
          @mousedown.stop
          @click.stop="clearSearch">
          <PfIcon :icon="clearIcon" size="sm" aria-hidden="true" />
        </button>
      </div>
    </div>
    <PfFormInputCheckbox
      v-for="option in filteredOptions"
      :id="option.value"
      :key="option.value"
      :label="option.label"
      :description="option.description"
      :col-span-class="colSpanClass"
      :errors="errors"
      :disabled="disabled"
      :checked="isChecked(option.value)"
      :is-filter="isFilter"
      @input="(e) => updateCheckbox(option.value, e)" />
    <p v-if="searchable && searchValue && !hasSearchMatches" class="text-sm text-grey-500 py-1">
      {{ effectiveNoResultsFound }}
    </p>
  </FormFieldset>
</template>

<script>
import { useI18n } from "vue-i18n";

import { commonFieldProps } from "../field/index";
import FormFieldset from "../fieldset";
import { normalizeForSearch } from "../../../lib/normalize-for-search";
import ICON_SEARCH from "../../../icons/search.json";
import ICON_CLOSE from "../../../icons/close.json";

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
    isFilter: Boolean,
    searchable: Boolean,
    searchPlaceholder: {
      type: String,
      default: ""
    },
    noResultsFound: {
      type: String,
      default: ""
    }
  },
  emits: ["input"],
  setup() {
    const { t } = useI18n();
    return { t };
  },
  data() {
    return {
      searchValue: "",
      leadingIcon: ICON_SEARCH,
      clearIcon: ICON_CLOSE
    };
  },
  computed: {
    hasErrorState() {
      return this.errors && this.errors.length > 0;
    },
    effectiveSearchPlaceholder() {
      return this.searchPlaceholder || this.t("search-placeholder");
    },
    effectiveNoResultsFound() {
      return this.noResultsFound || this.t("no-results-found");
    },
    filteredOptions() {
      if (!this.searchable || !this.searchValue) {
        return this.options;
      }
      const searchTerm = normalizeForSearch(this.searchValue);
      return this.options.filter(
        (option) =>
          !option.isDisabled &&
          (normalizeForSearch(option.label).includes(searchTerm) || this.isChecked(option.value))
      );
    },
    hasSearchMatches() {
      if (!this.searchable || !this.searchValue) {
        return true;
      }
      const searchTerm = normalizeForSearch(this.searchValue);
      return this.options.some(
        (option) => !option.isDisabled && normalizeForSearch(option.label).includes(searchTerm)
      );
    }
  },
  methods: {
    updateCheckbox(value, isChecked) {
      this.$emit("input", { value, isChecked });
    },
    isChecked(value) {
      return this.value.indexOf(value) !== -1;
    },
    clearSearch() {
      this.searchValue = "";
      this.$nextTick(() => {
        this.$refs.searchInput?.focus();
      });
    }
  },
  beforeUnmount() {
    this.searchValue = "";
  }
};
</script>
