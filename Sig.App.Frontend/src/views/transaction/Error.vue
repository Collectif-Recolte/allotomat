<i18n>
{
	"en": {
		"card-cant-be-use-in-market": "The participant cannot make a purchase in this market",
		"card-deactivated": "The card is disabled.",
		"card-not-found": "QR code does not equate to a known user.",
		"create-new-transaction-btn": "Create a new transaction",
		"not-enought-fund": "The participant does not have enough funds",
    "card-cant-be-use-in-cash-register": "The participant cannot make a purchase with this cash register",
		"title": "Error during payment 🚫"
	},
	"fr": {
		"card-cant-be-use-in-market": "Le-la participant-e ne peut pas faire d'achat dans ce commerce",
		"card-deactivated": "La carte est désactivée.",
		"card-not-found": "Le code QR n'équivaut pas à un utilisateur connu.",
		"create-new-transaction-btn": "Créer une nouvelle transaction",
		"not-enought-fund": "Le-la participant-e ne possède pas assez de fond",
    "card-cant-be-use-in-cash-register": "Le-la participant-e ne peut pas faire d'achat avec cette caisse",
		"title": "Erreur lors du paiement 🚫"
	}
}
</i18n>

<template>
  <UiDialogModal
    :return-route="{ name: URL_TRANSACTION, query: { isScan: true } }"
    :title="t('title')"
    :close-label="t('create-new-transaction-btn')">
    <p>{{ error }}</p>
  </UiDialogModal>
</template>

<script setup>
import { useI18n } from "vue-i18n";
import { computed } from "vue";
import { useRoute } from "vue-router";

import { URL_TRANSACTION } from "@/lib/consts/urls";
import {
  CARD_CANT_BE_USED_IN_MARKET,
  CARD_NOT_FOUND,
  CARD_DEACTIVATED,
  NOT_ENOUGHT_FUND,
  CARD_CANT_BE_USED_WITH_CASH_REGISTER
} from "@/lib/consts/qr-code-error";

const { t } = useI18n();
const route = useRoute();

const error = computed(() => {
  switch (route.query.error) {
    case CARD_CANT_BE_USED_IN_MARKET:
      return t("card-cant-be-use-in-market");
    case CARD_NOT_FOUND:
      return t("card-not-found");
    case CARD_DEACTIVATED:
      return t("card-deactivated");
    case NOT_ENOUGHT_FUND:
      return t("not-enought-fund");
    case CARD_CANT_BE_USED_WITH_CASH_REGISTER:
      return t("card-cant-be-use-in-cash-register");
    default:
      return "";
  }
});
</script>
