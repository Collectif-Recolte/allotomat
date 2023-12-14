/* eslint-disable @intlify/vue-i18n/no-unused-keys */
<i18n>
{
	"en": {
		"amount-label": "{productGroupName}",
    "amount-after-label": "Balance: {amountAvailable}",
    "confirmation-amount-label": "{productGroupName}",
    "amount-validation-label": "Amount",
		"amount-placeholder": "Ex. {amount}",
		"cancel": "Cancel",
    "product-groups": "Product groups",
		"product-group-fund-not-enought": "The product group does not have enough funds",
		"card-selected": "Card #{cardProgramCardId}",
		"create-transaction": "Pay",
		"title": "Transaction",
    "title-confirm": "Confirmation",
    "amount-charged": "The card will be charged ",
    "confirm": "Confirm",
    "edit": "Revise",
    "gift-card": "Gift card",
    "no-product-group-transaction":"At least one product group must have an amount to create a transaction.",
    "no-funds-message": "There are no available funds on this card."
	},
	"fr": {
		"amount-label": "{productGroupName}",
    "amount-after-label": "Solde: {amountAvailable}",
    "confirmation-amount-label": "{productGroupName}",
    "amount-validation-label": "Solde",
		"amount-placeholder": "Ex. {amount}",
		"cancel": "Annuler",
    "product-groups": "Groupes de produits",
		"product-group-fund-not-enought": "Le groupe de produits ne possède pas assez de fonds",
		"card-selected": "Carte #{cardProgramCardId}",
		"create-transaction": "Payer",
		"title": "Transaction",
    "title-confirm": "Confirmation",
    "amount-charged": "La carte sera débitée de ",
    "confirm": "Confirmer",
    "edit": "Réviser",
    "gift-card": "Carte-cadeau",
    "no-product-group-transaction":"Au minimum un groupe de produit doit avoir un montant pour créer une transaction.",
    "no-funds-message": "Il n'y a pas de fonds disponibles sur cette carte."
	}
}
</i18n>

<template>
  <div class="py-5 px-4 xs:px-8">
    <div class="bg-white rounded-2xl pt-6 pb-3 px-3 xs:p-6 h-remove-margin">
      <h1 class="font-semibold mb-2">{{ currentStep === 0 ? t("title") : t("title-confirm") }}</h1>
      <AddTransaction
        :cardId="props.cardId"
        :marketId="myMarket.id"
        @updateStep="(e) => emit('onUpdateStep', e)"
        @updateLoadingState="(e) => emit('onUpdateLoadingState', e)" />
    </div>
  </div>
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult } from "@vue/apollo-composable";
import { defineEmits, defineProps } from "vue";

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
