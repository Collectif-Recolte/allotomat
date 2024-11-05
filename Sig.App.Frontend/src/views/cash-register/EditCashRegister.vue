<i18n>
  {
    "en": {
      "title": "Edit",
      "edit-cash-register": "Save",
      "edit-cash-register-success-notification": "Edition of cash register {name} was successful."
    },
    "fr": {
      "title": "Modifier",
      "edit-cash-register": "Sauvegarder",
      "edit-cash-register-success-notification": "L’édition de la caisse {name} a été un succès.",
    }
  }
</i18n>

<template>
  <UiDialogModal v-slot="{ closeModal }" :title="t('title')" :has-footer="false" :return-route="{ name: URL_CASH_REGISTER }">
    <CashRegisterForm
      v-if="cashRegister"
      :submit-btn="t('edit-cash-register')"
      :name="cashRegister.name"
      :market-groups="cashRegister.marketGroups"
      @submit="onSubmit"
      @closeModal="closeModal" />
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { useRouter, useRoute } from "vue-router";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";

import { useNotificationsStore } from "@/lib/store/notifications";

import { URL_CASH_REGISTER } from "@/lib/consts/urls";

import CashRegisterForm from "@/views/cash-register/_Form";

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
const { addSuccess } = useNotificationsStore();

const { result } = useQuery(
  gql`
    query CashRegister($id: ID!) {
      cashRegister(id: $id) {
        id
        name
        marketGroups {
          id
          name
          project {
            id
            name
          }
        }
      }
    }
  `,
  {
    id: route.params.cashRegisterId
  }
);
let cashRegister = useResult(result);

const { mutate: editCashRegister } = useMutation(
  gql`
    mutation EditCashRegister($input: EditCashRegisterInput!) {
      editCashRegister(input: $input) {
        cashRegister {
          id
          name
        }
      }
    }
  `
);

async function onSubmit({ name }) {
  await editCashRegister({
    input: {
      cashRegisterId: route.params.cashRegisterId,
      name: { value: name }
    }
  });
  router.push({ name: URL_CASH_REGISTER });
  addSuccess(t("edit-cash-register-success-notification", { name }));
}
</script>
