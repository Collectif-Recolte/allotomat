<i18n>
{
	"en": {
		"add-project-manager-success-notification": "The addition of the project manager was a success. The manager will receive an email for the creation of his account in the next few minutes.",
		"remove-manager-success-notification": "The manager {email} has been successfully removed.",
		"title": "Managers - {projectName}"
	},
	"fr": {
		"add-project-manager-success-notification": "L’ajout du gestionnaire de programme a été un succès. Le gestionnaire va recevoir un courriel pour la création de son compte dans les prochaines minutes.",
		"remove-manager-success-notification": "Le gestionnaire {email} a été enlevé avec succès.",
		"title": "Gestionnaires - {projectName}"
	}
}
</i18n>

<template>
  <ManageManagersModal
    ref="manageManagersModal"
    :return-route="{ name: URL_PROJECT_ADMIN }"
    :managers="managers"
    :title="t('title', { projectName: getProjectName() })"
    @removeManager="removeManager"
    @addManager="addManager" />
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { ref, computed } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute } from "vue-router";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_PROJECT_ADMIN } from "@/lib/consts/urls";

import ManageManagersModal from "@/components/managers/manage-managers-modal.vue";

const { t } = useI18n();
const route = useRoute();
const { addSuccess } = useNotificationsStore();

const manageManagersModal = ref(null);

const { result, refetch } = useQuery(
  gql`
    query Project($id: ID!) {
      project(id: $id) {
        id
        name
        managers {
          id
          email
          isConfirmed
          confirmationLink
          resetPasswordLink
          profile {
            id
            firstName
            lastName
          }
          type
        }
      }
    }
  `,
  {
    id: route.params.projectId
  }
);
const project = useResult(result);

const { mutate: removeManagerFromProject } = useMutation(
  gql`
    mutation RemoveManagerFromProject($input: RemoveManagerFromProjectInput!) {
      removeManagerFromProject(input: $input) {
        project {
          id
          managers {
            id
            email
            isConfirmed
            confirmationLink
            resetPasswordLink
            profile {
              id
              firstName
              lastName
            }
            type
          }
        }
      }
    }
  `
);

const { mutate: addManagerToProject } = useMutation(
  gql`
    mutation AddManagerToProject($input: AddManagerToProjectInput!) {
      addManagerToProject(input: $input) {
        managers {
          id
          email
          isConfirmed
          confirmationLink
          resetPasswordLink
          profile {
            id
            firstName
            lastName
          }
          type
        }
      }
    }
  `
);

function getProjectName() {
  return project.value ? project.value.name : "";
}

const managers = computed(() => {
  return project.value ? project.value.managers : [];
});

async function removeManager(manager) {
  let input = { projectId: project.value.id, managerId: manager.id };

  await removeManagerFromProject({ input });
  addSuccess(t("remove-manager-success-notification", { email: manager.email }));
  refetch();
}

async function addManager(email) {
  await addManagerToProject({
    input: {
      projectId: project.value.id,
      managerEmails: [email]
    }
  });
  addSuccess(t("add-project-manager-success-notification"));
  refetch();
  manageManagersModal.value.hideAddManagerForm();
}
</script>
