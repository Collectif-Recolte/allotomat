<i18n>
  {
      "en": {
          "options": "Options",
          "product-group-delete": "Delete",
          "product-group-edit": "Edit configuration",
          "product-group-name": "Name of the group",
          "product-group-color": "Color",
          "product-group-order": "Order",
          "delete-product-group-error-notification": "The product group {productGroupName} cannot be deleted since it is currently being used."
      },
      "fr": {
          "options": "Options",
          "product-group-delete": "Supprimer",
          "product-group-edit": "Modifier la configuration",
          "product-group-name": "Nom du groupe",
          "product-group-color": "Couleur",
          "product-group-order": "Ordre",
          "delete-product-group-error-notification": "Le groupe de produits {productGroupName} ne peut pas être supprimé puisqu’il est actuellement utilisé."
      }
  }
</i18n>

<template>
  <UiTable v-if="props.productGroups" :items="props.productGroups" :cols="cols">
    <template #default="slotProps">
      <td>
        {{ getProductGroupOrder(slotProps.item) }}
      </td>
      <td>
        <UiColorChip
          :label="getProductGroupColor(slotProps.item).label"
          :color="getProductGroupColor(slotProps.item).color"
          size="lg" />
      </td>
      <td>
        {{ getProductGroupName(slotProps.item) }}
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
import { getColorName, getColorBgClass } from "@/lib/helpers/products-color";

import ICON_TRASH from "@/lib/icons/trash.json";
import ICON_PENCIL from "@/lib/icons/pencil.json";
import { URL_PRODUCT_GROUP_EDIT, URL_PRODUCT_GROUP_DELETE } from "@/lib/consts/urls";

const { addError } = useNotificationsStore();
const router = useRouter();
const { t } = useI18n();

const props = defineProps({
  productGroups: { type: Object, default: null }
});

const cols = computed(() => {
  let cols = [];
  cols.push({ label: t("product-group-order") });
  cols.push({ label: t("product-group-color") });
  cols.push({ label: t("product-group-name") });
  cols.push({
    label: t("options"),
    hasHiddenLabel: true
  });
  return cols;
});

const getBtnGroup = (productGroup) => [
  {
    icon: ICON_PENCIL,
    label: t("product-group-edit"),
    route: { name: URL_PRODUCT_GROUP_EDIT, params: { productGroupId: productGroup.id } }
  },
  {
    icon: ICON_TRASH,
    label: t("product-group-delete"),
    onClick: () => manageDeleteProductGroup(productGroup)
  }
];

function getProductGroupName(item) {
  return `${item.name}`;
}

function getProductGroupOrder(item) {
  return `${item.orderOfAppearance}`;
}

function getProductGroupColor(item) {
  return {
    color: getColorBgClass(item.color),
    label: getColorName(item.color)
  };
}

function manageDeleteProductGroup(productGroup) {
  const canDelete = productGroup.types.length === 0;
  if (canDelete) {
    router.push({ name: URL_PRODUCT_GROUP_DELETE, params: { productGroupId: productGroup.id } });
  } else {
    addError(t("delete-product-group-error-notification", { productGroupName: productGroup.name }));
  }
}
</script>
