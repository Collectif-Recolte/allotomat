<i18n>
{
	"en": {
		"add-organization": "Add",
		"title": "Management of group",
    "empty-list": "No group is associated with the program.",
    "organization-count": "{count} group | {count} groups"
	},
	"fr": {
		"add-organization": "Ajouter",
		"title": "Gestion des groupes",
    "empty-list": "Aucun groupe n'est associé au programme.",
    "organization-count": "{count} groupe | {count} groupes"
	}
}
</i18n>

<template>
  <RouterView v-slot="{ Component }">
    <AppShell :title="t('title')" :loading="loading || projectsLoading">
      <div v-if="projects && projects.length > 0 && organizations">
        <template v-if="organizations.length > 0">
          <UiTableHeader
            :title="t('organization-count', { count: organizations.length })"
            :cta-label="t('add-organization')"
            :cta-route="addOrganizationRoute" />
          <OrganizationTable :organizations="organizations" />
        </template>
        <UiEmptyPage v-else>
          <UiCta
            :img-src="require('@/assets/img/organismes.jpg')"
            :description="t('empty-list')"
            :primary-btn-label="t('add-organization')"
            :primary-btn-route="addOrganizationRoute" />
        </UiEmptyPage>
      </div>

      <Component :is="Component" />
    </AppShell>
  </RouterView>
</template>

<script setup>
import gql from "graphql-tag";
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { onBeforeRouteUpdate } from "vue-router";
import { useQuery, useResult } from "@vue/apollo-composable";
import { usePageTitle } from "@/lib/helpers/page-title";

import OrganizationTable from "@/components/organization/organization-table";

import { URL_ORGANIZATION_ADD, URL_ORGANIZATION_ADMIN } from "@/lib/consts/urls";

const { t } = useI18n();

usePageTitle(t("title"));

const {
  result: resultOrganizations,
  loading,
  refetch
} = useQuery(
  gql`
    query Organizations {
      organizations {
        id
        name
      }
    }
  `
);
const organizations = useResult(resultOrganizations);

const { result: resultProjects, loading: projectsLoading } = useQuery(
  gql`
    query Projects {
      projects {
        id
        name
      }
    }
  `
);
const projects = useResult(resultProjects);

const addOrganizationRoute = computed(() => {
  return {
    name: URL_ORGANIZATION_ADD,
    query: { projectId: projects.value[0].id }
  };
});

onBeforeRouteUpdate((to) => {
  if (to.name === URL_ORGANIZATION_ADMIN) {
    refetch();
  }
});
</script>
