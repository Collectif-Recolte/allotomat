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
    "gift-card": "Gift card"
	},
	"fr": {
		"title": "Remboursement",
    "subtitle": "Transaction du {date} — Carte #{cardLongId}",
    "cancel":"Annuler",
    "refund-transaction": "Rembourser",
    "available-refund": "Disponible au remboursement : {amountAvailable}",
    "amount-validation-label": "Montant de remboursement",
    "product-group-refund-too-much": "Le montant de remboursement ne peut pas être supérieur au montant disponible.",
    "gift-card": "Carte-cadeau"
	}
}
</i18n>

<template>
  <div v-if="!loading">
    <UiDialogModal v-if="refundTransactionId === null" :title="t('title')" hide-main-btn="false">
      <div>
        <p>
          {{
            t("subtitle", {
              date: getTransactionDate(),
              cardLongId: transaction.card.cardNumber
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
              :disable-submit="Object.keys(formErrors).length > 0"
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
                          input-mode="decimal">
                          <template #trailingIcon>
                            <UiDollarSign :errors="fieldErrors" />
                          </template>
                        </PfFormInputText>
                      </Field>
                    </div>
                  </div>
                </FieldArray>
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

import { PRODUCT_GROUP_LOYALTY } from "@/lib/consts/enums";

import { formatDate, textualFormat } from "@/lib/helpers/date";
import { getMoneyFormat } from "@/lib/helpers/money";
import { usePageTitle } from "@/lib/helpers/page-title";
import { getColorBgClass } from "@/lib/helpers/products-color";
import { URL_TRANSACTION_LIST } from "@/lib/consts/urls";

import CompleteRefundTransaction from "@/views/transaction/CompleteRefund";

const audio = new Audio(require("@/assets/audio/confirmation.mp3"));

const route = useRoute();
const router = useRouter();
const { t } = useI18n();

usePageTitle(t("title"));

const refundTransactionId = ref(null);

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
  return t(key, { amountAvailable: getMoneyFormat(productGroup.amount - productGroup.refundAmount) });
}

const goToTransactionList = () => {
  router.push({ name: URL_TRANSACTION_LIST });
};

async function onSubmit({ productGroups }) {
  const result = await refundTransaction({
    input: {
      transactions: productGroups
        .filter((x) => parseFloat(x.amount) > 0)
        .map((x) => ({ amount: parseFloat(x.amount), productGroupId: x.productGroupId })),
      initialTransactionId: route.params.transactionId
    }
  });

  audio.play();
  setTimeout(() => {
    refundTransactionId.value = result.data.refundTransaction.transaction.id;
  }, 200);
}
</script>
