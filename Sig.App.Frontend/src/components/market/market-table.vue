<i18n>
  {
      "en": {
          "edit-market": "Edit",
          "market-archive": "Archive",
          "market-delete": "Delete",
          "market-edit-managers": "Edit managers",
          "market-name": "Name",
          "options": "Options",
      },
      "fr": {
          "edit-market": "Modifier",
          "market-archive": "Archiver",
          "market-delete": "Supprimer",
          "market-edit-managers": "Modifier les gestionnaires",
          "market-name": "Nom",
          "options": "Options",
      }
  }
</i18n>

<template>
  <UiTable v-if="props.markets" :items="props.markets" :cols="cols">
    <template #default="slotProps">
      <td class="py-3">
        {{ getMarketName(slotProps.item) }}
      </td>
      <td>
        <UiButtonGroup :items="getBtnGroup(slotProps.item)" tooltip-position="left" />
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

const { t } = useI18n();

const props = defineProps({
  markets: { type: Object, default: null },
  urlNameMarketArchive: { type: String, default: "" },
  urlNameMarketDelete: { type: String, default: "" },
  urlNameMarketEdit: { type: String, default: "" },
  urlNameMarketManageManagers: { type: String, default: "" },
  canEdit: Boolean
});

const cols = computed(() => {
  return props.canEdit || props.urlNameMarketDelete || props.urlNameMarketArchive
    ? [
        { label: t("market-name") },
        {
          label: t("options"),
          hasHiddenLabel: true
        }
      ]
    : [{ label: t("market-name") }];
});

const getBtnGroup = (market) => [
  {
    icon: ICON_PENCIL,
    label: t("edit-market"),
    route: { name: props.urlNameMarketEdit, params: { marketId: market.id } },
    if: props.canEdit
  },
  {
    icon: ICON_BRIEFCASE,
    label: t("market-edit-managers"),
    route: { name: props.urlNameMarketManageManagers, params: { marketId: market.id } },
    if: props.canEdit
  },
  {
    icon: ICON_TRASH,
    label: t("market-delete"),
    route: { name: props.urlNameMarketDelete, params: { marketId: market.id } },
    if: props.urlNameMarketDelete
  },
  {
    icon: ICON_FOLDER,
    label: t("market-archive"),
    route: { name: props.urlNameMarketArchive, params: { marketId: market.id } },
    if: props.urlNameMarketArchive
  }
];

function getMarketName(item) {
  return `${item.name}`;
}
</script>
