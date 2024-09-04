<i18n>
{
	"en": {
    "delete-text-card-number-error": "Text must match card number",
    "delete-text-card-number-label": "Type the card number to confirm",
		"description": "Warning! If you continue, the card will be available for assignment. Make sure you have found the card before marking it as found.",
    "title": "Mark card as found - {cardId}",
		"unlock-btn-label": "Unlock",
		"unlock-card-success-notification": "The card is now available for assignment."
	},
	"fr": {
    "delete-text-card-number-error": "Le texte doit correspondre au numéro de la carte",
    "delete-text-card-number-label": "Taper le numéro de la carte pour confirmer",
		"description": "Avertissement ! Si vous continuez, la carte sera disponible pour l'attribution. Assurez-vous d'avoir retrouvé la carte avant de la marquer comme retrouvée.",
		"title": "Marquer la carte comme retrouvée - {cardId}",
		"unlock-btn-label": "Marquer comme retrouvée",
		"unlock-card-success-notification": "La carte est maintenant disponible pour l'attribution."
	}
}
</i18n>

<template>
  <UiDialogDeleteModal
    :return-route="{ name: URL_CARDS }"
    :title="t('title', { cardId: getCardNumber() })"
    :description="t('description')"
    :validation-text="getCardNumber()"
    :delete-text-label="t('delete-text-card-number-label')"
    :delete-text-error="t('delete-text-card-number-error')"
    :delete-button-label="t('unlock-btn-label')"
    @onDelete="onUnlockCard" />
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_CARDS } from "@/lib/consts/urls";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { addSuccess } = useNotificationsStore();

const { result } = useQuery(
  gql`
    query Card($id: ID!) {
      card(id: $id) {
        id
        cardNumber
        beneficiary {
          id
          firstname
          lastname
        }
      }
    }
  `,
  {
    id: route.params.cardId
  }
);
const card = useResult(result);

const { mutate: unlockCard } = useMutation(
  gql`
    mutation UnlockCard($input: UnlockCardInput!) {
      unlockCard(input: $input) {
        card {
          id
          status
        }
      }
    }
  `
);

function getCardNumber() {
  if (!card.value) return "";
  return card.value.cardNumber.replaceAll("-", " ");
}

async function onUnlockCard() {
  await unlockCard({
    input: {
      cardId: route.params.cardId
    }
  });

  addSuccess(t("unlock-card-success-notification"));
  router.push({ name: URL_CARDS });
}
</script>
