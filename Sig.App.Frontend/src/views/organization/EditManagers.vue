<i18n>
{
	"en": {
		"add-organization-manager-success-notification": "The addition of the organization manager was a success. The manager will receive an email for the creation of his account in the next few minutes.",
		"remove-manager-success-notification": "The manager {email} has been successfully removed.",
		"title": "Managers - {organizationName}"
	},
	"fr": {
		"add-organization-manager-success-notification": "L’ajout du gestionnaire de l'organisation a été un succès. Le gestionnaire va recevoir un courriel pour la création de son compte dans les prochaines minutes.",
		"remove-manager-success-notification": "Le gestionnaire {email} a été enlevé avec succès.",
		"title": "Gestionnaires - {organizationName}"
	}
}
</i18n>

<template>
  <ManageManagersModal
    ref="manageManagersModal"
    :return-route="{ name: URL_ORGANIZATION_ADMIN }"
    :managers="managers"
    :title="t('title', { organizationName: getOrganizationName() })"
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
import { URL_ORGANIZATION_ADMIN } from "@/lib/consts/urls";

import ManageManagersModal from "@/components/managers/manage-managers-modal.vue";

const { t } = useI18n();
const route = useRoute();
const { addSuccess } = useNotificationsStore();

const manageManagersModal = ref(null);

const { result, refetch } = useQuery(
  gql`
    query Organization($id: ID!) {
      organization(id: $id) {
        id
        name
        managers {
          id
          profile {
            id
            firstName
            lastName
          }
          email
        }
      }
    }
  `,
  {
    id: route.params.organizationId
  },
  () => ({
    enabled: route.params.organizationId !== null
  })
);
const organization = useResult(result);

const { mutate: removeManagerFromOrganization } = useMutation(
  gql`
    mutation RemoveManagerFromOrganization($input: RemoveManagerFromOrganizationInput!) {
      removeManagerFromOrganization(input: $input) {
        organization {
          id
          managers {
            id
            profile {
              id
              firstName
              lastName
            }
            email
          }
        }
      }
    }
  `
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

function getOrganizationName() {
  return organization.value ? organization.value.name : "";
}

const managers = computed(() => {
  return organization.value ? organization.value.managers : [];
});

async function removeManager(manager) {
  let input = { organizationId: organization.value.id, managerId: manager.id };

  await removeManagerFromOrganization({ input });
  addSuccess(t("remove-manager-success-notification", { email: manager.email }));
  refetch();
}

async function addManager(email) {
  await addManagerToOrganization({
    input: {
      organizationId: organization.value.id,
      managerEmails: [email]
    }
  });
  addSuccess(t("add-organization-manager-success-notification"));
  refetch();
  manageManagersModal.value.hideAddManagerForm();
}
</script>
