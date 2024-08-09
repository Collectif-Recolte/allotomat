<i18n>
{
  "en": {
      "assign-card-success-detail": "{beneficiaryName} is now linked to card {cardAssignId}.",
      "assign-card": "Assign",
      "cancel": "Cancel",
      "card-deactivated":"The card you are trying to assign is deactivated",
      "card-already-gift-card":"The card you are trying to assign is already assigned as a gift card",
      "card-not-found": "Card ID does not exist.",
      "existing-card-id": "ID of an existing card",
      "existing-card-id-placeholder": "Ex. 98671",
      "no-available-cards": "No available cards",
      "auto-assign-card": "Assign a card automatically",
      "card-already-assign": "The card you are trying to assign is already assigned to another participant",
      "card-lost": "The card you are trying to assign is lost",
      "card-not-found-error": "QR code does not equate to a known card.",
      "retry-scan": "Retry scan",
      "manually-enter-card-btn": "or enter the number manually",
      "scan-card-btn": "or scan the card with my camera"
  },
  "fr": {
    "assign-card-success-detail": "{beneficiaryName} est maintenant lié à la carte {cardAssignId}.",
    "assign-card": "Assigner",
    "cancel": "Annuler",
		"card-deactivated":"La carte que vous essayez d'attribuer est désactivée",
    "card-already-gift-card":"La carte que vous essayez d'attribuer est déjà attribuée en tant que carte-cadeau",
    "card-not-found": "L'ID de la carte n'existe pas.",
		"existing-card-id": "ID d'une carte existante",
		"existing-card-id-placeholder": "Ex. 98671",
    "no-available-cards": "Aucune carte disponible",
    "auto-assign-card": "Assigner une carte automatiquement",
    "card-already-assign": "La carte que vous essayez d'assigner est déjà assignée à un-e autre participant-e",
    "card-lost": "La carte que vous essayez d'assigner est perdue",
    "card-not-found-error": "Le code QR n'équivaut pas à une carte connue.",
    "retry-scan": "Réessayer le scan",
    "manually-enter-card-btn": "ou entrer le numéro manuellement",
    "scan-card-btn": "ou scanner la carte avec ma caméra"
  }
}
</i18n>

<template>
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
    <div v-if="showError">
      <PfNote bg-color-class="bg-red-50">
        <template #content>
          <p class="text-sm text-red-900">{{ t("card-not-found-error") }}</p>
        </template>
      </PfNote>
      <div class="flex items-center gap-x-6 flex-shrink-0 justify-end ml-auto mr-0 mt-10">
        <PfButtonAction :label="t('retry-scan')" @click="retryScan" />
        <PfButtonAction btn-style="link" :label="t('cancel')" @click="props.closeModal" />
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
          @cancel="props.closeModal">
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
</template>

<script setup>
import gql from "graphql-tag";
import { computed, defineProps, ref, defineEmits } from "vue";
import { string, number, object, lazy } from "yup";
import { useQuery, useMutation, useResult, useApolloClient } from "@vue/apollo-composable";
import { storeToRefs } from "pinia";
import { useI18n } from "vue-i18n";

import { GLOBAL_MANAGE_ORGANIZATIONS } from "@/lib/consts/permissions";
import { URL_CARD_ERROR } from "@/lib/consts/urls";

import { useAuthStore } from "@/lib/store/auth";

import QRCodeScanner from "@/components/transaction/qr-code-scanner.vue";

import { useGraphQLErrorMessages } from "@/lib/helpers/error-handler";

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

const emit = defineEmits(["cardAssignSuccess"]);

const { getGlobalPermissions } = storeToRefs(useAuthStore());
const { t } = useI18n();
const { resolveClient } = useApolloClient();
const client = resolveClient();

const cardAssignSuccess = ref(false);
const cardAssignId = ref(0);
const project = ref(undefined);
const isScanning = ref(true);
const showError = ref(false);
const existingCardId = ref("");

const props = defineProps({
  closeModal: {
    type: Function,
    required: true
  },
  beneficiary: {
    type: Object,
    required: true
  }
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

const canManageOrganizations = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_ORGANIZATIONS);
});

const getBeneficiaryName = computed(() => {
  return props.beneficiary ? `${props.beneficiary.firstname} ${props.beneficiary.lastname}` : "";
});

function getBeneficiaryNotes() {
  return props.beneficiary && props.beneficiary.notes ? `${props.beneficiary.notes}` : "";
}

function getBeneficiaryCoordonate() {
  let result = [];

  if (props.beneficiary) {
    if (props.beneficiary.address) {
      var address = props.beneficiary.address;
      if (props.beneficiary.postalCode) {
        address += ` (${props.beneficiary.postalCode})`;
      }
      result.push(address);
    }
    if (props.beneficiary.phone) {
      result.push(props.beneficiary.phone);
    }
    if (props.beneficiary.email) {
      result.push(props.beneficiary.email);
    }
  }

  return result.join(", ");
}

const validationSchema = computed(() =>
  object({
    existingCardId: lazy((value) => {
      if (value === "") return string().label(t("existing-card-id")).required();
      return number().label(t("existing-card-id")).required();
    })
  })
);

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

async function onSubmit({ existingCardId }) {
  await assignCardToBeneficiary({
    input: {
      beneficiaryId: props.beneficiary.id,
      cardId: parseInt(existingCardId)
    }
  });

  cardAssignId.value = parseInt(existingCardId);
  cardAssignSuccess.value = true;
  emit("cardAssignSuccess");
}

async function assignCardAutomatically() {
  var result = await assignUnassignedCardToBeneficiary({
    input: {
      beneficiaryId: props.beneficiary.id
    }
  });
  cardAssignId.value = result.data.assignUnassignedCardToBeneficiary.beneficiary.card.programCardId;
  cardAssignSuccess.value = true;
  emit("cardAssignSuccess");
}
</script>
