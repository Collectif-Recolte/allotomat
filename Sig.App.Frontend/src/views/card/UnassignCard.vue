<i18n>
{
	"en": {
		"delete-text-error": "Text must match participant's first and last name.",
		"delete-text-label": "Type the participant's full name to confirm",
    "delete-text-anonymous-label": "Type the participant's ID1 to confirm",
    "delete-text-anonymous-error": "Text must match participant's ID1",
		"description": "Warning! Unassigning the card currently belonging to <strong>{beneficiaryName}</strong> cannot be undone. If you continue, the card will be unassigned and the card funds will be removed. Payments to the card can no longer be traced.",
		"title": "Unassign Card - {beneficiaryName}",
		"unassign-btn-label": "Unassign",
		"unassign-card-success-notification": "The card has been successfully unassigned."
	},
	"fr": {
		"delete-text-error": "Le texte doit correspondre au prénom et au nom de famille du ou de la participant·e",
		"delete-text-label": "Taper le nom complet du ou de la participant·e pour confirmer",
    "delete-text-anonymous-label": "Taper le ID1 du participant·e pour confirmer",
    "delete-text-anonymous-error": "Le texte doit correspondre au ID1 du participant·e",
		"description": "Avertissement ! La désassignation de la carte de <strong>{beneficiaryName}</strong> ne peut pas être annulée. Si vous continuez, la carte sera désassignée et les fonds de la carte vont être supprimé. Les paiements sur la carte ne pourront plus être retracés.",
		"title": "Désassigner la carte - {beneficiaryName}",
		"unassign-btn-label": "Désassigner",
		"unassign-card-success-notification": "La carte a été désassignée avec succès."
	}
}
</i18n>

<template>
  <UiDialogDeleteModal :return-route="returnRoute()" :title="t('title', { beneficiaryName: getBeneficiaryName() })"
    :description="t('description', { beneficiaryName: getBeneficiaryName() })" :validation-text="getBeneficiaryName()"
    :delete-text-label="deleteTextLabel" :delete-text-error="deleteTextError"
    :delete-button-label="t('unassign-btn-label')" @onDelete="unassignCard" />
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { computed } from "vue";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_CARDS, URL_BENEFICIARY_ADMIN, URL_CARDS_UNASSIGN } from "@/lib/consts/urls";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { addSuccess } = useNotificationsStore();

const deleteTextLabel = computed(() => {
  if (!beneficiary.value) return "";
  return beneficiary.value?.organization?.project?.beneficiariesAreAnonymous
    ? t("delete-text-anonymous-label")
    : t("delete-text-label");
});

const deleteTextError = computed(() => {
  if (!beneficiary.value) return "";
  return beneficiary.value?.organization?.project?.beneficiariesAreAnonymous
    ? t("delete-text-anonymous-error")
    : t("delete-text-error");
});

const { result } = useQuery(
  gql`
    query Beneficiary($id: ID!) {
      beneficiary(id: $id) {
        id
        firstname
        lastname
        id1
        organization {
          id
          project {
            id
            beneficiariesAreAnonymous
          }
        }
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
  if (!beneficiary.value) return "";

  if (beneficiary.value.organization.project.beneficiariesAreAnonymous) {
    return beneficiary.value.id1;
  }
  return `${beneficiary.value.firstname} ${beneficiary.value.lastname}`;
}

async function unassignCard() {
  await unassignCardFromBeneficiary({
    input: {
      beneficiaryId: route.params.beneficiaryId,
      cardId: route.params.cardId
    }
  });

  addSuccess(t("unassign-card-success-notification", { beneficiaryName: getBeneficiaryName() }));
  router.push(returnRoute());
}

function returnRoute() {
  if (route.name === URL_CARDS_UNASSIGN) return { name: URL_CARDS };
  else return { name: URL_BENEFICIARY_ADMIN };
}
</script>
