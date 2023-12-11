<i18n>
{
	"en": {
		"card-description": "Balance",
		"create-new-transaction-btn": "Done",
		"payment-description": "Refund",
		"title": "Refund completed",
    "gift-card": "Gift card"
	},
	"fr": {
		"card-description": "Solde",
		"create-new-transaction-btn": "Terminer",
		"payment-description": "Remboursement",
		"title": "Remboursement complété",
    "gift-card": "Carte-cadeau"
	}
}
</i18n>

<template>
  <div class="min-h-app px-4 xs:px-8 py-10 flex items-center justify-center">
    <div class="bg-white rounded-2xl pt-6 pb-3 px-3 h-remove-margin relative xs:p-6 w-full xs:w-auto xs:min-w-[460px]">
      <UiIconComplete />
      <h1 class="font-semibold text-center mt-4">{{ t("title") }}</h1>
      <div v-if="refundTransaction" class="flex mx-2 gap-x-2">
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
            :class="[
              getColorBgClass(item.productGroup.color),
              getIsGiftCard(item.productGroup.name) ? 'bg-diagonal-pattern' : ''
            ]">
            <div class="absolute -translate-y-1/2 top-1/2 left-2 max-w-20 xs:max-w-24 truncate font-bold">
              {{ getProductGroupName(item) }}
            </div>

            <span class="w-1/2 text-right">
              <span class="ml-2">{{ getMoneyFormat(item.amount) }}</span>
            </span>
            <span class="w-1/2 text-right">
              <span class="ml-2">{{ getMoneyFormat(getAvailableFundByProductGroupId(item.productGroup.id)) }}</span>
            </span>
          </div>
        </li>
      </ul>

      <PfButtonAction
        class="w-full"
        btn-style="secondary"
        :label="t('create-new-transaction-btn')"
        @click="goToTransactionList" />
    </div>
  </div>
</template>

<script setup>
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { computed, defineProps } from "vue";
import { useQuery, useResult } from "@vue/apollo-composable";
import { useRouter } from "vue-router";

import { PRODUCT_GROUP_LOYALTY } from "@/lib/consts/enums";
import { URL_TRANSACTION_LIST } from "@/lib/consts/urls";

import { getMoneyFormat } from "@/lib/helpers/money";
import { getColorBgClass } from "@/lib/helpers/products-color";
import { usePageTitle } from "@/lib/helpers/page-title";

const { t } = useI18n();
const router = useRouter();

usePageTitle(t("title"));

const props = defineProps({
  transactionId: {
    type: String,
    required: true
  }
});

const { result } = useQuery(
  gql`
    query RefundTransaction($id: ID!) {
      refundTransaction(id: $id) {
        id
        amount
        ... on RefundTransactionGraphType {
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
          initialTransaction {
            market {
              id
              name
            }
          }
        }
      }
    }
  `,
  {
    id: props.transactionId
  }
);
const refundTransaction = useResult(result);

const amount = computed(() => {
  if (refundTransaction.value !== undefined) {
    return refundTransaction.value.amount;
  }
  return "";
});

const fund = computed(() => {
  if (refundTransaction.value !== undefined) {
    return refundTransaction.value.card.totalFund + (loyaltyFund.value ? loyaltyFund.value.amount : 0);
  }
  return "";
});

const transactionByProductGroups = computed(() => {
  if (refundTransaction.value !== undefined) {
    return refundTransaction.value.transactionByProductGroups;
  }
  return [];
});

const funds = computed(() => {
  if (refundTransaction.value !== undefined) {
    return refundTransaction.value.card.funds;
  }
  return [];
});

const loyaltyFund = computed(() => {
  if (refundTransaction.value !== undefined) {
    return refundTransaction.value.card.loyaltyFund;
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
  return productGroupName === PRODUCT_GROUP_LOYALTY;
}

const goToTransactionList = () => {
  router.push({ name: URL_TRANSACTION_LIST });
};
</script>
