<i18n>
  {
    "en": {
      "options": "Options",
      "organization-delete": "Delete",
      "organization-edit": "Edit",
      "organization-edit-managers": "Edit managers",
      "organization-edit-associated-merchants": "Edit associated merchants",
      "organization-name": "Name"
    },
    "fr": {
      "options": "Options",
      "organization-delete": "Supprimer",
      "organization-edit": "Modifier",
      "organization-edit-managers": "Modifier les gestionnaires",
      "organization-edit-associated-merchants": "Modifier les commerces associés",
      "organization-name": "Nom"
    }
  }
  </i18n>

<template>
  <UiTable :items="props.organizations" :cols="cols">
    <template #default="slotProps">
      <td>
        {{ getOrganizationName(slotProps.item) }}
      </td>
      <td>
        <div class="inline-flex items-center gap-x-2">
          <PfButtonLink
            tag="routerLink"
            btn-style="outline"
            size="sm"
            :label="t('organization-edit-associated-merchants')"
            :to="{ name: URL_ORGANIZATION_MANAGE_MERCHANTS, params: { organizationId: slotProps.item.id } }" />
          <UiButtonGroup :items="getBtnGroup(slotProps.item.id)" tooltip-position="left" />
        </div>
      </td>
    </template>
  </UiTable>
</template>

<script setup>
import { defineProps, computed } from "vue";
import { useI18n } from "vue-i18n";

import {
  URL_ORGANIZATION_DELETE,
  URL_ORGANIZATION_MANAGE_MANAGERS,
  URL_ORGANIZATION_EDIT,
  URL_ORGANIZATION_MANAGE_MERCHANTS
} from "@/lib/consts/urls";

import ICON_BRIEFCASE from "@/lib/icons/briefcase.json";
import ICON_TRASH from "@/lib/icons/trash.json";
import ICON_PENCIL from "@/lib/icons/pencil.json";

const { t } = useI18n();

const props = defineProps({
  organizations: { type: Array, required: true }
});

const cols = computed(() => [
  {
    label: t("organization-name")
  },
  {
    label: t("options"),
    hasHiddenLabel: true
  }
]);

const getBtnGroup = (organizationId) => [
  {
    icon: ICON_PENCIL,
    label: t("organization-edit"),
    route: { name: URL_ORGANIZATION_EDIT, params: { organizationId: organizationId } }
  },
  {
    icon: ICON_BRIEFCASE,
    label: t("organization-edit-managers"),
    route: { name: URL_ORGANIZATION_MANAGE_MANAGERS, params: { organizationId: organizationId } }
  },
  {
    icon: ICON_TRASH,
    label: t("organization-delete"),
    route: { name: URL_ORGANIZATION_DELETE, params: { organizationId: organizationId } }
  }
];

function getOrganizationName(item) {
  return `${item.name}`;
}
</script>
