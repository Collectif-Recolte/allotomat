<i18n>
{
	"en": {
		"add-user": "Add admin",
		"title": "Admin management",
    "user-count": "{count} user | {count} users"
	},
	"fr": {
		"add-user": "Ajouter un administrateur",
		"title": "Gestion des utilisateurs",
    "user-count": "{count} utilisateur | {count} utilisateurs"
	}
}
</i18n>

<template>
  <RouterView v-slot="{ Component }">
    <AppShell :loading="loading" :title="t('title')">
      <div v-if="users">
        <UiTableHeader
          :title="t('user-count', { count: users.totalCount })"
          :cta-label="t('add-user')"
          :cta-route="{ name: $consts.urls.URL_ADMIN_ADD_USER }" />
        <UserTable v-if="users.items.length > 0" :users="users.items" />
        <UiPagination v-if="users.totalPages > 1" v-model:page="page" :total-pages="users.totalPages"> </UiPagination>
      </div>
      <Component :is="Component" />
    </AppShell>
  </RouterView>
</template>

<script setup>
import gql from "graphql-tag";
import { ref } from "vue";
import { useI18n } from "vue-i18n";
import { useQuery, useResult } from "@vue/apollo-composable";
import { usePageTitle } from "@/lib/helpers/page-title";

import UserTable from "@/components/admin/user-table";

const { t } = useI18n();

usePageTitle(t("title"));

let page = ref(1);

const { result, loading } = useQuery(
  gql`
    query GetUsers($page: Int!) {
      users(page: $page, limit: 30) {
        items {
          id
          email
          isConfirmed
          lastConnectionTime
          profile {
            id
            firstName
            lastName
          }
          type
        }
        totalPages
        totalCount
      }
    }
  `,
  {
    page
  }
);

const users = useResult(result);
</script>
