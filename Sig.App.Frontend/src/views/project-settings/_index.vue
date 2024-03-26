<i18n>
  {
    "en": {
      "manage-categories": "Participant categories",
      "manage-product-groups": "Product groups",
      "title": "Settings",
    },
    "fr": {
      "manage-categories": "Catégories de participants",
      "manage-product-groups": "Groupes de produits",
      "title": "Paramètres"
    }
  }
  </i18n>

<template>
  <RouterView v-slot="{ Component }">
    <AppShell :loading="loading">
      <template #title>
        <Title :title="t('title')" :subpages="subpages" />
      </template>
      <Component :is="Component" @isLoading="isLoading" @loadingFinish="loadingFinish" />
    </AppShell>
  </RouterView>
</template>

<script setup>
import { computed, ref } from "vue";
import { useI18n } from "vue-i18n";

import { usePageTitle } from "@/lib/helpers/page-title";

import { useAuthStore } from "@/lib/store/auth";

import { URL_CATEGORY_ADMIN, URL_PRODUCT_GROUP_ADMIN } from "@/lib/consts/urls";
import { GLOBAL_MANAGE_CATEGORIES } from "@/lib/consts/permissions";

import Title from "@/components/app/title";

const { t } = useI18n();
const authStore = useAuthStore();

const loading = ref(false);

usePageTitle(t("title"));

const subpages = computed(() => {
  if (authStore.getGlobalPermissions.includes(GLOBAL_MANAGE_CATEGORIES)) {
    return [
      {
        route: { name: URL_CATEGORY_ADMIN },
        label: t("manage-categories"),
        isActive: true
      },
      {
        route: { name: URL_PRODUCT_GROUP_ADMIN },
        label: t("manage-product-groups"),
        isActive: false
      }
    ];
  } else {
    return [
      {
        route: { name: URL_PRODUCT_GROUP_ADMIN },
        label: t("manage-product-groups"),
        isActive: false
      }
    ];
  }
});

const isLoading = () => {
  loading.value = true;
};

const loadingFinish = () => {
  loading.value = false;
};
</script>
