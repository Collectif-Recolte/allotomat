<i18n>
{
	"en": {
		"submit": "Create my account",
    "error": "There was a problem confirming your account.",
    "resend": "Resend link",
    "resend-complete": "A message to complete the account creation was sent to {email}.",
		"title": "Hello"
	},
	"fr": {
		"submit": "Créer mon compte",
    "error": "Il y a eu un problème lors de la confirmation de votre compte.",
    "resend": "M'envoyer le lien de nouveau",
    "resend-complete": "Un message pour compléter la création de compte a été envoyé à {email}.",
		"title": "Bonjour"
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
      <PfButtonAction class="pf-button mt-8" :label="t('resend')" :disabled="resendEmailDisabled" @click="resendEmail" />
    </template>
    <AccountForm v-else :submit-label="t('submit')" :initial-values="initialFormValues" is-in-edition @submit="onSubmit" />
  </PublicShell>
</template>

<script setup>
import gql from "graphql-tag";
import { ref, computed, defineProps } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";

import { TOKEN_STATUS_INVALID, TOKEN_STATUS_USER_CONFIRMED } from "@/lib/consts/tokens";
import { URL_ACCOUNT_LOGIN } from "@/lib/consts/urls";
import AuthenticationService from "@/lib/services/authentication";
import { usePageTitle } from "@/lib/helpers/page-title";
import { useNotificationsStore } from "@/lib/store/notifications";

import AccountForm from "@/views/account/_Form";

const route = useRoute();
const router = useRouter();
const { t } = useI18n();
const { addSuccess } = useNotificationsStore();

const showError = ref(false);
const resendEmailDisabled = ref(false);

const props = defineProps({
  tokenType: {
    type: String,
    required: true
  }
});

usePageTitle(t("title"));

const { email, token } = route.query;

const { result, onResult } = useQuery(
  gql`
    query VerifyToken($email: String, $token: String, $type: TokenType!) {
      verifyToken(email: $email, token: $token, type: $type) {
        status
        user {
          id
          email
          profile {
            id
            firstName
            lastName
          }
        }
      }
    }
  `,
  {
    email,
    token,
    type: props.tokenType
  }
);

const { mutate: completeUserRegistration } = useMutation(
  gql`
    mutation CompleteUserRegistration($input: CompleteUserRegistrationInput!) {
      completeUserRegistration(input: $input) {
        user {
          id
          profile {
            id
            firstName
            lastName
          }
        }
      }
    }
  `
);

const { mutate: resendConfirmationEmailMutation } = useMutation(
  gql`
    mutation ResendConfirmationEmail($input: ResendConfirmationEmailInput!) {
      resendConfirmationEmail(input: $input)
    }
  `
);

const verifyToken = useResult(result);

onResult(async (queryResult) => {
  if (queryResult.loading) return;
  if (queryResult.data.verifyToken.status === TOKEN_STATUS_INVALID) {
    showError.value = true;
  } else if (queryResult.data.verifyToken.status === TOKEN_STATUS_USER_CONFIRMED) {
    goToLoginPage();
  }
});

function goToLoginPage() {
  router.push({ name: URL_ACCOUNT_LOGIN, query: { email } });
}

const initialFormValues = computed(() => {
  if (!verifyToken?.value?.user) return null;
  return {
    email: verifyToken.value.user.email,
    firstName: verifyToken.value.user.profile?.firstName || "",
    lastName: verifyToken.value.user.profile?.lastName || ""
  };
});

async function onSubmit({ email, firstName, lastName, password }) {
  let input = {
    userId: verifyToken.value.user.id,
    emailAddress: email,
    password,
    firstName,
    lastName,
    inviteToken: token,
    tokenType: props.tokenType
  };

  await completeUserRegistration({ input });
  await AuthenticationService.login(input.emailAddress, input.password);
}

async function resendEmail() {
  await resendConfirmationEmailMutation({ input: { email } });
  addSuccess(t("resend-complete", { email: email }));
  resendEmailDisabled.value = true;
}
</script>
