<i18n>
  {
    "en": {
      "add-category": "Add a category",
      "title": "Category management",
      "empty-list": "This program does not contain any categories.",
      "create-category": "Create a category",
      "category-count": "{count} categorie | {count} categories"
    },
    "fr": {
      "add-category": "Ajouter une catégorie",
      "title": "Gestion des catégories",
      "empty-list":"Ce programme ne contient aucune catégorie.",
      "create-category": "Créer une catégorie",
      "category-count": "{count} catégorie | {count} catégories"
    }
  }
  </i18n>

<template>
  <RouterView v-slot="{ Component }">
    <div v-if="projects && projects.length > 0">
      <template v-if="showCategoryList">
        <UiTableHeader :title="categoryCount" :cta-label="t('add-category')" :cta-route="addCategoryRoute" />
        <CategoryTable :categories="projects[0].beneficiaryTypes" />
      </template>

      <UiEmptyPage v-else>
        <UiCta :description="t('empty-list')" :primary-btn-label="t('create-category')" :primary-btn-route="addCategoryRoute" />
      </UiEmptyPage>
      <Component :is="Component" />
    </div>
  </RouterView>
</template>

<script setup>
import { computed, defineEmits, onBeforeMount } from "vue";
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { useQuery, useResult } from "@vue/apollo-composable";

import { usePageTitle } from "@/lib/helpers/page-title";

import { URL_CATEGORY_ADD } from "@/lib/consts/urls";

import CategoryTable from "@/components/category/category-table";

const { t } = useI18n();
const emit = defineEmits(["isLoading", "loadingFinish"]);

usePageTitle(t("title"));

const { result, refetch } = useQuery(
  gql`
    query Projects {
      projects {
        id
        beneficiaryTypes {
          id
          name
          keys
          beneficiaries {
            id
          }
        }
      }
    }
  `
);
const projects = useResult(result, null, (data) => {
  if (data.projects !== null) {
    emit("loadingFinish");
  }
  return data.projects;
});

const showCategoryList = computed(() => projects.value[0].beneficiaryTypes.length > 0);

const categoryCount = computed(() => t("category-count", { count: projects.value[0].beneficiaryTypes.length }));

const addCategoryRoute = computed(() => {
  return {
    name: URL_CATEGORY_ADD,
    query: { projectId: projects.value[0].id }
  };
});

onBeforeMount(() => {
  refetch();
  emit("isLoading");
});
</script>
