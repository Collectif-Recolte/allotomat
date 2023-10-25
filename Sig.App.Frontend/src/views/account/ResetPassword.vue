<i18n>
{
	"en": {
		"back": "Cancel",
		"bad-token-message": "This password reset link is invalid or expired.",
		"confirm-message": "Un message pour réinitialiser votre mot de passe a été envoyé à {email}.",
		"loading": "Update in progress...",
		"password": "Password",
		"password-confirmation": "Re-enter password",
		"password-rules": "The password must contain a minimum of 10 characters, 1 capital letter, a number and a special character (for example: %, {'@'}, #, $ and &).",
		"resend-reset-password": "Resend link",
		"submit": "Reset password",
		"title": "Password reset"
	},
	"fr": {
		"back": "Annuler",
		"bad-token-message": "Ce lien de réinitialisation de mot de passe est invalide ou expiré.",
		"confirm-message": "Un message pour réinitialiser votre mot de passe a été envoyé à {email}.",
		"loading": "Mise à jour en cours...",
		"password": "Mot de passe",
		"password-confirmation": "Confirmation du mot de passe",
		"password-rules": "Le mot de passe doit contenir un minimum de 10 caractères, une majuscule, un chiffre et un caractère spécial (par exemple: %, {'@'}, #, $ et &).",
		"resend-reset-password": "M'envoyer le lien de nouveau",
		"submit": "Réinitialiser le mot de passe",
		"title": "Réinitialisation du mot de passe"
	}
}
</i18n>

<template>
  <PublicShell :title="t('title')">
    <div v-if="badToken">
      <PfNote bg-color-class="bg-red-50">
        <template #content>
          <p class="text-sm text-red-900">{{ t("bad-token-message", { email }) }}</p>
        </template>
      </PfNote>
      <PfButtonAction class="mt-8" :label="t('resend-reset-password')" @click="resendEmail" />
    </div>
    <div v-else-if="emailSentTo">
      <PfNote bg-color-class="bg-primary-50">
        <template #content>
          <p class="text-sm text-primary-900">{{ t("confirm-message", { email: emailSentTo }) }}</p>
        </template>
      </PfNote>
      <RouterLink
        class="pf-button pf-button--link relative mt-8 h-extend-cursor-area"
        :to="{ name: $consts.urls.URL_ACCOUNT_LOGIN }"
        >{{ t("back") }}</RouterLink
      >
    </div>

    <Form v-else v-slot="{ isSubmitting, errors: formErrors }" :validation-schema="validationSchema" @submit="onSubmit">
      <PfForm
        has-footer
        :disable-submit="Object.keys(formErrors).length > 0"
        :submit-label="t('submit')"
        :cancel-label="t('back')"
        :processing="isSubmitting"
        :loading-label="t('loading')"
        :cancel-route="{ name: $consts.urls.URL_ACCOUNT_LOGIN }">
        <PfFormSection>
          <Field v-slot="{ field, errors }" name="password">
            <PfFormInputText
              id="password"
              v-bind="field"
              :label="t('password')"
              :errors="errors"
              input-type="password"
              :description="t('password-rules')" />
          </Field>

          <Field v-slot="{ field, errors }" name="passwordConfirmation">
            <PfFormInputText
              id="passwordConfirmation"
              v-bind="field"
              :label="t('password-confirmation')"
              :errors="errors"
              input-type="password" />
          </Field>
        </PfFormSection>
      </PfForm>
    </Form>
  </PublicShell>
</template>

<script setup>
import gql from "graphql-tag";
import { onMounted, ref, computed } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute } from "vue-router";
import { useQuery, useMutation } from "@vue/apollo-composable";
import { object, string, ref as yupRef } from "yup";

import { TOKEN_TYPE_RESET_PASSWORD, TOKEN_STATUS_INVALID } from "@/lib/consts/tokens";
import AuthenticationService from "@/lib/services/authentication";
import { usePageTitle } from "@/lib/helpers/page-title";

const { t } = useI18n();

usePageTitle(t("title"));

const badToken = ref(false);

const route = useRoute();
const { email, token } = route.query;

onMounted(checkToken);

const validationSchema = computed(() =>
  object({
    password: string().label(t("password")).required().password(),
    passwordConfirmation: string().label(t("password-confirmation")).required().samePassword(yupRef("password"))
  })
);

const { mutate: sendPasswordReset } = useMutation(
  gql`
    mutation SendPasswordReset($input: SendPasswordResetInput!) {
      sendPasswordReset(input: $input)
    }
  `
);

const { mutate: resetPassword } = useMutation(
  gql`
    mutation ResetPassword($input: ResetPasswordInput!) {
      resetPassword(input: $input) {
        user {
          id
        }
      }
    }
  `
);

async function onSubmit({ password }) {
  await resetPassword({ input: { emailAddress: email, newPassword: password, token } });
  await AuthenticationService.login(email, password);
}

const emailSentTo = ref();
async function resendEmail() {
  await sendPasswordReset({ input: { email } });
  emailSentTo.value = email;
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
      type: TOKEN_TYPE_RESET_PASSWORD
    },
    {
      fetchPolicy: "no-cache"
    }
  );

  onResult((queryResult) => {
    if (queryResult.data.verifyToken.status === TOKEN_STATUS_INVALID) {
      badToken.value = true;
    }
  });
}
</script>
