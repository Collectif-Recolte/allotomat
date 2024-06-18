<i18n>
{
	"en": {
		"title": "Refund",
    "subtitle": "Transaction of {date} — Card #{cardLongId}",
    "cancel":"Cancel",
    "refund-transaction": "Refund",
    "available-refund": "Available for refund: {amountAvailable}",
    "amount-validation-label": "Refund amount",
    "product-group-refund-too-much": "Refund amount cannot be greater than available amount.",
    "product-group-refund-amount-isnan": "Refund amount must be a number.",
    "gift-card": "Gift card",
    "password": "Password",
    "no-amount-to-refund": "No amount to refund.",
    "wrong-password-error-notification": "The password is invalid.",
    "expired-funds":"Some of the funds used in this transaction have expired and can no longer be reimbursed."
	},
	"fr": {
		"title": "Remboursement",
    "subtitle": "Transaction du {date} — Carte #{cardLongId}",
    "cancel":"Annuler",
    "refund-transaction": "Rembourser",
    "available-refund": "Disponible au remboursement : {amountAvailable}",
    "amount-validation-label": "Montant de remboursement",
    "product-group-refund-too-much": "Le montant de remboursement ne peut pas être supérieur au montant disponible.",
    "product-group-refund-amount-isnan": "Le montant de remboursement doit être un nombre.",
    "gift-card": "Carte-cadeau",
    "password": "Mot de passe",
    "no-amount-to-refund": "Aucun montant à rembourser.",
    "wrong-password-error-notification": "Le mot de passe est invalide.",
    "expired-funds":"Certains des fonds utilisés dans cette transaction ont expiré et ne peuvent plus être remboursés."
	}
}
</i18n>

<template>
  <div v-if="!loading">
    <UiDialogModal v-if="refundTransactionId === null" :title="t('title')" hide-main-btn="false" :return-route="returnRoute()">
      <div>
        <p v-if="haveExpiredFunds">
          <b>{{ t("expired-funds") }}</b>
        </p>
        <p>
          {{
            t("subtitle", {
              date: getTransactionDate(),
              cardLongId: getLast4Digit(transaction.card.cardNumber)
            })
          }}
        </p>
        <Form
          v-if="productGroups"
          v-slot="{ isSubmitting, errors: formErrors }"
          :initial-values="initialValues"
          keep-values
          :validation-schema="validationSchema"
          @submit="onSubmit">
          <div>
            <PfForm
              has-footer
              footer-alt-style
              can-cancel
              :disable-submit="!haveRefundAmount() || Object.keys(formErrors).length > 0"
              :submit-label="t('refund-transaction')"
              :cancel-label="t('cancel')"
              :processing="isSubmitting"
              @cancel="goToTransactionList">
              <PfFormSection>
                <FieldArray v-slot="{ fields }" key-path="id" name="productGroups">
                  <div
                    v-for="(field, idx) in fields"
                    :key="field.key"
                    :class="getIsGiftCard(productGroups[idx].productGroup.name) ? 'pt-6 border-t border-grey-100' : ''">
                    <div
                      class="p-4 pt-2.5 rounded-lg"
                      :class="[
                        getColorBgClass(productGroups[idx].productGroup.color),
                        getIsGiftCard(productGroups[idx].productGroup.name) ? 'bg-diagonal-pattern' : 'dark'
                      ]">
                      <Field
                        :id="`productGroups[${idx}].transactionProductGroupId`"
                        v-slot="{ field: inputField, errors: fieldErrors }"
                        :name="`productGroups[${idx}].amount`">
                        <PfFormInputText
                          :id="`productGroups[${idx}].amount`"
                          class="grow"
                          v-bind="inputField"
                          :label="productGroupLabel(productGroups[idx].productGroup)"
                          :after-label="availableAmountLabel(productGroups[idx], 'available-refund')"
                          :errors="fieldErrors"
                          input-mode="decimal"
                          @input="(value) => onProductGroupAmountInput(idx, value)">
                          <template #trailingIcon>
                            <UiDollarSign :errors="fieldErrors" />
                          </template>
                        </PfFormInputText>
                      </Field>
                    </div>
                  </div>
                </FieldArray>
                <Field v-slot="{ field, errors }" name="password">
                  <PfFormInputText
                    id="password"
                    v-bind="field"
                    :label="t('password')"
                    :errors="errors"
                    input-type="password"></PfFormInputText>
                </Field>
              </PfFormSection>
            </PfForm>
          </div>
        </Form>
      </div>
    </UiDialogModal>
    <div v-else>
      <DialogOverlay class="transition-opacity fixed inset-0 bg-primary-700/80" />
      <CompleteRefundTransaction
        class="fixed z-40 inset-0 overflow-y-auto"
        :transaction-id="refundTransactionId"
        @onUpdateStep="updateStep"
        @onUpdateLoadingState="updateLoadingState" />
    </div>
  </div>
