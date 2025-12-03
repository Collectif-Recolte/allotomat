<i18n>
  {
    "en": {
      "title": "Transfer a lost or damaged card",
      "lost-card": "Lost card",
      "new-card": "New card",
      "warning-message": "All subscriptions and gift card funds will be transferred to the new card.",
      "transfer-funds": "Transfer funds",
      "cancel": "Cancel",
      "confirm-title": "Everything is transferred!",
      "confirm-desc": "The card #{cardNumber} is now active and ready to use.",
      "close": "Close",
      "original-card-not-found": "The lost card was not assigned to a participant.",
      "original-card-not-assign": "Lost card ID does not exist.",
      "new-card-not-found": "New card ID does not exist.",
      "new-card-already-assign": "The card you are trying to assign is already assigned to another participant.",
      "new-card-already-lost": "The card you are trying to assign is already lost or damaged.",
      "new-card-already-gift-card": "The card you are trying to assign is a gift card."
    },
    "fr": {
      "title": "Transférer une carte perdue ou endommagée",
      "lost-card": "Carte perdue",
      "new-card": "Nouvelle carte",
      "warning-message": "Tous les abonnements et les fonds de carte-cadeau seront transférés vers la nouvelle carte.",
      "transfer-funds": "Transférer les fonds",
      "cancel": "Annuler",
      "confirm-title": "Tout est transféré!",
      "confirm-desc": "La carte #{cardNumber} est maintenant active et prête à l'emploi.",
      "close": "Fermer",
      "original-card-not-found": "L'ID de la carte perdue n'existe pas.",
      "original-card-not-assign": "La carte perdue n'est pas assignée à un-e participant-e.",
      "new-card-not-found": "L'ID de la nouvelle carte n'existe pas.",
      "new-card-already-assign": "La carte que vous essayez d'assigner est déjà assignée à un-e autre participant-e.",
      "new-card-already-lost": "La carte que vous essayez d'assigner est déjà perdue ou endommagée.",
      "new-card-already-gift-card": "La carte que vous essayez d'assigner est une carte-cadeau."
    }
  }
  </i18n>

<template>
  <UiDialogModal v-if="isInEdition" :return-route="returnRoute()" :title="t('title')" :has-footer="false">
    <template #default="{ closeModal }">
      <Form v-slot="{ isSubmitting }" :validation-schema="validationSchema" @submit="onSubmit">
        <PfForm
          has-footer
          can-cancel
          :submit-label="t('transfer-funds')"
          :cancel-label="t('cancel')"
          :processing="isSubmitting"
          @cancel="closeModal">
          <PfFormSection is-grid>
            <PfFormInputText
              v-if="lostCard"
              id="lostCardNumber"
              :model-value="lostCard.programCardId"
              :label="t('lost-card')"
              input-type="number"
              col-span-class="sm:col-span-6"
              is-read-only />
            <Field v-slot="{ field, errors: fieldErrors }" name="newCardNumber">
              <PfFormInputText
                id="newCardNumber"
                :model-value="field.value"
                :label="t('new-card')"
                :errors="fieldErrors"
                input-type="number"
                col-span-class="sm:col-span-6"
                @update:modelValue="field.onChange" />
            </Field>
          </PfFormSection>

          <template #warning>
            <p class="text-p4 leading-tight mb-0">{{ t("warning-message") }}</p>
          </template>
        </PfForm>
      </Form>
    </template>
  </UiDialogModal>
  <UiDialogConfirmModal
    v-else
    :title="t('confirm-title')"
    :description="t('confirm-desc', { cardNumber: newCardAssignedNumber })"
    :confirm-button-label="t('close')"
    :confirm-route="returnRoute()" />
</template>

<script setup>
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { ref, computed } from "vue";
import { string, number, object, lazy } from "yup";
import { useRoute } from "vue-router";
import { useMutation, useResult, useQuery } from "@vue/apollo-composable";
import { useGraphQLErrorMessages } from "@/lib/helpers/error-handler";

import { URL_CARDS, URL_BENEFICIARY_ADMIN, URL_CARDS_LOST } from "@/lib/consts/urls";

const { t } = useI18n();
const route = useRoute();
const isInEdition = ref(true);
const newCardAssignedNumber = ref(null);

useGraphQLErrorMessages({
  ORIGINAL_CARD_NOT_FOUND: () => {
    return t("original-card-not-found");
  },
  ORIGINAL_CARD_NOT_ASSIGN: () => {
    return t("original-card-not-assign");
  },
  NEW_CARD_NOT_FOUND: () => {
    return t("new-card-not-found");
  },
  NEW_CARD_ALREADY_ASSIGN: () => {
    return t("new-card-already-assign");
  },
  NEW_CARD_ALREADY_GIFT_CARD: () => {
    return t("new-card-already-gift-card");
  },
  NEW_CARD_ALREADY_LOST: () => {
    return t("new-card-already-lost");
  }
});

const validationSchema = computed(() =>
  object({
    newCardNumber: lazy((value) => {
      if (value === "") return string().label(t("new-card")).required();
      return number().label(t("new-card")).required();
    })
  })
);

const { result } = useQuery(
  gql`
    query Card($id: ID!) {
      card(id: $id) {
        id
        programCardId
      }
    }
  `,
  {
    id: route.params.cardId
  }
);
const lostCard = useResult(result, null, (data) => data.card);

const { mutate: transfertCard } = useMutation(
  gql`
    mutation TransfertCard($input: TransfertCardInput!) {
      transfertCard(input: $input) {
        card {
          id
          programCardId
        }
      }
    }
  `
);

async function onSubmit({ newCardNumber }) {
  var result = await transfertCard({
    input: {
      newCardId: parseInt(newCardNumber),
      originalCardId: route.params.cardId
    }
  });

  newCardAssignedNumber.value = result.data.transfertCard.card.programCardId;
  isInEdition.value = false;
}

function returnRoute() {
  if (route.name === URL_CARDS_LOST) return { name: URL_CARDS };
  else return { name: URL_BENEFICIARY_ADMIN };
}
</script>
