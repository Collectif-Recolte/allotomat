<i18n>
{
	"en": {
		"add-beneficiary": "Add participant",
		"add-beneficiary-success-notification": "Adding {firstname} {lastname} was successful.",
		"title": "Add a participant"
	},
	"fr": {
		"add-beneficiary": "Ajouter le-la participant-e",
		"add-beneficiary-success-notification": "L’ajout de {firstname} {lastname} a été un succès.",
		"title": "Ajouter un-e participant-e"
	}
}
</i18n>

<template>
  <UiDialogModal v-slot="{ closeModal }" :title="t('title')" :has-footer="false" :return-route="{ name: URL_BENEFICIARY_ADMIN }">
    <BeneficiaryForm
      :submit-btn="t('add-beneficiary')"
      :organization-id="currentOrganization"
      @submit="onSubmit"
      @closeModal="closeModal" />
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useMutation } from "@vue/apollo-composable";

import { useOrganizationStore } from "@/lib/store/organization";
import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_BENEFICIARY_ADMIN } from "@/lib/consts/urls";

import BeneficiaryForm from "@/views/beneficiary/_Form";

const { t } = useI18n();
const router = useRouter();
const { addSuccess } = useNotificationsStore();
const { currentOrganization } = useOrganizationStore();

const { mutate: createBeneficiaryInOrganization } = useMutation(
  gql`
    mutation CreateBeneficiaryInOrganization($input: CreateBeneficiaryInOrganizationInput!) {
      createBeneficiaryInOrganization(input: $input) {
        beneficiary {
          id
          firstname
          lastname
          email
          phone
          address
          notes
          postalCode
          id1
          id2
          ... on BeneficiaryGraphType {
            beneficiaryType {
              id
            }
          }
        }
      }
    }
  `
);

async function onSubmit({ firstname, lastname, email, phone, address, notes, postalCode, id1, id2, beneficiaryTypeId }) {
  await createBeneficiaryInOrganization({
    input: {
      organizationId: currentOrganization,
      firstname,
      lastname,
      email,
      phone,
      address,
      notes,
      postalCode,
      id1,
      id2,
      beneficiaryTypeId
    }
  });
  router.push({ name: URL_BENEFICIARY_ADMIN });
  addSuccess(t("add-beneficiary-success-notification", { firstname, lastname }));
}
</script>
