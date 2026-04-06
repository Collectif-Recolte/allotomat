<i18n>
{
	"en": {
		"first": "First",
		"last": "Last",
		"next": "Next",
		"previous": "Previous"
	},
	"fr": {
		"first": "Première",
		"last": "Dernière",
		"next": "Suivant",
		"previous": "Précédent"
	}
}
</i18n>

<template>
  <nav class="border-t border-grey-200 dark:border-grey-800 px-4 flex items-center justify-between sm:px-0">
    <div class="-mt-px flex gap-x-1">
      <button v-if="props.page > 1"
        class="border-t-2 border-transparent pt-4 pr-1 inline-flex items-center text-sm font-semibold text-grey-500 hover:text-grey-700 hover:border-grey-300 dark:text-grey-400 dark:hover:text-grey-300 dark:hover:border-grey-700"
        @click="onPageClick(1)">
        <PfIcon class="shrink-0 mr-1" size="sm" :icon="CHEVRON_DOUBLE_LEFT_ICON" aria-hidden="true" />
        {{ t("first") }}
      </button>
      <button v-if="props.page > 1"
        class="border-t-2 border-transparent pt-4 pr-1 inline-flex items-center text-sm font-semibold text-grey-500 hover:text-grey-700 hover:border-grey-300 dark:text-grey-400 dark:hover:text-grey-300 dark:hover:border-grey-700"
        @click="onPageClick(props.page - 1)">
        <PfIcon class="shrink-0 mr-1" size="sm" :icon="CHEVRON_LEFT_ICON" aria-hidden="true" />
        {{ t("previous") }}
      </button>
    </div>

    <div class="hidden md:-mt-px md:flex">
      <template v-for="(pageItem, index) in paginationItems" :key="index">
        <span v-if="pageItem === '...'"
          class="border-transparent text-grey-500 dark:text-grey-400 border-t-2 pt-4 px-4 inline-flex items-center text-sm font-semibold">{{
          pageItem }}</span>
        <span v-else-if="parseInt(pageItem) === props.page"
          class="border-primary-500 text-primary-700 dark:text-primary-300 border-t-2 pt-4 px-4 inline-flex items-center text-sm font-semibold"
          :aria-current="pageItem">
          {{ pageItem }}
        </span>
        <button v-else
          class="border-transparent text-grey-500 hover:text-grey-700 hover:border-grey-300 dark:text-grey-400 dark:hover:text-grey-300 dark:hover:border-grey-700 border-t-2 pt-4 px-4 inline-flex items-center text-sm font-semibold"
          @click="onPageClick(parseInt(pageItem))">
          {{ pageItem }}
        </button>
      </template>
    </div>

    <div class="-mt-px flex gap-x-1 justify-end">
      <button v-if="props.page < props.totalPages"
        class="border-t-2 border-transparent pt-4 pl-1 inline-flex items-center text-sm font-semibold text-grey-500 hover:text-grey-700 hover:border-grey-300 dark:text-grey-400 dark:hover:text-grey-300 dark:hover:border-grey-700"
        @click="onPageClick(props.page + 1)">
        {{ t("next") }}
        <PfIcon class="shrink-0 ml-1" size="sm" :icon="CHEVRON_RIGHT_ICON" aria-hidden="true" />
      </button>
      <button v-if="props.page < props.totalPages"
        class="border-t-2 border-transparent pt-4 pl-1 inline-flex items-center text-sm font-semibold text-grey-500 hover:text-grey-700 hover:border-grey-300 dark:text-grey-400 dark:hover:text-grey-300 dark:hover:border-grey-700"
        @click="onPageClick(props.totalPages)">
        {{ t("last") }}
        <PfIcon class="shrink-0 ml-1" size="sm" :icon="CHEVRON_DOUBLE_RIGHT_ICON" aria-hidden="true" />
      </button>
    </div>
  </nav>
</template>

<script setup>
import { defineProps, defineEmits, computed } from "vue";
import { useI18n } from "vue-i18n";

import CHEVRON_DOUBLE_LEFT_ICON from "@/lib/icons/chevron-double-left.json";
import CHEVRON_LEFT_ICON from "@/lib/icons/chevron-left.json";
import CHEVRON_RIGHT_ICON from "@/lib/icons/chevron-right.json";
import CHEVRON_DOUBLE_RIGHT_ICON from "@/lib/icons/chevron-double-right.json";

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

const edgeCount = 2;
const windowCount = 1;

const paginationItems = computed(() => {
  const total = props.totalPages;
  const current = props.page;

  const pageSet = new Set();

  for (let i = 1; i <= Math.min(edgeCount, total); i++) pageSet.add(i);
  for (let i = Math.max(total - edgeCount + 1, 1); i <= total; i++) pageSet.add(i);
  for (let i = Math.max(1, current - windowCount); i <= Math.min(total, current + windowCount); i++) pageSet.add(i);

  const sorted = Array.from(pageSet).sort((a, b) => a - b);
  const result = [];

  for (let i = 0; i < sorted.length; i++) {
    if (i > 0 && sorted[i] - sorted[i - 1] > 1) result.push("...");
    result.push(sorted[i].toString());
  }

  return result;
});

function onPageClick(pageIndex) {
  emit("update:page", pageIndex);
}
</script>
