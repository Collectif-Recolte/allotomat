<i18n>
{
	"en": {
		"skip-to-content": "Skip to content"
	},
	"fr": {
		"skip-to-content": "Aller au contenu"
	}
}
</i18n>

<template>
  <a
    id="skip-nav"
    class="group sr-only focus:not-sr-only focus:block focus:ring-0 focus:outline-none focus:ml-2 focus:my-4"
    href="#main-content">
    <span
      class="inline-block rounded font-semibold bg-white dark:bg-grey-900 text-primary-700 dark:text-primary-300 px-4 py-2 group-focus:ring-2 ring-primary-500"
      >{{ t("skip-to-content") }}</span
    >
  </a>
</template>

<script setup>
import { useI18n } from "vue-i18n";
const { t } = useI18n();
</script>
