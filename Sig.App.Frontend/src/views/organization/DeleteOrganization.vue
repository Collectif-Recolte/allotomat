<i18n>
{
	"en": {
		"delete-organization-success-notification": "The organization {organizationName} has been successfully deleted.",
		"delete-text-error": "The text must match the name of the organization",
		"delete-text-label": "Type the name of the organization to confirm",
		"description": "Warning ! The deletion of <strong>{organizationName}</strong> cannot be undone. If you continue, the organization will be permanently deleted along with all the elements it contains. Card payments can no longer be traced. However, the funds of the cards will remain unchanged.",
		"title": "Delete - {organizationName}"
	},
	"fr": {
		"delete-organization-success-notification": "L'organisation {organizationName} a été supprimé avec succès.",
		"delete-text-error": "Le texte doit correspondre au nom de l'organisation",
		"delete-text-label": "Taper le nom de l'organisation pour confirmer",
		"description": "Avertissement ! La suppression de <strong>{organizationName}</strong> ne peut pas être annulée. Si vous continuez, l'organisation sera supprimé ainsi que tous les éléments qu'il contient de façon définitive. Les paiements sur les cartes ne pourront plus être retracés. Par contre, les fonds des cartes vont rester inchangés.",
		"title": "Supprimer - {organizationName}"
	}
}
</i18n>

<template>
  <UiDialogDeleteModal
    :return-route="{ name: URL_ORGANIZATION_ADMIN }"
    :title="t('title', { organizationName: getOrganizationName() })"
    :description="t('description', { organizationName: getOrganizationName() })"
    :validation-text="getOrganizationName()"
    :delete-text-label="t('delete-text-label')"
    :delete-text-error="t('delete-text-error')"
    @onDelete="deleteOrganization" />
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_ORGANIZATION_ADMIN } from "@/lib/consts/urls";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { addSuccess } = useNotificationsStore();

const { result } = useQuery(
  gql`
    query Organization($id: ID!) {
      organization(id: $id) {
        id
        name
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

const { mutate: deleteOrganizationMutation } = useMutation(
  gql`
    mutation DeleteOrganization($input: DeleteOrganizationInput!) {
      deleteOrganization(input: $input)
    }
  `
);

function getOrganizationName() {
  return organization.value ? organization.value.name : "";
}

async function deleteOrganization() {
  await deleteOrganizationMutation({
    input: {
      organizationId: route.params.organizationId
    }
  });

  addSuccess(t("delete-organization-success-notification", { organizationName: organization.value.name }));
  router.push({ name: URL_ORGANIZATION_ADMIN });
}
</script>