</template>

<script setup>
import { computed, ref } from "vue";
import { useI18n } from "vue-i18n";
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useRoute, useRouter } from "vue-router";
import { FieldArray } from "vee-validate";
import { number, object, lazy, array, string } from "yup";
import { storeToRefs } from "pinia";

import {
  PRODUCT_GROUP_LOYALTY,
  USER_TYPE_PROJECTMANAGER,
  ADDING_FUND_TRANSACTION_STATUS_ACTIVED,
  ADDING_FUND_TRANSACTION_STATUS_EXPIRED
} from "@/lib/consts/enums";
import { URL_TRANSACTION_LIST, URL_TRANSACTION_ADMIN } from "@/lib/consts/urls";

import { useNotificationsStore } from "@/lib/store/notifications";
import { useAuthStore } from "@/lib/store/auth";

import { useGraphQLErrorMessages } from "@/lib/helpers/error-handler";
import { formatDate, textualFormat } from "@/lib/helpers/date";
import { getMoneyFormat } from "@/lib/helpers/money";
import { usePageTitle } from "@/lib/helpers/page-title";
import { getColorBgClass } from "@/lib/helpers/products-color";

import CompleteRefundTransaction from "@/views/transaction/CompleteRefund";

const audio = new Audio(require("@/assets/audio/confirmation.mp3"));

const { addWarning } = useNotificationsStore();
const route = useRoute();
const router = useRouter();
const { t } = useI18n();
const { userType } = storeToRefs(useAuthStore());

usePageTitle(t("title"));

useGraphQLErrorMessages({
  // Ce code est lancé quand le mot de passe est invalid
  WRONG_PASSWORD: () => {
    return t("wrong-password-error-notification");
  }
});

const refundTransactionId = ref(null);
const productGroupsValue = ref({});

const { result, loading } = useQuery(
  gql`
    query Transaction($id: ID!) {
      transaction(id: $id) {
        id
        amount
        ... on PaymentTransactionGraphType {
          transactionByProductGroups {
            id
            amount
            refundAmount
            productGroup {
              id
              name
              color
            }
          }
          paymentTransactionAddingFundTransactions {
            id
            amount
            refundAmount
            addingFundTransaction {
              id
              status
              productGroup {
                id
              }
            }
          }
          createdAt
          card {
            id
            cardNumber
          }
        }
      }
    }
  `,
  {
    id: route.params.transactionId
  }
);
const transaction = useResult(result, null, (data) => {
  initialValues.productGroups = data.transaction.transactionByProductGroups.map((x) => ({
    amount: "",
    transactionProductGroupId: x.id,
    productGroupId: x.productGroup.id
  }));

  return data.transaction;
});

const { mutate: refundTransaction } = useMutation(
  gql`
    mutation RefundTransaction($input: RefundTransactionInput!) {
      refundTransaction(input: $input) {
        transaction {
          id
        }
      }
    }
  `
);

const initialValues = {
  productGroups: []
};

const validationSchema = object({
  productGroups: array().of(
    object({
      amount: lazy((value) => {
        if (value === undefined || value === "" || value === null) return string().notRequired();
        if (isNaN(value)) {
          return string().test({
            name: "productGroupAmountMustBeNumber",
            exclusive: false,
            params: {},
            message: t("product-group-refund-amount-isnan"),
            test: function () {
              return false;
            }
          });
        }
        return number()
          .label(t("amount-validation-label"))
          .transform((_, value) => {
            return +value.toString().replace(/,/, ".");
          })
          .test({
            name: "maxProductGroupAmount",
            exclusive: false,
            params: {},
            message: t("product-group-refund-too-much"),
            test: function (value, context) {
              var transactionProductGroupId = context.parent.transactionProductGroupId;
              var transactionProductGroup = productGroups.value.find((x) => x.id === transactionProductGroupId);

              if (transaction.value.paymentTransactionAddingFundTransactions.length > 0) {
                var paymentTransactionAddingFundTransactions = transaction.value.paymentTransactionAddingFundTransactions;

                var availableAmount = 0;
                var refundedAmount = 0;

                paymentTransactionAddingFundTransactions.forEach((x) => {
                  if (
                    x.addingFundTransaction.productGroup.id === transactionProductGroup.productGroup.id &&
                    x.addingFundTransaction.status === ADDING_FUND_TRANSACTION_STATUS_ACTIVED
                  ) {
                    availableAmount += x.amount;
                    refundedAmount += x.refundAmount;
                  }
                });

                return value <= parseFloat(availableAmount - refundedAmount).toFixed(2);
              }

              if (transactionProductGroup)
                return value <= parseFloat(transactionProductGroup.amount - transactionProductGroup.refundAmount).toFixed(2);
              return false;
            }
          })
          .min(0.01)
          .required();
      })
    })
  )
});

