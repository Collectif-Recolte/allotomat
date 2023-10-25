<i18n>
{
	"en": {
		"add-organization-manager": "Add manager",
		"add-organization-manager-success-notification": "The addition of the organization manager was a success. The manager will receive an email for the creation of his account in the next few minutes.",
		"cancel": "Cancel",
		"organization-manager-email": "Email",
		"organization-manager-email-placeholder": "Ex. john.doe{'@'}exemple.com",
		"title": "Add an organization manager",
		"user-already-manager": "One of the managers is already the manager of a program.",
		"user-not-organization-manager": "One of the managers is not an program manager."
	},
	"fr": {
		"add-organization-manager": "Ajouter le gestionnaire",
		"add-organization-manager-success-notification": "L’ajout du gestionnaire de l'organisation a été un succès. Le gestionnaire va recevoir un courriel pour la création de son compte dans les prochaines minutes.",
		"cancel": "Annuler",
		"organization-manager-email": "Courriel",
		"organization-manager-email-placeholder": "Ex. john.doe{'@'}example.com",
		"title": "Ajouter un gestionnaire de l'organisation",
		"user-already-manager": "Un des gestionnaires est déjà gestionnaire d'un programme.",
		"user-not-organization-manager": "Un des gestionnaires n'est pas du type gestionnaire de programme."
	}
}
</i18n>

<template>
  <UiDialogModal
    v-slot="{ closeModal }"
    :return-route="{ name: URL_ORGANIZATION_MANAGER_ADMIN }"
    :title="t('title')"
    :has-footer="false">
    <Form
      v-slot="{ isSubmitting, errors: formErrors }"
      :validation-schema="validationSchema"
      :initial-values="initialValues"
      @submit="onSubmit">
      <PfForm
        has-footer
        can-cancel
        :disable-submit="Object.keys(formErrors).length > 0"
        :submit-label="t('add-organization-manager')"
        :cancel-label="t('cancel')"
        :processing="isSubmitting"
        @cancel="closeModal">
        <Field v-slot="{ field, errors: fieldErrors }" name="email">
          <PfFormInputText
            id="email"
            v-bind="field"
            :label="t('organization-manager-email')"
            :placeholder="t('organization-manager-email-placeholder')"
            :errors="fieldErrors"
            col-span-class="sm:col-span-4" />
        </Field>
      </PfForm>
    </Form>
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { string, object } from "yup";
import { useRouter, useRoute } from "vue-router";
import { useMutation } from "@vue/apollo-composable";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_ORGANIZATION_MANAGER_ADMIN } from "@/lib/consts/urls";
import { useGraphQLErrorMessages } from "@/lib/helpers/error-handler";

useGraphQLErrorMessages({
  USER_ALREADY_MANAGER: () => {
    return t("user-already-manager");
  },
  EXISTING_USER_NOT_ORGANIZATION_MANAGER: () => {
    return t("user-not-organization-manager");
  }
});

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
const { addSuccess } = useNotificationsStore();

const initialValues = {
  email: ""
};

const validationSchema = computed(() =>
  object({
    email: string().label(t("organization-manager-email")).required().email()
  })
);

const { mutate: addManagerToOrganization } = useMutation(
  gql`
    mutation AddManagerToOrganization($input: AddManagerToOrganizationInput!) {
      addManagerToOrganization(input: $input) {
        managers {
          id
          email
        }
      }
    }
  `
);

async function onSubmit({ email }) {
  await addManagerToOrganization({
    input: {
      organizationId: route.query.organizationId,
      managerEmails: [email]
    }
  });
  router.push({ name: URL_ORGANIZATION_MANAGER_ADMIN });
  addSuccess(t("add-organization-manager-success-notification"));
}
</script>
