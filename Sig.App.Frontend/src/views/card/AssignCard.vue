<i18n>
{
	"en": {
		"assign-card": "Assign",
		"assign-card-success-detail": "{beneficiaryName} is now linked to card {cardAssignId}.",
		"auto-assign-card": "Assign a card automatically",
		"cancel": "Cancel",
		"card-already-assign": "The card you are trying to assign is already assigned to another participant",
    "card-lost": "The card you are trying to assign is lost",
		"card-deactivated":"The card you are trying to assign is deactivated",
    "card-already-gift-card":"The card you are trying to assign is already assigned as a gift card",
    "card-not-found": "Card ID does not exist.",
		"existing-card-id": "ID of an existing card",
		"existing-card-id-placeholder": "Ex. 98671",
		"title": "Assign card - {beneficiaryName}",
    "no-available-cards": "No available cards",
    "card-not-found-error": "QR code does not equate to a known card.",
    "retry-scan": "Retry scan",
    "manually-enter-card-btn": "or enter the number manually",
    "scan-card-btn": "or scan the card with my camera"
	},
	"fr": {
		"assign-card": "Assigner",
		"assign-card-success-detail": "{beneficiaryName} est maintenant lié à la carte {cardAssignId}.",
		"auto-assign-card": "Assigner une carte automatiquement",
		"cancel": "Annuler",
		"card-already-assign": "La carte que vous essayez d'assigner est déjà assignée à un-e autre participant-e",
    "card-lost": "La carte que vous essayez d'assigner est perdue",
		"card-deactivated":"La carte que vous essayez d'attribuer est désactivée",
    "card-already-gift-card":"La carte que vous essayez d'attribuer est déjà attribuée en tant que carte-cadeau",
    "card-not-found": "L'ID de la carte n'existe pas.",
		"existing-card-id": "ID d'une carte existante",
		"existing-card-id-placeholder": "Ex. 98671",
		"title": "Assigner une carte - {beneficiaryName}",
    "no-available-cards": "Aucune carte disponible",
    "card-not-found-error": "Le code QR n'équivaut pas à une carte connue.",
    "retry-scan": "Réessayer le scan",
    "manually-enter-card-btn": "ou entrer le numéro manuellement",
    "scan-card-btn": "ou scanner la carte avec ma caméra"
  }
}
</i18n>

<template>
  <UiDialogModal
    v-slot="{ closeModal }"
    :return-route="{ name: URL_CARDS_ASSIGNATION }"
    :title="t('title', { beneficiaryName: getBeneficiaryName })"
    :has-footer="cardAssignSuccess">
    <template v-if="cardAssignSuccess">
      <p>
        {{ t("assign-card-success-detail", { beneficiaryName: getBeneficiaryName, cardAssignId }) }}
      </p>
      <ul role="list" class="mb-0 list-disc pl-5 space-y-1">
        <li v-if="getBeneficiaryNotes() !== ''">{{ getBeneficiaryNotes() }}</li>
        <li v-if="getBeneficiaryCoordonate() !== ''">{{ getBeneficiaryCoordonate() }}</li>
      </ul>
    </template>
    <template v-else>
      <div class="inline-block">
        <PfTooltip
          v-if="project && canManageOrganizations && !project.allowOrganizationsAssignCards"
          :hide-tooltip="project.cardStats.cardsUnassigned > 0"
          class="group-pfone"
          :label="t('no-available-cards')">
          <PfButtonAction
            v-if="canManageOrganizations && !project.allowOrganizationsAssignCards"
            class="mb-5"
            type="button"
            :label="t('auto-assign-card')"
            :is-disabled="project.cardStats.cardsUnassigned === 0"
            @click="assignCardAutomatically()" />
        </PfTooltip>
      </div>
      <div v-if="showError">
        <PfNote bg-color-class="bg-red-50">
          <template #content>
            <p class="text-sm text-red-900">{{ t("card-not-found-error") }}</p>
          </template>
        </PfNote>
        <div class="flex items-center gap-x-6 flex-shrink-0 justify-end ml-auto mr-0 mt-10">
          <PfButtonAction :label="t('retry-scan')" @click="retryScan" />
          <PfButtonAction btn-style="link" :label="t('cancel')" @click="closeModal" />
        </div>
      </div>
      <div v-else-if="isScanning">
        <QRCodeScanner
          :error-url-const="URL_CARD_ERROR"
          :cancel-label="t('manually-enter-card-btn')"
          @checkQRCode="checkQRCode"
          @cancel="isScanning = false" />
      </div>
      <div v-else-if="organizations">
        <Form v-slot="{ isSubmitting }" :validation-schema="validationSchema" @submit="onSubmit">
          <PfForm
            has-footer
            can-cancel
            :submit-label="t('assign-card')"
            :cancel-label="t('cancel')"
            :processing="isSubmitting"
            :disable-submit="project.cardStats.cardsUnassigned === 0"
            :submit-tooltip-label="t('no-available-cards')"
            :hide-tooltip="project.cardStats.cardsUnassigned > 0"
            @cancel="closeModal">
            <PfFormSection>
              <Field v-slot="{ field, errors: fieldErrors }" v-model="existingCardId" name="existingCardId">
                <PfFormInputText
                  id="existingCardId"
                  v-bind="field"
                  :label="t('existing-card-id')"
                  :placeholder="t('existing-card-id-placeholder')"
                  :errors="fieldErrors"
                  input-type="number"
                  col-span-class="sm:col-span-4" />
              </Field>
              <PfButtonAction
                btn-type="button"
                btn-style="link"
                class="mr-auto"
                :label="t('scan-card-btn')"
                @click="isScanning = true" />
            </PfFormSection>
          </PfForm>
        </Form>
      </div>
    </template>
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult, useMutation, useApolloClient } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { ref, computed } from "vue";
import { string, number, object, lazy } from "yup";
import { useRoute } from "vue-router";
import { storeToRefs } from "pinia";

