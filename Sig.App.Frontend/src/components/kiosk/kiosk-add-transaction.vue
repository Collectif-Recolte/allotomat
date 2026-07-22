/* eslint-disable @intlify/vue-i18n/no-unused-keys */
<i18n>
{
  "en": {
    "amount-validation-label": "Amount",
    "cancel": "Cancel",
    "check-and-pay": "Check and pay",
    "card-is-disabled": "The card is deactivated.",
    "gift-card": "Gift card",
    "no-funds-message": "There are no available funds on this card.",
    "no-product-group-transaction": "At least one product group must have an amount to create a transaction.",
    "product-group-amount-isnan": "The amount must be a number.",
    "product-group-fund-not-enought": "There are not enough funds in this product group.",
    "transaction-submit-error": "The payment could not be completed. Please try again.",
    "card-not-found": "The card could not be found."
  },
  "fr": {
    "amount-validation-label": "Solde",
    "cancel": "Annuler",
    "check-and-pay": "Vérifier et payer",
    "card-is-disabled": "La carte est désactivée.",
    "gift-card": "Carte-cadeau",
    "no-funds-message": "Il n'y a pas de fonds disponibles sur cette carte.",
    "no-product-group-transaction": "Au minimum un groupe de produit doit avoir un montant pour créer une transaction.",
    "product-group-amount-isnan": "Le montant doit être un nombre.",
    "product-group-fund-not-enought": "Il n'y a pas assez de fonds pour ce groupe de produits.",
    "transaction-submit-error": "Le paiement n'a pas pu être effectué. Veuillez réessayer.",
    "card-not-found": "La carte est introuvable."
  }
}
</i18n>

<template>
  <div v-if="card" class="flex flex-col flex-1">
    <p v-if="card.isDisabled" class="text-red-500 font-bold mb-4">{{ t("card-is-disabled") }}</p>

    <Form
      v-if="funds && funds.length > 0"
      v-slot="{ isSubmitting, errors: formErrors, values }"
      class="flex flex-col"
      :initial-values="initialValues"
      :validation-schema="validationSchema"
      keep-values
      @submit="onFormSubmit">
      <KioskPaymentConfirm
        v-if="currentStep === 1"
        :funds="funds"
        :card-program-card-id="card.programCardId"
        :total-amount="getTotalTransactionAmount()"
        :processing="isSubmitting || submitting"
        @cancel="prevStep"
        @confirm="onConfirmSubmit" />

      <template v-else>
        <div class="p-8 xs:px-12 xs:pt-12">
          <FieldArray v-slot="{ fields }" key-path="id" name="funds">
            <div class="flex flex-col gap-7">
              <div v-if="getFundFields(fields, false).length" class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div v-for="{ field, idx } in getFundFields(fields, false)" :key="field.key">
                  <KioskFundAmountField :idx="idx" :fund="funds[idx].fund" :is-gift-card="false" />
                </div>
              </div>
              <div
                v-if="getFundFields(fields, true).length"
                :class="getFundFields(fields, false).length ? 'pt-7 border-t border-grey-100' : ''">
                <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div v-for="{ field, idx } in getFundFields(fields, true)" :key="field.key">
                    <KioskFundAmountField :idx="idx" :fund="funds[idx].fund" :is-gift-card="true" />
                  </div>
                </div>
              </div>
            </div>
          </FieldArray>
        </div>

        <div class="flex flex-col xs:flex-row justify-between items-center gap-4 mt-auto p-8 xs:pb-12 xs:px-12 pt-0">
          <PfButtonAction
            size="lg"
            class="min-h-20 rounded-2xl text-d6 w-full xs:w-auto"
            btn-style="kiosk-btn-cancel"
            has-icon-left
            :icon="ICON_CLOSE"
            :label="t('cancel')"
            @click="emit('onCloseModal')" />
          <PfButtonAction
            size="lg"
            class="min-h-20 rounded-2xl text-d6 w-full xs:w-auto"
            btn-style="primary"
            has-icon-left
            :icon="ICON_SHOPPING_CART"
            type="submit"
            :label="t('check-and-pay')"
            :is-disabled="isPayDisabled(formErrors, values) || card.isDisabled || isSubmitting" />
        </div>
      </template>
    </Form>

    <div v-else-if="funds">
      <p class="mb-6">{{ t("no-funds-message") }}</p>
      <PfButtonAction
        class="kiosk-btn-cancel"
        size="lg"
        btn-style="kiosk-btn-cancel"
        :label="t('cancel')"
        @click="emit('onCloseModal')" />
    </div>
  </div>
