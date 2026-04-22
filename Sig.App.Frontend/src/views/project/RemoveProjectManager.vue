<i18n>
{
	"en": {
		"delete-text-error": "Text must match manager email",
		"delete-text-label": "Type the manager's email to confirm",
		"description": "Warning! Removing the manager <strong>{managerName}</strong> cannot be undone. If you continue, the manager will be permanently removed from the program.",
		"remove-manager-success-notification": "Manager {managerName} has been successfully removed from the program.",
		"title": "Remove - {managerEmail}"
	},
	"fr": {
		"delete-text-error": "Le texte doit correspondre au courriel du ou de la gestionnaire",
		"delete-text-label": "Taper le courriel du ou de la gestionnaire pour confirmer",
		"description": "Avertissement ! Le retrait du ou de la gestionnaire <strong>{managerName}</strong> ne peut pas être annulé. Si vous continuez, la·e gestionnaire sera retiré·e du programme de façon définitive.",
		"remove-manager-success-notification": "Le·a gestionnaire {managerName} a été retiré·e deu programme avec succès.",
		"title": "Retirer - {managerEmail}"
	}
}
</i18n>

<template>
  <UiDialogDeleteModal
    :return-route="{ name: URL_PROJECT_MANAGER_ADMIN }"
    :title="t('title', { managerEmail: getManagerEmail() })"
    :description="t('description', { managerName: getManagerName() })"
    :validation-text="getManagerEmail()"
    :delete-text-label="t('delete-text-label')"
    :delete-text-error="t('delete-text-error')"
    @onDelete="removeManager" />
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_PROJECT_MANAGER_ADMIN } from "@/lib/consts/urls";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { addSuccess } = useNotificationsStore();

const { result } = useQuery(
  gql`
    query User($id: ID!) {
      user(id: $id) {
        id
        email
        profile {
          id
          firstName
          lastName
        }
      }
    }
  `,
  {
    id: route.params.managerId
  }
);
const manager = useResult(result);

const { mutate: removeManagerFromProject } = useMutation(
  gql`
    mutation RemoveManagerFromProject($input: RemoveManagerFromProjectInput!) {
      removeManagerFromProject(input: $input) {
        project {
          id
        }
      }
    }
  `
);

function getManagerName() {
  return manager.value
    ? manager.value.profile.firstName && manager.value.profile.lastName
      ? `${manager.value.profile.firstName} ${manager.value.profile.lastName}`
      : manager.value.email
    : "";
}

function getManagerEmail() {
  return manager.value ? manager.value.email : "";
}

async function removeManager() {
  await removeManagerFromProject({
    input: {
      projectId: route.query.projectId,
      managerId: route.params.managerId
    }
  });

  addSuccess(
    t("remove-manager-success-notification", {
      managerName:
        manager.value.profile.firstName && manager.value.profile.lastName
          ? `${manager.value.profile.firstName} ${manager.value.profile.lastName}`
          : manager.value.email
    })
  );
  router.push({ name: URL_PROJECT_MANAGER_ADMIN });
}
</script>
