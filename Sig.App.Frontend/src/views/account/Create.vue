<i18n>
{
	"en": {
		"confirm-message": "A message to complete the account creation was sent to {email}.",
		"submit": "Create my account",
		"title": "Open account"
	},
	"fr": {
		"confirm-message": "Un message pour compléter la création de compte a été envoyé à {email}.",
		"submit": "Créer mon compte",
		"title": "Ouvrir un compte"
	}
}
</i18n>

<template>
  <PublicShell :title="t('title')">
    <AccountForm :submit-label="t('submit')" @submit="onSubmit" />
  </PublicShell>
</template>

<script setup>
import { useI18n } from "vue-i18n";
import gql from "graphql-tag";
import { useMutation } from "@vue/apollo-composable";
import { useRouter } from "vue-router";

import { URL_ROOT } from "@/lib/consts/urls";
import { useNotificationsStore } from "@/lib/store/notifications";
import { usePageTitle } from "@/lib/helpers/page-title";

import AccountForm from "@/views/account/_Form";

const router = useRouter();
const { t } = useI18n();
const { addInfo } = useNotificationsStore();

usePageTitle(t("title"));

const { mutate: createUserAccount } = useMutation(
  gql`
    mutation CreateUserAccount($input: CreateUserAccountInput!) {
      createUserAccount(input: $input) {
        user {
          id
        }
      }
    }
  `
);

async function onSubmit({ email, firstName, lastName, password }) {
  await createUserAccount({ input: { email, firstName, lastName, password } });
  addInfo(t("confirm-message", { email }));
  router.push({ name: URL_ROOT });
}
</script>
