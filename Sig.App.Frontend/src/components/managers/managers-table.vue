<i18n>
  {
      "en": {
          "options": "Options",
          "project-manager-delete": "Remove",
          "username": "Manager name",
          "resend-confirmation-email": "Resend confirmation email",
          "copy-confirmation-email": "Copy confirmation link",
          "resend-complete": "A message to complete the account creation was sent to {email}.",
          "confirmation-link-copied": "Confirmation link copied to clipboard.",
          "copy-reset-password-email": "Copy reset password link",
          "confirmation-reset-password-link-copied": "Reset password link copied to clipboard.",
      },
      "fr": {
          "options": "Options",
          "project-manager-delete": "Retirer",
          "username": "Nom du gestionnaire",
          "resend-confirmation-email": "Renvoyer le courriel de confirmation",
          "copy-confirmation-email": "Copier le lien de confirmation",
          "resend-complete": "Un message pour compléter la création de compte a été envoyé à {email}.",
          "confirmation-link-copied": "Lien de confirmation copié dans le presse-papiers.",
          "copy-reset-password-email": "Copier le lien de réinitialisation du mot de passe",
          "confirmation-reset-password-link-copied": "Lien de réinitialisation du mot de passe copié dans le presse-papiers."
      }
  }
</i18n>

<template>
  <UiTable v-if="props.managers" :items="props.managers" :cols="cols">
    <template #default="slotProps">
      <td>
        <p v-if="getUserName(slotProps.item)" class="mb-0">{{ getUserName(slotProps.item) }}</p>
        <!-- eslint-disable-next-line vue/no-v-html -->
        <p v-if="slotProps.item.email" class="mb-0 text-p4" v-html="getUserEmail(slotProps.item)"></p>
      </td>
      <td>
        <UiButtonGroup :items="getBtnGroup(slotProps.item)" tooltip-position="left" />
      </td>
    </template>
  </UiTable>
</template>

<script setup>
import { defineProps, computed, defineEmits } from "vue";
import { useI18n } from "vue-i18n";
import gql from "graphql-tag";
import { useMutation } from "@vue/apollo-composable";

import { useNotificationsStore } from "@/lib/store/notifications";
import { copyTextToClipboard } from "@/lib/helpers/clipboard";

import ICON_TRASH from "@/lib/icons/trash.json";
import MAIL_ICON from "@/lib/icons/mail.json";
import COPY_ICON from "@/lib/icons/copy.json";

const { addSuccess } = useNotificationsStore();
const { t } = useI18n();

const emit = defineEmits(["removeManager"]);

const props = defineProps({
  managers: { type: Object, default: null }
});

const cols = computed(() => [
  {
    label: t("username")
  },
  {
    label: t("options"),
    hasHiddenLabel: true
  }
]);

const getBtnGroup = (manager) => [
  {
    icon: MAIL_ICON,
    label: t("resend-confirmation-email"),
    onClick: () => resendConfirmationEmail(manager),
    if: !manager.isConfirmed
  },
  {
    icon: COPY_ICON,
    label: t("copy-confirmation-email"),
    onClick: () => copyConfirmationLink(manager),
    if: !manager.isConfirmed
  },
  {
    icon: COPY_ICON,
    label: t("copy-reset-password-email"),
    onClick: () => copyResetPasswordLink(manager),
    if: manager.isConfirmed
  },
  {
    icon: ICON_TRASH,
    label: t("project-manager-delete"),
    onClick: () => removeManager(manager)
  }
];

const { mutate: resendConfirmationEmailMutation } = useMutation(
  gql`
    mutation ResendConfirmationEmail($input: ResendConfirmationEmailInput!) {
      resendConfirmationEmail(input: $input)
    }
  `
);

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

function getUserName(item) {
  if (item.profile !== null && item.profile.firstName !== null && item.profile.lastName !== null) {
    return `${item.profile.firstName} ${item.profile.lastName}`;
  }
  return "";
}

function getUserEmail(item) {
  return item.email !== null ? `<a href="mailto:${item.email}">${item.email}</a>` : "";
}

function removeManager(manager) {
  emit("removeManager", manager);
}
</script>
