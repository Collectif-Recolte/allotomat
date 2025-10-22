<i18n>
  {
    "en": {
      "admin-pca": "Administrator",
      "edit-profile": "Edit user profile",
      "last-login-date": "Last login",
      "merchant": "Merchant",
      "merchant-group-manager": "Market group manager",
      "options": "Options",
      "organization-manager": "Group manager",
      "project-manager": "Program manager",
      "resend-complete": "A message to complete the account creation was sent to {email}.",
      "resend-confirmation-email": "Resend confirmation email",
      "role": "Role",
      "status": "Status",
      "user-confirmed": "User confirmed",
      "user-not-confirmed": "User not confirmed",
      "username": "Username",
      "copy-confirmation-email": "Copy confirmation link",
      "confirmation-link-copied": "Confirmation link copied to clipboard.",
      "copy-reset-password-email": "Copy reset password link",
      "confirmation-reset-password-link-copied": "Reset password link copied to clipboard.",
      "delete-user": "Delete user",
      "reactivate-user": "Reactivate user",
      "disable-user": "Disable user",
      "associated-program-market-group-merchant": "Program, market group, and/or merchant associated"
    },
    "fr": {
      "admin-pca": "Administrateur",
      "edit-profile": "Modifier le profil de l'utilisateur",
      "last-login-date": "Dernière connexion",
      "merchant": "Marchand",
      "merchant-group-manager": "Gestionnaire de groupe de commerce",
      "options": "Actions possibles",
      "organization-manager": "Gestionnaire de groupe",
      "project-manager": "Gestionnaire de programme",
      "resend-complete": "Un message pour compléter la création de compte a été envoyé à {email}.",
      "resend-confirmation-email": "Renvoyer le courriel de confirmation",
      "role": "Rôle",
      "status": "Statut",
      "user-confirmed": "Utilisateur confirmé",
      "user-not-confirmed": "Utilisateur non confirmé",
      "username": "Nom d'utilisateur",
      "copy-confirmation-email": "Copier le lien de confirmation",
      "confirmation-link-copied": "Lien de confirmation copié dans le presse-papiers.",
      "copy-reset-password-email": "Copier le lien de réinitialisation du mot de passe",
      "confirmation-reset-password-link-copied": "Lien de réinitialisation du mot de passe copié dans le presse-papiers.",
      "delete-user": "Supprimer l'utilisateur",
      "reactivate-user": "Réactiver l'utilisateur",
      "disable-user": "Désactiver l'utilisateur",
      "associated-program-market-group-merchant": "Programme, le groupe, et/ou le commerce associé"
    }
  }
  </i18n>

<template>
  <UiTable v-if="me" :items="props.users" :cols="cols">
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
        <PfTag
          v-for="item in getUserAssociatedProgramsMarketGroupsMerchants(slotProps.item)"
          :key="item.id"
          class="max-w-full"
          :label="item"
          is-dark-theme
          bg-color-class="bg-primary-700" />
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
import { useQuery, useResult } from "@vue/apollo-composable";

import { useNotificationsStore } from "@/lib/store/notifications";

import { copyTextToClipboard } from "@/lib/helpers/clipboard";

import {
  USER_TYPE_PCAADMIN,
  USER_TYPE_PROJECTMANAGER,
  USER_TYPE_ORGANIZATIONMANAGER,
  USER_TYPE_MARKETGROUPMANAGER
} from "@/lib/consts/enums";
import {
  URL_ADMIN_USER_PROFILE,
  URL_ADMIN_DELETE_USER,
  URL_ADMIN_DISABLE_USER,
  URL_ADMIN_REACTIVATE_USER
} from "@/lib/consts/urls";

import PENCIL_ICON from "@/lib/icons/pencil.json";
import MAIL_ICON from "@/lib/icons/mail.json";
import COPY_ICON from "@/lib/icons/copy.json";
import ICON_TRASH from "@/lib/icons/trash.json";
import ICON_FOLDER from "@/lib/icons/folder.json";

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
  { label: t("associated-program-market-group-merchant") },
  {
    label: t("options"),
    hasHiddenLabel: true
  }
]);

function getBtnGroup(user) {
  return [
    {
      icon: MAIL_ICON,
      label: t("resend-confirmation-email"),
      onClick: () => resendConfirmationEmail(user),
      if: !user.isConfirmed
    },
    {
      icon: COPY_ICON,
      label: t("copy-confirmation-email"),
      onClick: () => copyConfirmationLink(user),
      if: !user.isConfirmed
    },
    {
      icon: COPY_ICON,
      label: t("copy-reset-password-email"),
      onClick: () => copyResetPasswordLink(user),
      if: user.isConfirmed
    },
    {
      icon: PENCIL_ICON,
      label: t("edit-profile"),
      route: { name: URL_ADMIN_USER_PROFILE, params: { id: user.id } }
    },
    {
      if: user.status === "ACTIVED" && me.value.email !== user.email,
      icon: ICON_FOLDER,
      label: t("disable-user"),
      route: { name: URL_ADMIN_DISABLE_USER, params: { id: user.id } }
    },
    {
      if: user.status === "DISABLED" && me.value.email !== user.email,
      icon: ICON_FOLDER,
      label: t("reactivate-user"),
      route: { name: URL_ADMIN_REACTIVATE_USER, params: { id: user.id } }
    },
    {
      if: user.status === "DISABLED" && me.value.email !== user.email,
      icon: ICON_TRASH,
      label: t("delete-user"),
      route: { name: URL_ADMIN_DELETE_USER, params: { id: user.id } }
    }
  ];
}

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

async function copyConfirmationLink(item) {
  copyTextToClipboard(item.confirmationLink);
  addSuccess(t("confirmation-link-copied"));
}

async function copyResetPasswordLink(item) {
  copyTextToClipboard(item.resetPasswordLink);
  addSuccess(t("confirmation-reset-password-link-copied"));
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
    : item?.type === USER_TYPE_MARKETGROUPMANAGER
    ? t("merchant-group-manager")
    : t("merchant");
}

function getUserAssociatedProgramsMarketGroupsMerchants(item) {
  return item?.type === USER_TYPE_PCAADMIN
    ? ""
    : item?.type === USER_TYPE_PROJECTMANAGER
    ? item.projects.map((project) => project.name)
    : item?.type === USER_TYPE_ORGANIZATIONMANAGER
    ? item.organizations.map((organization) => organization.name)
    : item?.type === USER_TYPE_MARKETGROUPMANAGER
    ? item.marketGroups.map((marketGroup) => marketGroup.name)
    : item.markets.map((market) => market.name);
}
</script>
