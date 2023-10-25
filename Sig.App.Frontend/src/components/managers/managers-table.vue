<i18n>
  {
      "en": {
          "options": "Options",
          "project-manager-delete": "Remove",
          "username": "Manager name"
      },
      "fr": {
          "options": "Options",
          "project-manager-delete": "Retirer",
          "username": "Nom du gestionnaire"
      }
  }
</i18n>

<template>
  <UiTable v-if="props.managers" :items="props.managers" :cols="cols">
    <template #default="slotProps">
      <td>
        <p v-if="getUserName(slotProps.item)" class="mb-0">{{ getUserName(slotProps.item) }}</p>
        <!-- eslint-disable-next-line vue/no-v-html -->
        <p v-if="slotProps.item.email" class="mb-0 text-p4" v-html="getUserEmail(slotProps.item)"></p>
      </td>
      <td>
        <UiButtonGroup :items="getBtnGroup(slotProps.item)" tooltip-position="left" />
      </td>
    </template>
  </UiTable>
</template>

<script setup>
import { defineProps, computed, defineEmits } from "vue";
import { useI18n } from "vue-i18n";

import ICON_TRASH from "@/lib/icons/trash.json";

const { t } = useI18n();

const emit = defineEmits(["removeManager"]);

const props = defineProps({
  managers: { type: Object, default: null }
});

const cols = computed(() => [
  {
    label: t("username")
  },
  {
    label: t("options"),
    hasHiddenLabel: true
  }
]);

const getBtnGroup = (manager) => [
  {
    icon: ICON_TRASH,
    label: t("project-manager-delete"),
    onClick: () => removeManager(manager)
  }
];

function getUserName(item) {
  if (item.profile !== null && item.profile.firstName !== null && item.profile.lastName !== null) {
    return `${item.profile.firstName} ${item.profile.lastName}`;
  }
  return "";
}

function getUserEmail(item) {
  return item.email !== null ? `<a href="mailto:${item.email}">${item.email}</a>` : "";
}

function removeManager(manager) {
  emit("removeManager", manager);
}
</script>
