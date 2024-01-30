<i18n>
{
	"en": {
		"edit-project": "Edit",
		"edit-project-success-notification": "Editing project {projectName} was successful.",
		"title": "Edit a project"
	},
	"fr": {
		"edit-project": "Modifier",
		"edit-project-success-notification": "L’édition du programme {projectName} a été un succès.",
		"title": "Modifier un programme"
	}
}
</i18n>

<template>
  <UiDialogModal v-slot="{ closeModal }" :return-route="{ name: URL_PROJECT_ADMIN }" :title="t('title')" :has-footer="false">
    <ProjectForm
      v-if="project"
      :submit-btn="t('edit-project')"
      :name="project.name"
      :url="projectUrl(project)"
      :allow-organizations-assign-cards="project.allowOrganizationsAssignCards"
      :beneficiaries-are-anonymous="project.beneficiariesAreAnonymous"
      :administration-subscriptions-off-platform="project.administrationSubscriptionsOffPlatform"
      @closeModal="closeModal"
      @submit="onSubmit" />
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { useRouter, useRoute } from "vue-router";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_PROJECT_ADMIN } from "@/lib/consts/urls";

import ProjectForm from "@/views/project/_Form.vue";

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
const { addSuccess } = useNotificationsStore();

const { result } = useQuery(
  gql`
    query Project($id: ID!) {
      project(id: $id) {
        id
        name
        url
        allowOrganizationsAssignCards
        beneficiariesAreAnonymous
        administrationSubscriptionsOffPlatform
      }
    }
  `,
  {
    id: route.params.projectId
  }
);
let project = useResult(result);

const { mutate: editProject } = useMutation(
  gql`
    mutation EditProject($input: EditProjectInput!) {
      editProject(input: $input) {
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

async function onSubmit({ name, url, allowOrganizationsAssignCards, beneficiariesAreAnonymous }) {
  let input = {
    projectId: route.params.projectId,
    name: { value: name },
    url: { value: url },
    allowOrganizationsAssignCards: { value: allowOrganizationsAssignCards },
    beneficiariesAreAnonymous: { value: beneficiariesAreAnonymous }
  };

  await editProject({
    input
  });

  router.push({ name: URL_PROJECT_ADMIN });
  addSuccess(t("edit-project-success-notification", { projectName: name }));
}

function projectUrl(project) {
  if (project.url === null) return "";
  return project.url;
}
</script>
