<i18n>
{
	"en": {
		"archive-cash-register-success-notification": "The cash register {cashRegisterName} has been successfully archived.",
		"archive-text-error": "The text must match the name of the cash register",
		"archive-text-label": "Type the name of the cash register to confirm",
		"description": "Warning ! The archive of <strong>{cashRegisterName}</strong> cannot be undone. If you continue, the cash register will be permanently archived along with all the elements it contains. However, the transaction of the cash register will remain unchanged.",
		"title": "Archive - {cashRegisterName}",
    "archive-btn-label": "Archive"
	},
	"fr": {
		"archive-cash-register-success-notification": "La caisse {cashRegisterName} a été archivée avec succès.",
		"archive-text-error": "Le texte doit correspondre au nom de la caisse",
		"archive-text-label": "Taper le nom de la caisse pour confirmer",
		"description": "Avertissement ! L'archivage de <strong>{cashRegisterName}</strong> ne peut pas être annulé. Si vous continuez, la caisse sera archivé ainsi que tous les éléments qu'il contient de façon définitive. Par contre, les transactions associés à la caisse vont rester inchangés.",
		"title": "Archiver - {cashRegisterName}",
    "archive-btn-label": "Archiver"
	}
}
</i18n>

<template>
  <UiDialogDeleteModal
    :return-route="returnRoute()"
    :title="t('title', { cashRegisterName: getCashRegisterName() })"
    :description="t('description', { cashRegisterName: getCashRegisterName() })"
    :validation-text="getCashRegisterName()"
    :delete-text-label="t('archive-text-label')"
    :delete-text-error="t('archive-text-error')"
    :delete-button-label="t('archive-btn-label')"
    @onDelete="archiveCashRegister" />
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";

import { useNotificationsStore } from "@/lib/store/notifications";
import { URL_CASH_REGISTER } from "@/lib/consts/urls";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { addSuccess } = useNotificationsStore();

const { result } = useQuery(
  gql`
    query CashRegister($id: ID!) {
      cashRegister(id: $id) {
        id
        name
      }
    }
  `,
  {
    id: route.params.cashRegisterId
  }
);
const cashRegister = useResult(result);

const { mutate: archiveCashRegisterMutation } = useMutation(
  gql`
    mutation ArchiveCashRegister($input: ArchiveCashRegisterInput!) {
      archiveCashRegister(input: $input)
    }
  `
);

function getCashRegisterName() {
  return cashRegister.value ? cashRegister.value.name : "";
}

async function archiveCashRegister() {
  await archiveCashRegisterMutation({
    input: {
      cashRegisterId: route.params.cashRegisterId
    }
  });

  addSuccess(t("archive-cash-register-success-notification", { cashRegisterName: cashRegister.value.name }));
  router.push(returnRoute());
}

function returnRoute() {
  return { name: URL_CASH_REGISTER };
}
</script>
