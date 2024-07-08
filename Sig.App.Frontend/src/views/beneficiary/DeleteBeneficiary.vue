<i18n>
{
	"en": {
		"delete-beneficiary-success-notification": "The deletion was successful.",
		"delete-text-error": "Text must match participant's first and last name",
		"delete-text-label": "Type the participant's name to confirm",
		"description": "Warning ! The deletion of <strong>{beneficiaryName}</strong> cannot be undone. If you continue, the participant and all their data will be permanently deleted.",
		"title": "Supprimer - {beneficiaryName}",
    "beneficiary-cant-have-active-subscription-error-notification": "The participant cannot be deleted because they have an active subscription."
	},
	"fr": {
		"delete-beneficiary-success-notification": "La supression a été effectuée avec succès.",
		"delete-text-error": "Le texte doit correspondre au prénom et au nom de famille du-de la participant-e",
		"delete-text-label": "Taper le nom du-de la participant-e pour confirmer",
		"description": "Avertissement ! La suppression de <strong>{beneficiaryName}</strong> ne peut pas être annulée. Si vous continuez, le participant ou la participante ainsi que toutes ses données seront supprimé-e-s de façon définitive.",
		"title": "Supprimer - {beneficiaryName}",
    "beneficiary-cant-have-active-subscription-error-notification": "Le participant ne peut pas être supprimé car il a un abonnement actif."
	}
}
</i18n>

<template>
  <UiDialogDeleteModal
    :return-route="{ name: URL_BENEFICIARY_ADMIN }"
    :title="t('title', { beneficiaryName: getBeneficiaryName() })"
    :description="t('description', { beneficiaryName: getBeneficiaryName() })"
    :validation-text="getBeneficiaryName()"
    :delete-text-label="t('delete-text-label')"
    :delete-text-error="t('delete-text-error')"
    @onDelete="deleteProject" />
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";

import { useGraphQLErrorMessages } from "@/lib/helpers/error-handler";
import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_BENEFICIARY_ADMIN } from "@/lib/consts/urls";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { addSuccess } = useNotificationsStore();

// Configure les messages en lien avec les erreurs graphql susceptibles d'être lancées par ce composant
useGraphQLErrorMessages({
  // Ce code est lancé quand le mot de passe est invalid
  BENEFICIARY_CANT_HAVE_ACTIVE_SUBSCRIPTION: () => {
    return t("beneficiary-cant-have-active-subscription-error-notification");
  }
});

const { result: resultProject } = useQuery(
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
const beneficiary = useResult(resultProject);

const { mutate: deleteBeneficiaryMutation } = useMutation(
  gql`
    mutation DeleteBeneficiary($input: DeleteBeneficiaryInput!) {
      deleteBeneficiary(input: $input)
    }
  `
);

function getBeneficiaryName() {
  return beneficiary.value ? `${beneficiary.value.firstname} ${beneficiary.value.lastname}` : "";
}

async function deleteProject() {
  await deleteBeneficiaryMutation({
    input: {
      beneficiaryId: route.params.beneficiaryId
    }
  });

  addSuccess(
    t("delete-beneficiary-success-notification", {
      beneficiaryName: `${beneficiary.value.firstname} ${beneficiary.value.lastname}`
    })
  );
  router.push({ name: URL_BENEFICIARY_ADMIN });
}
</script>