</template>

<script setup>
import { computed, defineEmits, defineProps, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { number, object, lazy, array, string } from "yup";
import { FieldArray } from "vee-validate";

import KioskFundAmountField from "@/components/kiosk/kiosk-fund-amount-field";
import KioskPaymentConfirm from "@/components/kiosk/kiosk-payment-confirm";
import { PRODUCT_GROUP_LOYALTY, TRANSACTION_STEPS_COMPLETE } from "@/lib/consts/enums";
import { KIOSK_ACCESS_INVALID, NOT_ENOUGHT_FUND, CARD_NOT_FOUND, CARD_DEACTIVATED } from "@/lib/consts/qr-code-error";
import { getMoneyFormat } from "@/lib/helpers/money";
import { useNotificationsStore } from "@/lib/store/notifications";

import ICON_CLOSE from "@/lib/icons/close.json";
import ICON_SHOPPING_CART from "@/lib/icons/shopping-cart.json";

const audio = new Audio(require("@/assets/audio/confirmation.mp3"));
const { t } = useI18n();
const { addError } = useNotificationsStore();

const props = defineProps({
  cardId: { type: String, required: true },
  kioskToken: { type: String, required: true }
});

const emit = defineEmits(["onUpdateStep", "onUpdateLoadingState", "onCloseModal", "kioskAuthError", "cardLoaded", "stepChange"]);

const currentStep = ref(0);
const submitting = ref(false);
const initialValues = { funds: [] };

const { result, loading } = useQuery(
  gql`
    query KioskCardForTransaction($kioskToken: String!, $id: ID!) {
      kioskCard(kioskToken: $kioskToken, id: $id) {
        id
        isDisabled
        programCardId
        project {
          id
          name
        }
        funds {
          id
          amount
          productGroup {
            id
            name
            orderOfAppearance
            color
          }
        }
        loyaltyFund {
          id
          amount
          productGroup {
            id
            name
            orderOfAppearance
            color
          }
        }
      }
    }
  `,
  () => ({ kioskToken: props.kioskToken, id: props.cardId }),
  () => ({ enabled: !!props.kioskToken && !!props.cardId })
);

const card = useResult(result, null, (data) => data.kioskCard);

const funds = useResult(result, null, (data) => {
  const results = data.kioskCard.funds.map((x) => ({
    amount: 0,
    id: x.id,
    fund: x
  }));

  if (data.kioskCard.loyaltyFund !== null) {
    results.push({
      amount: 0,
      id: data.kioskCard.loyaltyFund.id,
      fund: data.kioskCard.loyaltyFund
    });
  }

  initialValues.funds = results.map((x) => ({ amount: "", fundId: x.id }));
  return results;
});

watch(loading, (value) => emit("onUpdateLoadingState", value));

watch(card, (value) => {
  if (value) {
    emit("cardLoaded", {
      programCardId: value.programCardId ?? "",
      programName: value.project?.name ?? ""
    });
  }
});

const validationSchema = computed(() =>
  object({
    funds: array().of(
      object({
        amount: lazy((value) => {
          if (value === undefined || value === "" || value === null) return string().notRequired();
          const normalized = value.toString().replace(/,/, ".");
          if (isNaN(normalized)) {
            return string().test({
              name: "productGroupAmountMustBeNumber",
              message: t("product-group-amount-isnan"),
              test: () => false
            });
          }
          return number()
            .label(t("amount-validation-label"))
            .transform((_, val) => +val.toString().replace(/,/, "."))
            .test({
              name: "maxProductGroupAmount",
              message: t("product-group-fund-not-enought"),
              test(value, context) {
                const fundId = context.parent.fundId;
                const fund = funds.value?.find((x) => x.id === fundId)?.fund;
                if (fund) return value <= parseFloat(fund.amount);
                return false;
              }
            })
            .min(0.01)
            .required();
        })
      })
    )
  })
);

const { mutate: createKioskTransaction } = useMutation(
  gql`
    mutation CreateKioskTransaction($input: CreateKioskTransactionInput!) {
      createKioskTransaction(input: $input) {
        transaction {
          id
        }
      }
    }
  `
);

function getIsGiftCard(productGroupName) {
  return productGroupName === PRODUCT_GROUP_LOYALTY;
}

function getFundFields(fields, giftCardOnly) {
  return fields
    .map((field, idx) => ({ field, idx }))
    .filter(({ idx }) => {
      const isGiftCard = getIsGiftCard(funds.value[idx].fund.productGroup.name);
      return giftCardOnly ? isGiftCard : !isGiftCard;
    });
}

function getTotalTransactionAmount() {
  if (!funds.value) return getMoneyFormat(0);
  let total = 0;
  for (const item of funds.value) {
    if (item.amount === undefined || item.amount === null || item.amount === "") continue;
    total += parseFloat(String(item.amount).replace(/,/, "."));
  }
  return getMoneyFormat(total);
}

function isPayDisabled(formErrors, values) {
  if (Object.keys(formErrors).length > 0) return true;
  if (!values?.funds) return true;
  const total = values.funds.reduce((sum, item) => {
    const amount = item.amount;
    if (amount === undefined || amount === null || amount === "") return sum;
    return sum + parseFloat(String(amount).replace(/,/, "."));
  }, 0);
  return total <= 0;
}

function onFormSubmit(values) {
  let haveAtLeastOneProductGroup = false;
  for (let i = 0; i < values.funds.length; i++) {
    const amount = values.funds[i].amount;
    if (amount !== undefined && amount !== null && amount !== "") {
      haveAtLeastOneProductGroup = true;
    }
    if (funds.value) {
      funds.value[i].amount = amount ? String(amount).replace(/,/, ".") : "";
    }
  }

  if (!haveAtLeastOneProductGroup) {
    addError(t("no-product-group-transaction"));
    return;
  }

  currentStep.value = 1;
  emit("stepChange", 1);
}

function prevStep() {
  currentStep.value = 0;
  emit("stepChange", 0);
}

function getConfirmSubmitErrorMessage(exception) {
  const message = exception.message || "";

  if (message.indexOf(NOT_ENOUGHT_FUND) !== -1) {
    return t("product-group-fund-not-enought");
  }
  if (message.indexOf(CARD_NOT_FOUND) !== -1) {
    return t("card-not-found");
  }
  if (message.indexOf(CARD_DEACTIVATED) !== -1 || message.indexOf("CardIsDisabledException") !== -1) {
    return t("card-is-disabled");
  }

  return t("transaction-submit-error");
}

async function onConfirmSubmit() {
  if (!funds.value) return;
  submitting.value = true;
  emit("onUpdateLoadingState", true);

  const transactions = funds.value
    .filter((x) => parseFloat(x.amount) > 0)
    .map((x) => ({ amount: parseFloat(x.amount), productGroupId: x.fund.productGroup.id }));

  try {
    const result = await createKioskTransaction({
      input: {
        kioskToken: props.kioskToken,
        cardId: props.cardId,
        transactions
      }
    });
    audio.play();
    emit("onUpdateStep", TRANSACTION_STEPS_COMPLETE, {
      transactionId: result.data.createKioskTransaction.transaction.id
    });
  } catch (exception) {
    if ((exception.message || "").indexOf(KIOSK_ACCESS_INVALID) !== -1) {
      emit("kioskAuthError");
    } else {
      addError(getConfirmSubmitErrorMessage(exception));
    }
    emit("onUpdateLoadingState", false);
  } finally {
    submitting.value = false;
  }
}
</script>

<style scoped>
:deep(.pf-button.kiosk-btn-cancel) {
  @apply bg-primary-200 text-primary-900 ring-primary-300 hover:bg-primary-300 border-0;
}
</style>
