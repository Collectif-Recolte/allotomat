<i18n>
  {
    "en": {
      "title": "Add a cash register to a program",
      "add-cash-register": "Add",
      "add-cash-register-success-notification": "Successfully added cash register {cashRegisterName} to program {projectName}."
    },
    "fr": {
      "title": "Ajouter une caisse à un programme",
      "add-cash-register": "Ajouter",
      "add-cash-register-success-notification": "L'ajout de la caisse {cashRegisterName} au programme {projectName} a été un succès."
    }
  }
</i18n>

<template>
  <UiDialogModal v-slot="{ closeModal }" :title="t('title')" :has-footer="false" :return-route="{ name: URL_CASH_REGISTER }">
    <CashRegisterForm
      v-if="cashRegister"
      :submit-btn="t('add-cash-register')"
      is-add-project
      :name="cashRegister.name"
      :market="cashRegister.market"
      :market-groups="cashRegister.marketGroups"
      @submit="onSubmit"
      @closeModal="closeModal" />
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

const { result } = useQuery(
  gql`
    query CashRegister($id: ID!) {
      cashRegister(id: $id) {
        id
        name
        market {
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

const { mutate: addCashRegisterToMarketGroup } = useMutation(
  gql`
    mutation AddCashRegisterToMarketGroup($input: AddCashRegisterToMarketGroupInput!) {
      addCashRegisterToMarketGroup(input: $input) {
        cashRegister {
          id
          name
        }
      }
    }
  `
);

async function onSubmit({ selectedMarketGroup, selectedProject }) {
  await addCashRegisterToMarketGroup({
    input: {
      cashRegisterId: cashRegister.value.id,
      marketGroupId: selectedMarketGroup
    }
  });
  router.push({ name: URL_CASH_REGISTER });
  addSuccess(
    t(
      "add-cash-register-success-notification",
      { cashRegisterName: cashRegister.value.name },
      { projectName: cashRegister.value.market.projects.find((x) => x.id === selectedProject).name }
    )
  );
}
</script>
