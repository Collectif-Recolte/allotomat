<i18n>
  {
    "en": {
      "title": "Manually enter card number",
      "card-number": "Card number",
      "card-number-description": "Card number must be of format \"1234 5678 9012 3456\"",
      "card-number-error": "Card number must be of format \"1234 5678 9012 3456\"",
      "next-step": "Next",
      "cancel": "Cancel",
      "card-number-invalid": "Card number does not match any card linked to the market.",
      "warning-text": "The program that issued this card may require that a physical card be present to make purchases. Your business may be responsible for improper purchases, so please use caution."
    },
    "fr": {
      "title": "Saisir le numéro de la carte",
      "card-number": "N° de carte",
      "card-number-description": "Le numéro de carte doit être de format \"1234 5678 9012 3456\"",
      "card-number-error": "Le numéro de carte doit être de format \"1234 5678 9012 3456\"",
      "next-step": "Suivant",
      "cancel": "Annuler",
      "card-number-invalid": "Le numéro de carte ne correspond à aucune carte reliée au marché.",
      "warning-text": "Attention : Le programme qui a émis cette carte peut exiger la présence d'une carte physique pour effectuer des achats. Votre entreprise peut être tenue pour responsable des achats non conformes ; soyez donc prudent."
    }
  }
  </i18n>

<template>
  <UiDialogModal :title="t('title')" :has-footer="false" @onClose="closeModal">
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
          <p class="text-p1">
            {{ t("warning-text") }}
          </p>
        </PfFormSection>
      </PfForm>
    </Form>
  </UiDialogModal>
</template>

<script setup>
import gql from "graphql-tag";
import { computed, defineEmits } from "vue";
import { useI18n } from "vue-i18n";
import { string, object } from "yup";
import { useQuery, useResult, useApolloClient } from "@vue/apollo-composable";
import { useRouter } from "vue-router";

import { URL_TRANSACTION, URL_TRANSACTION_ERROR } from "@/lib/consts/urls";
import { TRANSACTION_STEPS_ADD, TRANSACTION_STEPS_START } from "@/lib/consts/enums";
import {
  CARD_CANT_BE_USED_IN_MARKET,
  CARD_NOT_FOUND,
  CARD_DEACTIVATED,
  CARD_CANT_BE_USED_WITH_CASH_REGISTER
} from "@/lib/consts/qr-code-error";

import { useNotificationsStore } from "@/lib/store/notifications";
import { useCashRegisterStore } from "@/lib/store/cash-register";

const { t } = useI18n();
const { resolveClient } = useApolloClient();

const router = useRouter();
const client = resolveClient();

const { addError } = useNotificationsStore();
const { currentCashRegister } = useCashRegisterStore();

const emit = defineEmits(["onUpdateStep"]);

const validationSchema = computed(() =>
  object({
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

const { result: resultMarkets } = useQuery(
  gql`
    query Markets {
      markets {
        id
        projects {
          id
          name
        }
      }
    }
  `
);
const market = useResult(resultMarkets, null, (data) => {
  return data.markets[0];
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

  if (card === null || market.value.projects.find((project) => project.id === card.project.id) === undefined) {
    addError(t("card-number-invalid"));
  } else {
    let canBeUsedInMarket = null;
    try {
      canBeUsedInMarket = await client.query({
        query: gql`
          query VerifyCardCanBeUsedInMarket($cardId: ID!, $marketId: ID!, $cashRegisterId: ID!) {
            verifyCardCanBeUsedInMarket(cardId: $cardId, marketId: $marketId, cashRegisterId: $cashRegisterId)
          }
        `,
        variables: {
          cardId: card.id,
          marketId: market.value.id,
          cashRegisterId: currentCashRegister
        }
      });

      if (canBeUsedInMarket.data.verifyCardCanBeUsedInMarket) {
        emit("onUpdateStep", TRANSACTION_STEPS_ADD, {
          marketId: values.marketId,
          cardNumber: values.cardNumber,
          cardId: card.id
        });
      }
    } catch (exception) {
      emit("onUpdateStep", TRANSACTION_STEPS_START, {});
      if (exception.message.indexOf(CARD_CANT_BE_USED_WITH_CASH_REGISTER) !== -1) {
        router.push({
          name: URL_TRANSACTION_ERROR,
          query: { error: CARD_CANT_BE_USED_WITH_CASH_REGISTER, returnRoute: URL_TRANSACTION }
        });
      } else if (exception.message.indexOf(CARD_CANT_BE_USED_IN_MARKET) !== -1) {
        router.push({ name: URL_TRANSACTION_ERROR, query: { error: CARD_CANT_BE_USED_IN_MARKET, returnRoute: URL_TRANSACTION } });
      } else if (exception.message.indexOf(CARD_NOT_FOUND) !== -1) {
        router.push({ name: URL_TRANSACTION_ERROR, query: { error: CARD_NOT_FOUND, returnRoute: URL_TRANSACTION } });
      } else if (exception.message.indexOf(CARD_DEACTIVATED) !== -1) {
        router.push({ name: URL_TRANSACTION_ERROR, query: { error: CARD_DEACTIVATED, returnRoute: URL_TRANSACTION } });
      }
    }
  }
}

function closeModal() {
  emit("onUpdateStep", TRANSACTION_STEPS_START, {});
}
</script>
