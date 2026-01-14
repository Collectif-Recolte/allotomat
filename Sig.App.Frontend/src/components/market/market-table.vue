<i18n>
  {
      "en": {
          "edit-market": "Edit",
          "market-archive": "Archive",
          "market-delete": "Delete",
          "market-edit-managers": "Edit managers",
          "market-name": "Name",
          "options": "Options",
          "disabled-market": "Disable",
          "enabled-market": "Enable"
      },
      "fr": {
          "edit-market": "Modifier",
          "market-archive": "Archiver",
          "market-delete": "Supprimer",
          "market-edit-managers": "Modifier les gestionnaires",
          "market-name": "Nom",
          "options": "Options",
          "disabled-market": "Désactiver",
          "enabled-market": "Réactiver"
      }
  }
</i18n>

<template>
  <UiTable v-if="props.markets" :items="markets" :cols="cols">
    <template #default="slotProps">
      <td class="py-3">
        {{ getMarketName(slotProps.item) }}
      </td>
      <td>
        <div class="inline-flex items-center gap-x-2">
          <template v-if="props.canEdit">
            <PfButtonLink
              v-if="!slotProps.item.isDisabled"
              tag="routerLink"
              btn-style="outline"
              size="sm"
              :label="t('disabled-market')"
              :to="{ name: props.urlNameMarketDisabled, params: { marketId: slotProps.item.id } }" />
            <PfButtonLink
              v-else
              tag="routerLink"
              btn-style="outline"
              size="sm"
              :label="t('enabled-market')"
              :to="{ name: props.urlNameMarketEnabled, params: { marketId: slotProps.item.id } }" />
          </template>
          <UiButtonGroup :items="getBtnGroup(slotProps.item)" tooltip-position="left" />
        </div>
      </td>
    </template>
  </UiTable>
</template>

<script setup>
import { defineProps, computed } from "vue";
import { useI18n } from "vue-i18n";
import { URL_MARKET_DISABLED, URL_MARKET_ENABLED } from "@/lib/consts/urls";

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
  urlNameMarketEnabled: { type: String, default: URL_MARKET_ENABLED },
  urlNameMarketDisabled: { type: String, default: URL_MARKET_DISABLED },
  canEdit: Boolean,
  canDelete: Boolean,
  orderByMarketName: { type: Boolean, default: false }
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

const markets = computed(() => {
  if (props.orderByMarketName) {
    return [...props.markets].sort((a, b) => {
      return a.name.localeCompare(b.name);
    });
  }
  return props.markets;
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
    if: (market.isArchived || props.canDelete) && props.urlNameMarketDelete
  },
  {
    icon: ICON_FOLDER,
    label: t("market-archive"),
    route: { name: props.urlNameMarketArchive, params: { marketId: market.id } },
    if: !market.isArchived && props.urlNameMarketArchive
  }
];

function getMarketName(item) {
  return `${item.name}`;
}
</script>
