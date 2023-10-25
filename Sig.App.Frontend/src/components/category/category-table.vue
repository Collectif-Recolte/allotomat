<i18n>
  {
      "en": {
          "options": "Options",
          "category-delete": "Delete",
          "category-edit": "Edit configuration",
          "category-name": "Category name",
          "category-keys": "Tags",
          "delete-category-error-notification": "The category {categoryName} cannot be deleted since it is currently being used."
      },
      "fr": {
          "options": "Options",
          "category-delete": "Supprimer",
          "category-edit": "Modifier la configuration",
          "category-name": "Nom de la catégorie",
          "category-keys": "Clés",
          "delete-category-error-notification": "La catégorie {categoryName} ne peut pas être supprimée puisqu’elle est actuellement utilisée."
      }
  }
</i18n>

<template>
  <UiTable v-if="props.categories" :items="props.categories" :cols="cols">
    <template #default="slotProps">
      <td>
        {{ getCategoryName(slotProps.item) }}
      </td>
      <td>
        {{ getCategoryKeys(slotProps.item) }}
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
import { useRouter } from "vue-router";
import { useNotificationsStore } from "@/lib/store/notifications";

import ICON_TRASH from "@/lib/icons/trash.json";
import ICON_PENCIL from "@/lib/icons/pencil.json";
import { URL_CATEGORY_EDIT, URL_CATEGORY_DELETE } from "@/lib/consts/urls";

const { addError } = useNotificationsStore();
const router = useRouter();
const { t } = useI18n();

const props = defineProps({
  categories: { type: Object, default: null }
});

const cols = computed(() => {
  let cols = [];
  cols.push({ label: t("category-name") });
  cols.push({ label: t("category-keys") });
  cols.push({
    label: t("options"),
    hasHiddenLabel: true
  });
  return cols;
});

const getBtnGroup = (category) => [
  {
    icon: ICON_PENCIL,
    label: t("category-edit"),
    route: { name: URL_CATEGORY_EDIT, params: { categoryId: category.id } }
  },
  {
    icon: ICON_TRASH,
    label: t("category-delete"),
    onClick: () => manageDeleteCategory(category)
  }
];

function getCategoryName(item) {
  return `${item.name}`;
}

function getCategoryKeys(item) {
  return `${item.keys}`;
}

function manageDeleteCategory(category) {
  const canDelete = category.beneficiaries.length === 0;
  if (canDelete) {
    router.push({ name: URL_CATEGORY_DELETE, params: { categoryId: category.id } });
  } else {
    addError(t("delete-category-error-notification", { categoryName: category.name }));
  }
}
</script>
