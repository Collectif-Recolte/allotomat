<i18n>
{
	"en": {
		"add-beneficiary": "Add participant",
    "set-subscription": "Assign subscriptions",
    "set-card": "Assign card",
    "next-step-btn-add-beneficiary": "Add participant and assign subscriptions",
    "next-step-btn-set-subscription": "Add subscription(s) and assign card",
		"add-beneficiary-success-notification": "Adding {firstname} {lastname} was successful.",
		"title": "Add a participant",
    "set-beneficiary": "Set participant information",
	},
	"fr": {
		"add-beneficiary": "Ajouter le-la participant-e",
    "set-subscription": "Assigner les abonnements",
    "set-card": "Assigner la carte",
    "next-step-btn-add-beneficiary": "Ajouter le-la participant-e et assigner des abonnements",
    "next-step-btn-set-subscription": "Ajouter des abonnements et assigner une carte",
		"add-beneficiary-success-notification": "L’ajout de {firstname} {lastname} a été un succès.",
		"title": "Ajouter un-e participant-e",
    "set-beneficiary": "Définir les informations du participant-e",
    "set-subscription": "Définir les abonnements",
    "set-card": "Définir la carte"
	}
}
</i18n>

<template>
  <UiDialogModal v-slot="{ closeModal }" :title="t('title')" :has-footer="false" :return-route="{ name: URL_BENEFICIARY_ADMIN }">
    <UiStepper
      class="mb-6"
      :step-label="currentStep === 0 ? t('set-beneficiary') : currentStep === 1 ? t('set-subscription') : t('set-card')"
      :step-count="3"
      :step-number="currentStep + 1" />
    <BeneficiaryForm
      v-if="currentStep === 0"
      :submit-btn="submitBtnLabel"
      :next-step-btn="nextStepBtnLabel"
      :organization-id="currentOrganization"
      @submit="onBeneficiaryFormSubmit"
      @next-step="onBeneficiaryFormNextStep"
      @closeModal="closeModal"
      is-new />
    <AssignSubscriptionForm v-else-if="currentStep === 1" />
    <AssignCardForm v-else />
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useMutation } from "@vue/apollo-composable";
import { ref, computed } from "vue";

import { useOrganizationStore } from "@/lib/store/organization";
import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_BENEFICIARY_ADMIN } from "@/lib/consts/urls";

import BeneficiaryForm from "@/views/beneficiary/_Form";

const currentStep = ref(0);

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

async function onBeneficiaryFormSubmit({
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
}) {
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

async function onBeneficiaryFormNextStep({
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
}) {
  var result = await createBeneficiaryInOrganization({
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
  console.log(result);
  currentStep.value++;
}

const nextStepBtnLabel = computed(() => {
  switch (currentStep.value) {
    case 0:
      return t("next-step-btn-add-beneficiary");
    case 1:
      return t("next-step-btn-set-card");
    default:
      return null;
  }
});

const submitBtnLabel = computed(() => {
  switch (currentStep.value) {
    case 0:
      return t("add-beneficiary");
    case 1:
      return t("set-subscription");
    case 2:
      return t("set-card");
    default:
      return null;
  }
});
</script>
