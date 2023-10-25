<i18n>
{
	"en": {
		"change-email-standby-notification": "A verification email has been sent to {email}. Go to your inbox to finalize the change.",
		"change-password-error-notification": "The password is incorrect. Please try again.",
		"change-password-success-notification": "Your password has been changed.",
		"email": "Email associated with the account",
		"email-desc": "Example: you{'@'}exemple.com",
		"loading": "Update in progress...",
		"old-password": "Old password",
		"password": "New password",
		"password-confirmation": "Re-enter password",
		"password-rules": "The password must contain a minimum of 10 characters, 1 capital letter, a number and a special character (for example: %, {'@'}, #, $ and &).",
		"submit": "Update",
		"title": "Account settings"
	},
	"fr": {
		"change-email-standby-notification": "Un courriel de vérification a été envoyé à {email}. Rendez-vous dans votre boîte courriel pour finaliser le changement.",
		"change-password-error-notification": "Le mot de passe est incorrect. Veuillez recommencer.",
		"change-password-success-notification": "Votre mot de passe a été changé.",
		"email": "Courriel lié au compte",
		"email-desc": "Exemple: vous{'@'}exemple.com",
		"loading": "Mise à jour en cours...",
		"old-password": "Ancien mot de passe",
		"password": "Nouveau mot de passe",
		"password-confirmation": "Confirmation du mot de passe",
		"password-rules": "Le mot de passe doit contenir un minimum de 10 caractères, une majuscule, un chiffre et un caractère spécial (par exemple: %, {'@'}, #, $ et &).",
		"submit": "Mettre à jour",
		"title": "Réglages du compte"
	}
}
</i18n>

<template>
  <AppShell :loading="loading" :title="t('title')">
    <div v-if="user" class="max-w-sm lg:w-96">
      <Form
        v-slot="{ isSubmitting }"
        :validation-schema="validationSchemaEmail"
        :initial-values="initialFormValues"
        @submit="onSubmitChangeEmail">
        <PfForm has-footer :submit-label="t('submit')" :loading-label="t('loading')" :processing="isSubmitting">
          <PfFormSection>
            <Field v-slot="{ field, errors }" name="email">
              <PfFormInputText
                id="email"
                v-bind="field"
                :label="t('email')"
                :errors="errors"
                input-type="email"
                :description="t('email-desc')" />
            </Field>
          </PfFormSection>
        </PfForm>
      </Form>

      <Form
        v-slot="{ isSubmitting }"
        class="mt-12"
        :validation-schema="validationSchemaPassword"
        @submit="onSubmitChangePassword">
        <PfForm has-footer :submit-label="t('submit')" :loading-label="t('loading')" :processing="isSubmitting">
          <PfFormSection>
            <Field v-slot="{ field, errors }" name="oldPassword">
              <PfFormInputText
                id="oldPassword"
                v-bind="field"
                :label="t('old-password')"
                :errors="errors"
                input-type="password" />
            </Field>

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
    </div>
  </AppShell>
</template>

<script setup>
import gql from "graphql-tag";
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { object, string, ref as yupRef } from "yup";

import { URL_ROOT } from "@/lib/consts/urls";
import { useGraphQLErrorMessages } from "@/lib/helpers/error-handler";
import { useNotificationsStore } from "@/lib/store/notifications";
import { usePageTitle } from "@/lib/helpers/page-title";

const { t } = useI18n();
const router = useRouter();
const { addSuccess } = useNotificationsStore();

usePageTitle(t("title"));

// Configure les messages en lien avec les erreurs graphql susceptibles d'être lancées par ce composant
useGraphQLErrorMessages({
  // Ce code est lancé quand le mot de passe est invalid
  WRONG_PASSWORD: () => {
    return t("change-password-error-notification");
  }
});

const { user, loading } = getUserEmail();

const initialFormValues = computed(() => {
  return { email: user?.value?.email };
});
const validationSchemaEmail = object({
  email: string().label(t("email")).required().email()
});
const validationSchemaPassword = computed(() =>
  object({
    oldPassword: string().label(t("old-password")).required(),
    password: string().label(t("password")).required().password(),
    passwordConfirmation: string().label(t("password-confirmation")).required().samePassword(yupRef("password"))
  })
);

const { mutate: changeEmail } = useMutation(
  gql`
    mutation ChangeEmail($input: ChangeEmailInput!) {
      changeEmail(input: $input)
    }
  `
);
const { mutate: changePassword } = useMutation(
  gql`
    mutation ChangePassword($input: ChangePasswordInput!) {
      changePassword(input: $input) {
        user {
          id
        }
      }
    }
  `
);

function getUserEmail() {
  const { result, loading } = useQuery(
    gql`
      query GetUserProfile {
        me {
          id
          email
        }
      }
    `
  );

  const user = useResult(result, null, (data) => data.me);

  return { user, loading };
}

async function onSubmitChangeEmail({ email }) {
  if (email !== user?.value?.email) {
    await changeEmail({ input: { newEmail: email } });
    addSuccess(t("change-email-standby-notification", { email }));
    router.push({ name: URL_ROOT });
  }
}

async function onSubmitChangePassword({ oldPassword, password }) {
  await changePassword({ input: { currentPassword: oldPassword, newPassword: password } });
  addSuccess(t("change-password-success-notification"));
  router.push({ name: URL_ROOT });
}
</script>
