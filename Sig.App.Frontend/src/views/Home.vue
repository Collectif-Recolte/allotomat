<i18n>
{
	"en": {
		"title": "Home"
	},
	"fr": {
		"title": "Accueil"
	}
}
</i18n>

<template>
  <AppShell :loading="loading" :title="t('title')"></AppShell>
</template>

<script setup>
import { useI18n } from "vue-i18n";
import { ref, onMounted } from "vue";

import { usePageTitle } from "@/lib/helpers/page-title";

const { t } = useI18n();

usePageTitle(t("title"));

// Simuler l'état de chargement
let loading = ref(true);
onMounted(() => {
  setTimeout(() => {
    loading.value = false;
  }, 2000);
});
</script>
