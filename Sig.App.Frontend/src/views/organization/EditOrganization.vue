<i18n>
{
	"en": {
		"edit-organization": "Edit",
		"edit-organization-success-notification": "Editing group {organizationName} was successful.",
		"title": "Edit an group"
	},
	"fr": {
		"edit-organization": "Modifier",
		"edit-organization-success-notification": "L’édition du groupe {organizationName} a été un succès.",
		"title": "Modifier un groupe"
	}
}
</i18n>

<template>
  <UiDialogModal v-slot="{ closeModal }" :return-route="{ name: URL_ORGANIZATION_ADMIN }" :title="t('title')" :has-footer="false">
    <OrganizationForm
      v-if="organization"
      :submit-btn="t('edit-organization')"
      :name="organization.name"
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
import { URL_ORGANIZATION_ADMIN } from "@/lib/consts/urls";

import OrganizationForm from "@/views/organization/_Form.vue";

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
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
let organization = useResult(result);

const { mutate: editOrganization } = useMutation(
  gql`
    mutation EditOrganization($input: EditOrganizationInput!) {
      editOrganization(input: $input) {
        organization {
          id
          name
        }
      }
    }
  `
);

async function onSubmit(values) {
  await editOrganization({
    input: {
      organizationId: route.params.organizationId,
      name: { value: values.name }
    }
  });
  router.push({ name: URL_ORGANIZATION_ADMIN });
  addSuccess(t("edit-organization-success-notification", { organizationName: values.name }));
}
</script>
