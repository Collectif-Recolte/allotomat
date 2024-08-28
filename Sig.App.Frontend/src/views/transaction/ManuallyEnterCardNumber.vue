<i18n>
  {
    "en": {
      "sub-title": "Purchase on behalf of program",
      "select-market": "Merchant",
      "card-number": "Card number",
      "card-number-description": "Card number must be of format \"1234 5678 9012 3456\"",
      "card-number-error": "Card number must be of format \"1234 5678 9012 3456\"",
      "choose-market": "Select",
      "next-step": "Next",
      "cancel": "Cancel",
      "card-number-invalid": "Card number does not match any card linked to the program.",
      "transaction-in-program-name": "Purchase on behalf of program"
    },
    "fr": {
      "sub-title": "Achat au nom du programme",
      "select-market": "Marchand",
      "card-number": "N° de carte",
      "card-number-description": "Le numéro de carte doit être de format \"1234 5678 9012 3456\"",
      "card-number-error": "Le numéro de carte doit être de format \"1234 5678 9012 3456\"",
      "choose-market": "Sélectionner",
      "next-step": "Suivant",
      "cancel": "Annuler",
      "card-number-invalid": "Le numéro de carte ne correspond à aucune carte reliée au programme.",
      "transaction-in-program-name": "Achat au nom du programme"
    }
  }
  </i18n>

<template>
  <p class="text-p1">
    {{ t("transaction-in-program-name") }}
  </p>
  <Form v-slot="{ errors: formErrors }" :validation-schema="validationSchema" keep-values @submit="nextStep">
    <PfForm
      has-footer
      :disable-submit="Object.keys(formErrors).length > 0"
      :submit-label="t('next-step')"
      :cancel-label="t('cancel')"
      footer-alt-style
      can-cancel
      @cancel="closeModal">
      <PfFormSection>
        <Field v-slot="{ field: inputField, errors: fieldErrors }" name="marketId">
          <PfFormInputSelect
            id="marketId"
            v-bind="inputField"
            :placeholder="t('choose-market')"
            :label="t('select-market')"
            :options="markets"
            :errors="fieldErrors" />
        </Field>
        <div>
          <Field v-slot="{ field: inputField, errors: fieldErrors }" name="cardNumber">
            <PfFormInputText
              id="cardNumber"
              v-bind="inputField"
              :description="t('card-number-description')"
              :label="t('card-number')"
              :errors="fieldErrors" />
          </Field>
        </div>
      </PfFormSection>
    </PfForm>
  </Form>
</template>

<script setup>
import gql from "graphql-tag";
import { computed, defineEmits } from "vue";
import { useI18n } from "vue-i18n";
import { string, object } from "yup";
import { useQuery, useResult, useApolloClient } from "@vue/apollo-composable";

import { TRANSACTION_STEPS_ADD } from "@/lib/consts/enums";

import { useNotificationsStore } from "@/lib/store/notifications";

const { t } = useI18n();
const { resolveClient } = useApolloClient();

const client = resolveClient();

const { addError } = useNotificationsStore();

const emit = defineEmits(["onUpdateStep", "onCloseModal"]);

const validationSchema = computed(() =>
  object({
    marketId: string().label(t("select-market")).required(),
    cardNumber: string()
      .label(t("card-number"))
      .test({
        name: "tomatCardNumber",
        exclusive: false,
        params: {},
        message: t("card-number-error"),
        test: function (value) {
          if (value === undefined) {
            return false;
          }
          value = value.replaceAll("-", "");
          value = value.replaceAll(" ", "");
          var regex = /\b^\d{16}$\b/;
          return regex.test(value);
        }
      })
      .required()
  })
);

const { result: resultProjects } = useQuery(
  gql`
    query Projects {
      projects {
        id
        markets {
          id
          name
        }
      }
    }
  `
);
const markets = useResult(resultProjects, null, (data) => {
  return data.projects[0].markets.map((x) => {
    return {
      label: x.name,
      value: x.id
    };
  });
});
const project = useResult(resultProjects, null, (data) => {
  return data.projects[0];
});

async function nextStep(values) {
  const result = await client.query({
    query: gql`
      query GetCardByNumber($cardNumber: String!) {
        cardByNumber(cardNumber: $cardNumber) {
          id
          project {
            id
          }
        }
      }
    `,
    variables: {
      cardNumber: values.cardNumber
    }
  });

  const card = result.data.cardByNumber;

  if (card === null || card.project.id !== project.value.id) {
    addError(t("card-number-invalid"));
  } else {
    emit("onUpdateStep", TRANSACTION_STEPS_ADD, {
      marketId: values.marketId,
      cardNumber: values.cardNumber,
      cardId: card.id
    });
  }
}

function closeModal() {
  emit("onCloseModal");
}
</script>
