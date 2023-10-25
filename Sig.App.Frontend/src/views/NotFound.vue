<i18n>
{
	"en": {
		"go-home": "Go to home page",
		"title": "Page not found"
	},
	"fr": {
		"go-home": "Retourner à l'accueil",
		"title": "Page introuvable"
	}
}
</i18n>

<template>
  <div class="w-xs mx-auto px-section max-w-full my-12 md:my-16">
    <h1 class="text-3xl">{{ t("title") }}</h1>
    <RouterLink :to="{ name: $consts.urls.URL_ROOT }">{{ t("go-home") }}</RouterLink>
  </div>
</template>

<script setup>
import { useI18n } from "vue-i18n";
import { usePageTitle } from "@/lib/helpers/page-title";

const { t } = useI18n();

usePageTitle(t("title"));
</script>
