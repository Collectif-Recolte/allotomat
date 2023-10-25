<i18n>
{
	"en": {
		"next": "Next",
		"previous": "Previous"
	},
	"fr": {
		"next": "Suivant",
		"previous": "Précédent"
	}
}
</i18n>

<template>
  <nav class="border-t border-grey-200 dark:border-grey-800 px-4 flex items-center justify-between sm:px-0">
    <div class="-mt-px w-0 flex-1 flex">
      <button
        v-if="props.page > 1"
        class="border-t-2 border-transparent pt-4 pr-1 inline-flex items-center text-sm font-semibold text-grey-500 hover:text-grey-700 hover:border-grey-300 dark:text-grey-400 dark:hover:text-grey-300 dark:hover:border-grey-700"
        @click="onPageClick(props.page - 1)">
        <PfIcon class="shrink-0 mr-3" size="sm" :icon="CHEVRON_LEFT_ICON" aria-hidden="true" />
        {{ t("previous") }}
      </button>
    </div>

    <div class="hidden md:-mt-px md:flex">
      <template v-for="(pageItem, index) in paginationItems" :key="index">
        <span
          v-if="pageItem === '...'"
          class="border-transparent text-grey-500 dark:text-grey-400 border-t-2 pt-4 px-4 inline-flex items-center text-sm font-semibold"
          >{{ pageItem }}</span
        >
        <span
          v-else-if="parseInt(pageItem) === props.page"
          class="border-primary-500 text-primary-700 dark:text-primary-300 border-t-2 pt-4 px-4 inline-flex items-center text-sm font-semibold"
          :aria-current="pageItem">
          {{ pageItem }}
        </span>
        <button
          v-else
          class="border-transparent text-grey-500 hover:text-grey-700 hover:border-grey-300 dark:text-grey-400 dark:hover:text-grey-300 dark:hover:border-grey-700 border-t-2 pt-4 px-4 inline-flex items-center text-sm font-semibold"
          @click="onPageClick(parseInt(pageItem))">
          {{ pageItem }}
        </button>
      </template>
    </div>
    <div class="-mt-px w-0 flex-1 flex justify-end">
      <button
        v-if="props.page < props.totalPages"
        class="border-t-2 border-transparent pt-4 pl-1 inline-flex items-center text-sm font-semibold text-grey-500 hover:text-grey-700 hover:border-grey-300 dark:text-grey-400 dark:hover:text-grey-300 dark:hover:border-grey-700"
        @click="onPageClick(props.page + 1)">
        {{ t("next") }}
        <PfIcon class="shrink-0 ml-3" size="sm" :icon="CHEVRON_RIGHT_ICON" aria-hidden="true" />
      </button>
    </div>
  </nav>
</template>

<script setup>
import { defineProps, defineEmits, computed } from "vue";
import { useI18n } from "vue-i18n";

import CHEVRON_LEFT_ICON from "@/lib/icons/chevron-left.json";
import CHEVRON_RIGHT_ICON from "@/lib/icons/chevron-right.json";

const { t } = useI18n();

const props = defineProps({
  totalPages: {
    type: Number,
    required: true
  },
  page: {
    type: Number,
    required: true
  }
});

const emit = defineEmits(["update:page"]);

const contiguousElements = 3;

function getPageBoundaries() {
  let first = Math.max(props.page - contiguousElements, 1);
  let last = Math.min(props.page + contiguousElements, props.totalPages);

  const maxItems = 1 + contiguousElements * 2;
  const missing = maxItems - (last - first) - 1;

  if (missing > 0) {
    if (first > 1) first -= missing;
    if (last < props.totalPages) last += missing;

    if (first < 1) first = 1;
    if (last > props.totalPages) last = props.totalPages;
  }

  return { first, last };
}

const paginationItems = computed(() => {
  const boundaries = getPageBoundaries();
  const pagination = [];

  if (boundaries.first > 1) {
    if (boundaries.first > 2) pagination.push("...");
  }

  for (var i = boundaries.first; i <= boundaries.last; i++) {
    pagination.push(i.toString());
  }

  if (boundaries.last < props.totalPages) {
    if (boundaries.last < props.totalPages - 1) pagination.push("...");
  }

  return pagination;
});

function onPageClick(pageIndex) {
  emit("update:page", pageIndex);
}
</script>
