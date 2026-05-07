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
      "transaction-in-program-name": "Purchase on behalf of a merchant",
      "scan-card-btn": "Scan a card",
      "select-cash-register": "Cash Register",
      "choose-cash-register": "Select",
      "market-disabled-label": "{market} is deactivated"
    },
    "fr": {
      "select-market": "Commerce",
      "card-number": "N° de carte",
      "card-number-description": "Le numéro de carte doit être de format \"1234 5678 9012 3456\"",
      "card-number-error": "Le numéro de carte doit être de format \"1234 5678 9012 3456\"",
      "choose-market": "Sélectionner",
      "next-step": "Suivant",
      "cancel": "Annuler",
      "qr-code-invalid": "Le code QR n'équivaut pas à une carte connue.",
      "card-number-invalid": "Le numéro de carte ne correspond à aucune carte reliée au programme.",
      "transaction-in-program-name": "Achat au nom d'un commerce",
      "scan-card-btn": "Scanner une carte",
      "select-cash-register": "Caisse",
      "choose-cash-register": "Sélectionner",
      "market-disabled-label": "{market} est désactivé"
    }
  }
  </i18n>

<template>
  <p class="text-p1">
    {{ t("transaction-in-program-name") }}
  </p>
  <Form
    v-slot="{ errors: formErrors, setFieldValue }"
    :validation-schema="validationSchema"
    :initial-values="initialValues"
    keep-values
    @submit="nextStep">
    <PfForm
      has-footer
      :disable-submit="Object.keys(formErrors).length > 0"
      :submit-label="t('next-step')"
      :cancel-label="t('cancel')"
      footer-alt-style
      can-cancel
      @cancel="closeModal">
      <PfFormSection>
        <Field
          v-if="userType !== USER_TYPE_MARKETGROUPMANAGER"
          v-slot="{ field: inputField, errors: fieldErrors }"
          name="marketId">
          <PfFormInputSelect
            id="marketId"
            v-bind="inputField"
            :placeholder="t('choose-market')"
            :label="t('select-market')"
            :options="markets"
            :errors="fieldErrors"
            @input="(val) => onMarketSelected(val, setFieldValue)" />
        </Field>
        <Field v-slot="{ field: inputField, errors: fieldErrors }" name="cashRegisterId">
          <PfFormInputSelect
            id="cashRegisterId"
            v-bind="inputField"
            :disabled="
              (userType !== USER_TYPE_MARKETGROUPMANAGER && !selectedMarket) ||
              !!singleMarketGroupCashRegister ||
              !!singleCashRegisterForMarket
            "
            :placeholder="t('choose-cash-register')"
            :label="t('select-cash-register')"
            :options="cashRegisters"
            :errors="fieldErrors" />
        </Field>
        <div>
          <Field v-slot="{ field: inputField, errors: fieldErrors }" name="cardNumber">
            <PfFormInputText
              v-if="enterCardNumber || props.cardNumber !== ''"
              id="cardNumber"
              v-bind="inputField"
              :disabled="props.cardNumber !== ''"
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
        v-if="enterCardNumber && props.cardNumber === ''"
        class="w-full"
        btn-style="secondary"
        :label="t('scan-card-btn')"
        @click="enterCardNumber = !enterCardNumber" />
    </PfForm>
  </Form>
</template>

<script setup>
import gql from "graphql-tag";
import { computed, defineEmits, ref, defineProps } from "vue";
import { useI18n } from "vue-i18n";
import { string, object } from "yup";
import { useQuery, useResult, useApolloClient } from "@vue/apollo-composable";
import { storeToRefs } from "pinia";

import { useAuthStore } from "@/lib/store/auth";
import { TRANSACTION_STEPS_ADD, USER_TYPE_MARKETGROUPMANAGER } from "@/lib/consts/enums";

import { useNotificationsStore } from "@/lib/store/notifications";

import QRCodeScanner from "@/components/transaction/qr-code-scanner.vue";

const props = defineProps({
  cardNumber: {
    type: String,
    default: ""
  }
});

const { userType } = storeToRefs(useAuthStore());

const initialValues = computed(() => {
  return {
    cardNumber: props.cardNumber,
    cashRegisterId: singleMarketGroupCashRegister.value ?? ""
  };
});

const enterCardNumber = ref(props.cardNumber === "");
const selectedMarket = ref("");

