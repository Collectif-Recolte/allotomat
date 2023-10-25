<i18n>
  {
    "en": {
      "edit-project": "Edit program",
      "options": "Options",
      "project-delete": "Delete",
      "project-edit-associated-merchants": "Edit associated merchants",
      "project-edit-managers": "Edit managers",
      "project-name": "Program name",
      "project-beneficiaries-are-anonymous-tooltip": "Anonymous beneficiaries"
    },
    "fr": {
      "edit-project": "Modifier le programme",
      "options": "Options",
      "project-delete": "Supprimer",
      "project-edit-associated-merchants": "Modifier les commerces associés",
      "project-edit-managers": "Modifier les gestionnaires",
      "project-name": "Nom du programme",
      "project-beneficiaries-are-anonymous-tooltip": "Participant-e-s anonymes"
    }
  }
  </i18n>

<template>
  <UiTable :items="props.projects" :cols="cols">
    <template #default="slotProps">
      <td>
        <div class="inline-flex items-center">
          <PfTooltip
            position="right"
            class="group-pfone"
            :hide-tooltip="!beneficiariesAreAnonymous(slotProps.item)"
            :label="t('project-beneficiaries-are-anonymous-tooltip')">
            <PfIcon
              class="mr-4"
              :class="{ 'opacity-50': !beneficiariesAreAnonymous(slotProps.item) }"
              size="sm"
              :icon="ICON_ANONYMOUS" />
          </PfTooltip>
          {{ getProjectName(slotProps.item) }}
        </div>
      </td>
      <td>
        <div class="inline-flex items-center gap-x-2">
          <PfButtonLink
            tag="routerLink"
            btn-style="outline"
            size="sm"
            :label="t('project-edit-associated-merchants')"
            :to="{ name: URL_PROJECT_MANAGE_MERCHANTS, params: { projectId: slotProps.item.id } }" />
          <UiButtonGroup :items="getBtnGroup(slotProps.item)" tooltip-position="left" />
        </div>
      </td>
    </template>
  </UiTable>
</template>

<script setup>
import { defineProps, computed } from "vue";
import { useI18n } from "vue-i18n";
import ICON_ANONYMOUS from "@/lib/icons/anonymous.json";

import {
  URL_PROJECT_MANAGE_MERCHANTS,
  URL_PROJECT_DELETE,
  URL_PROJECT_MANAGE_MANAGERS,
  URL_PROJECT_EDIT
} from "@/lib/consts/urls";

import ICON_TRASH from "@/lib/icons/trash.json";
import ICON_BRIEFCASE from "@/lib/icons/briefcase.json";
import ICON_PENCIL from "@/lib/icons/pencil.json";

const { t } = useI18n();

const props = defineProps({
  projects: { type: Array, required: true }
});

const cols = computed(() => [
  { label: t("project-name") },
  {
    label: t("options"),
    hasHiddenLabel: true
  }
]);

const getBtnGroup = (project) => [
  {
    icon: ICON_PENCIL,
    label: t("edit-project"),
    route: { name: URL_PROJECT_EDIT, params: { projectId: project.id } }
  },
  {
    icon: ICON_BRIEFCASE,
    label: t("project-edit-managers"),
    route: { name: URL_PROJECT_MANAGE_MANAGERS, params: { projectId: project.id } }
  },
  {
    icon: ICON_TRASH,
    label: t("project-delete"),
    route: { name: URL_PROJECT_DELETE, params: { projectId: project.id } }
  }
];

function getProjectName(item) {
  return `${item.name}`;
}

function beneficiariesAreAnonymous(project) {
  return project.beneficiariesAreAnonymous;
}
</script>
