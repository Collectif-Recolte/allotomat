<i18n>
{
	"en": {
		"cancel": "Cancel",
		"clear-date": "Clear selected date",
		"close-time-picker": "",
		"locale": "en-US",
		"open-time-picker": "",
		"select-date": "Select"
	},
	"fr": {
		"cancel": "Annuler",
		"clear-date": "Effacer la date sélectionnée",
		"close-time-picker": "Fermer le sélecteur d'heure",
		"locale": "fr-CA",
		"open-time-picker": "Ouvrir le sélecteur d'heure",
		"select-date": "Sélectionner"
	}
}
</i18n>

<template>
  <Datepicker
    ref="datepicker"
    :model-value="props.value"
    :locale="t('locale')"
    text-input
    position="left"
    :inline="inline"
    :range="range"
    :partial-range="partialRange"
    :multi-calendars="multiCalendars"
    :enable-time-picker="enableTimePicker"
    :markers="markers"
    :dark="dark"
    auto-apply
    :min-date="minDate"
    :format="regularFormatValue"
    :disabled="props.disabled"
    @update:modelValue="(v) => emit('update:modelValue', v)">
    <template #dp-input="{ value: dpValue }">
      <PfFormInputText
        :id="props.id"
        :label="props.label"
        :has-hidden-label="props.hasHiddenLabel"
        :errors="props.errors"
        :value="dpValue"
        :leading-icon="CALENDAR_ICON"
        :disabled="props.disabled"
        is-stroke-icon
        is-datepicker
        @keydown.enter="openCalendar" />
    </template>
    <template #clear-icon>
      <button
        v-if="props.value"
        class="text-grey-600 dark:text-grey-400 hover:text-primary-700 focus:text-primary-700 dark:hover:text-primary-300 focus:hover:text-primary-300"
        @click.stop="clearInput">
        <PfIcon size="sm" :icon="CLOSE_ICON" aria-hidden="true" />
        <span class="sr-only">{{ t("clear-date") }}</span>
      </button>
    </template>

    <template #calendar-header="{ day }">
      <div class="text-p4 uppercase font-normal text-grey-500 dark:text-grey-400">
        {{ day }}
      </div>
    </template>

    <template #day="{ day }">
      <span class="text-p3">
        {{ day }}
      </span>
    </template>

    <template #clock-icon>
      <PfIcon size="lg" :icon="CLOCK_ICON" aria-hidden="true" />
      <span class="sr-only">{{ t("open-time-picker") }}</span>
    </template>

    <template #calendar-icon>
      <PfIcon size="lg" :icon="CALENDAR_ICON" aria-hidden="true" />
      <span class="sr-only">{{ t("close-time-picker") }}</span>
    </template>

    <template #action-select>
      <div class="flex items-center">
        <PfButtonAction v-if="!inline" btn-style="outline" class="mr-2" size="sm" :label="t('cancel')" @click="closeCalendar" />
        <PfButtonAction size="sm" :label="t('select-date')" @click="selectDate" />
      </div>
    </template>
  </Datepicker>
</template>

<script setup>
import { useI18n } from "vue-i18n";
import { ref, defineProps, defineEmits } from "vue";
import Datepicker from "@vuepic/vue-datepicker";
import "@vuepic/vue-datepicker/dist/main.css";
import { useDarkMode } from "@/lib/helpers/dark-mode";
import { regularFormatValue } from "@/lib/helpers/date";

import CALENDAR_ICON from "@/lib/icons/calendar.json";
import CLOSE_ICON from "@/lib/icons/close.json";
import CLOCK_ICON from "@/lib/icons/clock.json";

const { t } = useI18n();

const dark = useDarkMode();

const props = defineProps({
  id: {
    type: String,
    required: true
  },
  label: {
    type: String,
    required: true
  },
  value: {
    type: [Date, Array],
    default: undefined
  },
  errors: {
    type: Array,
    default() {
      [];
    }
  },
  markers: {
    type: Array,
    default() {
      [];
    }
  },
  minDate: {
    type: [Date, String],
    default: null
  },
  inline: Boolean,
  range: Boolean,
  multiCalendars: Boolean,
  partialRange: Boolean,
  enableTimePicker: Boolean,
  isInsideModal: Boolean,
  hasHiddenLabel: Boolean,
  disabled: Boolean
});

const emit = defineEmits(["update:modelValue"]);

const datepicker = ref(null);

const selectDate = () => {
  datepicker.value.selectDate();
};

