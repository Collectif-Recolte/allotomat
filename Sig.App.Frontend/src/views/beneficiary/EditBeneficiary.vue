<i18n>
{
	"en": {
		"edit-beneficiary": "Edit",
		"edit-beneficiary-success-notification": "Edition of {firstname} {lastname} was successful.",
		"title": "Edit a participant",
    "warning-message": "These changes will permanently delete previous data.",
    "beneficiary-with-same-id1-already-exists": "A participant with the same ID 1 already exists.",
    "beneficiary-with-same-id2-already-exists": "A participant with the same ID 2 already exists."
	},
	"fr": {
		"edit-beneficiary": "Modifier",
		"edit-beneficiary-success-notification": "L’édition de {firstname} {lastname} a été un succès.",
		"title": "Modifier un-e participant-e",
    "warning-message": "Ces changements supprimeront définitivement les données précédentes.",
    "beneficiary-with-same-id1-already-exists": "Un-e participant-e avec le même identifiant 1 existe déjà.",
    "beneficiary-with-same-id2-already-exists": "Un-e participant-e avec le même identifiant 2 existe déjà."
	}
}
</i18n>

<template>
  <UiDialogModal v-slot="{ closeModal }" :title="t('title')" :has-footer="false" :return-route="{ name: URL_BENEFICIARY_ADMIN }">
    <BeneficiaryForm
      v-if="beneficiary"
      :submit-btn="t('edit-beneficiary')"
      :firstname="beneficiary.firstname"
      :lastname="beneficiary.lastname"
      :email="beneficiary.email ?? ''"
      :phone="beneficiary.phone ?? ''"
      :address="beneficiary.address ?? ''"
      :notes="beneficiary.notes ?? ''"
      :postal-code="beneficiary.postalCode ?? ''"
      :id-1="beneficiary.id1 ?? ''"
      :id-2="beneficiary.id2 ?? ''"
      :organization-id="beneficiary.organization.id"
      :beneficiary-type-id="beneficiary.beneficiaryType.id"
      :warning-message="t('warning-message')"
      :is-unsubscribe-to-receipt="beneficiary.isUnsubscribeToReceipt"
      @submit="onSubmit"
      @closeModal="closeModal" />
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { useRouter, useRoute } from "vue-router";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_BENEFICIARY_ADMIN } from "@/lib/consts/urls";
import { useGraphQLErrorMessages } from "@/lib/helpers/error-handler";

import BeneficiaryForm from "@/views/beneficiary/_Form";

useGraphQLErrorMessages({
  BENEFICIARY_WITH_SAME_ID_1_ALREADY_EXISTS: () => {
    return t("beneficiary-with-same-id1-already-exists");
  },
  BENEFICIARY_WITH_SAME_ID_2_ALREADY_EXISTS: () => {
    return t("beneficiary-with-same-id2-already-exists");
  }
});

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
const { addSuccess } = useNotificationsStore();

const { result } = useQuery(
  gql`
    query Beneficiary($id: ID!) {
      beneficiary(id: $id) {
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
        organization {
          id
        }
        isUnsubscribeToReceipt
      }
    }
  `,
  {
    id: route.params.beneficiaryId
  }
);
let beneficiary = useResult(result);

const { mutate: editBeneficiary } = useMutation(
  gql`
    mutation EditBeneficiary($input: EditBeneficiaryInput!) {
      editBeneficiary(input: $input) {
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
          isUnsubscribeToReceipt
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

async function onSubmit({
  firstname,
  lastname,
  email,
  phone,
  address,
  notes,
  postalCode,
  id1,
  id2,
  beneficiaryTypeId,
  isUnsubscribeToReceipt
}) {
  await editBeneficiary({
    input: {
      beneficiaryId: route.params.beneficiaryId,
      firstname: { value: firstname },
      lastname: { value: lastname },
      email: { value: email ?? "" },
      phone: { value: phone ?? "" },
      address: { value: address ?? "" },
      notes: { value: notes ?? "" },
      postalCode: { value: postalCode ?? "" },
      id1: { value: id1 ?? "" },
      id2: { value: id2 ?? "" },
      beneficiaryTypeId: { value: beneficiaryTypeId },
      isUnsubscribeToReceipt: { value: isUnsubscribeToReceipt }
    }
  });
  router.push({ name: URL_BENEFICIARY_ADMIN });
  addSuccess(t("edit-beneficiary-success-notification", { firstname, lastname }));
}
</script>
