<i18n>
  {
    "en": {
      "beneficiary-count-label": "Number of participants",
      "manage-beneficiary-btn": "Manage participants",
      "unspend-loyalty-fund-label": "Unspent amount on gift cards",
      "total-active-subscriptions-envelopes-label": "Budget allowance total (active subscriptions)",
      "organization-list-stats": "By subscription",
      "empty-list": "No group is associated with the program.",
      "funds-expiration-trigger-number-of-days":"{numberOfDays} days after the first use",
      "funds-not-accumulable-expiration": "Expiration at the next payment period"
    },
    "fr": {
      "beneficiary-count-label": "Nombre de participants",
      "manage-beneficiary-btn": "Gérer les participants",
      "unspend-loyalty-fund-label": "Montant non-dépensé sur les cartes-cadeaux",
      "total-active-subscriptions-envelopes-label": "Total des enveloppes (abonnements actifs)",
      "organization-list-stats": "Par abonnement",
      "empty-list": "Aucun abonnement n'est associé au groupe.",
      "funds-expiration-trigger-number-of-days": "{numberOfDays} jours après la première utilisation",
      "funds-not-accumulable-expiration": "Expiration lors de la prochaine période de versement"
    }
  }
  </i18n>

<template>
  <div class="flex flex-col gap-6 sm:flex-row mt-4 mb-12">
    <UiStat
      class="sm:w-1/3"
      :stat="organization?.beneficiaries.totalCount"
      :label="t('beneficiary-count-label')"
      :secondary-btn-route="{ name: URL_BENEFICIARY_ADMIN }"
      :secondary-btn-label="t('manage-beneficiary-btn')" />
    <UiStat
      class="sm:w-1/3"
      :stat="getMoneyFormat(organization?.budgetAllowancesTotal)"
      :label="t('total-active-subscriptions-envelopes-label')" />
  </div>
  <div v-if="subscriptionStats">
    <template v-if="subscriptionStats.length > 0">
      <SubscriptionStatsTable :subscription-stats="subscriptionStats" />
    </template>
    <div v-else class="flex items-center justify-center my-8 lg:my-16">
      <UiCta :img-src="require('@/assets/img/organismes.jpg')" :description="t('empty-list')" />
    </div>
  </div>
</template>

<script setup>
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { useQuery, useResult } from "@vue/apollo-composable";

import { URL_BENEFICIARY_ADMIN } from "@/lib/consts/urls";
import { SPECIFIC_DATE, NUMBER_OF_DAYS } from "@/lib/consts/funds-expiration-trigger";

import { getMoneyFormat } from "@/lib/helpers/money";
import { formatDate, textualFormat } from "@/lib/helpers/date";

import SubscriptionStatsTable from "@/components/dashboard/subscription-stats-table.vue";

const { t } = useI18n();

const { result: resultOrganizations } = useQuery(
  gql`
    query Organizations {
      organizations {
        id
        beneficiaries(page: 1, limit: 20) {
          totalCount
        }
        budgetAllowancesTotal
        budgetAllowances {
          id
          availableFund
          originalFund
          subscription {
            id
            name
            startDate
            endDate
            fundsExpirationDate
            triggerFundExpiration
            isFundsAccumulable
            numberDaysUntilFundsExpire
          }
        }
      }
    }
  `
);
const organization = useResult(resultOrganizations, null, (data) => {
  return data.organizations[0];
});

const subscriptionStats = useResult(resultOrganizations, null, (data) => {
  return data.organizations[0].budgetAllowances.map((budgetAllowance) => {
    return {
      name: budgetAllowance.subscription.name,
      startDate: budgetAllowance.subscription.startDate,
      endDate: budgetAllowance.subscription.endDate,
      expiration: !budgetAllowance.subscription.isFundsAccumulable
        ? t("funds-not-accumulable-expiration")
        : budgetAllowance.subscription.triggerFundExpiration === SPECIFIC_DATE
        ? formatDate(new Date(budgetAllowance.subscription.fundsExpirationDate), textualFormat)
        : budgetAllowance.subscription.triggerFundExpiration === NUMBER_OF_DAYS
        ? t("funds-expiration-trigger-number-of-days", {
            numberOfDays: budgetAllowance.subscription.numberDaysUntilFundsExpire
          })
        : "",
      originalFund: budgetAllowance.originalFund,
      availableFund: budgetAllowance.availableFund,
      amountAllocated: budgetAllowance.originalFund - budgetAllowance.availableFund
    };
  });
});
</script>
