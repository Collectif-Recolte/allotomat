<i18n>
{
	"en": {
		"add-organization": "Add organization",
		"add-organization-success-notification": "Adding organization {organizationName} was successful. Managers will receive an email for the creation of their account in the next few minutes.",
		"manager-email": "Email",
		"organization-name": "Organization name",
		"title": "Add an Organization",
		"user-already-manager": "One of the managers is already the manager of an organization.",
		"user-not-oganization-manager": "One of the managers is not an organization manager."
	},
	"fr": {
		"add-organization": "Ajouter l'organisme",
		"add-organization-success-notification": "L’ajout de l'organisme {organizationName} a été un succès. Les gestionnaires vont recevoir un courriel pour la création de leur compte dans les prochaines minutes.",
		"manager-email": "Courriel",
		"organization-name": "Nom de l'organisme",
		"title": "Ajouter un organisme",
		"user-already-manager": "Un des gestionnaires est déjà gestionnaire d'un organisme.",
		"user-not-oganization-manager": "Un des gestionnaires n'est pas du type gestionnaire d'organisme."
	}
}
</i18n>

<template>
  <UiDialogModal v-slot="{ closeModal }" :return-route="{ name: URL_ORGANIZATION_ADMIN }" :title="t('title')" :has-footer="false">
    <OrganizationForm
      :submit-btn="t('add-organization')"
      :validation-schema="validationSchema"
      :initial-values="initialValues"
      @closeModal="closeModal"
      @submit="onSubmit">
      <FormSectionManagers />
    </OrganizationForm>
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { string, object, array } from "yup";
import { useRouter, useRoute } from "vue-router";
import { useMutation } from "@vue/apollo-composable";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_ORGANIZATION_ADMIN } from "@/lib/consts/urls";
import { useGraphQLErrorMessages } from "@/lib/helpers/error-handler";

import OrganizationForm from "@/views/organization/_Form.vue";
import FormSectionManagers from "@/components/managers/form-section-managers";

useGraphQLErrorMessages({
  USER_ALREADY_MANAGER: () => {
    return t("user-already-manager");
  },
  EXISTING_USER_NOT_ORGANIZATION_MANAGER: () => {
    return t("user-not-oganization-manager");
  }
});

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
const { addSuccess } = useNotificationsStore();

const initialValues = {
  managers: [{ email: "" }]
};

const validationSchema = computed(() =>
  object({
    name: string().label(t("organization-name")).required(),
    managers: array().of(
      object({
        email: string().label(t("manager-email")).required().email()
      })
    )
  })
);

const { mutate: createOrganizationInProject } = useMutation(
  gql`
    mutation CreateOrganizationInProject($input: CreateOrganizationInProjectInput!) {
      createOrganizationInProject(input: $input) {
        organization {
          id
          name
        }
      }
    }
  `
);

async function onSubmit(values) {
  await createOrganizationInProject({
    input: {
      projectId: route.query.projectId,
      name: values.name,
      managerEmails: values.managers.map((x) => x.email)
    }
  });
  router.push({ name: URL_ORGANIZATION_ADMIN });
  addSuccess(t("add-organization-success-notification", { ...values.name }));
}
</script>
