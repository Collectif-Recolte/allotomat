<i18n>
{
	"en": {
		"delete-text-beneficiary-name-error": "Text must match recipient's first name and recipient's last name",
    "delete-text-card-number-error": "Text must match card number",
		"delete-text-beneficiary-name-label": "Type the participant name to confirm",
    "delete-text-card-number-label": "Type the card number to confirm",
		"description": "Warning ! If you continue, the card will be enabled and the participant will be able to use the card.",
		"title": "Enable card - {beneficiaryName}",
		"enable-btn-label": "Enable",
		"enable-card-success-notification": "The card has been successfully enabled."
	},
	"fr": {
		"delete-text-beneficiary-name-error": "Le texte doit correspondre au prénom et au nom de famille du-de la participant-e",
    "delete-text-card-number-error": "Le texte doit correspondre au numéro de la carte",
		"delete-text-beneficiary-name-label": "Taper le nom du-de la participant-e pour confirmer",
    "delete-text-card-number-label": "Taper le numéro de la carte pour confirmer",
		"description": "Avertissement ! Si vous continuez, la carte sera réactivée et le-la participant-e ne pourra plus utiliser la carte.",
		"title": "Activer la carte - {beneficiaryName}",
		"enable-btn-label": "Activer",
		"enable-card-success-notification": "La carte a été réactivée avec succès."
	}
}
</i18n>

<template>
  <UiDialogDeleteModal
    :return-route="returnRoute()"
    :title="t('title', { beneficiaryName: getBeneficiaryName() })"
    :description="t('description', { beneficiaryName: getBeneficiaryName() })"
    :validation-text="getBeneficiaryName()"
    :delete-text-label="deleteTextLabel"
    :delete-text-error="deleteTextError"
    :delete-button-label="t('enable-btn-label')"
    @onDelete="onEnableCard" />
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { computed } from "vue";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_CARDS, URL_BENEFICIARY_ADMIN, URL_BENEFICIARY_CARD_ENABLE } from "@/lib/consts/urls";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { addSuccess } = useNotificationsStore();

const deleteTextLabel = computed(() => {
  return card.value
    ? card.value.beneficiary
      ? t("delete-text-beneficiary-name-label")
      : t("delete-text-card-number-label")
    : "";
});

const deleteTextError = computed(() => {
  return card.value
    ? card.value.beneficiary
      ? t("delete-text-beneficiary-name-error")
      : t("delete-text-card-number-error")
    : "";
});

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

const { mutate: enableCard } = useMutation(
  gql`
    mutation EnableCard($input: EnableCardInput!) {
      enableCard(input: $input) {
        card {
          id
          isDisabled
        }
      }
    }
  `
);

function getBeneficiaryName() {
  return card.value
    ? card.value.beneficiary
      ? `${card.value.beneficiary.firstname} ${card.value.beneficiary.lastname}`
      : card.value.cardNumber.replaceAll("-", " ")
    : "";
}

async function onEnableCard() {
  await enableCard({
    input: {
      cardId: route.params.cardId
    }
  });

  addSuccess(t("enable-card-success-notification"));
  router.push(returnRoute());
}

function returnRoute() {
  if (route.name === URL_BENEFICIARY_CARD_ENABLE) return { name: URL_BENEFICIARY_ADMIN };
  else return { name: URL_CARDS };
}
</script>
