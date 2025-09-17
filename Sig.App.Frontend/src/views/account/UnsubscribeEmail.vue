<i18n>
{
	"en": {
		"unsubscribing": "Unsubscribing from {emailType} emails...",
		"unsubscribed": "You have been unsubscribed from {emailType} emails.",
		"error": "There was a problem unsubscribing from {emailType} emails.",
		"title": "Unsubscribe",
    "created-card-pdf-email": "created card pdf",
    "monthly-balance-report-email": "monthly balance report",
    "monthly-card-balance-report-email": "monthly card balance report",
    "subscription-expiration-email": "subscription expiration"
	},
	"fr": {
		"unsubscribing": "Désabonnement des courriels de {emailType}...",
		"unsubscribed": "Vous avez été désabonné des courriels de {emailType}.",
		"error": "Il y a eu un problème lors du désabonnement des courriels de {emailType}.",
		"title": "Désabonnement",
    "created-card-pdf-email": "fichier d'impression des cartes",
    "monthly-balance-report-email": "rapport mensuel de transaction",
    "monthly-card-balance-report-email": "rapport mensuel des cartes",
    "subscription-expiration-email": "fin d'abonnement"
	}
}
</i18n>
<template>
  <PublicShell :title="t('title', { emailType: getEmailTypeText() })">
    <template v-if="showError">
      <PfNote bg-color-class="bg-red-50">
        <template #content>
          <p class="text-sm text-red-900">{{ t("error", { emailType: getEmailTypeText() }) }}</p>
        </template>
      </PfNote>
    </template>
    <div v-else class="flex items-center">
      <PfSpinner class="mr-3" is-small aria-hidden="true" />
      <p class="mb-0">{{ t("unsubscribing", { emailType: getEmailTypeText() }) }}</p>
    </div>
  </PublicShell>
</template>

<script setup>
import gql from "graphql-tag";
import { onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { useMutation } from "@vue/apollo-composable";

import { useNotificationsStore } from "@/lib/store/notifications";

import LoggerService from "@/lib/services/logger";
import { usePageTitle } from "@/lib/helpers/page-title";

import { URL_ROOT } from "@/lib/consts/urls";

const { t } = useI18n();
const { addSuccess } = useNotificationsStore();

const showError = ref(false);

const route = useRoute();
const router = useRouter();

const { emailType } = route.query;

const { mutate: unsubscribeFromEmailMutation } = useMutation(
  gql`
    mutation UnsubscribeFromEmail($input: UnsubscribeFromEmailInput!) {
      unsubscribeFromEmail(input: $input)
    }
  `
);

usePageTitle(t("title"));

onMounted(unsubscribeBeneficiary);

async function unsubscribeBeneficiary() {
  try {
    await unsubscribeFromEmailMutation({ input: { emailType: getEmailType() } });
    addSuccess(t("unsubscribed", { emailType: getEmailTypeText() }));
    router.push({ name: URL_ROOT });
  } catch (err) {
    showError.value = true;
    LoggerService.logError(`Error unsubscribing from email. ${err}`);
  }
}

function getEmailType() {
  switch (emailType) {
    case "CreatedCardPdfEmail":
      return "CREATED_CARD_PDF_EMAIL";
    case "MonthlyBalanceReportEmailJanuary":
      return "MONTHLY_BALANCE_REPORT_EMAIL_JANUARY";
    case "MonthlyCardBalanceReportEmailJanuary":
      return "MONTHLY_CARD_BALANCE_REPORT_EMAIL_JANUARY";
    case "SubscriptionExpirationEmail":
      return "SUBSCRIPTION_EXPIRATION_EMAIL";
  }
}

function getEmailTypeText() {
  switch (emailType) {
    case "CreatedCardPdfEmail":
      return t("created-card-pdf-email");
    case "MonthlyBalanceReportEmailJanuary":
      return t("monthly-balance-report-email");
    case "MonthlyCardBalanceReportEmailJanuary":
      return t("monthly-card-balance-report-email");
    case "SubscriptionExpirationEmail":
      return t("subscription-expiration-email");
  }
}
</script>
