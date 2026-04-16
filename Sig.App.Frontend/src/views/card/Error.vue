<i18n>
{
	"en": {
		"card-deactivated": "The card is deactivated.",
		"card-not-found": "QR code does not equate to a known card.",
		"scan-btn": "Scan another card",
		"error-description": "Balance check could not be completed for the following reason:",
		"title": "Error 🚫"
	},
	"fr": {
		"card-deactivated": "La carte est désactivée.",
		"card-not-found": "Le code QR n'équivaut pas à une carte connue.",
		"scan-btn": "Scanner une autre carte",
		"error-description": "La vérification du solde n'a pas pu être complétée pour la raison suivante:",
		"title": "Erreur 🚫"
	}
}
</i18n>

<template>
  <UiDialogModal
    :return-route="{ name: URL_CARD_CHECK, query: { isScan: true } }"
    :title="t('title')"
    :close-label="t('scan-btn')">
    <p>{{ t("error-description") }}</p>
    <p>{{ error }}</p>
  </UiDialogModal>
</template>

<script setup>
import { useI18n } from "vue-i18n";
import { computed } from "vue";
import { useRoute } from "vue-router";

import { URL_CARD_CHECK } from "@/lib/consts/urls";
import { CARD_NOT_FOUND, CARD_DEACTIVATED } from "@/lib/consts/qr-code-error";

const { t } = useI18n();
const route = useRoute();

const error = computed(() => {
  switch (route.query.error) {
    case CARD_NOT_FOUND:
      return t("card-not-found");
    case CARD_DEACTIVATED:
      return t("card-deactivated");
    default:
      return "";
  }
});
</script>
