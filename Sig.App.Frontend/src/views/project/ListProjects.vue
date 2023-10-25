<i18n>
{
	"en": {
		"add-project": "Add a program",
		"title": "Program management",
    "create-project": "Create a program",
    "empty-list": "No programs have been created yet.",
    "project-count": "{count} program | {count} programs"
	},
	"fr": {
		"add-project": "Ajouter un programme",
		"title": "Gestion des programmes",
    "create-project": "Créer un programme",
    "empty-list": "Aucun programme n'a encore été créé.",
    "project-count": "{count} programme | {count} programmes"
	}
}
</i18n>

<template>
  <RouterView v-slot="{ Component }">
    <AppShell :title="t('title')" :loading="loading">
      <div v-if="projects">
        <template v-if="projects.length > 0">
          <UiTableHeader
            :title="t('project-count', { count: projects.length })"
            :cta-route="addProjectRoute"
            :cta-label="t('add-project')" />
          <ProjectTable :projects="projects" />
        </template>

        <UiEmptyPage v-else>
          <UiCta :description="t('empty-list')" :primary-btn-label="t('create-project')" :primary-btn-route="addProjectRoute" />
        </UiEmptyPage>
      </div>
      <Component :is="Component" />
    </AppShell>
  </RouterView>
</template>

<script setup>
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { onBeforeRouteUpdate } from "vue-router";
import { useQuery, useResult } from "@vue/apollo-composable";

import { URL_PROJECT_ADD, URL_PROJECT_ADMIN } from "@/lib/consts/urls";
import { usePageTitle } from "@/lib/helpers/page-title";

import ProjectTable from "@/components/project/project-table";

const { t } = useI18n();

usePageTitle(t("title"));

const { result, loading, refetch } = useQuery(
  gql`
    query Projects {
      projects {
        id
        name
        beneficiariesAreAnonymous
      }
    }
  `
);
const projects = useResult(result);

const addProjectRoute = { name: URL_PROJECT_ADD };

onBeforeRouteUpdate((to) => {
  if (to.name === URL_PROJECT_ADMIN) {
    refetch();
  }
});
</script>
