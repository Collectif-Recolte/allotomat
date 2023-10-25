<i18n>
{
	"en": {
		"delete-project-success-notification": "The program {projectName} has been successfully deleted.",
		"delete-text-error": "The text must match the name of the program",
		"delete-text-label": "Type the name of the program to confirm",
		"description": "Warning ! The deletion of <strong>{projectName}</strong> cannot be undone. If you continue, the program will be permanently deleted along with all the elements it contains.",
		"title": "Delete - {projectName}",
    "cant-delete-project-with-subscriptions": "It is not possible to delete a program that has active subscriptions."
	},
	"fr": {
		"delete-project-success-notification": "Le programme {projectName} a été supprimé avec succès.",
		"delete-text-error": "Le texte doit correspondre au nom du programme",
		"delete-text-label": "Taper le nom du programme pour confirmer",
		"description": "Avertissement ! La suppression de <strong>{projectName}</strong> ne peut pas être annulée. Si vous continuez, le programme sera supprimé ainsi que tous les éléments qu'il contient de façon définitive.",
		"title": "Supprimer - {projectName}",
    "cant-delete-project-with-subscriptions": "Il n'est pas possible de supprimer un programme qui possède des abonnements actifs."
	}
}
</i18n>

<template>
  <UiDialogDeleteModal
    :return-route="{ name: URL_PROJECT_ADMIN }"
    :title="t('title', { projectName: getProjectName() })"
    :description="t('description', { projectName: getProjectName() })"
    :validation-text="getProjectName()"
    :delete-text-label="t('delete-text-label')"
    :delete-text-error="t('delete-text-error')"
    @onDelete="deleteProject" />
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";

import { useGraphQLErrorMessages } from "@/lib/helpers/error-handler";
import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_PROJECT_ADMIN } from "@/lib/consts/urls";

useGraphQLErrorMessages({
  PROJECT_CANT_HAVE_ACTIVE_SUBSCRIPTION: () => {
    return t("cant-delete-project-with-subscriptions");
  }
});

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { addSuccess } = useNotificationsStore();

const { result: resultProject } = useQuery(
  gql`
    query Project($id: ID!) {
      project(id: $id) {
        id
        name
      }
    }
  `,
  {
    id: route.params.projectId
  }
);
const project = useResult(resultProject);

const { mutate: deleteProjectMutation } = useMutation(
  gql`
    mutation DeleteProject($input: DeleteProjectInput!) {
      deleteProject(input: $input)
    }
  `
);

function getProjectName() {
  return project.value ? project.value.name : "";
}

async function deleteProject() {
  await deleteProjectMutation({
    input: {
      projectId: route.params.projectId
    }
  });

  addSuccess(t("delete-project-success-notification", { projectName: project.value.name }));
  router.push({ name: URL_PROJECT_ADMIN });
}
</script>
