<i18n>
{
	"en": {
		"back": "Cancel",
		"confirm-message": "A message to reset your password has been sent to {email}.",
		"email": "Email",
		"email-desc": "Example: you{'@'}example.com",
		"loading": "We are processing your form, please wait...",
		"submit": "Send an email",
		"title": "Forgot your password?",
    "reset-password-troubleshooting": "If you do not receive your password reset email, please contact <b><a href=\"mailto:support{'@'}allotomat.com\">support{'@'}allotomat.com</a></b>"
	},
	"fr": {
		"back": "Annuler",
		"confirm-message": "Un message pour réinitialiser votre mot de passe a été envoyé à {email}.",
		"email": "Courriel",
		"email-desc": "Exemple: vous{'@'}exemple.com",
		"loading": "Nous traitons votre formulaire, veuillez patienter...",
		"submit": "Envoyer un courriel",
		"title": "Mot de passe oublié?",
    "reset-password-troubleshooting": "Si vous ne recevez pas le courriel de réinitialisation de votre mot de passe, veuillez contacter <b><a href=\"mailto:support{'@'}allotomat.com\">support{'@'}allotomat.com</a></b>"
	}
}
</i18n>
<template>
  <PublicShell :title="t('title')">
    <div v-if="emailSentTo">
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

    <Form v-else v-slot="{ isSubmitting, meta }" :validation-schema="validationSchema" @submit="onSubmit">
      <PfForm
        has-footer
        :disable-submit="!meta.valid"
        :submit-label="t('submit')"
        :cancel-label="t('back')"
        :processing="isSubmitting"
        :loading-label="t('loading')"
        :cancel-route="{ name: URL_ACCOUNT_LOGIN }">
        <PfFormSection>
          <Field v-slot="{ field, errors }" name="email">
            <PfFormInputText
              id="email"
              :model-value="field.value"
              :label="t('email')"
              :errors="errors"
              input-type="email"
              :description="t('email-desc')"
              @update:modelValue="field.onChange"></PfFormInputText>
          </Field>
          <!-- eslint-disable vue/no-v-html @intlify/vue-i18n/no-v-html -->
          <p v-html="t('reset-password-troubleshooting')"></p>
        </PfFormSection>
      </PfForm>
    </Form>
  </PublicShell>
</template>

<script setup>
import gql from "graphql-tag";
import { ref, computed } from "vue";
import { useI18n } from "vue-i18n";
import { useMutation } from "@vue/apollo-composable";
import { object, string } from "yup";

import { usePageTitle } from "@/lib/helpers/page-title";

import { URL_ACCOUNT_LOGIN } from "@/lib/consts/urls";

const { t } = useI18n();

usePageTitle(t("title"));

const validationSchema = computed(() =>
  object({
    email: string().label(t("email")).required().email()
  })
);

const { mutate: sendPasswordReset } = useMutation(
  gql`
    mutation SendPasswordReset($input: SendPasswordResetInput!) {
      sendPasswordReset(input: $input)
    }
  `
);

const emailSentTo = ref();

async function onSubmit({ email }) {
  await sendPasswordReset({ input: { email } });
  emailSentTo.value = email;
}
</script>
