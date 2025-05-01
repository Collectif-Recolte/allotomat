<i18n>
  {
    "en": {
      "select-market": "Merchant",
      "card-number": "Card number",
      "card-number-description": "Card number must be of format \"1234 5678 9012 3456\"",
      "card-number-error": "Card number must be of format \"1234 5678 9012 3456\"",
      "choose-market": "Select",
      "next-step": "Next",
      "cancel": "Cancel",
      "qr-code-invalid": "QR code does not equate to a known card.",
      "card-number-invalid": "Card number does not match any card linked to the program.",
      "transaction-in-program-name": "Purchase on behalf of program",
      "scan-card-btn": "Scan a card",
      "select-cash-register": "Cash Register",
      "choose-cash-register": "Select"
    },
    "fr": {
      "select-market": "Marchand",
      "card-number": "N° de carte",
      "card-number-description": "Le numéro de carte doit être de format \"1234 5678 9012 3456\"",
      "card-number-error": "Le numéro de carte doit être de format \"1234 5678 9012 3456\"",
      "choose-market": "Sélectionner",
      "next-step": "Suivant",
      "cancel": "Annuler",
      "qr-code-invalid": "Le code QR n'équivaut pas à une carte connue.",
      "card-number-invalid": "Le numéro de carte ne correspond à aucune carte reliée au programme.",
      "transaction-in-program-name": "Achat au nom du programme",
      "scan-card-btn": "Scanner une carte",
      "select-cash-register": "Caisse",
      "choose-cash-register": "Sélectionner"
    }
  }
  </i18n>

<template>
  <p class="text-p1">
    {{ t("transaction-in-program-name") }}
  </p>
  <Form v-slot="{ errors: formErrors, setFieldValue }" :validation-schema="validationSchema" keep-values @submit="nextStep">
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
            :errors="fieldErrors"
            @input="onMarketSelected" />
        </Field>
        <Field v-slot="{ field: inputField, errors: fieldErrors }" name="cashRegisterId">
          <PfFormInputSelect
            id="cashRegisterId"
            v-bind="inputField"
            :disabled="!selectedMarket"
            :placeholder="t('choose-cash-register')"
            :label="t('select-cash-register')"
            :options="cashRegisters"
            :errors="fieldErrors" />
        </Field>
        <div>
          <Field v-slot="{ field: inputField, errors: fieldErrors }" name="cardNumber">
            <PfFormInputText
              v-if="enterCardNumber"
              id="cardNumber"
              v-bind="inputField"
              :description="t('card-number-description')"
              :label="t('card-number')"
              :errors="fieldErrors" />
            <QRCodeScanner
              v-else
              @cancel="enterCardNumber = true"
              @triggerError="checkQRCode('', setFieldValue)"
              @checkQRCode="(cardId) => checkQRCode(cardId, setFieldValue)" />
          </Field>
        </div>
      </PfFormSection>
      <PfButtonAction
        v-if="enterCardNumber"
        class="w-full"
        btn-style="secondary"
        :label="t('scan-card-btn')"
        @click="enterCardNumber = !enterCardNumber" />
    </PfForm>
  </Form>
</template>

<script setup>
import gql from "graphql-tag";
import { computed, defineEmits, ref } from "vue";
import { useI18n } from "vue-i18n";
import { string, object } from "yup";
import { useQuery, useResult, useApolloClient } from "@vue/apollo-composable";

import { TRANSACTION_STEPS_ADD } from "@/lib/consts/enums";

import { useNotificationsStore } from "@/lib/store/notifications";

import QRCodeScanner from "@/components/transaction/qr-code-scanner.vue";

const enterCardNumber = ref(true);
const selectedMarket = ref("");

const { t } = useI18n();
const { resolveClient } = useApolloClient();

const client = resolveClient();

const { addError } = useNotificationsStore();

const emit = defineEmits(["onUpdateStep", "onCloseModal"]);

const validationSchema = computed(() =>
  object({
    marketId: string().label(t("select-market")).required(),
    cashRegisterId: string().label(t("select-cash-register")).required(),
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
          cashRegisters {
            id
            name
          }
        }
      }
    }
  `
);
const markets = useResult(resultProjects, null, (data) => {
  return data.projects[0].markets
    .map((x) => {
      return {
        label: x.name,
        value: x.id
      };
    })
    .sort((a, b) => a.label.localeCompare(b.label));
});
const project = useResult(resultProjects, null, (data) => {
  return data.projects[0];
});
const cashRegisters = useResult(resultProjects, null, (data) => {
  return data.projects[0].markets
    .find((x) => x.id === selectedMarket.value)
    .cashRegisters.map((x) => {
      return {
        label: x.name,
        value: x.id
      };
    })
    .sort((a, b) => a.label.localeCompare(b.label));
});

async function checkQRCode(cardId, callback) {
  let card = null;
  if (cardId !== "") {
    const result = await client.query({
      query: gql`
        query Card($id: ID!) {
          card(id: $id) {
            id
            cardNumber
          }
        }
      `,
      variables: {
        id: cardId
      }
    });
    card = result.data.card;
  }

  if (!card) {
    addError(t("qr-code-invalid"));
  } else {
    callback("cardNumber", card.cardNumber.replaceAll("-", " "));
  }

  enterCardNumber.value = true;
}

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
      cashRegisterId: values.cashRegisterId,
      cardNumber: values.cardNumber,
      cardId: card.id
    });
  }
}

function onMarketSelected(e) {
  selectedMarket.value = e;
}

function closeModal() {
  emit("onCloseModal");
}
</script>
