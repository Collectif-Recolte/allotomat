<i18n>
{
	"en": {
    "date-selector-from": "Interval from",
    "date-selector-to": "to",
    "date-start-label": "Start date of transactions",
    "date-end-label": "End date of transactions",
    "organizations": "Groups",
    "subscriptions": "Subscriptions",
    "last-year": "Last year",
    "current-year": "Current year",
    "last-month": "Last month",
    "all-time": "All time",
    "reset-filters": "Reset"
	},
	"fr": {
    "date-selector-from": "Intervalle du",
    "date-selector-to": "au",
    "date-start-label": "Date de début des transactions",
    "date-end-label": "Date de fin des transactions",
    "organizations": "Groupes",
    "subscriptions": "Abonnements",
    "last-year": "Année dernière",
    "current-year": "Année en cours",
    "last-month": "Mois dernier",
    "all-time": "Toutes les dates",
    "reset-filters": "Réinitialiser"
	}
}
</i18n>

<template>
  <div class="text-right mb-2 w-full xs:flex xs:gap-x-4 xs:justify-end sm:mb-0 xl:w-auto">
    <div class="flex flex-col gap-y-4">
      <div class="flex flex-row gap-x-4 text-right mb-2 w-full xs:flex xs:gap-x-4 xs:justify-end sm:mb-0 xl:w-auto">
        <div class="flex items-center justify-end gap-x-4 mb-2 xs:mb-0">
          <PfTooltip v-slot="{ tooltipId }" class="mb-0" :label="t('reset-filters')" position="top">
            <PfButtonAction
              class="min-w-0 my-3 sm:my-0"
              btn-style="link"
              size="sm"
              is-icon-only
              :icon="ICON_RESET"
              :aria-labelledby="tooltipId"
              @click="onResetFilters" />
          </PfTooltip>
          <span class="text-sm text-primary-700">{{ t("date-selector-from") }}</span>
          <UiDatePicker
            id="datefrom"
            :value="props.modelValue.dateFrom"
            class="sm:col-span-6"
            :label="t('date-start-label')"
            has-hidden-label
            @update:modelValue="onDateFromUpdated" />
        </div>
        <div class="flex items-center justify-end gap-x-4">
          <span class="text-sm text-primary-700">{{ t("date-selector-to") }}</span>
          <UiDatePicker
            id="dateTo"
            :value="props.modelValue.dateTo"
            :min-date="props.dateFrom"
            class="sm:col-span-6"
            :label="t('date-end-label')"
            has-hidden-label
            @update:modelValue="onDateToUpdated" />
        </div>
      </div>
      <div class="flex flex-row gap-x-4">
        <PfButtonAction btn-style="outline" :label="t('last-year')" @click="setDates('last-year')" />
        <PfButtonAction btn-style="outline" :label="t('current-year')" @click="setDates('current-year')" />
        <PfButtonAction btn-style="outline" :label="t('last-month')" @click="setDates('last-month')" />
        <PfButtonAction btn-style="outline" :label="t('all-time')" @click="setDates('all-time')" />
        <UiFilterSelect
          v-if="availableOrganizations.length > 0"
          :label="t('organizations')"
          :active-filters-count="organizationActiveFiltersCount">
          <PfFormInputCheckboxGroup
            id="organizations"
            is-filter
            :value="props.modelValue.selectedOrganizations"
            :options="availableOrganizations"
            @input="onOrganizationsChecked" />
        </UiFilterSelect>
        <UiFilterSelect
          v-if="availableSubscriptions.length > 0"
          :label="t('subscriptions')"
          :active-filters-count="subscriptionActiveFiltersCount">
          <PfFormInputCheckboxGroup
            id="subscriptions"
            class="mt-3"
            is-filter
            :value="props.modelValue.selectedSubscriptions"
            :options="availableSubscriptions"
            @input="onSubscriptionsChecked" />
        </UiFilterSelect>
      </div>
    </div>
  </div>
</template>

