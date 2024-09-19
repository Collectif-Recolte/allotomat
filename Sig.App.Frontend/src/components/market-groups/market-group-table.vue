<i18n>
  {
      "en": {
          "edit-market-group": "Edit",
          "market-group-archive": "Archive",
          "market-group-delete": "Delete",
          "market-group-edit-managers": "Edit managers",
          "market-group-edit-associated-merchants": "Edit associated merchants",
          "market-group-name": "Name",
          "options": "Options",
      },
      "fr": {
          "edit-market-group": "Modifier",
          "market-group-archive": "Archiver",
          "market-group-delete": "Supprimer",
          "market-group-edit-managers": "Modifier les gestionnaires",
          "market-group-edit-associated-merchants": "Modifier les commerces associés",
          "market-group-name": "Nom",
          "options": "Options",
      }
  }
</i18n>

<template>
  <UiTable v-if="props.marketGroups" :items="props.marketGroups" :cols="cols">
    <template #default="slotProps">
      <td class="py-3">
        {{ getMarketGroupName(slotProps.item) }}
      </td>
      <td>
        <div class="inline-flex items-center gap-x-2">
          <PfButtonLink
            tag="routerLink"
            btn-style="outline"
            size="sm"
            :label="t('market-group-edit-associated-merchants')"
            :to="{ name: URL_MARKET_GROUP_MANAGE_MERCHANTS, params: { marketGroupId: slotProps.item.id } }" />
          <UiButtonGroup :items="getBtnGroup(slotProps.item)" tooltip-position="left" />
        </div>
      </td>
    </template>
  </UiTable>
</template>

<script setup>
import { defineProps, computed } from "vue";
import { useI18n } from "vue-i18n";

import ICON_BRIEFCASE from "@/lib/icons/briefcase.json";
import ICON_TRASH from "@/lib/icons/trash.json";
import ICON_PENCIL from "@/lib/icons/pencil.json";
import ICON_FOLDER from "@/lib/icons/folder.json";

import { URL_MARKET_GROUP_MANAGE_MANAGERS, URL_MARKET_GROUP_EDIT, URL_MARKET_GROUP_MANAGE_MERCHANTS } from "@/lib/consts/urls";

const { t } = useI18n();

const props = defineProps({
  marketGroups: { type: Object, default: null },
  urlNameMarketGroupArchive: { type: String, default: "" },
  urlNameMarketGroupDelete: { type: String, default: "" },
  canEdit: Boolean
});

const cols = computed(() => {
  return props.canEdit || props.urlNameMarketGroupDelete || props.urlNameMarketGroupArchive
    ? [
        { label: t("market-group-name") },
        {
          label: t("options"),
          hasHiddenLabel: true
        }
      ]
    : [{ label: t("market-group-name") }];
});

const getBtnGroup = (marketGroup) => [
  {
    icon: ICON_PENCIL,
    label: t("edit-market-group"),
    route: { name: URL_MARKET_GROUP_EDIT, params: { marketGroupId: marketGroup.id } },
    if: props.canEdit
  },
  {
    icon: ICON_BRIEFCASE,
    label: t("market-group-edit-managers"),
    route: { name: URL_MARKET_GROUP_MANAGE_MANAGERS, params: { marketGroupId: marketGroup.id } },
    if: props.canEdit
  },
  {
    icon: ICON_TRASH,
    label: t("market-group-delete"),
    route: { name: props.urlNameMarketGroupDelete, params: { marketGroupId: marketGroup.id } },
    if: marketGroup.isArchived
  },
  {
    icon: ICON_FOLDER,
    label: t("market-group-archive"),
    route: { name: props.urlNameMarketGroupArchive, params: { marketGroupId: marketGroup.id } },
    if: !marketGroup.isArchived
  }
];

function getMarketGroupName(item) {
  return `${item.name}`;
}
</script>
