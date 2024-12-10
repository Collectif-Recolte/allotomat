<i18n>
  {
    "en": {
      "reactivate-user-success-notification": "The user {userName} has been successfully reactivated.",
      "reactivate-text-error": "The text must match the name of the user",
      "reactivate-text-label": "Type the name of the user to confirm",
      "title": "Reactivate - {userName}",
      "reactivate-button-label": "Reactivate"
    },
    "fr": {
      "reactivate-user-success-notification": "L'utilisateur {userName} a été réactivé avec succès.",
      "reactivate-text-error": "Le texte doit correspondre au nom de l'utilisateur",
      "reactivate-text-label": "Taper le nom de l'utilisateur pour confirmer",
      "title": "Réactiver - {userName}",
      "reactivate-button-label": "Réactiver"
    }
  }
  </i18n>

<template>
  <UiDialogDeleteModal
    :return-route="{ name: URL_ADMIN_USERS }"
    :title="t('title', { userName: getUserName() })"
    :validation-text="getUserName()"
    :delete-text-label="t('reactivate-text-label')"
    :delete-text-error="t('reactivate-text-error')"
    :delete-button-label="t('reactivate-button-label')"
    @onDelete="reactivateUser" />
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
        email
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

const { mutate: reactivateUserMutation } = useMutation(
  gql`
    mutation ReactivateUser($input: ReactivateUserInput!) {
      reactivateUser(input: $input) {
        user {
          id
          status
        }
      }
    }
  `
);

function getUserName() {
  return user.value
    ? user.value.profile.firstName !== null
      ? `${user.value.profile.firstName} ${user.value.profile.lastName}`
      : user.value.email
    : "";
}

async function reactivateUser() {
  await reactivateUserMutation({
    input: {
      userId: route.params.id
    }
  });

  addSuccess(t("reactivate-user-success-notification", { userName: getUserName() }));
  router.push({ name: URL_ADMIN_USERS });
}
</script>