<script setup>
import { defineProps, defineEmits, computed } from "vue";
import { useI18n } from "vue-i18n";
import { subscriptionName } from "@/lib/helpers/subscription";

import ICON_RESET from "@/lib/icons/reset.json";

const { t } = useI18n();

const emit = defineEmits(["update:modelValue", "resetFilters"]);

const props = defineProps({
  modelValue: {
    type: Object,
    default() {
      return {
        dateFrom: undefined,
        dateTo: undefined,
        selectedOrganizations: [],
        selectedSubscriptions: []
      };
    }
  },
  availableOrganizations: {
    type: Array,
    default() {
      return [];
    }
  },
  availableSubscriptions: {
    type: Array,
    default() {
      return [];
    }
  }
});

const organizationActiveFiltersCount = computed(() => {
  return props.modelValue.selectedOrganizations?.length ?? 0;
});

const subscriptionActiveFiltersCount = computed(() => {
  return props.modelValue.selectedSubscriptions?.length ?? 0;
});

const availableOrganizations = computed(() => {
  if (!props.availableOrganizations || props.availableOrganizations?.length <= 0) return [];
  return props.availableOrganizations.map((x) => ({ value: x.id, label: x.name }));
});

const availableSubscriptions = computed(() => {
  if (!props.availableSubscriptions || props.availableSubscriptions?.length <= 0) return [];
  return props.availableSubscriptions.map((x) => ({ value: x.id, label: subscriptionName(x) }));
});

function onOrganizationsChecked(input) {
  let newSelectedOrganizations = [];
  if (input.isChecked) {
    newSelectedOrganizations = [...props.modelValue.selectedOrganizations, input.value];
  } else {
    newSelectedOrganizations = props.modelValue.selectedOrganizations.filter((x) => x !== input.value);
  }
  emit("update:modelValue", { ...props.modelValue, selectedOrganizations: newSelectedOrganizations });
}

function onSubscriptionsChecked(input) {
  let newSelectedSubscriptions = [];
  if (input.isChecked) {
    newSelectedSubscriptions = [...props.modelValue.selectedSubscriptions, input.value];
  } else {
    newSelectedSubscriptions = props.modelValue.selectedSubscriptions.filter((x) => x !== input.value);
  }
  emit("update:modelValue", { ...props.modelValue, selectedSubscriptions: newSelectedSubscriptions });
}

function onDateFromUpdated(value) {
  emit("update:modelValue", { ...props.modelValue, dateFrom: value });
}

function onDateToUpdated(value) {
  emit("update:modelValue", { ...props.modelValue, dateTo: value });
}

function setDates(type) {
  switch (type) {
    case "last-year": {
      const newDateFrom = new Date(new Date().getFullYear() - 1, 0, 1);
      const newDateTo = new Date(new Date().getFullYear() - 1, 11, 31);
      emit("update:modelValue", { ...props.modelValue, dateFrom: newDateFrom, dateTo: newDateTo });
      break;
    }
    case "current-year": {
      const newDateFrom = new Date(new Date().getFullYear(), 0, 1);
      const newDateTo = new Date(new Date().getFullYear(), 11, 31);
      emit("update:modelValue", { ...props.modelValue, dateFrom: newDateFrom, dateTo: newDateTo });
      break;
    }
    case "last-month": {
      const newDateFrom = new Date(new Date().getFullYear(), new Date().getMonth() - 1, 1);
      const newDateTo = new Date(new Date().getFullYear(), new Date().getMonth(), 0);
      emit("update:modelValue", { ...props.modelValue, dateFrom: newDateFrom, dateTo: newDateTo });
      break;
    }
    case "all-time": {
      const newDateFrom = new Date(2023, 0, 1);
      const newDateTo = new Date(new Date().getFullYear(), 11, 31);
      emit("update:modelValue", { ...props.modelValue, dateFrom: newDateFrom, dateTo: newDateTo });
      break;
    }
  }
}

function onResetFilters() {
  emit("resetFilters");
}
</script>