const closeCalendar = () => {
  datepicker.value.closeMenu();
};

const openCalendar = () => {
  datepicker.value.openMenu();
};

const clearInput = () => {
  emit("update:modelValue", undefined);
};
</script>

<style lang="postcss">
.dp__theme_light {
  --dp-background-color: theme("colors.white");
  --dp-text-color: theme("colors.black");
  --dp-hover-color: theme("colors.grey.200");
  --dp-hover-text-color: theme("colors.black");
  --dp-hover-icon-color: theme("colors.black");
  --dp-primary-color: theme("colors.primary.600");
  --dp-primary-text-color: theme("colors.white");
  --dp-secondary-color: theme("colors.grey.400");
  --dp-border-color: theme("colors.grey.100");
  --dp-menu-border-color: theme("colors.grey.100");
  --dp-border-color-hover: theme("colors.grey.300");
  --dp-disabled-color: theme("colors.grey.200");
  --dp-scroll-bar-background: theme("colors.grey.50");
  --dp-scroll-bar-color: theme("colors.grey.500");
  --dp-success-color: theme("colors.secondary.500");
  --dp-success-color-disabled: theme("colors.secondary.200");
  --dp-icon-color: theme("colors.grey.500");
  --dp-danger-color: theme("colors.red.500");
  --dp-marker-color: theme("colors.primary.900");
  --dp-tooltip-bg: theme("colors.white");
  --dp-button-bg: theme("colors.grey.100");
  --custom-primary-marker-color: theme("colors.primary.400");
  --custom-secondary-marker-color: theme("colors.grey.400");
}

.dp__theme_dark {
  --dp-background-color: theme("colors.grey.900");
  --dp-text-color: theme("colors.grey.200");
  --dp-hover-color: theme("colors.grey.700");
  --dp-hover-text-color: theme("colors.white");
  --dp-hover-icon-color: theme("colors.white");
  --dp-primary-color: theme("colors.primary.400");
  --dp-primary-text-color: theme("colors.black");
  --dp-secondary-color: theme("colors.grey.400");
  --dp-border-color: theme("colors.grey.700");
  --dp-menu-border-color: theme("colors.grey.700");
  --dp-border-color-hover: theme("colors.grey.900");
  --dp-disabled-color: theme("colors.grey.700");
  --dp-scroll-bar-background: theme("colors.grey.900");
  --dp-scroll-bar-color: theme("colors.grey.800");
  --dp-success-color: theme("colors.secondary.500");
  --dp-success-color-disabled: theme("colors.secondary.200");
  --dp-icon-color: theme("colors.grey.400");
  --dp-danger-color: theme("colors.red.500");
  --dp-marker-color: theme("colors.primary.400");
  --dp-tooltip-bg: theme("colors.grey.700");
  --dp-button-bg: theme("colors.grey.800");
  --custom-primary-marker-color: theme("colors.primary.500");
  --custom-secondary-marker-color: theme("colors.grey.400");
}

.dp__menu {
  padding: 0.5rem;
  border: 1px solid var(--dp-border-color);
  box-shadow: theme("boxShadow.sm");
  transition: top ease 0.2s;
}

.dp__arrow_top {
  border-top: 1px solid var(--dp-border-color);
  border-left: 1px solid var(--dp-border-color);
}

.dp__input_wrap {
  position: relative;
}

.dp__clear_icon {
  cursor: default;
  top: theme("spacing.12");
  right: theme("spacing.3");
  display: flex;
  align-items: center;
}

.pf-form-field--hidden-label ~ .dp__clear_icon {
  top: 50%;
}

.dp__month_year_select {
  text-transform: capitalize;
}

.dp__action_row {
  flex-direction: column;
  align-items: flex-end;
  padding-left: 0;
  padding-right: 0;
  padding-bottom: 0;

  @media screen and (max-width: 600px) {
    align-items: center;
    margin: 0 auto;
  }
}

.dp__button {
  border-radius: theme("borderRadius.sm");
  background-color: var(--dp-button-bg);
  color: var(--dp-primary-color);
}

.dp__selection_preview {
  width: auto;
}

.dp__action_buttons {
  width: auto;
}

.dp__marker_tooltip {
  font-size: theme("fontSize.p3");
  padding: theme("spacing.3");
  border-radius: theme("borderRadius.sm");
  box-shadow: theme("boxShadow.md");
  background-color: var(--dp-tooltip-bg);
}

.dp__arrow_bottom_tp {
  background-color: var(--dp-tooltip-bg);
}
</style>
