<i18n>
{
	"en": {
		"confirm-message": "A message to complete the account creation was sent to {email}.",
		"submit": "Add",
		"title": "Add an admin"
	},
	"fr": {
		"confirm-message": "Un message pour compléter la création de compte a été envoyé à {email}.",
		"submit": "Ajouter",
		"title": "Ajout d'un administrateur"
	}
}
</i18n>

<template>
  <UiDialogModal v-slot="{ closeModal }" :title="t('title')" :has-footer="false" :return-route="{ name: URL_ADMIN_USERS }">
    <AccountForm :submit-label="t('submit')" @submit="onSubmit" @closeModal="closeModal" />
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useMutation } from "@vue/apollo-composable";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_ADMIN_USERS } from "@/lib/consts/urls";

import AccountForm from "@/views/admin/_Form";

const { t } = useI18n();
const router = useRouter();
const { addSuccess } = useNotificationsStore();

const { mutate: createAdminAccount } = useMutation(
  gql`
    mutation CreateAdminAccount($input: CreateAdminAccountInput!) {
      createAdminAccount(input: $input) {
        user {
          id
        }
      }
    }
  `
);

async function onSubmit({ firstName, lastName, email }) {
  await createAdminAccount({
    input: {
      firstName,
      lastName,
      email
    }
  });
  addSuccess(t("confirm-message", { email }));
  router.push({ name: URL_ADMIN_USERS });
}
</script>
