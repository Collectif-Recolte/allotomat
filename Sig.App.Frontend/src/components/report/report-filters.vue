<i18n>
{
	"en": {
    "date-selector-from": "Interval from",
    "date-selector-to": "to",
    "date-start-label": "Start date of transactions",
    "date-end-label": "End date of transactions"
	},
	"fr": {
    "date-selector-from": "Intervalle du",
    "date-selector-to": "au",
    "date-start-label": "Date de début des transactions",
    "date-end-label": "Date de fin des transactions"
	}
}
</i18n>

<template>
  <div class="text-right mb-2 w-full xs:flex xs:gap-x-4 xs:justify-end sm:mb-0 xl:w-auto">
    <div class="flex items-center justify-end gap-x-4 mb-2 xs:mb-0">
      <span class="text-sm text-primary-700">{{ t("date-selector-from") }}</span>
      <UiDatePicker
        id="datefrom"
        :value="props.dateFrom"
        class="sm:col-span-6"
        :label="t('date-start-label')"
        has-hidden-label
        @update:modelValue="(e) => emit('dateFromUpdated', e)" />
    </div>
    <div class="flex items-center justify-end gap-x-4">
      <span class="text-sm text-primary-700">{{ t("date-selector-to") }}</span>
      <UiDatePicker
        id="dateTo"
        :value="props.dateTo"
        :min-date="props.dateFrom"
        class="sm:col-span-6"
        :label="t('date-end-label')"
        has-hidden-label
        @update:modelValue="(e) => emit('dateToUpdated', e)" />
    </div>
  </div>
</template>

<script setup>
import { defineProps, defineEmits } from "vue";
import { useI18n } from "vue-i18n";

const { t } = useI18n();

const emit = defineEmits(["dateFromUpdated", "dateToUpdated"]);

const props = defineProps({
  dateFrom: {
    type: Date,
    default: undefined
  },
  dateTo: {
    type: Date,
    default: undefined
  }
});
</script>