function getTransactionDate() {
  if (transaction.value.createdAt === undefined || transaction.value.createdAt === null) {
    return null;
  }
  return formatDate(new Date(transaction.value.createdAt), textualFormat);
}

function getLast4Digit(cardNumber) {
  if (cardNumber === undefined || cardNumber === null) {
    return null;
  }
  return `****-****-****-${cardNumber.slice(-4)}`;
}

const productGroups = computed(() => {
  return transaction.value.transactionByProductGroups;
});

function getIsGiftCard(productGroupName) {
  if (productGroupName === PRODUCT_GROUP_LOYALTY) return true;
  else return false;
}

function productGroupLabel(productGroup) {
  let productGroupName = productGroup.name;
  if (productGroupName === PRODUCT_GROUP_LOYALTY) {
    productGroupName = t("gift-card");
  }

  return productGroupName;
}

function availableAmountLabel(productGroup, key) {
  if (transaction.value.paymentTransactionAddingFundTransactions.length > 0) {
    var paymentTransactionAddingFundTransactions = transaction.value.paymentTransactionAddingFundTransactions;

    var availableAmount = 0;
    var refundedAmount = 0;

    paymentTransactionAddingFundTransactions.forEach((x) => {
      if (
        x.addingFundTransaction.productGroup.id === productGroup.productGroup.id &&
        x.addingFundTransaction.status === ADDING_FUND_TRANSACTION_STATUS_ACTIVED
      ) {
        availableAmount += x.amount;
        refundedAmount += x.refundAmount;
      }
    });

    return t(key, { amountAvailable: getMoneyFormat(availableAmount - refundedAmount) });
  }
  return t(key, { amountAvailable: getMoneyFormat(productGroup.amount - productGroup.refundAmount) });
}

const haveExpiredFunds = computed(() => {
  return transaction.value.paymentTransactionAddingFundTransactions.some(
    (x) => x.addingFundTransaction.status === ADDING_FUND_TRANSACTION_STATUS_EXPIRED
  );
});

const goToTransactionList = () => {
  if (userType.value === USER_TYPE_PROJECTMANAGER) router.push({ name: URL_TRANSACTION_ADMIN });
  else router.push({ name: URL_TRANSACTION_LIST });
};

function onProductGroupAmountInput(idx, value) {
  productGroupsValue.value[idx] = value;
}

function haveRefundAmount() {
  for (var prop in productGroupsValue.value) {
    if (Object.prototype.hasOwnProperty.call(productGroupsValue.value, prop)) {
      if (productGroupsValue.value[prop] === null || productGroupsValue.value[prop] === "") continue;
      if (isNaN(productGroupsValue.value[prop])) {
        return false;
      }
      if (parseFloat(productGroupsValue.value[prop]) > 0) {
        return true;
      }
    }
  }
}

async function onSubmit({ productGroups, password }) {
  const transactions = productGroups
    .filter((x) => parseFloat(x.amount) > 0)
    .map((x) => ({ amount: parseFloat(x.amount), productGroupId: x.productGroupId }));

  if (transactions.length === 0) {
    addWarning(t("no-amount-to-refund"));
    return;
  }
  const result = await refundTransaction({
    input: {
      password: password ?? "",
      transactions,
      initialTransactionId: route.params.transactionId
    }
  });

  audio.play();
  setTimeout(() => {
    refundTransactionId.value = result.data.refundTransaction.transaction.id;
  }, 200);
}

function returnRoute() {
  if (userType.value === USER_TYPE_PROJECTMANAGER) return { name: URL_TRANSACTION_ADMIN };
  else return { name: URL_TRANSACTION_LIST };
}
</script>
