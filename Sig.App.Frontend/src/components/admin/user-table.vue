<i18n>
  {
    "en": {
      "admin-pca": "Administrator",
      "edit-profile": "Edit user profile",
      "last-login-date": "Last login",
      "merchant": "Merchant",
      "options": "Options",
      "organization-manager": "Organization manager",
      "project-manager": "Program manager",
      "resend-complete": "A message to complete the account creation was sent to {email}.",
      "resend-confirmation-email": "Resend confirmation email",
      "role": "Rôle",
      "status": "Status",
      "user-confirmed": "User confirmed",
      "user-not-confirmed": "User not confirmed",
      "username": "Username"
    },
    "fr": {
      "admin-pca": "Administrateur",
      "edit-profile": "Modifier le profil de l'utilisateur",
      "last-login-date": "Dernière connexion",
      "merchant": "Marchand",
      "options": "Actions possibles",
      "organization-manager": "Gestionnaire d'organisme",
      "project-manager": "Gestionnaire de programme",
      "resend-complete": "Un message pour compléter la création de compte a été envoyé à {email}.",
      "resend-confirmation-email": "Renvoyer le courriel de confirmation",
      "role": "Rôle",
      "status": "Statut",
      "user-confirmed": "Utilisateur confirmé",
      "user-not-confirmed": "Utilisateur non confirmé",
      "username": "Nom d'utilisateur"
    }
  }
  </i18n>

<template>
  <UiTable :items="props.users" :cols="cols">
    <template #default="slotProps">
      <td>
        <p v-if="getUserName(slotProps.item)" class="mb-0">{{ getUserName(slotProps.item) }}</p>
        <!-- eslint-disable-next-line vue/no-v-html -->
        <p v-if="slotProps.item.email" class="mb-0 text-p4" v-html="getUserEmail(slotProps.item)"></p>
      </td>
      <td>
        {{ getUserStatus(slotProps.item) }}
      </td>
      <td>
        {{ getUserLastConnectionTime(slotProps.item) }}
      </td>
      <td>
        {{ getUserRole(slotProps.item) }}
      </td>
      <td>
        <UiButtonGroup :items="getBtnGroup(slotProps.item)" tooltip-position="left" />
      </td>
    </template>
  </UiTable>
</template>

<script setup>
import { formatDate, regularWithTimeFormat } from "@/lib/helpers/date";
import gql from "graphql-tag";
import { defineProps, computed } from "vue";
import { useI18n } from "vue-i18n";
import { useMutation } from "@vue/apollo-composable";

import { useNotificationsStore } from "@/lib/store/notifications";

import { USER_TYPE_PCAADMIN, USER_TYPE_PROJECTMANAGER, USER_TYPE_ORGANIZATIONMANAGER } from "@/lib/consts/enums";
import { URL_ADMIN_USER_PROFILE } from "@/lib/consts/urls";
import PENCIL_ICON from "@/lib/icons/pencil.json";
import MAIL_ICON from "@/lib/icons/mail.json";

const { addSuccess } = useNotificationsStore();
const { t } = useI18n();

const props = defineProps({
  users: { type: Array, required: true }
});

const cols = computed(() => [
  { label: t("username") },
  { label: t("status") },
  { label: t("last-login-date") },
  { label: t("role") },
  {
    label: t("options"),
    hasHiddenLabel: true
  }
]);

function getBtnGroup(user) {
  return [
    {
      icon: MAIL_ICON,
      label: t("resend-confirmation-email", { name: getUserName(user) }),
      onClick: () => resendConfirmationEmail(user),
      if: !user.isConfirmed
    },
    {
      icon: PENCIL_ICON,
      label: t("edit-profile", { name: getUserName(user) }),
      route: { name: URL_ADMIN_USER_PROFILE, params: { id: user.id } }
    }
  ];
}

const { mutate: resendConfirmationEmailMutation } = useMutation(
  gql`
    mutation ResendConfirmationEmail($input: ResendConfirmationEmailInput!) {
      resendConfirmationEmail(input: $input)
    }
  `
);

function getUserName(item) {
  return item.profile.firstName !== null && item.profile.lastName !== null
    ? `${item.profile.firstName} ${item.profile.lastName}`
    : "";
}

function getUserEmail(item) {
  return item.email !== null ? `<a href="mailto:${item.email}">${item.email}</a>` : "";
}

function getUserStatus(item) {
  return item.isConfirmed ? t("user-confirmed") : t("user-not-confirmed");
}

async function resendConfirmationEmail(item) {
  await resendConfirmationEmailMutation({ input: { email: item.email } });
  addSuccess(t("resend-complete", { email: item.email }));
}

function getUserLastConnectionTime(item) {
  if (item.lastConnectionTime === null) {
    return null;
  }
  return formatDate(new Date(item.lastConnectionTime), regularWithTimeFormat);
}

function getUserRole(item) {
  return item?.type === USER_TYPE_PCAADMIN
    ? t("admin-pca")
    : item?.type === USER_TYPE_PROJECTMANAGER
    ? t("project-manager")
    : item?.type === USER_TYPE_ORGANIZATIONMANAGER
    ? t("organization-manager")
    : t("merchant");
}
</script>
