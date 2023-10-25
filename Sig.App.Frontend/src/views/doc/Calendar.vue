<i18n>
{
	"en": {
		"date-simple": "Inline calendar",
		"first-marker": "First marker",
		"marker-with-tooltip": "Marker with tooltip",
		"result": "Results",
		"second-marker": "Second marker",
		"section-1-description": "You can disable the time picker with the property \"enable-time-picker\"",
		"section-1-title": "Simple calendar with time picker",
		"section-2-description": "With or without tooltip",
		"section-2-title": "Calendar with highlighted events",
		"subtitle": "With Vue 3 Datepicker",
		"title": "Example of calendar"
	},
	"fr": {
		"date-simple": "Calendrier à même la page",
		"first-marker": "Premier marqueur",
		"marker-with-tooltip": "Marqueur avec info-bulle",
		"result": "Résultat:",
		"second-marker": "Deuxième marqueur",
		"section-1-description": "On peut désactiver le sélecteur d'heure avec la propriété \"enable-time-picker\"",
		"section-1-title": "Calendrier simple avec sélecteur d'heure",
		"section-2-description": "Avec ou sans info-bulle",
		"section-2-title": "Calendrier avec événements surlignés",
		"subtitle": "Avec Vue 3 Datepicker",
		"title": "Exemple de calendrier"
	}
}
</i18n>

<template>
  <AppShell>
    <template #title>
      <Title :title="t('title')">
        <template #bottom>
          <span>{{ t("subtitle") }}</span>
        </template>
      </Title>
    </template>

    <div class="w-xs max-w-full">
      <div class="mb-16">
        <h2 class="text-lg mt-0 mb-2">{{ t("section-1-title") }}</h2>
        <p>{{ t("section-1-description") }}</p>
        <DatePicker id="dateSimple" v-model="result" :label="t('date-simple')" inline enable-time-picker />
        <div class="mt-6">
          <strong>{{ t("result") }}</strong> {{ result }}
        </div>
      </div>
      <div class="mb-16">
        <h2 class="text-lg mt-0 mb-2">{{ t("section-2-title") }}</h2>
        <p>{{ t("section-2-description") }}</p>
        <DatePicker id="dateSimple" v-model="result" :label="t('date-simple')" :markers="marker" inline />
      </div>
    </div>
  </AppShell>
</template>

<script setup>
import { useI18n } from "vue-i18n";
import { ref, computed } from "vue";
import addDays from "date-fns/addDays";

import Title from "@/components/app/title.vue";
import DatePicker from "@/components/ui/date-picker.vue";

import { usePageTitle } from "@/lib/helpers/page-title";

const { t } = useI18n();

usePageTitle(t("title"));

const result = ref(addDays(new Date(), 4));

const marker = computed(() => [
  {
    date: addDays(new Date(), 1),
    type: "dot",
    tooltip: [{ text: t("marker-with-tooltip"), color: "var(--custom-primary-marker-color)" }]
  },
  {
    date: addDays(new Date(), 2),
    type: "line",
    tooltip: [
      { text: t("first-marker"), color: "var(--custom-secondary-marker-color)" },
      { text: t("second-marker"), color: "var(--custom-primary-marker-color)" }
    ]
  },
  {
    date: addDays(new Date(), 3),
    type: "dot",
    color: "var(--custom-secondary-marker-color)"
  }
]);
</script>
