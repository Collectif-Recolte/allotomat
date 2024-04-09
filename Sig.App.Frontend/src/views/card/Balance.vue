<i18n>
{
	"en": {
		"card-description": "Program {programName} — Card #{cardId}",
		"done": "Check another card",
		"title": "Card balance"
	},
	"fr": {
		"card-description": "Programme {programName} — Carte #{cardId}",
		"done": "Vérifier une autre carte",
		"title": "Solde de la carte"
	}
}
</i18n>

<template>
  <div class="min-h-app px-8 py-10 flex items-center justify-center">
    <div class="bg-white rounded-2xl p-6 h-remove-margin relative text-center">
      <UiIconComplete />
      <h1 class="font-semibold mt-4 mb-2 text-h2 xs:text-h1">{{ t("title") }}</h1>
      <p v-if="card" class="text-p3">
        {{ t("card-description", { programName, cardId }) }}
      </p>
      <p v-if="card" class="font-bold text-primary-700 text-h1 xs:text-d2 my-6">
        {{ getMoneyFormat(fund) }}
      </p>

      <ProductGroupFundList class="flex justify-center mb-6" :product-groups="getProductGroups(allFunds)" />

      <div class="border-t border-grey-100 pt-4">
        <PfButtonAction btn-style="link" size="sm" :label="t('done')" @click="emit('onUpdateStep', CHECK_CARD_STEPS_START)" />
      </div>
    </div>
  </div>
</template>

<script setup>
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { computed, defineEmits, defineProps, watch } from "vue";
import { useQuery, useResult } from "@vue/apollo-composable";

import { CHECK_CARD_STEPS_START, PRODUCT_GROUP_LOYALTY } from "@/lib/consts/enums";

import { getMoneyFormat } from "@/lib/helpers/money";

import ProductGroupFundList from "@/components/product-groups/product-group-fund-list";

const { t } = useI18n();

const props = defineProps({
  cardId: {
    type: String,
    required: true
  }
});

const emit = defineEmits(["onUpdateStep", "onUpdateLoadingState"]);

const { result, loading } = useQuery(
  gql`
    query Card($id: ID!) {
      card(id: $id) {
        id
        project {
          id
          name
        }
        totalFund
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
    id: props.cardId
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
    if (fund.expirationDate > new Date().toISOString() || fund.productGroup.name === PRODUCT_GROUP_LOYALTY) {
      productGroups.push({
        color: fund.productGroup.color,
        label: fund.productGroup.name,
        fund: fund.amount ?? fund.availableFund,
        expirationDate: fund.expirationDate
      });
    }
  }
  return productGroups;
};
</script>
