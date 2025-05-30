<i18n>
{
	"en": {
		"unsubscribing": "Unsubscribing from payment receipt emails...",
		"unsubscribed": "You have been unsubscribed from payment receipt emails.",
		"error": "There was a problem unsubscribing from payment receipt emails.",
		"title": "Unsubscribe"
	},
	"fr": {
		"unsubscribing": "Désabonnement des courriels de reçu de paiement...",
		"unsubscribed": "Vous avez été désabonné des courriels de reçu de paiement.",
		"error": "Il y a eu un problème lors du désabonnement des courriels de reçu de paiement.",
		"title": "Désabonnement"
	}
}
</i18n>
<template>
  <PublicShell :title="t('title')">
    <template v-if="showError">
      <PfNote bg-color-class="bg-red-50">
        <template #content>
          <p class="text-sm text-red-900">{{ t("error") }}</p>
        </template>
      </PfNote>
    </template>
    <div v-else-if="isUnsubscribed" class="flex items-center">
      <p class="mb-0">{{ t("unsubscribed") }}</p>
    </div>
    <div v-else class="flex items-center">
      <PfSpinner class="mr-3" is-small aria-hidden="true" />
      <p class="mb-0">{{ t("unsubscribing") }}</p>
    </div>
  </PublicShell>
</template>

<script setup>
import gql from "graphql-tag";
import { onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute } from "vue-router";
import { useMutation } from "@vue/apollo-composable";

import LoggerService from "@/lib/services/logger";
import { usePageTitle } from "@/lib/helpers/page-title";

const { t } = useI18n();

const showError = ref(false);
const isUnsubscribed = ref(false);

const route = useRoute();

const { beneficiaryId } = route.query;

const { mutate: unsubscribeBeneficiaryFromTransactionReceiptMutation } = useMutation(
  gql`
    mutation UnsubscribeBeneficiaryFromTransactionReceipt($input: UnsubscribeBeneficiaryFromTransactionReceiptInput!) {
      unsubscribeBeneficiaryFromTransactionReceipt(input: $input)
    }
  `
);

usePageTitle(t("title"));

onMounted(unsubscribeBeneficiary);

async function unsubscribeBeneficiary() {
  try {
    await unsubscribeBeneficiaryFromTransactionReceiptMutation({ input: { beneficiaryId } });
    isUnsubscribed.value = true;
  } catch (err) {
    showError.value = true;
    LoggerService.logError(`Error unsubscribing from transaction receipt. ${err}`);
  }
}
</script>