import QRCodeScanner from "@/components/transaction/qr-code-scanner.vue";

import { URL_CARDS_ASSIGNATION, URL_CARD_ERROR } from "@/lib/consts/urls";
import { GLOBAL_MANAGE_ORGANIZATIONS } from "@/lib/consts/permissions";

import { useGraphQLErrorMessages } from "@/lib/helpers/error-handler";

import { useAuthStore } from "@/lib/store/auth";

const { t } = useI18n();
const { getGlobalPermissions } = storeToRefs(useAuthStore());
const route = useRoute();
const { resolveClient } = useApolloClient();
const client = resolveClient();

const cardAssignSuccess = ref(false);
const cardAssignId = ref(0);
const project = ref(undefined);
const isScanning = ref(true);
const showError = ref(false);
const existingCardId = ref("");

const validationSchema = computed(() =>
  object({
    existingCardId: lazy((value) => {
      if (value === "") return string().label(t("existing-card-id")).required();
      return number().label(t("existing-card-id")).required();
    })
  })
);

useGraphQLErrorMessages({
  CARD_NOT_FOUND: () => {
    return t("card-not-found");
  },
  CARD_ALREADY_ASSIGN: () => {
    return t("card-already-assign");
  },
  CARD_LOST: () => {
    return t("card-lost");
  },
  CARD_DEACTIVATED: () => {
    return t("card-deactivated");
  },
  CARD_ALREADY_GIFT_CARD: () => {
    return t("card-already-gift-card");
  }
});

const { result: resultBeneficiary } = useQuery(
  gql`
    query Beneficiary($id: ID!) {
      beneficiary(id: $id) {
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
  `,
  {
    id: route.params.beneficiaryId
  }
);
let beneficiary = useResult(resultBeneficiary);

const { result: resultOrganizations } = useQuery(
  gql`
    query Organizations {
      organizations {
        id
        name
        project {
          id
          allowOrganizationsAssignCards
          cardStats {
            cardsAssigned
            cardsUnassigned
          }
        }
      }
    }
  `
);
const organizations = useResult(resultOrganizations, null, (data) => {
  project.value = data.organizations[0].project;
  return data.organizations;
});

const { mutate: assignCardToBeneficiary } = useMutation(
  gql`
    mutation AssignCardToBeneficiary($input: AssignCardToBeneficiaryInput!) {
      assignCardToBeneficiary(input: $input) {
        beneficiary {
          id
          card {
            id
            status
          }
        }
      }
    }
  `
);

const { mutate: assignUnassignedCardToBeneficiary } = useMutation(
  gql`
    mutation AssignUnassignedCardToBeneficiary($input: AssignUnassignedCardToBeneficiaryInput!) {
      assignUnassignedCardToBeneficiary(input: $input) {
        beneficiary {
          id
          card {
            id
            programCardId
            status
          }
        }
      }
    }
  `
);

const canManageOrganizations = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_ORGANIZATIONS);
});

async function assignCardAutomatically() {
  var result = await assignUnassignedCardToBeneficiary({
    input: {
      beneficiaryId: route.params.beneficiaryId
    }
  });
  cardAssignId.value = result.data.assignUnassignedCardToBeneficiary.beneficiary.card.programCardId;
  cardAssignSuccess.value = true;
}

const getBeneficiaryName = computed(() => {
  return beneficiary.value ? `${beneficiary.value.firstname} ${beneficiary.value.lastname}` : "";
});

function getBeneficiaryNotes() {
  return beneficiary.value && beneficiary.value.notes ? `${beneficiary.value.notes}` : "";
}

function getBeneficiaryCoordonate() {
  let result = [];

  if (beneficiary.value) {
    if (beneficiary.value.address) {
      var address = beneficiary.value.address;
      if (beneficiary.value.postalCode) {
        address += ` (${beneficiary.value.postalCode})`;
      }
      result.push(address);
    }
    if (beneficiary.value.phone) {
      result.push(beneficiary.value.phone);
    }
    if (beneficiary.value.email) {
      result.push(beneficiary.value.email);
    }
  }

  return result.join(", ");
}

async function onSubmit({ existingCardId }) {
  await assignCardToBeneficiary({
    input: {
      beneficiaryId: route.params.beneficiaryId,
      cardId: parseInt(existingCardId)
    }
  });

  cardAssignId.value = parseInt(existingCardId);
  cardAssignSuccess.value = true;
}

function retryScan() {
  isScanning.value = true;
  showError.value = false;
}

async function checkQRCode(cardId) {
  try {
    const result = await client.query({
      query: gql`
        query Card($id: ID!) {
          card(id: $id) {
            id
            programCardId
          }
        }
      `,
      variables: {
        id: cardId
      },
      errorPolicy: "ignore"
    });
    const card = result.data.card;

    if (card) {
      existingCardId.value = parseInt(card.programCardId);
      isScanning.value = false;
    }
  } catch (exception) {
    showError.value = true;
  }
}
</script>
