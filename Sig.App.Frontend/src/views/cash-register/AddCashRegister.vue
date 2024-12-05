<i18n>
  {
    "en": {
      "title": "Add Cash Register",
      "add-cash-register": "Add",
      "add-cash-register-success-notification": "Adding cash register {name} was successful."
    },
    "fr": {
      "title": "Ajouter une caisse",
      "add-cash-register": "Ajouter",
      "add-cash-register-success-notification": "L'ajout de la caisse {name} a été un succès."
    }
  }
</i18n>

<template>
  <UiDialogModal
    v-if="!loading"
    v-slot="{ closeModal }"
    :title="t('title')"
    :has-footer="false"
    :return-route="{ name: URL_CASH_REGISTER }">
    <CashRegisterForm :submit-btn="t('add-cash-register')" is-new :market="market" @submit="onSubmit" @closeModal="closeModal" />
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useRouter, useRoute } from "vue-router";

import { useNotificationsStore } from "@/lib/store/notifications";

import { URL_CASH_REGISTER } from "@/lib/consts/urls";

import CashRegisterForm from "@/views/cash-register/_Form";

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
const { addSuccess } = useNotificationsStore();

const { result, loading } = useQuery(
  gql`
    query Market($id: ID!) {
      market(id: $id) {
        id
        projects {
          id
          name
          marketGroups {
            id
            name
            isArchived
          }
        }
      }
    }
  `,
  {
    id: route.query.marketId
  }
);
let market = useResult(result);

const { mutate: addCashRegister } = useMutation(
  gql`
    mutation CreateCashRegister($input: CreateCashRegisterInput!) {
      createCashRegister(input: $input) {
        cashRegister {
          id
          name
        }
      }
    }
  `
);

async function onSubmit({ name, selectedMarketGroup }) {
  await addCashRegister({
    input: {
      name,
      marketId: route.query.marketId,
      marketGroupId: selectedMarketGroup
    }
  });
  router.push({ name: URL_CASH_REGISTER });
  addSuccess(t("add-cash-register-success-notification", { name }));
}
</script>
