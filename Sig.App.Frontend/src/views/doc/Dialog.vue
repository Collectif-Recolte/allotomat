<i18n>
{
	"en": {
		"open-confirm-modal": "Open confirmation modal",
		"open-modal": "Open modal",
		"open-delete-modal": "Open delition modal",
		"section-1-description": "This UI component can be used as a basis for implementing different types of dialogs, and can display an alert message.",
		"section-1-title": "Basic Dialog",
		"section-2-description": "Used to warn the user before triggering an action with irreversible effect.",
		"section-2-title": "Confirm dialog",
		"section-3-description": "Used to warn the user before deleting an element.",
		"section-3-title": "Delete dialog",
		"title": "Examples of dialogs"
	},
	"fr": {
		"open-confirm-modal": "Ouvrir le dialogue de confirmation",
		"open-modal": "Ouvrir le dialogue",
		"open-delete-modal": "Ouvrir le dialogue de suppression",
		"section-1-description": "Ce composant UI peut servir de base pour implémenter différents types de dialogues, et permet d'afficher un message d'alerte.",
		"section-1-title": "Dialogue de base",
		"section-2-description": "Utilisé pour avertir l'utilisateur avant de déclencher une action à l'effet irréversible.",
		"section-2-title": "Dialogue de confirmation",
		"section-3-description": "Utilisé pour avertir l'utilisateur avant la suppression d'un élément.",
		"section-3-title": "Dialogue de suppression",
		"title": "Exemples de dialogues"
	}
}
</i18n>

<template>
  <RouterView v-slot="{ Component }">
    <AppShell :title="t('title')">
      <div class="w-xs max-w-full">
        <div class="mb-16">
          <h2 class="text-lg mt-0 mb-2">{{ t("section-1-title") }}</h2>
          <p>{{ t("section-1-description") }}</p>
          <PfButtonLink tag="routerLink" :label="t('open-modal')" :to="{ name: URL_DOC_DIALOG_BASIC }" />
        </div>
        <div class="mb-16">
          <h2 class="text-lg mt-0 mb-2">{{ t("section-2-title") }}</h2>
          <p>{{ t("section-2-description") }}</p>
          <PfButtonLink tag="routerLink" :label="t('open-confirm-modal')" :to="{ name: URL_DOC_DIALOG_CONFIRM }" />
        </div>
        <div class="mb-16">
          <h2 class="text-lg mt-0 mb-2">{{ t("section-3-title") }}</h2>
          <p>{{ t("section-3-description") }}</p>
          <PfButtonLink tag="routerLink" :label="t('open-delete-modal')" :to="{ name: URL_DOC_DIALOG_DELETE }" />
        </div>
      </div>
      <Component :is="Component" />
    </AppShell>
  </RouterView>
</template>

<script setup>
import { useI18n } from "vue-i18n";

import { usePageTitle } from "@/lib/helpers/page-title";

import { URL_DOC_DIALOG_BASIC, URL_DOC_DIALOG_DELETE, URL_DOC_DIALOG_CONFIRM } from "@/lib/consts/urls";

const { t } = useI18n();

usePageTitle(t("title"));
</script>
