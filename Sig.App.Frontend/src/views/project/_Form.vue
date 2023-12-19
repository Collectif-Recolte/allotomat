<i18n>
{
	"en": {
		"cancel": "Cancel",
		"project-name": "Program name",
		"project-name-placeholder": "Ex. Proximity card",
    "project-url": "Program website",
    "project-url-description": "Ex. https://carteproximite.org/",
    "project-allow-organizations-assign-cards": "Allow organizations to assign cards",
    "project-beneficiaries-are-anonymous": "Anonymous beneficiaries",
    "project-administration-subscriptions-off-platform": "The administration of participants is done off-platform",
    "password": "Password for transaction refund",
		"password-confirmation": "Re-enter password",
		"password-rules": "The password must contain a minimum of 10 characters, 1 capital letter, a number and a special character (for example: %, {'@'}, #, $ and &).",
    "reset-password-btn": "Reset password"
	},
	"fr": {
		"cancel": "Annuler",
		"project-name": "Nom du programme",
		"project-name-placeholder": "Ex. Carte proximité",
    "project-url": "Site web du programme",
    "project-url-description": "Ex. https://carteproximite.org/",
    "project-allow-organizations-assign-cards": "Permettre aux organismes d’assigner des cartes",
    "project-beneficiaries-are-anonymous": "Participant-e-s anonymes",
    "project-administration-subscriptions-off-platform": "L'administration des participant-e-s se fait hors plateforme",
    "password": "Mot de passe pour remboursement des transactions",
		"password-confirmation": "Confirmation du mot de passe",
		"password-rules": "Le mot de passe doit contenir un minimum de 10 caractères, une majuscule, un chiffre et un caractère spécial (par exemple: %, {'@'}, #, $ et &).",
    "reset-password-btn": "Réinitialiser le mot de passe"
  }
}
</i18n>

<template>
  <Form
    v-slot="{ isSubmitting, errors: formErrors }"
    :validation-schema="validationSchema || baseValidationSchema"
    :initial-values="initialValues || baseInitialValues"
    @submit="onSubmit">
    <PfForm
      has-footer
      can-cancel
      :disable-submit="Object.keys(formErrors).length > 0"
      :submit-label="props.submitBtn"
      :cancel-label="t('cancel')"
      :processing="isSubmitting"
      :warning-message="
        props.administrationSubscriptionsOffPlatform ? t('project-administration-subscriptions-off-platform') : ''
      "
      @cancel="closeModal">
      <PfFormSection>
        <Field v-slot="{ field, errors: fieldErrors }" name="name">
          <PfFormInputText
            id="name"
            v-bind="field"
            :label="t('project-name')"
            :placeholder="t('project-name-placeholder')"
            :errors="fieldErrors"
            col-span-class="sm:col-span-4" />
        </Field>
        <Field v-slot="{ field, errors: fieldErrors }" name="url">
          <PfFormInputText
            id="url"
            v-bind="field"
            :label="t('project-url')"
            :errors="fieldErrors"
            :description="t('project-url-description')"
            col-span-class="sm:col-span-4" />
        </Field>
        <Field v-slot="{ field }" name="beneficiariesAreAnonymous">
          <PfFormInputCheckbox
            id="beneficiariesAreAnonymous"
            v-bind="field"
            :label="t('project-beneficiaries-are-anonymous')"
            :checked="field.value"
            col-span-class="sm:col-span-4" />
        </Field>
        <Field v-slot="{ field }" name="allowOrganizationsAssignCards">
          <PfFormInputCheckbox
            id="allowOrganizationsAssignCards"
            v-bind="field"
            :label="t('project-allow-organizations-assign-cards')"
            :checked="field.value"
            col-span-class="sm:col-span-4" />
        </Field>
        <Field v-if="isNewProject" v-slot="{ field }" name="administrationSubscriptionsOffPlatform">
          <PfFormInputCheckbox
            id="administrationSubscriptionsOffPlatform"
            v-bind="field"
            :label="t('project-administration-subscriptions-off-platform')"
            :checked="field.value"
            col-span-class="sm:col-span-4" />
        </Field>
        <Field v-slot="{ field, errors }" name="password">
          <PfFormInputText
            id="password"
            v-bind="field"
            :label="t('password')"
            :errors="errors"
            input-type="password"
            :description="t('password-rules')"
            col-span-class="sm:col-span-4" />
        </Field>

        <Field v-slot="{ field, errors }" name="passwordConfirmation">
          <PfFormInputText
            id="passwordConfirmation"
            v-bind="field"
            :label="t('password-confirmation')"
            :errors="errors"
            input-type="password"
            col-span-class="sm:col-span-4" />
        </Field>
      </PfFormSection>
      <PfButtonAction v-if="!isNew" class="pf-button mt-8" :label="t('reset-password-btn')" @click="resetPassword" />
      <slot></slot>
    </PfForm>
  </Form>
</template>

<script setup>
import { defineEmits, defineProps, computed } from "vue";
import { useI18n } from "vue-i18n";
import { string, object, lazy, mixed, ref as yupRef } from "yup";

const { t } = useI18n();
const emit = defineEmits(["submit", "closeModal", "reset-password"]);

const props = defineProps({
  title: {
    type: String,
    default: ""
  },
  submitBtn: {
    type: String,
    default: ""
  },
  name: {
    type: String,
    default: ""
  },
  url: {
    type: String,
    default: ""
  },
  allowOrganizationsAssignCards: {
    type: Boolean,
    default: false
  },
  beneficiariesAreAnonymous: {
    type: Boolean,
    default: false
  },
  administrationSubscriptionsOffPlatform: {
    type: Boolean,
    default: false
  },
  initialValues: {
    type: Object,
    default: null
  },
  validationSchema: {
    type: Object,
    default: null
  },
  isNewProject: {
    type: Boolean,
    default: false
  },
  isNew: {
    type: Boolean,
    default: false
  }
});

const baseInitialValues = {
  name: props.name,
  url: props.url,
  allowOrganizationsAssignCards: props.allowOrganizationsAssignCards,
  beneficiariesAreAnonymous: props.beneficiariesAreAnonymous,
  administrationSubscriptionsOffPlatform: props.administrationSubscriptionsOffPlatform
};

const baseValidationSchema = computed(() =>
  object({
    name: string().label(t("project-name")).required(),
    url: string().label(t("project-url")).url(),
    password: lazy((value) => {
      if (value !== "" && value !== undefined) {
        return string().label(t("password")).required().password();
      } else {
        return mixed().test({
          test: function () {
            return true;
          }
        });
      }
    }),
    passwordConfirmation: lazy((value) => {
      if (value !== "" && value !== undefined) {
        return string().label(t("password-confirmation")).samePassword(yupRef("password"));
      } else {
        return mixed().test({
          test: function () {
            return true;
          }
        });
      }
    })
  })
);

function closeModal() {
  emit("closeModal");
}

async function onSubmit(values) {
  emit("submit", values);
}

function resetPassword() {
  emit("reset-password");
}
</script>
