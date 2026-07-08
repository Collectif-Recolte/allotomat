<i18n>
  {
    "en": {
      "card-description": "Balance",
      "create-new-transaction-btn": "Done",
      "payment-description": "Payment",
      "title": "Transaction completed",
      "gift-card": "Gift card",
      "card-id": "card #",
      "returning-to-menu": "Returning to the main menu in {seconds} s"
    },
    "fr": {
      "card-description": "Solde",
      "create-new-transaction-btn": "Terminer",
      "payment-description": "Paiement",
      "title": "Transaction complétée",
      "gift-card": "Carte-cadeau",
      "card-id": "# de carte",
      "returning-to-menu": "Retour au menu principal dans {seconds} s"
    }
  }
  </i18n>

<template>
  <h1 class="text-center" :class="props.isKiosk ? 'text-d2 font-bold text-primary-700' : 'mt-4 font-semibold'">
    {{ t("title") }}
  </h1>
  <div v-if="transaction" class="flex mx-2 gap-x-2">
    <p class="w-1/3 leading-tight text-center">
      <span class="inline-block max-w-32 uppercase text-p3 font-bold leading-none">{{ t("card-id") }}</span>
      <span class="block font-bold text-primary-700 text-d7 xs:text-d3">{{ transaction.card?.programCardId }}</span>
    </p>
    <p class="w-1/3 leading-tight text-center">
      <span class="inline-block max-w-32 uppercase text-p3 font-bold leading-none">{{ t("payment-description") }}</span>
      <span class="block font-bold text-primary-700 text-d7 xs:text-d3">{{ getMoneyFormat(amount) }}</span>
    </p>
    <p class="w-1/3 leading-tight text-center">
      <span class="inline-block max-w-24 uppercase text-p3 font-bold leading-none">{{ t("card-description") }}</span>
      <span class="block font-bold text-primary-700 text-d7 xs:text-d3">{{ getMoneyFormat(fund) }}</span>
    </p>
  </div>

  <ul class="mb-6 w-full" :class="{ 'max-h-[40vh] overflow-y-auto': props.isKiosk }">
    <li
      v-for="item in transactionByProductGroups"
      :key="item.id"
      class="mb-4 last:mb-0 text-p2 first:pt-5 first:border-t-2 first:border-grey-100 last:border-b-2 last:border-grey-100 last:pb-5"
      :class="getIsGiftCard(item.productGroup.name) ? 'mt-6 pt-5 border-t border-grey-100' : 'dark'">
      <div v-if="props.isKiosk" class="flex mx-2 gap-x-2">
        <div class="w-1/3 flex justify-center min-w-0">
          <span
            class="rounded-md border-2 px-3 py-1 font-bold truncate max-w-full text-p2 text-center"
            :class="getKioskCategoryClasses(item)">
            {{ getProductGroupName(item) }}
          </span>
        </div>
        <div class="w-1/3 text-center min-w-0">
          <span class="font-bold text-d6 text-primary-900">
            {{ getMoneyFormat(item.amount) }}
          </span>
        </div>
        <div class="w-1/3 text-center min-w-0">
          <span class="font-bold text-d6 text-grey-600">
            {{ getMoneyFormat(getAvailableFundByProductGroupId(item.productGroup.id)) }}
          </span>
        </div>
      </div>
      <div
        v-else
        class="relative flex items-center w-full rounded-md py-1 px-2 text-primary-900 dark:text-white"
        :class="getIsGiftCard(item.productGroup.name) ? getGiftCardBgClass() : getColorBgClass(item.productGroup.color)">
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
    :class="{ 'min-h-20 rounded-2xl text-d6': props.isKiosk }"
    :btn-style="props.isKiosk ? 'primary' : 'secondary'"
    :size="props.isKiosk ? 'lg' : undefined"
    :label="t('create-new-transaction-btn')"
    @click="onFinish" />
  <p v-if="props.isKiosk && purchaseCompleteSecondsRemaining > 0" class="text-p1 text-grey-600 mt-3 mb-0 text-center">
    {{ t("returning-to-menu", { seconds: purchaseCompleteSecondsRemaining }) }}
  </p>
</template>

<script setup>
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { computed, defineProps, defineEmits } from "vue";
import { useQuery, useResult } from "@vue/apollo-composable";

import { TRANSACTION_FINISH, PRODUCT_GROUP_LOYALTY } from "@/lib/consts/enums";
import { useKioskPurchaseCompleteCountdown } from "@/lib/composables/use-kiosk-shell";

import { getMoneyFormat } from "@/lib/helpers/money";
import { getColorBgClass, getGiftCardBgClass, getKioskProductGroupCardClasses } from "@/lib/helpers/products-color";
import { usePageTitle } from "@/lib/helpers/page-title";

const { t } = useI18n();
usePageTitle(t("title"));

const props = defineProps({
  transactionId: {
    type: String,
    required: true
  },
  isKiosk: Boolean
});

const emit = defineEmits(["onUpdateStep", "onUpdateLoadingState", "finished"]);

const purchaseCompleteSecondsRemaining = useKioskPurchaseCompleteCountdown();

function onFinish() {
  if (props.isKiosk) {
    emit("finished");
    return;
  }
  emit("onUpdateStep", TRANSACTION_FINISH, {});
}

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

function getKioskCategoryClasses(item) {
  const styles = getKioskProductGroupCardClasses(item.productGroup.color, getIsGiftCard(item.productGroup.name), true);
  return [styles.border, styles.bg, styles.text];
}
</script>
