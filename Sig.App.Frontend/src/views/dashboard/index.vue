<i18n>
  {
    "en": {
      "title": "Dashboard"
    },
    "fr": {
      "title": "Tableau de bord"
    }
  }
  </i18n>

<template>
  <AppShell :title="t('title')">
    <ProgramDashboard v-if="isProjectManager" />
    <OrganizationDashboard v-else
  /></AppShell>
</template>

<script setup>
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { storeToRefs } from "pinia";

import ProgramDashboard from "@/components/dashboard/program-dashboard.vue";
import OrganizationDashboard from "@/components/dashboard/organization-dashboard.vue";

import { useAuthStore } from "@/lib/store/auth";
import { usePageTitle } from "@/lib/helpers/page-title";

import { USER_TYPE_PROJECTMANAGER } from "@/lib/consts/enums";

const { userType } = storeToRefs(useAuthStore());
const { t } = useI18n();

usePageTitle(t("title"));

const isProjectManager = computed(() => {
  return userType.value === USER_TYPE_PROJECTMANAGER;
});
</script>
