<i18n>
  {
    "en": {
      "delete-user-success-notification": "The user {userName} has been successfully deleted.",
      "delete-text-error": "The text must match the name of the user",
      "delete-text-label": "Type the name of the user to confirm",
      "description": "Warning ! The deletion of <strong>{userName}</strong> cannot be undone. If you continue, the user will be permanently deleted.",
      "title": "Delete - {userName}"
    },
    "fr": {
      "delete-user-success-notification": "L'utilisateur {userName} a été supprimé avec succès.",
      "delete-text-error": "Le texte doit correspondre au nom de l'utilisateur",
      "delete-text-label": "Taper le nom de l'utilisateur pour confirmer",
      "description": "Avertissement ! La suppression de <strong>{userName}</strong> ne peut pas être annulée. Si vous continuez, l'utilisateur sera supprimé.",
      "title": "Supprimer - {userName}"
    }
  }
  </i18n>

<template>
  <UiDialogDeleteModal
    :return-route="{ name: URL_ADMIN_USERS }"
    :title="t('title', { userName: getUserName() })"
    :description="t('description', { userName: getUserName() })"
    :validation-text="getUserName()"
    :delete-text-label="t('delete-text-label')"
    :delete-text-error="t('delete-text-error')"
    @onDelete="deleteUser" />
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

const { mutate: deleteUserMutation } = useMutation(
  gql`
    mutation DeleteUser($input: DeleteUserInput!) {
      deleteUser(input: $input)
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

async function deleteUser() {
  await deleteUserMutation({
    input: {
      userId: route.params.id
    }
  });

  addSuccess(t("delete-user-success-notification", { userName: getUserName() }));
  router.push({ name: URL_ADMIN_USERS });
}
</script>
