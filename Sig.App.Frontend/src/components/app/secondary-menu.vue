<i18n>
{
	"en": {
		"menu-title": "{name} program",
    "program-settings": "Program settings",
		"manage-organization-managers": "User management",
		"manage-project-managers": "User management",
    "manage-project-export-all-participants": "Export all participants",
    "reconciliation-report": "Reconciliation report"
	},
	"fr": {
		"menu-title": "Programme {name}",
    "program-settings": "Paramètres du programme",
		"manage-organization-managers": "Gestion des utilisateurs",
		"manage-project-managers": "Gestion des utilisateurs",
    "manage-project-export-all-participants": "Exporter tous les participants",
    "reconciliation-report": "Rapport de réconciliation"
	}
}
</i18n>

<template>
  <div v-if="showSecondaryMenu" class="shrink-0 flex flex-col items-start border-t border-primary-300 dark:border-grey-900 py-4">
    <nav class="px-2 space-y-0.5 w-full" aria-labelledby="secondaryMenuTitle">
      <span
        v-if="projects && projects.length > 0"
        id="secondaryMenuTitle"
        class="text-p4 uppercase font-semibold inline-block px-2 mb-1"
        >{{ t("menu-title", { name: projects[0].name }) }}</span
      >
      <span
        v-if="marketGroups && marketGroups.length > 0"
        id="secondaryMenuTitle"
        class="text-p4 uppercase font-semibold inline-block px-2 mb-1"
        >{{ t("menu-title", { name: marketGroups[0].name }) }}</span
      >
      <SecondaryMenuItem
        v-if="manageProgram"
        :router-link="{ name: $consts.urls.URL_PROJECT_SETTINGS }"
        :label="t('program-settings')" />
      <SecondaryMenuItem
        v-if="manageOrganizationManagers"
        :router-link="{ name: $consts.urls.URL_ORGANIZATION_MANAGER_ADMIN }"
        :label="t('manage-organization-managers')" />
      <SecondaryMenuItem
        v-if="manageProjectManagers"
        :router-link="{ name: $consts.urls.URL_PROJECT_MANAGER_ADMIN }"
        :label="t('manage-project-managers')" />
      <SecondaryMenuItem
        v-if="manageProjectManagers || manageSpecificMarketGroup"
        :router-link="{ name: $consts.urls.URL_RECONCILIATION_REPORT }"
        :label="t('reconciliation-report')" />
      <button
        v-if="manageProjectManagers && manageBeneficiaries"
        class="secondary-menu-item secondary-menu-item--is-inactive"
        style="width: 285px"
        @click="onExportReport">
        {{ t("manage-project-export-all-participants") }}
      </button>
    </nav>
  </div>
</template>

<script setup>
import gql from "graphql-tag";
import { computed } from "vue";
import { storeToRefs } from "pinia";
import { useI18n } from "vue-i18n";
import { useQuery, useResult, useApolloClient } from "@vue/apollo-composable";

import { useAuthStore } from "@/lib/store/auth";

import SecondaryMenuItem from "@/components/app/secondary-menu-item";

import {
  GLOBAL_MANAGE_PROJECT_MANAGERS,
  GLOBAL_MANAGE_ORGANIZATION_MANAGERS,
  GLOBAL_MANAGE_CARDS,
  GLOBAL_MANAGE_SPECIFIC_PROJECT,
  GLOBAL_MANAGE_CATEGORIES,
  GLOBAL_MANAGE_PRODUCT_GROUP,
  GLOBAL_MANAGE_BENEFICIARIES,
  GLOBAL_MANAGE_SPECIFIC_MARKET_GROUP
} from "@/lib/consts/permissions";
import { LANG_EN } from "@/lib/consts/langs";
import { LANGUAGE_FILTER_EN, LANGUAGE_FILTER_FR } from "@/lib/consts/enums";

const { t, locale } = useI18n();

const { resolveClient } = useApolloClient();
const client = resolveClient();

const { getGlobalPermissions } = storeToRefs(useAuthStore());

const manageSpecificProject = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_SPECIFIC_PROJECT);
});

const manageSpecificMarketGroup = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_SPECIFIC_MARKET_GROUP);
});

const manageProjectManagers = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_PROJECT_MANAGERS);
});

const manageCards = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_CARDS);
});

const manageProgram = computed(() => {
  return (
    getGlobalPermissions.value.includes(GLOBAL_MANAGE_CATEGORIES) ||
    getGlobalPermissions.value.includes(GLOBAL_MANAGE_PRODUCT_GROUP)
  );
});

const manageOrganizationManagers = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_ORGANIZATION_MANAGERS);
});

const manageBeneficiaries = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_BENEFICIARIES);
});

const showSecondaryMenu = computed(() => {
  const showLink =
    manageProjectManagers.value || manageCards.value || manageOrganizationManagers.value || manageSpecificMarketGroup.value;
  return (manageSpecificProject.value || manageSpecificMarketGroup.value) && showLink;
});

const { result: resultProjects } = useQuery(
  gql`
    query SecondaryMenuProjects {
      projects {
        id
        name
        administrationSubscriptionsOffPlatform
      }
    }
  `,
  {},
  { fetchPolicy: "cache-first" }
);
const projects = useResult(resultProjects);

const { result: resultMarketGroups } = useQuery(
  gql`
    query SecondaryMenuMarketGroups {
      marketGroups {
        id
        name
      }
    }
  `,
  {},
  { fetchPolicy: "cache-first" }
);
const marketGroups = useResult(resultMarketGroups);

async function onExportReport() {
  let result = null;
  const timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
  const project = projects.value[0];

  if (project.administrationSubscriptionsOffPlatform) {
    result = await client.query({
      query: gql`
        query ExportOffPlatformBeneficiariesList($projectId: ID!, $timeZoneId: String!) {
          exportOffPlatformBeneficiariesList(id: $projectId, timeZoneId: $timeZoneId)
        }
      `,
      variables: {
        projectId: project.id,
        timeZoneId: timeZone
      }
    });
    window.open(result.data.exportOffPlatformBeneficiariesList, "_blank");
  } else {
    result = await client.query({
      query: gql`
        query ExportBeneficiariesList($projectId: ID!, $timeZoneId: String!, $language: Language!) {
          exportBeneficiariesList(id: $projectId, timeZoneId: $timeZoneId, language: $language)
        }
      `,
      variables: {
        projectId: project.id,
        timeZoneId: timeZone,
        language: locale.value === LANG_EN ? LANGUAGE_FILTER_EN : LANGUAGE_FILTER_FR
      }
    });
    window.open(result.data.exportBeneficiariesList, "_blank");
  }
}
</script>
