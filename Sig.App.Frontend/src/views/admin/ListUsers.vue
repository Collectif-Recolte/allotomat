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
        <UiTableHeader :title="t('user-count', { count: users.totalCount })">
          <template #right>
            <div class="flex items-center gap-x-4">
              <PfButtonLink tag="RouterLink" :to="{ name: $consts.urls.URL_ADMIN_ADD_USER }" :label="t('add-user')" />
              <UserFilters
                v-model="searchInput"
                :selected-user-types="selectedUserTypes"
                :search-filter="searchText"
                @userTypesChecked="onUserTypesChecked"
                @userTypesUnchecked="onUserTypesUnchecked"
                @resetFilters="onResetFilters"
                @search="onSearch" />
            </div>
          </template>
        </UiTableHeader>
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
import { onBeforeRouteUpdate } from "vue-router";

import UserFilters from "@/components/admin/user-filters";
import UserTable from "@/components/admin/user-table";
import { URL_ADMIN_USERS } from "@/lib/consts/urls";

const { t } = useI18n();

usePageTitle(t("title"));

const page = ref(1);
const searchInput = ref("");
const searchText = ref("");
const selectedUserTypes = ref([]);

const {
  result,
  loading,
  refetch: refetchUsers
} = useQuery(
  gql`
    query GetUsers($page: Int!, $searchText: String, $userTypes: [UserType!]) {
      users(page: $page, limit: 30, searchText: $searchText, userTypes: $userTypes) {
        items {
          id
          email
          isConfirmed
          confirmationLink
          resetPasswordLink
          lastConnectionTime
          status
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
  userVariables
);

function userVariables() {
  return {
    page: page.value,
    searchText: searchText.value,
    userTypes: selectedUserTypes.value.length > 0 ? selectedUserTypes.value : null
  };
}

function onSearch() {
  page.value = 1;
  searchText.value = searchInput.value;
}

function onResetFilters() {
  selectedUserTypes.value = [];
  onResetSearch();
}

function onResetSearch() {
  page.value = 1;
  searchText.value = "";
  searchInput.value = "";
}

function onUserTypesChecked(value) {
  selectedUserTypes.value.push(value);
}

function onUserTypesUnchecked(value) {
  selectedUserTypes.value = selectedUserTypes.value.filter((x) => x !== value);
}

const users = useResult(result);

onBeforeRouteUpdate((to) => {
  if (to.name === URL_ADMIN_USERS) {
    refetchUsers();
  }
});
</script>