const { t } = useI18n();
const { resolveClient } = useApolloClient();

const client = resolveClient();

const { addError } = useNotificationsStore();

const emit = defineEmits(["onUpdateStep", "onCloseModal"]);

const validationSchema = computed(() => {
  const cardNumberValidator = string()
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
    .required();

  if (userType.value === USER_TYPE_MARKETGROUPMANAGER) {
    return object({
      cashRegisterId: string().label(t("select-cash-register")).required(),
      cardNumber: cardNumberValidator
    });
  }

  return object({
    marketId: string().label(t("select-market")).required(),
    cashRegisterId: string().label(t("select-cash-register")).required(),
    cardNumber: cardNumberValidator
  });
});

const { result: resultProjects } = useQuery(
  gql`
    query Projects {
      projects {
        id
        markets {
          id
          name
          isDisabled
          cashRegisters {
            id
            name
            isArchived
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
        label: x.isDisabled ? t("market-disabled-label", { market: x.name }) : x.name,
        value: x.id,
        isDisabled: x.isDisabled
      };
    })
    .sort((a, b) => a.label.localeCompare(b.label));
});
const project = useResult(resultProjects, null, (data) => {
  return data.projects[0];
});
const projectCashRegisters = useResult(resultProjects, null, (data) => {
  return data.projects[0].markets
    .find((x) => x.id === selectedMarket.value)
    .cashRegisters.filter((x) => !x.isArchived)
    .map((x) => {
      return {
        label: x.name,
        value: x.id
      };
    })
    .sort((a, b) => a.label.localeCompare(b.label));
});

const { result: resultMarketGroups } = useQuery(
  gql`
    query MarketGroups {
      marketGroups {
        id
        cashRegisters {
          id
          name
          market {
            id
          }
        }
      }
    }
  `,
  null,
  { enabled: computed(() => userType.value === USER_TYPE_MARKETGROUPMANAGER) }
);
const marketGroupCashRegisters = useResult(resultMarketGroups, null, (data) => {
  return data.marketGroups
    .flatMap((mg) => mg.cashRegisters)
    .map((cr) => ({ label: cr.name, value: cr.id }))
    .sort((a, b) => a.label.localeCompare(b.label));
});

const singleMarketGroupCashRegister = computed(() => {
  if (userType.value !== USER_TYPE_MARKETGROUPMANAGER) return null;
  const list = marketGroupCashRegisters.value;
  return list && list.length === 1 ? list[0].value : null;
});
const cashRegisterMarketMap = useResult(resultMarketGroups, {}, (data) => {
  return Object.fromEntries(data.marketGroups.flatMap((mg) => mg.cashRegisters.map((cr) => [cr.id, cr.market.id])));
});

const cashRegisters = computed(() => {
  if (userType.value === USER_TYPE_MARKETGROUPMANAGER) return marketGroupCashRegisters.value;
  return projectCashRegisters.value;
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

  if (card === null) {
    addError(t("card-number-invalid"));
    return;
  }

  if (userType.value !== USER_TYPE_MARKETGROUPMANAGER && card.project.id !== project.value.id) {
    addError(t("card-number-invalid"));
    return;
  }

  const marketId =
    userType.value === USER_TYPE_MARKETGROUPMANAGER ? cashRegisterMarketMap.value[values.cashRegisterId] : values.marketId;

  emit("onUpdateStep", TRANSACTION_STEPS_ADD, {
    marketId,
    cashRegisterId: values.cashRegisterId,
    cardNumber: values.cardNumber,
    cardId: card.id
  });
}

const singleCashRegisterForMarket = computed(() => {
  if (!selectedMarket.value || !resultProjects.value) return null;
  const crs =
    resultProjects.value.projects[0].markets
      .find((x) => x.id === selectedMarket.value)
      ?.cashRegisters.filter((x) => !x.isArchived) ?? [];
  return crs.length === 1 ? crs[0].id : null;
});

function onMarketSelected(e, setFieldValue) {
  selectedMarket.value = e;
  const crs = resultProjects.value.projects[0].markets.find((x) => x.id === e)?.cashRegisters.filter((x) => !x.isArchived) ?? [];
  setFieldValue("cashRegisterId", crs.length === 1 ? crs[0].id : "");
}

function closeModal() {
  emit("onCloseModal");
}
</script>
