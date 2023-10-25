<i18n>
{
	"en": {
    "manage-cards": "Card management",
    "cards-summary": "Dashboard",
		"generate-cards": "Generate new cards",
		"title": "Cards",
    "selected-organization": "Organization",
    "empty-list": "You haven't generated any cards yet.",
    "empty-list-organisation":"No organization is associated with the program.",
    "add-organization": "Add an organization",
    "empty-card-list": "The program has not generated any cards yet."
	},
	"fr": {
		"manage-cards": "Gérer des cartes",
    "cards-summary": "Tableau de bord",
		"generate-cards": "Générer de nouvelles cartes",
		"title": "Cartes",
    "selected-organization": "Organisme",
    "empty-list": "Vous n'avez pas encore généré de cartes.",
    "empty-list-organisation": "Aucun organisme n'est associé au programme.",
    "add-organization": "Ajouter un organisme",
    "empty-card-list": "Le programme n'a pas encore généré de cartes."
	}
}
</i18n>

<template>
  <RouterView v-slot="{ Component }">
    <AppShell :loading="loadingOrganizations || loadingProjects">
      <template #title>
        <Title :title="t('title')" :subpages="subpages">
          <template v-if="canManageOrganizations && organizations && organizations.length > 0" #right>
            <div class="flex items-center gap-x-4">
              <span class="text-sm text-primary-700" aria-hidden>{{ t("selected-organization") }}</span>
              <PfFormInputSelect
                id="selectedOrganization"
                has-hidden-label
                :label="t('selected-organization')"
                :value="selectedOrganization"
                :options="organizations"
                col-span-class="sm:col-span-3"
                @input="onOrganizationSelected" />
            </div>
          </template>
          <template v-if="canManageOrganizations && projects" #subpagesCta>
            <PfButtonLink
              v-if="project"
              tag="routerLink"
              :label="t('generate-cards')"
              :to="{ name: URL_CARDS_ADD, query: { projectId: project.id } }" />
          </template>
        </Title>
      </template>
      <Component :is="Component" v-if="showContent" :selected-organization="selectedOrganization" />
      <UiEmptyPage v-else-if="organizations && organizations.length === 0">
        <UiCta
          :img-src="require('@/assets/img/organismes.jpg')"
          :description="t('empty-list-organisation')"
          :primary-btn-label="t('add-organization')"
          :primary-btn-route="addOrganizationRoute">
        </UiCta>
      </UiEmptyPage>
      <UiEmptyPage v-else-if="canManageOrganizations && organizations && project">
        <UiCta
          :img-src="require('@/assets/img/cards.jpg')"
          :description="t('empty-list')"
          :primary-btn-label="t('generate-cards')"
          :primary-btn-route="{ name: URL_CARDS_ADD, query: { projectId: project.id } }" />
      </UiEmptyPage>
      <UiEmptyPage v-else-if="!canManageOrganizations && organizations && project">
        <UiCta :img-src="require('@/assets/img/cards.jpg')" :description="t('empty-card-list')" />
      </UiEmptyPage>
    </AppShell>
  </RouterView>
</template>

<script setup>
import gql from "graphql-tag";
import { ref, computed } from "vue";
import { useI18n } from "vue-i18n";
import { useQuery, useResult } from "@vue/apollo-composable";
import { useRoute } from "vue-router";
import { storeToRefs } from "pinia";
import { onBeforeRouteUpdate } from "vue-router";

import { useOrganizationStore } from "@/lib/store/organization";
import { usePageTitle } from "@/lib/helpers/page-title";
import { useAuthStore } from "@/lib/store/auth";

import { URL_CARDS_ADD, URL_CARDS_ASSIGNATION, URL_CARDS_SUMMARY, URL_ORGANIZATION_ADD } from "@/lib/consts/urls";
import { GLOBAL_MANAGE_ORGANIZATIONS } from "@/lib/consts/permissions";

import Title from "@/components/app/title";

const { getGlobalPermissions } = storeToRefs(useAuthStore());
const { t } = useI18n();

const selectedOrganization = ref("");
const project = ref(undefined);
const route = useRoute();
const { currentOrganization, changeOrganization } = useOrganizationStore();

usePageTitle(t("title"));

const { result: resultOrganizations, loading: loadingOrganizations } = useQuery(
  gql`
    query Organizations {
      organizations {
        id
        name
        project {
          id
          cardStats {
            cardsAssigned
            cardsUnassigned
          }
        }
      }
    }
  `
);
const organizations = useResult(resultOrganizations, null, (data) => {
  if (data.organizations.length > 0) {
    const organisation = data.organizations.find((x) => x.id === currentOrganization) ?? data.organizations[0];
    selectedOrganization.value = `${organisation.id}`;
    changeOrganization(organisation.id);
    if (project.value === undefined) {
      project.value = organisation.project;
    }
    return data.organizations.map((x) => ({
      label: x.name,
      value: `${x.id}`
    }));
  } else {
    return [];
  }
});

const canManageOrganizations = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_ORGANIZATIONS);
});

const {
  result: resultProjects,
  loading: loadingProjects,
  refetch: refetchProjects
} = useQuery(
  gql`
    query Projects {
      projects {
        id
        cardStats {
          cardsAssigned
          cardsUnassigned
        }
      }
    }
  `,
  null,
  {
    enabled: canManageOrganizations
  }
);
const projects = useResult(resultProjects, null, (data) => {
  project.value = data.projects[0];
  return data.projects;
});

const hasCards = computed(
  () => project.value && project.value.cardStats.cardsAssigned + project.value.cardStats.cardsUnassigned > 0
);

const showContent = computed(() => hasCards.value || route.name == URL_CARDS_ADD);

const subpages = computed(() =>
  project.value !== undefined
    ? [
        {
          route: { name: URL_CARDS_SUMMARY, query: { projectId: project.value.id } },
          label: t("cards-summary"),
          isActive: true
        },
        {
          route: {
            name: URL_CARDS_ASSIGNATION,
            query: { projectId: project.value.id }
          },
          label: t("manage-cards"),
          isActive: false
        }
      ]
    : []
);

function onOrganizationSelected(e) {
  selectedOrganization.value = e;
  changeOrganization(e);
}

onBeforeRouteUpdate((to) => {
  if (to.name === URL_CARDS_SUMMARY || to.name === URL_CARDS_ASSIGNATION) {
    refetchProjects();
  }
});

const addOrganizationRoute = computed(() => {
  if (projects.value) {
    return {
      name: URL_ORGANIZATION_ADD,
      query: { projectId: projects.value[0].id }
    };
  }
  return "";
});
</script>
