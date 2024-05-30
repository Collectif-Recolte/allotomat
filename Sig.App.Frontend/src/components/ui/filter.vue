<i18n>
  {
    "en": {
      "filter": "Filter",
      "sort": "Sort order",
      "reset-filters": "Reset"
    },
    "fr": {
      "filter": "Filtrer",
      "sort": "Ordre de tri",
      "reset-filters": "Réinitialiser"
    }
  }
</i18n>

<template>
  <div class="flex items-start sm:items-center sm:space-x-4 justify-end">
    <PfTooltip v-slot="{ tooltipId }" class="mb-3 sm:mb-0" :label="t('reset-filters')" position="top">
      <PfButtonAction
        v-if="props.hasActiveFilters"
        class="min-w-0 my-3 sm:my-0"
        btn-style="link"
        size="sm"
        is-icon-only
        :icon="ICON_RESET"
        :aria-labelledby="tooltipId"
        @click="onResetFilters" />
    </PfTooltip>
    <div
      class="flex flex-col items-end sm:flex-row sm:items-center sm:space-x-4 sm:justify-end"
      :class="{ 'flex-wrap sm:gap-y-2': itemsCanWrap }">
      <slot name="prependElements" />
      <UiSearch
        v-if="props.hasSearch"
        class="mb-2 sm:mb-0"
        :model-value="modelValue"
        :beneficiaries-are-anonymous="props.beneficiariesAreAnonymous"
        :placeholder="props.placeholder"
        @search="onSearch"
        @update:modelValue="(e) => emit('update:modelValue', e)" />
      <div v-if="props.hasSort" class="relative inline-block group pf-transition-visibility pf-transition-visibility--focus-only">
        <button class="pf-button pf-button--outline px-3 min-h-11">
          <PfIcon :icon="iconSortOrder" size="s" />
          {{ sortLabel }}
          <PfIcon :icon="ICON_ARROW_BOTTOM" size="xxs" :class="activeFiltersCount > 0 ? 'ml-2' : 'ml-12'" />
        </button>
        <div
          class="text-left absolute z-50 bottom-1 right-0 translate-y-full min-w-64 max-h-60 overflow-auto bg-white rounded-md border rounded-tr-none border-primary-700 px-4 py-3 transition ease-in-out duration-300 pf-transition-visibility__content h-remove-margin">
          <slot name="sortOrder"></slot>
        </div>
      </div>
      <div
        v-if="props.hasFilters"
        class="relative inline-block group pf-transition-visibility pf-transition-visibility--focus-only">
        <button class="pf-button pf-button--outline px-3 min-h-11">
          {{ t("filter") }}
          <span
            v-if="activeFiltersCount > 0"
            class="bg-tertiary-500 w-6 h-6 flex items-center justify-center rounded-full text-p3 leading-none ml-4 text-white">
            {{ activeFiltersCount }}
          </span>
          <PfIcon :icon="ICON_ARROW_BOTTOM" size="xxs" :class="activeFiltersCount > 0 ? 'ml-2' : 'ml-12'" />
        </button>
        <div
          class="text-left absolute z-50 bottom-1 right-0 translate-y-full min-w-64 max-h-60 overflow-auto bg-white rounded-md border rounded-tr-none border-primary-700 px-4 py-3 transition ease-in-out duration-300 pf-transition-visibility__content h-remove-margin">
          <slot></slot>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { defineEmits, defineProps, computed } from "vue";
import { useI18n } from "vue-i18n";

import { ASC } from "@/lib/consts/card-sort-order";

import ICON_ARROW_BOTTOM from "@/lib/icons/arrow-bottom.json";
import ICON_RESET from "@/lib/icons/reset.json";

import ICON_CHEVRON_DOWN from "@/lib/icons/chevron-down.json";
import ICON_CHEVRON_UP from "@/lib/icons/chevron-right.json";

const emit = defineEmits(["resetFilters", "search", "update:modelValue"]);

const props = defineProps({
  hasActiveFilters: {
    type: Boolean
  },
  hasSearch: Boolean,
  hasFilters: Boolean,
  hasSort: Boolean,
  sortLabel: {
    type: String,
    default: ""
  },
  sortOrder: {
    type: String,
    default: ASC
  },
  activeFiltersCount: {
    type: Number,
    default: 0,
    validator(value) {
      return value >= 0;
    }
  },
  modelValue: {
    type: String,
    default: ""
  },
  beneficiariesAreAnonymous: {
    type: Boolean,
    default: false
  },
  itemsCanWrap: Boolean,
  placeholder: {
    type: String,
    default: null
  }
});

const { t } = useI18n();

const sortLabel = computed(() => {
  return props.sortLabel !== "" ? props.sortLabel : t("sort");
});

const iconSortOrder = computed(() => {
  return props.sortOrder === ASC ? ICON_CHEVRON_UP : ICON_CHEVRON_DOWN;
});

function onResetFilters() {
  emit("resetFilters");
}

function onSearch() {
  emit("search");
}
</script>
