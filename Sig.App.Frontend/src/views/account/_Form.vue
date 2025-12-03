<i18n>
{
	"en": {
		"back": "Cancel",
		"email": "Email",
		"email-desc": "Example: you{'@'}example.com",
		"first-name": "First name",
		"last-name": "Last name",
		"loading": "We are processing your form, please wait...",
		"password": "Password",
		"password-confirmation": "Re-enter password",
		"password-rules": "The password must contain a minimum of 10 characters, 1 capital letter, a number and a special character (for example: %, {'@'}, #, $ and &).",
    "tos-accepted": "I have read and agree to the <a href=\"{url1}\" target=\"_blank\" onclick=\"event.stopPropagation();\" style=\"text-decoration:underline\">Terms of Service</a> and the <a href=\"{url2}\" target=\"_blank\" onclick=\"event.stopPropagation();\" style=\"text-decoration:underline\">Privacy Policy</a>.",
    "tos-accepted-label": "Read and agree to the Terms of Service",
    "tos-accepted-required": "You must agree to the Terms of Service to complete your registration"
	},
	"fr": {
		"back": "Annuler",
		"email": "Courriel",
		"email-desc": "Exemple: vous{'@'}exemple.com",
		"first-name": "Prénom",
		"last-name": "Nom",
		"loading": "Nous traitons votre formulaire, veuillez patienter...",
		"password": "Mot de passe",
		"password-confirmation": "Confirmation du mot de passe",
		"password-rules": "Le mot de passe doit contenir un minimum de 10 caractères, une majuscule, un chiffre et un caractère spécial (par exemple: %, {'@'}, #, $ et &).",
    "tos-accepted": "J'ai lu et j'accepte les <a href=\"{url1}\" target=\"_blank\" onclick=\"event.stopPropagation();\" style=\"text-decoration:underline\">Conditions générales d'utilisation</a> et la <a href=\"{url2}\" target=\"_blank\" onclick=\"event.stopPropagation();\" style=\"text-decoration:underline\">Politique de confidentialité</a>.",
    "tos-accepted-label": "Lire et accepter les conditions d'utilisation",
    "tos-accepted-required": "Vous devez accepter les Conditions générales d'utilisation pour compléter votre inscription"
	}
}
</i18n>

<template>
  <Form
    v-slot="{ isSubmitting, meta }"
    :validation-schema="validationSchema"
    :initial-values="props.initialValues"
    @submit="onSubmit">
    <PfForm
      has-footer
      :disable-submit="!meta.valid"
      :submit-label="props.submitLabel"
      :cancel-label="t('back')"
      :processing="isSubmitting"
      :loading-label="t('loading')"
      :cancel-route="{ name: $consts.urls.URL_ACCOUNT_LOGIN }">
      <PfFormSection>
        <Field v-slot="{ field, errors }" name="email">
          <PfFormInputText
            id="email"
            :model-value="field.value"
            :label="t('email')"
            :errors="errors"
            input-type="email"
            :description="!isInEdition ? t('email-desc') : null"
            :disabled="isInEdition"
            @update:modelValue="field.onChange" />
        </Field>

        <Field v-slot="{ field, errors }" name="firstName">
          <PfFormInputText
            id="firstName"
            :model-value="field.value"
            :label="t('first-name')"
            :errors="errors"
            @update:modelValue="field.onChange" />
        </Field>

        <Field v-slot="{ field, errors }" name="lastName">
          <PfFormInputText
            id="lastName"
            :model-value="field.value"
            :label="t('last-name')"
            :errors="errors"
            @update:modelValue="field.onChange" />
        </Field>

        <Field v-slot="{ field, errors }" name="password">
          <PfFormInputText
            id="password"
            :model-value="field.value"
            :label="t('password')"
            :errors="errors"
            input-type="password"
            :description="t('password-rules')"
            @update:modelValue="field.onChange" />
        </Field>

        <Field v-slot="{ field, errors }" name="passwordConfirmation">
          <PfFormInputText
            id="passwordConfirmation"
            :model-value="field.value"
            :label="t('password-confirmation')"
            :errors="errors"
            input-type="password"
            @update:modelValue="field.onChange" />
        </Field>
      </PfFormSection>

      <PfFormSection>
        <Field v-slot="{ field, errors: fieldErrors }" name="tosAccepted">
          <PfFormInputCheckbox
            id="tosAccepted"
            :model-value="field.value"
            :label="
              t('tos-accepted', {
                url1: `/files/TermsOfUse-${locale}-20230505.pdf`,
                url2: `/files/PrivacyPolicy-${locale}-20230505.pdf`
              })
            "
            :errors="fieldErrors"
            @update:modelValue="field.onChange" />
        </Field>
      </PfFormSection>
    </PfForm>
  </Form>
</template>

<script setup>
import { defineProps, defineEmits, computed } from "vue";
import { useI18n } from "vue-i18n";
import { object, string, bool, ref as yupRef } from "yup";

const props = defineProps({
  submitLabel: { type: String, required: true },
  initialValues: {
    type: Object,
    default() {
      return {};
    }
  },
  isInEdition: Boolean
});
const emit = defineEmits(["submit"]);

const { t, locale } = useI18n();

const validationSchema = computed(() =>
  object({
    email: string().label(t("email")).required().email(),
    firstName: string().label(t("first-name")).required(),
    lastName: string().label(t("last-name")).required(),
    password: string().label(t("password")).required().password(),
    passwordConfirmation: string().label(t("password-confirmation")).required().samePassword(yupRef("password")),
    tosAccepted: bool()
      .oneOf([true], t("tos-accepted-required"))
      .label(t("tos-accepted-label"))
      .required(t("tos-accepted-required"))
  })
);

async function onSubmit(values) {
  emit("submit", values);
}
</script>
