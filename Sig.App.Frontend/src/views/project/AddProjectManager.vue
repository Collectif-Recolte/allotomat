<i18n>
{
	"en": {
		"add-project-manager": "Add manager",
		"add-project-manager-success-notification": "The addition of the project manager was a success. The manager will receive an email for the creation of his account in the next few minutes.",
		"cancel": "Cancel",
		"project-manager-email": "Email",
		"project-manager-email-placeholder": "Ex. john.doe{'@'}exemple.com",
		"title": "Add a project manager",
		"user-already-manager": "One of the managers is already the manager of a program.",
		"user-not-project-manager": "One of the managers is not an program manager."
	},
	"fr": {
		"add-project-manager": "Ajouter le gestionnaire",
		"add-project-manager-success-notification": "L’ajout du gestionnaire de programme a été un succès. Le gestionnaire va recevoir un courriel pour la création de son compte dans les prochaines minutes.",
		"cancel": "Annuler",
		"project-manager-email": "Courriel",
		"project-manager-email-placeholder": "Ex. john.doe{'@'}example.com",
		"title": "Ajouter un gestionnaire du programme",
		"user-already-manager": "Un des gestionnaires est déjà gestionnaire d'un programme.",
		"user-not-project-manager": "Un des gestionnaires n'est pas du type gestionnaire de programme."
	}
}
</i18n>

<template>
  <UiDialogModal
    v-slot="{ closeModal }"
    :return-route="{ name: URL_PROJECT_MANAGER_ADMIN }"
    :title="t('title')"
    :has-footer="false">
    <Form
      v-slot="{ isSubmitting, meta }"
      :validation-schema="validationSchema"
      :initial-values="initialValues"
      @submit="onSubmit">
      <PfForm
        has-footer
        can-cancel
        :disable-submit="!meta.valid"
        :submit-label="t('add-project-manager')"
        :cancel-label="t('cancel')"
        :processing="isSubmitting"
        @cancel="closeModal">
        <Field v-slot="{ field, errors: fieldErrors }" name="email">
          <PfFormInputText
            id="email"
            :model-value="field.value"
            :label="t('project-manager-email')"
            :placeholder="t('project-manager-email-placeholder')"
            :errors="fieldErrors"
            col-span-class="sm:col-span-4"
            @update:modelValue="field.onChange" />
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
import { URL_PROJECT_MANAGER_ADMIN } from "@/lib/consts/urls";
import { useGraphQLErrorMessages } from "@/lib/helpers/error-handler";

useGraphQLErrorMessages({
  USER_ALREADY_MANAGER: () => {
    return t("user-already-manager");
  },
  EXISTING_USER_NOT_PROJECT_MANAGER: () => {
    return t("user-not-project-manager");
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
    email: string().label(t("project-manager-email")).required().email()
  })
);

const { mutate: addManagerToProject } = useMutation(
  gql`
    mutation AddManagerToProject($input: AddManagerToProjectInput!) {
      addManagerToProject(input: $input) {
        managers {
          id
          email
        }
      }
    }
  `
);

async function onSubmit({ email }) {
  await addManagerToProject({
    input: {
      projectId: route.query.projectId,
      managerEmails: [email]
    }
  });
  router.push({ name: URL_PROJECT_MANAGER_ADMIN });
  addSuccess(t("add-project-manager-success-notification"));
}
</script>
