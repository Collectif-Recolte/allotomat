<i18n>
{
	"en": {
		"confirm-email-text": "Confirmation in progress ...",
		"info-confirm-change-email": "Your email change has been completed successfully.",
		"title": "Email change confirmation"
	},
	"fr": {
		"confirm-email-text": "Confirmation en cours ...",
		"info-confirm-change-email": "Votre changement de courriel a été complété avec succès",
		"title": "Changement de changement de courriel"
	}
}
</i18n>

<template>
  <PublicShell :title="t('title')">
    <div class="flex items-center">
      <PfSpinner class="mr-3" is-small aria-hidden="true" />
      <p class="mb-0">{{ t("confirm-email-text") }}</p>
    </div>
  </PublicShell>
</template>

<script setup>
import gql from "graphql-tag";
import { onMounted } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { useMutation } from "@vue/apollo-composable";

import { URL_ROOT } from "@/lib/consts/urls";
import { useNotificationsStore } from "@/lib/store/notifications";
import { usePageTitle } from "@/lib/helpers/page-title";

const { t } = useI18n();
const { addSuccess } = useNotificationsStore();
const route = useRoute();
const router = useRouter();

const { mutate: confirmChangeEmailMutation } = useMutation(
  gql`
    mutation ConfirmChangeEmail($input: ConfirmChangeEmailInput!) {
      confirmChangeEmail(input: $input) {
        user {
          id
          email
        }
      }
    }
  `
);

const data = {
  token: route.query.token,
  email: route.query.email
};

usePageTitle(t("title"));

onMounted(confirmChangeEmail);

async function confirmChangeEmail() {
  await confirmChangeEmailMutation({ input: { newEmail: data.email, token: data.token } });
  addSuccess(t("info-confirm-change-email"));
  router.push({ name: URL_ROOT });
}
</script>
