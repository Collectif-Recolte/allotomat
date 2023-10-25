<i18n>
{
	"en": {
		"delete-text-error": "Text must match recipient's first name and recipient's last name",
		"delete-text-label": "Type the participant name to confirm",
		"description": "Warning ! The unassignation of <strong>{beneficiaryName}</strong> card cannot be undone. If you continue, the card will be unassigned and the card funds will be removed. Payments to the card can no longer be traced.",
		"title": "Unassign card - {beneficiaryName}",
		"unassign-btn-label": "Unassign",
		"unassign-card-success-notification": "The card has been successfully unassigned."
	},
	"fr": {
		"delete-text-error": "Le texte doit correspondre au prénom et au nom de famille du-de la participant-e",
		"delete-text-label": "Taper le nom du-de la participant-e pour confirmer",
		"description": "Avertissement ! La désassignation de la carte de <strong>{beneficiaryName}</strong> ne peut pas être annulée. Si vous continuez, la carte sera désassignée et les fonds de la carte vont être supprimé. Les paiements sur la carte ne pourront plus être retracés.",
		"title": "Désassigner la carte - {beneficiaryName}",
		"unassign-btn-label": "Désassigner",
		"unassign-card-success-notification": "La carte a été désassignée avec succès."
	}
}
</i18n>

<template>
  <UiDialogDeleteModal
    :return-route="{ name: URL_CARDS_SUMMARY }"
    :title="t('title', { beneficiaryName: getBeneficiaryName() })"
    :description="t('description', { beneficiaryName: getBeneficiaryName() })"
    :validation-text="getBeneficiaryName()"
    :delete-text-label="t('delete-text-label')"
    :delete-text-error="t('delete-text-error')"
    :delete-button-label="t('unassign-btn-label')"
    @onDelete="unassignCard" />
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_CARDS_SUMMARY } from "@/lib/consts/urls";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { addSuccess } = useNotificationsStore();

const { result } = useQuery(
  gql`
    query Beneficiary($id: ID!) {
      beneficiary(id: $id) {
        id
        firstname
        lastname
      }
    }
  `,
  {
    id: route.params.beneficiaryId
  }
);
const beneficiary = useResult(result);

const { mutate: unassignCardFromBeneficiary } = useMutation(
  gql`
    mutation UnassignCardFromBeneficiary($input: UnassignCardFromBeneficiaryInput!) {
      unassignCardFromBeneficiary(input: $input) {
        beneficiary {
          id
        }
      }
    }
  `
);

function getBeneficiaryName() {
  return beneficiary.value ? `${beneficiary.value.firstname} ${beneficiary.value.lastname}` : "";
}

async function unassignCard() {
  await unassignCardFromBeneficiary({
    input: {
      beneficiaryId: route.params.beneficiaryId,
      cardId: route.params.cardId
    }
  });

  addSuccess(
    t("unassign-card-success-notification", { beneficiaryName: `${beneficiary.value.firstname} ${beneficiary.value.lastname}` })
  );
  router.push({ name: URL_CARDS_SUMMARY });
}
</script>
