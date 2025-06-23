<i18n>
{
	"en": {
    "add-project": "Add",
		"add-project-success-notification": "Adding program {projectName} was successful. Managers will receive an email for the creation of their account in the next few minutes.",
		"manager-email": "Email",
		"project-name": "Name",
		"title": "Add a program",
		"user-already-manager": "One of the managers is already the manager of an program.",
		"user-not-project-manager": "One of the managers is not an program manager.",
		"project-url": "Program website"
	},
	"fr": {
    "add-project": "Ajouter",
		"add-project-success-notification": "L’ajout du programme {projectName} a été un succès. Les gestionnaires vont recevoir un courriel pour la création de leur compte dans les prochaines minutes.",
		"manager-email": "Courriel",
		"project-name": "Nom",
		"title": "Ajouter un programme",
		"user-already-manager": "Un des gestionnaires est déjà gestionnaire d'un programme.",
		"user-not-project-manager": "Un des gestionnaires n'est pas du type gestionnaire de programme.",
		"project-url": "Site web du programme"
	}
}
</i18n>

<template>
  <UiDialogModal v-slot="{ closeModal }" :return-route="{ name: URL_PROJECT_ADMIN }" :title="t('title')" :has-footer="false">
    <ProjectForm
      :submit-btn="t('add-project')"
      :initial-values="initialValues"
      :validation-schema="validationSchema"
      is-new-project
      @closeModal="closeModal"
      @submit="onSubmit">
      <FormSectionManagers />
    </ProjectForm>
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { string, object, array } from "yup";
import { useRouter } from "vue-router";
import { useMutation } from "@vue/apollo-composable";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_PROJECT_ADMIN } from "@/lib/consts/urls";
import { useGraphQLErrorMessages } from "@/lib/helpers/error-handler";

import FormSectionManagers from "@/components/managers/form-section-managers";
import ProjectForm from "@/views/project/_Form.vue";

useGraphQLErrorMessages({
  USER_ALREADY_MANAGER: () => {
    router.push({ name: URL_PROJECT_ADMIN });
    return t("user-already-manager");
  },
  EXISTING_USER_NOT_PROJECT_MANAGER: () => {
    router.push({ name: URL_PROJECT_ADMIN });
    return t("user-not-project-manager");
  }
});

const { t } = useI18n();
const router = useRouter();
const { addSuccess } = useNotificationsStore();

const initialValues = {
  managers: [{ email: "" }]
};

const validationSchema = computed(() =>
  object({
    name: string().label(t("project-name")).required(),
    managers: array().of(
      object({
        email: string().label(t("manager-email")).required().email()
      })
    ),
    url: string().label(t("project-url")).url()
  })
);

const { mutate: createProject } = useMutation(
  gql`
    mutation CreateProject($input: CreateProjectInput!) {
      createProject(input: $input) {
        project {
          id
          name
          url
          allowOrganizationsAssignCards
          beneficiariesAreAnonymous
          administrationSubscriptionsOffPlatform
        }
      }
    }
  `
);

async function onSubmit({
  name,
  url,
  allowOrganizationsAssignCards,
  managers,
  beneficiariesAreAnonymous,
  administrationSubscriptionsOffPlatform,
  reconciliationReportDate
}) {
  let input = {
    name,
    url,
    managerEmails: managers.map((x) => x.email),
    allowOrganizationsAssignCards: allowOrganizationsAssignCards !== undefined ? allowOrganizationsAssignCards : false,
    beneficiariesAreAnonymous: beneficiariesAreAnonymous !== undefined ? beneficiariesAreAnonymous : false,
    administrationSubscriptionsOffPlatform:
      administrationSubscriptionsOffPlatform !== undefined ? administrationSubscriptionsOffPlatform : false,
    reconciliationReportDate: reconciliationReportDate !== undefined ? reconciliationReportDate : null
  };

  await createProject({ input });
  router.push({ name: URL_PROJECT_ADMIN });
  addSuccess(t("add-project-success-notification", { projectName: name }));
}
</script>
