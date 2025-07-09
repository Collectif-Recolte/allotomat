<i18n>
  {
    "en": {
      "card-description": "Balance",
      "create-new-transaction-btn": "Done",
      "payment-description": "Payment",
      "title": "Transaction completed",
      "gift-card": "Gift card"
    },
    "fr": {
      "card-description": "Solde",
      "create-new-transaction-btn": "Terminer",
      "payment-description": "Paiement",
      "title": "Transaction complétée",
      "gift-card": "Carte-cadeau"
    }
  }
  </i18n>

<template>
  <h1 class="font-semibold text-center mt-4">{{ t("title") }}</h1>
  <div v-if="transaction" class="flex mx-2 gap-x-2">
    <p class="w-1/2 leading-tight text-right">
      <span class="inline-block max-w-32 uppercase text-p3 font-bold leading-none">{{ t("payment-description") }}</span>
      <span class="block font-bold text-primary-700 text-h3 xs:text-h2">{{ getMoneyFormat(amount) }}</span>
    </p>
    <p class="w-1/2 leading-tight text-right">
      <span class="inline-block max-w-24 uppercase text-p3 font-bold leading-none">{{ t("card-description") }}</span>
      <span class="block font-bold text-primary-700 text-h3 xs:text-h2">{{ getMoneyFormat(fund) }}</span>
    </p>
  </div>

  <ul class="mb-6 w-full">
    <li
      v-for="item in transactionByProductGroups"
      :key="item.id"
      class="mb-2 last:mb-0 text-p2"
      :class="getIsGiftCard(item.productGroup.name) ? 'mt-6 pt-4 border-t border-grey-100' : 'dark'">
      <div
        class="relative flex items-center w-full rounded-md py-1 px-2 text-primary-900 dark:text-white"
        :class="[getColorBgClass(item.productGroup.color), getIsGiftCard(item.productGroup.name) ? 'bg-diagonal-pattern' : '']">
        <div class="absolute -translate-y-1/2 top-1/2 left-2 max-w-20 xs:max-w-24 truncate font-bold">
          {{ getProductGroupName(item) }}
        </div>

        <span class="w-1/2 text-right">
          <span class="ml-2 text-lg">{{ getMoneyFormat(item.amount) }}</span>
        </span>
        <span class="w-1/2 text-right">
          <span class="ml-2 text-lg">{{ getMoneyFormat(getAvailableFundByProductGroupId(item.productGroup.id)) }}</span>
        </span>
      </div>
    </li>
  </ul>

  <PfButtonAction
    class="w-full"
    btn-style="secondary"
    :label="t('create-new-transaction-btn')"
    @click="emit('onUpdateStep', TRANSACTION_FINISH, {})" />
</template>

<script setup>
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { computed, defineProps, defineEmits } from "vue";
import { useQuery, useResult } from "@vue/apollo-composable";

import { TRANSACTION_FINISH, PRODUCT_GROUP_LOYALTY } from "@/lib/consts/enums";

import { getMoneyFormat } from "@/lib/helpers/money";
import { getColorBgClass } from "@/lib/helpers/products-color";
import { usePageTitle } from "@/lib/helpers/page-title";

const { t } = useI18n();
usePageTitle(t("title"));

const props = defineProps({
  transactionId: {
    type: String,
    required: true
  }
});

const emit = defineEmits(["onUpdateStep", "onUpdateLoadingState"]);

const { result } = useQuery(
  gql`
    query Transaction($id: ID!) {
      transaction(id: $id) {
        id
        amount
        ... on PaymentTransactionGraphType {
          transactionByProductGroups {
            id
            amount
            productGroup {
              id
              name
              color
            }
          }
          card {
            id
            programCardId
            totalFund
            beneficiary {
              id
              firstname
              lastname
            }
            funds {
              id
              amount
              productGroup {
                id
                orderOfAppearance
                color
              }
            }
            loyaltyFund {
              id
              amount
              productGroup {
                id
                orderOfAppearance
                color
              }
            }
          }
          market {
            id
            name
          }
        }
      }
    }
  `,
  {
    id: props.transactionId
  }
);
const transaction = useResult(result);

const amount = computed(() => {
  if (transaction.value !== undefined) {
    return transaction.value.amount;
  }
  return "";
});

const fund = computed(() => {
  if (transaction.value !== undefined) {
    return transaction.value.card.totalFund + (loyaltyFund.value ? loyaltyFund.value.amount : 0);
  }
  return "";
});

const transactionByProductGroups = computed(() => {
  if (transaction.value !== undefined) {
    return transaction.value.transactionByProductGroups;
  }
  return [];
});

const funds = computed(() => {
  if (transaction.value !== undefined) {
    return transaction.value.card.funds;
  }
  return [];
});

const loyaltyFund = computed(() => {
  if (transaction.value !== undefined) {
    return transaction.value.card.loyaltyFund;
  }
  return null;
});

function getProductGroupName(fund) {
  let productGroupName = fund.productGroup.name;
  if (productGroupName === PRODUCT_GROUP_LOYALTY) {
    productGroupName = t("gift-card");
  }

  return productGroupName;
}

function getAvailableFundByProductGroupId(productGroupId) {
  const fund = funds.value.find((x) => x.productGroup.id === productGroupId);
  if (fund) {
    return fund.amount;
  }
  if (productGroupId === loyaltyFund.value.productGroup.id) {
    return loyaltyFund.value.amount;
  }

  return "";
}

function getIsGiftCard(productGroupName) {
  if (productGroupName === PRODUCT_GROUP_LOYALTY) return true;
  else return false;
}
</script>
