<i18n>
{
	"en": {
		"cancel": "Cancel",
		"market-name": "Market name",
		"market-name-placeholder": "Ex. Central market",
    "password": "Password for transaction refund",
		"password-confirmation": "Re-enter password",
		"password-rules": "The password must contain a minimum of 10 characters, 1 capital letter, a number and a special character (for example: %, {'@'}, #, $ and &).",
    "reset-password-btn": "Reset password",
	},
	"fr": {
		"cancel": "Annuler",
		"market-name": "Nom du commerce",
		"market-name-placeholder": "Ex. Marché centrale",
    "password": "Mot de passe pour remboursement des transactions",
		"password-confirmation": "Confirmation du mot de passe",
		"password-rules": "Le mot de passe doit contenir un minimum de 10 caractères, une majuscule, un chiffre et un caractère spécial (par exemple: %, {'@'}, #, $ et &).",
    "reset-password-btn": "Réinitialiser le mot de passe",
	}
}
</i18n>

<template>
  <Form
    v-slot="{ isSubmitting, errors: formErrors }"
    :validation-schema="validationSchema || baseValidationSchema"
    :initial-values="initialValues"
    @submit="onSubmit">
    <PfForm
      has-footer
      can-cancel
      :disable-submit="Object.keys(formErrors).length > 0"
      :submit-label="props.submitBtn"
      :cancel-label="t('cancel')"
      :processing="isSubmitting"
      @cancel="closeModal">
      <PfFormSection>
        <Field v-slot="{ field, errors: fieldErrors }" name="marketName">
          <PfFormInputText
            id="marketName"
            v-bind="field"
            :label="t('market-name')"
            :placeholder="t('market-name-placeholder')"
            :errors="fieldErrors" />
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
  submitBtn: {
    type: String,
    default: ""
  },
  marketName: {
    type: String,
    default: ""
  },
  initialValues: {
    type: Object,
    default(rawProps) {
      return { marketName: rawProps.marketName, password: "", passwordConfirmation: "" };
    }
  },
  validationSchema: {
    type: Object,
    default: null
  },
  isNew: {
    type: Boolean,
    default: false
  }
});

const baseValidationSchema = computed(() =>
  object({
    marketName: string().label(t("market-name")).required(),
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
