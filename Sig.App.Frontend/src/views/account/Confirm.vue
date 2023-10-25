<i18n>
{
	"en": {
		"confirming": "Confirming your account...",
		"email-confirmed": "You can now log in with your email.",
		"error": "There was a problem confirming your account.",
		"resend": "Resend link",
		"resend-complete": "A message to complete the account creation was sent to {email}.",
		"title": "Email confirmation"
	},
	"fr": {
		"confirming": "Confirmation de votre compte...",
		"email-confirmed": "Vous pouvez maintenant vous connecter avec votre courriel.",
		"error": "Il y a eu un problème lors de la confirmation de votre compte.",
		"resend": "M'envoyer le lien de nouveau",
		"resend-complete": "Un message pour compléter la création de compte a été envoyé à {email}.",
		"title": "Confirmation du courriel"
	}
}
</i18n>
<template>
  <PublicShell :title="t('title')">
    <template v-if="showError">
      <PfNote bg-color-class="bg-red-50">
        <template #content>
          <p class="text-sm text-red-900">{{ t("error") }}</p>
        </template>
      </PfNote>
      <PfButtonAction class="mt-8" :label="t('resend')" :disabled="resendEmailDisabled" @click="resendEmail" />
    </template>

    <div v-else class="flex items-center">
      <PfSpinner class="mr-3" is-small aria-hidden="true" />
      <p class="mb-0">{{ t("confirming") }}</p>
    </div>
  </PublicShell>
</template>

<script setup>
import gql from "graphql-tag";
import { onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { useQuery, useMutation } from "@vue/apollo-composable";

import { TOKEN_STATUS_INVALID, TOKEN_STATUS_USER_CONFIRMED, TOKEN_TYPE_CONFIRM_EMAIL } from "@/lib/consts/tokens";
import { URL_ACCOUNT_LOGIN } from "@/lib/consts/urls";
import { isGraphQLError } from "@/lib/helpers/error-handler";
import LoggerService from "@/lib/services/logger";
import { useNotificationsStore } from "@/lib/store/notifications";
import { usePageTitle } from "@/lib/helpers/page-title";

const { addSuccess, addInfo } = useNotificationsStore();

const { t } = useI18n();

const showError = ref(false);
const resendEmailDisabled = ref(false);

const route = useRoute();
const router = useRouter();

const { email, token } = route.query;

const { mutate: resendConfirmationEmailMutation } = useMutation(
  gql`
    mutation ResendConfirmationEmail($input: ResendConfirmationEmailInput!) {
      resendConfirmationEmail(input: $input)
    }
  `
);

const { mutate: confirmEmail } = useMutation(
  gql`
    mutation ConfirmEmail($input: ConfirmEmailInput!) {
      confirmEmail(input: $input)
    }
  `
);

usePageTitle(t("title"));

onMounted(checkToken);

async function resendEmail() {
  await resendConfirmationEmailMutation({ input: { email } });
  addSuccess(t("resend-complete", { email: email }));
  resendEmailDisabled.value = true;
}

async function checkToken() {
  const { onResult } = useQuery(
    gql`
      query VerifyToken($email: String, $token: String, $type: TokenType!) {
        verifyToken(email: $email, token: $token, type: $type) {
          status
        }
      }
    `,
    {
      email,
      token,
      type: TOKEN_TYPE_CONFIRM_EMAIL
    }
  );

  onResult(async (queryResult) => {
    if (queryResult.data.verifyToken.status === TOKEN_STATUS_INVALID) {
      showError.value = true;
    } else if (queryResult.data.verifyToken.status === TOKEN_STATUS_USER_CONFIRMED) {
      goToLoginPage();
    } else {
      try {
        await confirmEmail({ input: { token, email } });

        addInfo(t("email-confirmed"));
        goToLoginPage();
      } catch (err) {
        if (isGraphQLError(err, "NO_NEED_TO_CONFIRM")) {
          goToLoginPage();
        } else {
          showError.value = true;
          LoggerService.logError(`Error confirming account. ${err}`);
        }
      }
    }
  });

  function goToLoginPage() {
    router.push({ name: URL_ACCOUNT_LOGIN, query: { email } });
  }
}
</script>
