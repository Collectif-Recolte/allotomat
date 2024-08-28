<i18n>
{
	"en": {
		"card-description": "Program {programName}",
    "card-id": "Card #{cardId}",
		"done": "Check another card",
		"title": "Card balance",
    "card-is-disabled": "The card is disabled."
	},
	"fr": {
		"card-description": "Programme {programName}",
    "card-id": "Carte #{cardId}",
		"done": "Vérifier une autre carte",
		"title": "Solde de la carte",
    "card-is-disabled": "La carte est désactivée."
	}
}
</i18n>

<template>
  <div class="min-h-app px-8 py-10 flex items-center justify-center">
    <div class="bg-white rounded-2xl min-w-80 p-6 h-remove-margin relative text-center">
      <UiIconComplete />
      <h1 class="font-semibold mt-4 mb-2 text-h2 xs:text-h1">{{ t("title") }}</h1>
      <p v-if="card" class="text-p3">
        {{ t("card-description", { programName }) }}<br />
        {{ t("card-id", { cardId }) }}
      </p>
      <p v-if="card && card.isDisabled" class="text-red-500 font-bold">{{ t("card-is-disabled") }}</p>
      <p v-if="card" class="font-bold text-primary-700 text-h1 leading-none xs:text-d2 mt-6 mb-8">
        {{ getMoneyFormat(fund) }}
      </p>

      <ProductGroupFundList display-expiration-date class="mb-6" :product-groups="getProductGroups(allFunds)" />
      <TransactionList v-if="card" :transactions="card.transactions" :page="page" @update:page="updatePage" />

      <div class="mt-4">
        <PfButtonAction btn-style="link" size="sm" :label="t('done')" @click="emit('onUpdateStep', CHECK_CARD_STEPS_START)" />
      </div>
    </div>
  </div>
</template>

<script setup>
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { ref, computed, defineEmits, defineProps, watch } from "vue";
import { useQuery, useResult } from "@vue/apollo-composable";

import { CHECK_CARD_STEPS_START, PRODUCT_GROUP_LOYALTY } from "@/lib/consts/enums";

import { getMoneyFormat } from "@/lib/helpers/money";

import TransactionList from "@/components/transaction/transaction-list";
import ProductGroupFundList from "@/components/product-groups/product-group-fund-list";

const { t } = useI18n();

const page = ref(1);

const props = defineProps({
  cardId: {
    type: String,
    required: true
  }
});

const emit = defineEmits(["onUpdateStep", "onUpdateLoadingState"]);

const { result, loading } = useQuery(
  gql`
    query Card($id: ID!, $page: Int!) {
      card(id: $id) {
        id
        isDisabled
        project {
          id
          name
        }
        totalFund
        transactions(page: $page, limit: 8) {
          totalCount
          totalPages
          items {
            id
            amount
            createdAt
          }
        }
        addingFundTransactions {
          expirationDate
          availableFund
          status
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
        programCardId
      }
    }
  `,
  {
    id: props.cardId,
    page: page
  }
);
const card = useResult(result, null, (data) => data.card);

watch(loading, (loading) => {
  emit("onUpdateLoadingState", loading);
});

const fund = computed(() => {
  var total = 0;
  if (card.value !== undefined && card.value !== null) {
    total = card.value.totalFund;
    if (card.value.loyaltyFund) {
      total += card.value.loyaltyFund.amount;
    }
  }
  return total;
});

const cardId = computed(() => {
  if (card.value !== undefined) {
    return card.value.programCardId;
  }
  return "";
});

const allFunds = computed(() => {
  let funds = [];
  if (card.value !== undefined && card.value !== null) {
    if (card.value.addingFundTransactions) {
      funds = [...card.value.addingFundTransactions];
    }
    if (card.value.loyaltyFund) {
      funds.push(card.value.loyaltyFund);
    }
  }

  return funds;
});

const programName = computed(() => {
  if (card.value !== undefined) {
    return card.value.project.name;
  }
  return "";
});

const getProductGroups = (funds) => {
  const productGroups = [];
  for (let fund of funds) {
    if (
      fund.availableFund > 0 &&
      (fund.expirationDate > new Date().toISOString() || fund.productGroup.name === PRODUCT_GROUP_LOYALTY)
    ) {
      if (productGroups.find((pg) => pg.label === fund.productGroup.name && pg.expirationDate === fund.expirationDate)) {
        productGroups.find((pg) => pg.label === fund.productGroup.name).fund += fund.amount ?? fund.availableFund;
      } else {
        productGroups.push({
          color: fund.productGroup.color,
          label: fund.productGroup.name,
          fund: fund.amount ?? fund.availableFund,
          expirationDate: fund.expirationDate
        });
      }
    }
  }
  return productGroups;
};

function updatePage(value) {
  page.value = value;
}
</script>
