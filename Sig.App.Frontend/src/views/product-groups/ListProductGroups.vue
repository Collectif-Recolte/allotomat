<i18n>
  {
    "en": {
      "add-product-group": "Add a group",
      "title": "Product group management",
      "empty-list": "This program does not contain any product groups.",
      "create-product-group": "Create a product group",
      "product-group-count": "{count} product group | {count} product groups"
    },
    "fr": {
      "add-product-group": "Ajouter un groupe",
      "title": "Gestion des groupes de produits",
      "empty-list":"Ce programme ne contient aucun groupe de produits.",
      "create-product-group": "Créer un groupe de produits",
      "product-group-count": "{count} groupe de produits | {count} groupes de produits"
    }
  }
  </i18n>

<template>
  <RouterView v-slot="{ Component }">
    <div v-if="projects && projects.length > 0">
      <template v-if="showProductGroupList">
        <UiTableHeader :title="productGroupCount" :cta-label="t('add-product-group')" :cta-route="addProductGroupRoute" />
        <ProductGroupTable :product-groups="productGroups" />
      </template>

      <UiEmptyPage v-else>
        <UiCta
          :description="t('empty-list')"
          :primary-btn-label="t('create-product-group')"
          :primary-btn-route="addProductGroupRoute" />
      </UiEmptyPage>
      <Component :is="Component" />
    </div>
  </RouterView>
</template>

<script setup>
import { computed } from "vue";
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { onBeforeRouteUpdate } from "vue-router";
import { useQuery, useResult } from "@vue/apollo-composable";

import { usePageTitle } from "@/lib/helpers/page-title";

import { COLOR_0 } from "@/lib/consts/color";
import { URL_PRODUCT_GROUP_ADD, URL_PRODUCT_GROUP_ADMIN } from "@/lib/consts/urls";

import ProductGroupTable from "@/components/product-groups/product-group-table";

const { t } = useI18n();

usePageTitle(t("title"));

const { result, refetch } = useQuery(
  gql`
    query Projects {
      projects {
        id
        productGroups {
          id
          name
          color
          orderOfAppearance
          types {
            id
          }
        }
      }
    }
  `
);
const projects = useResult(result);

const showProductGroupList = computed(() => productGroups.value.length > 0);

const productGroupCount = computed(() => t("product-group-count", { count: productGroups.value.length }));

const productGroups = computed(() => {
  if (projects.value) {
    return projects.value[0].productGroups.filter((x) => x.color !== COLOR_0);
  }

  return [];
});

const addProductGroupRoute = computed(() => {
  return {
    name: URL_PRODUCT_GROUP_ADD,
    query: { projectId: projects.value[0].id }
  };
});

onBeforeRouteUpdate((to) => {
  if (to.name === URL_PRODUCT_GROUP_ADMIN) {
    refetch();
  }
});
</script>
