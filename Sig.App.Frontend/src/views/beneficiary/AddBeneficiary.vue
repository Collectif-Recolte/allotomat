<i18n>
{
	"en": {
    "close": "Close",
		"add-beneficiary": "Add participant",
    "set-subscription": "Assign subscriptions",
    "set-card": "Assign card",
    "next-step-btn-add-beneficiary": "Add participant and assign subscriptions",
    "next-step-btn-add-subscription": "Add subscription(s) and assign card",
		"add-beneficiary-success-notification": "Adding {firstname} {lastname} was successful.",
    "add-subscriptions-success-notification": "Adding subscriptions was successful.",
		"title": "Add a participant",
    "set-beneficiary": "Set participant information",
    "assign-card-description": "This card <b>has no funds on it</b>, and will remain empty until the next payment date."
	},
	"fr": {
    "close": "Fermer",
		"add-beneficiary": "Ajouter le-la participant-e",
    "set-subscription": "Assigner les abonnements",
    "set-card": "Définir la carte",
    "next-step-btn-add-beneficiary": "Ajouter le-la participant-e et assigner des abonnements",
    "next-step-btn-add-subscription": "Assigner les abonnements et assigner une carte",
		"add-beneficiary-success-notification": "L’ajout de {firstname} {lastname} a été un succès.",
    "add-subscriptions-success-notification": "L’ajout des abonnements a été un succès.",
		"title": "Ajouter un-e participant-e",
    "set-beneficiary": "Définir les informations du participant-e",
    "assign-card-description": "Cette carte <b>n'a pas de fonds</b> et restera vide jusqu'à la prochaine date de paiement."
	}
}
</i18n>

<template>
  <UiDialogModal v-slot="{ closeModal }" :title="t('title')" :has-footer="false" :return-route="{ name: URL_BENEFICIARY_ADMIN }">
    <UiStepper
      class="mb-6"
      :step-label="currentStep === 0 ? t('set-beneficiary') : currentStep === 1 ? t('set-subscription') : t('set-card')"
      :step-count="manageCards ? 3 : 2"
      :step-number="currentStep + 1" />
    <BeneficiaryForm
      v-if="currentStep === 0"
      :submit-btn="submitBtnLabel"
      :next-step-btn="nextStepBtnLabel"
      :organization-id="currentOrganization"
      is-new
      @submit="onBeneficiaryFormSubmit"
      @next-step="onBeneficiaryFormNextStep"
      @closeModal="closeModal" />
    <AssignSubscriptionForm
      v-else-if="currentStep === 1"
      :beneficiary-id="createBeneficiary.id"
      :organization-id="currentOrganization"
      :submit-btn="submitBtnLabel"
      :next-step-btn="nextStepBtnLabel"
      @submit="onSubscriptionFormSubmit"
      @next-step="onSubscriptionFormNextStep"
      @closeModal="closeModal" />
    <template v-else>
      <AssignCardForm
        show-number
        :beneficiary="createBeneficiary"
        :close-modal="closeModal"
        @cardAssignSuccess="onCardAssignSuccess" />
      <template v-if="cardAssignSuccess">
        <!-- eslint-disable vue/no-v-html @intlify/vue-i18n/no-v-html -->
        <p v-html="t('assign-card-description')"></p>
        <PfButtonAction :label="t('close')" class="px-8" @click="closeModal" />
      </template>
    </template>
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useMutation } from "@vue/apollo-composable";
import { ref, computed } from "vue";
import { storeToRefs } from "pinia";

import { useAuthStore } from "@/lib/store/auth";
import { useOrganizationStore } from "@/lib/store/organization";
import { useNotificationsStore } from "@/lib/store/notifications";

import { URL_BENEFICIARY_ADMIN } from "@/lib/consts/urls";
import { GLOBAL_MANAGE_CARDS } from "@/lib/consts/permissions";

import BeneficiaryForm from "@/views/beneficiary/_Form";
import AssignSubscriptionForm from "@/views/beneficiary/_AssignSubscriptionForm";
import AssignCardForm from "@/components/card/assign-card-form";

const currentStep = ref(0);
const createBeneficiary = ref(null);
const cardAssignSuccess = ref(false);

const { t } = useI18n();
const router = useRouter();
const { addSuccess } = useNotificationsStore();
const { currentOrganization } = useOrganizationStore();
const { getGlobalPermissions } = storeToRefs(useAuthStore());

const manageCards = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_CARDS);
});

const { mutate: createBeneficiaryInOrganization } = useMutation(
  gql`
    mutation CreateBeneficiaryInOrganization($input: CreateBeneficiaryInOrganizationInput!) {
      createBeneficiaryInOrganization(input: $input) {
        beneficiary {
          id
          firstname
          lastname
          notes
          address
          phone
          email
          postalCode
        }
      }
    }
  `
);

const { mutate: assignSubscriptionsToBeneficiary } = useMutation(
  gql`
    mutation AssignSubscriptionsToBeneficiary($input: AssignSubscriptionsToBeneficiaryInput!) {
      assignSubscriptionsToBeneficiary(input: $input) {
        beneficiary {
          id
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
  createBeneficiary.value = result.data.createBeneficiaryInOrganization.beneficiary;
  currentStep.value++;
  addSuccess(t("add-beneficiary-success-notification", { firstname, lastname }));
}

async function onSubscriptionFormSubmit(subscriptions) {
  await assignSubscriptionsToBeneficiary({
    input: {
      organizationId: currentOrganization,
      beneficiaryId: createBeneficiary.value.id,
      subscriptions
    }
  });
  router.push({ name: URL_BENEFICIARY_ADMIN });
  addSuccess(t("add-subscriptions-success-notification"));
}

async function onSubscriptionFormNextStep(subscriptions) {
  await assignSubscriptionsToBeneficiary({
    input: {
      organizationId: currentOrganization,
      beneficiaryId: createBeneficiary.value.id,
      subscriptions
    }
  });
  currentStep.value++;
  addSuccess(t("add-subscriptions-success-notification"));
}

const nextStepBtnLabel = computed(() => {
  switch (currentStep.value) {
    case 0:
      return t("next-step-btn-add-beneficiary");
    case 1:
      return t("next-step-btn-add-subscription");
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

function onCardAssignSuccess() {
  cardAssignSuccess.value = true;
}
</script>
