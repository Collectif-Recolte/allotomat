<i18n>
  {
    "en": {
      "add-user": "Add a group manager",
      "cant-remove-self-info": "It is impossible to remove yourself from the manager list of an group",
      "title": "User management",
      "manager-count": "{count} manager | {count} managers"
    },
    "fr": {
      "add-user": "Ajouter un gestionnaire de groupe",
      "cant-remove-self-info": "Il est impossible de se retirer soi-même de la liste de gestionnaire d'un groupe",
      "title": "Gestion des utilisateurs",
      "manager-count": "{count} gestionnaire | {count} gestionnaires"
    }
  }
</i18n>

<template>
  <RouterView v-slot="{ Component }">
    <AppShell :loading="loading" :title="t('title')">
      <div v-if="organizations">
        <UiTableHeader
          :title="t('manager-count', { count: organizations[0].managers.length })"
          :cta-label="t('add-user')"
          :cta-route="{ name: $consts.urls.URL_ORGANIZATION_MANAGER_ADD, query: { organizationId: organizations[0].id } }" />
        <ManagersTable :managers="organizations[0].managers" @removeManager="removeManager" />
      </div>
      <Component :is="Component" />
    </AppShell>
  </RouterView>
</template>

<script setup>
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { useRouter, onBeforeRouteUpdate } from "vue-router";
import { useQuery, useResult } from "@vue/apollo-composable";

import { URL_ORGANIZATION_MANAGER_ADMIN, URL_ORGANIZATION_MANAGER_REMOVE } from "@/lib/consts/urls";
import { usePageTitle } from "@/lib/helpers/page-title";
import { useNotificationsStore } from "@/lib/store/notifications";

import ManagersTable from "@/components/managers/managers-table";

const { t } = useI18n();
const router = useRouter();
const { addInfo } = useNotificationsStore();

usePageTitle(t("title"));

const { result, refetch } = useQuery(
  gql`
    query Organizations {
      organizations {
        id
        name
        managers {
          id
          email
          isConfirmed
          confirmationLink
          resetPasswordLink
          profile {
            id
            firstName
            lastName
          }
          type
        }
      }
    }
  `
);
const organizations = useResult(result);

const { result: resultMe } = useQuery(
  gql`
    query GetMe {
      me {
        id
        email
      }
    }
  `
);
const me = useResult(resultMe);

function removeManager(manager) {
  if (me.value.email === manager.email) {
    addInfo(t("cant-remove-self-info"));
  } else {
    router.push({
      name: URL_ORGANIZATION_MANAGER_REMOVE,
      params: { managerId: manager.id },
      query: { organizationId: organizations.value[0].id }
    });
  }
}

onBeforeRouteUpdate((to) => {
  if (to.name === URL_ORGANIZATION_MANAGER_ADMIN) {
    refetch();
  }
});
</script>
