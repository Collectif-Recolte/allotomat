/* eslint-disable @intlify/vue-i18n/no-unused-keys */
<i18n>
{
	"en": {
		"title": "Transaction"
	},
	"fr": {
		"title": "Transaction"
	}
}
</i18n>

<template>
  <div class="py-5 px-4 xs:px-8">
    <div class="bg-white rounded-2xl pt-6 pb-3 px-3 xs:p-6 h-remove-margin">
      <h1 class="font-semibold mb-2">{{ t("title") }}</h1>
      <AddTransaction
        v-if="myMarket"
        :cardId="props.cardId"
        :marketId="myMarket.id"
        @onUpdateStep="(stepName, values) => emit('onUpdateStep', stepName, values)"
        @onUpdateLoadingState="(e) => emit('onUpdateLoadingState', e)"
        @onCloseModal="emit('onUpdateStep', TRANSACTION_STEPS_START, {})" />
    </div>
  </div>
</template>

<script setup>
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { useQuery, useResult } from "@vue/apollo-composable";
import { defineEmits, defineProps } from "vue";

import { TRANSACTION_STEPS_START } from "@/lib/consts/enums";

import AddTransaction from "@/components/transaction/add-transaction";

const { t } = useI18n();

const props = defineProps({
  cardId: {
    type: String,
    required: true
  }
});

const { result: resultMarkets } = useQuery(
  gql`
    query Markets {
      markets {
        id
      }
    }
  `
);
const myMarket = useResult(resultMarkets, null, (data) => data.markets[0]);

const emit = defineEmits(["onUpdateStep", "onUpdateLoadingState"]);
</script>
