<i18n>
  {
    "en": {
      "disable-user-success-notification": "The user {userName} has been successfully disabled.",
      "disable-text-error": "The text must match the name of the user",
      "disable-text-label": "Type the name of the user to confirm",
      "title": "Disable - {userName}",
      "disable-button-label": "Disable"
    },
    "fr": {
      "disable-user-success-notification": "L'utilisateur {userName} a été désactivé avec succès.",
      "disable-text-error": "Le texte doit correspondre au nom de l'utilisateur",
      "disable-text-label": "Taper le nom de l'utilisateur pour confirmer",
      "title": "Désactiver - {userName}",
      "disable-button-label": "Désactiver"
    }
  }
  </i18n>

<template>
  <UiDialogDeleteModal
    :return-route="{ name: URL_ADMIN_USERS }"
    :title="t('title', { userName: getUserName() })"
    :validation-text="getUserName()"
    :delete-text-label="t('disable-text-label')"
    :delete-text-error="t('disable-text-error')"
    :delete-button-label="t('disable-button-label')"
    @onDelete="disableUser" />
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_ADMIN_USERS } from "@/lib/consts/urls";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { addSuccess } = useNotificationsStore();

const { result } = useQuery(
  gql`
    query User($id: ID!) {
      user(id: $id) {
        id
        profile {
          id
          firstName
          lastName
        }
      }
    }
  `,
  {
    id: route.params.id
  }
);
const user = useResult(result);

const { mutate: disableUserMutation } = useMutation(
  gql`
    mutation DisableUser($input: DisableUserInput!) {
      disableUser(input: $input) {
        user {
          id
          status
        }
      }
    }
  `
);

function getUserName() {
  return user.value ? `${user.value.profile.firstName} ${user.value.profile.lastName}` : "";
}

async function disableUser() {
  await disableUserMutation({
    input: {
      userId: route.params.id
    }
  });

  addSuccess(t("disable-user-success-notification", { userName: getUserName() }));
  router.push({ name: URL_ADMIN_USERS });
}
</script>
