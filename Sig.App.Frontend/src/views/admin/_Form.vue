<i18n>
{
	"en": {
		"cancel": "Cancel",
		"email": "Email",
		"email-desc": "Example: you{'@'}example.com",
		"first-name": "First name",
		"last-name": "Last name",
		"loading-user-list": "Loading user list..."
	},
	"fr": {
		"cancel": "Annuler",
		"email": "Courriel",
		"email-desc": "Exemple: vous{'@'}exemple.com",
		"first-name": "Prénom",
		"last-name": "Nom",
		"loading-user-list": "Chargement de la liste des utilisateurs..."
	}
}
</i18n>

<template>
  <Form v-slot="{ isSubmitting }" :validation-schema="validationSchema" :initial-values="props.initialValues" @submit="onSubmit">
    <PfForm
      has-footer
      can-cancel
      :cancel-label="t('cancel')"
      :submit-label="props.submitLabel"
      :loading-label="t('loading-user-list')"
      :processing="isSubmitting"
      @cancel="goToUserList()">
      <PfFormSection>
        <Field v-slot="{ field, errors }" name="firstName">
          <PfFormInputText id="firstName" v-bind="field" :label="t('first-name')" :errors="errors" />
        </Field>

        <Field v-slot="{ field, errors }" name="lastName">
          <PfFormInputText id="lastName" v-bind="field" :label="t('last-name')" :errors="errors" />
        </Field>

        <Field v-slot="{ field, errors }" name="email">
          <PfFormInputText
            id="email"
            v-bind="field"
            :label="t('email')"
            :errors="errors"
            input-type="email"
            :description="!isInEdition ? t('email-desc') : null"
            :disabled="isInEdition" />
        </Field>
      </PfFormSection>
    </PfForm>
  </Form>
</template>

<script setup>
import { defineProps, defineEmits, computed } from "vue";
import { useI18n } from "vue-i18n";
import { string, object } from "yup";
import { useRouter } from "vue-router";
import { URL_ADMIN_USERS } from "@/lib/consts/urls";

const router = useRouter();

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

const { t } = useI18n();

const validationSchema = computed(() =>
  object({
    email: string().label(t("email")).required().email(),
    firstName: string().label(t("first-name")).required(),
    lastName: string().label(t("last-name")).required()
  })
);

async function onSubmit(values) {
  emit("submit", values);
}

function goToUserList() {
  router.push({
    name: URL_ADMIN_USERS
  });
}
</script>
