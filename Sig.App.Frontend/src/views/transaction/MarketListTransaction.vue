<i18n>
  {
    "en": {
      "title": "Transaction history",
      "transaction-count": "{count} transaction / {money} | {count} transactions / {money}",
      "date-selector-from": "Interval from",
      "date-selector-to": "to",
      "date-start-label": "Start date of transactions",
      "date-end-label": "End date of transactions",
      "empty-list": "There are no transactions for the chosen dates.",
      "export-btn": "Export report",
    },
    "fr": {
      "title": "Historique des transactions",
      "transaction-count": "{count} transaction / {money} | {count} transactions / {money}",
      "date-selector-from": "Intervalle du",
      "date-selector-to": "au",
      "date-start-label": "Date de début des transactions",
      "date-end-label": "Date de fin des transactions",
      "empty-list": "Il n'existe aucune transaction pour les dates choisies.",
      "export-btn": "Exporter un rapport",
    }
  }
</i18n>

<template>
  <RouterView v-slot="{ Component }">
    <AppShell :title="t('title')" :loading="loading" class="market-list-transaction-vue">
      <template #title>
        <Title :title="t('title')">
          <template #center>
            <div class="flex items-center gap-x-4">
              <span class="text-sm text-primary-700">{{ t("date-selector-from") }}</span>
              <UiDatePicker
                id="datefrom"
                v-model="dateFrom"
                class="sm:col-span-6"
                :label="t('date-start-label')"
                has-hidden-label />
            </div>
          </template>
          <template #right>
            <div class="flex items-center gap-x-4">
              <span class="text-sm text-primary-700">{{ t("date-selector-to") }}</span>
              <UiDatePicker
                id="dateTo"
                v-model="dateTo"
                :min-date="dateFrom"
                class="sm:col-span-6"
                :label="t('date-end-label')"
                has-hidden-label />
            </div>
          </template>
          <template #subpagesCta>
            <div v-if="markets && markets.length > 0" class="flex flex-wrap justify-end items-center gap-2">
              <TransactionFilters
                :available-cash-register="availableCashRegisters"
                :selected-cash-registers="cashRegisters"
                :has-search="false"
                :items-can-wrap="false"
                hide-date
                hide-transaction-type
                @cashRegistersUnchecked="onCashRegistersUnchecked"
                @cashRegistersChecked="onCashRegistersChecked"
                @resetFilters="onResetFilters" />
              <PfButtonAction :label="t('export-btn')" :icon="ICON_DOWNLOAD" has-icon-left @click="onExportReport" />
            </div>
          </template>
        </Title>
      </template>

      <div v-if="markets && markets.length > 0">
        <template v-if="markets[0].transactions.length > 0">
          <UiTableHeader
            :title="
              t('transaction-count', {
                count: markets[0].transactions.length,
                money: getTotalTransactionAmount(markets[0].transactions)
              })
            " />
          <MarketTransactionTable :transactions="markets[0].transactions" />
        </template>
        <UiEmptyPage v-else>
          <UiCta :img-src="require('@/assets/img/cards.jpg')" :description="t('empty-list')" />
        </UiEmptyPage>
      </div>

      <Component :is="Component" />
    </AppShell>
  </RouterView>
</template>

<script setup>
import { ref, computed } from "vue";
import { useI18n } from "vue-i18n";
import { useQuery, useResult, useApolloClient } from "@vue/apollo-composable";
import gql from "graphql-tag";

import TransactionFilters from "@/components/transaction/transaction-filters";
import MarketTransactionTable from "@/components/transaction/market-transaction-table";
import Title from "@/components/app/title";

import { getMoneyFormat } from "@/lib/helpers/money";
import { usePageTitle } from "@/lib/helpers/page-title";
import { useCashRegisterStore } from "@/lib/store/cash-register";

import { LANG_EN } from "@/lib/consts/langs";
import { LANGUAGE_FILTER_EN, LANGUAGE_FILTER_FR } from "@/lib/consts/enums";

const dateFrom = ref(new Date(Date.now()));
const dateTo = ref(new Date(Date.now()));
const { currentCashRegister } = useCashRegisterStore();
const cashRegisters = ref(currentCashRegister !== null ? [currentCashRegister] : []);

const { t, locale } = useI18n();
const { resolveClient } = useApolloClient();
const client = resolveClient();

usePageTitle(t("title"));

const dateFromStartOfDay = computed(
  () => new Date(dateFrom.value.getFullYear(), dateFrom.value.getMonth(), dateFrom.value.getDate(), 0, 0, 0, 0)
);
const dateToEndOfDay = computed(
  () => new Date(dateTo.value.getFullYear(), dateTo.value.getMonth(), dateTo.value.getDate(), 23, 59, 59, 999)
);

const { result, loading } = useQuery(
  gql`
    query Markets($startDate: DateTime!, $endDate: DateTime!, $cashRegisters: [ID!]) {
      markets {
        id
        name
        transactions(startDate: $startDate, endDate: $endDate, cashRegisters: $cashRegisters) {
          id
          amount
          card {
            id
            programCardId
          }
          createdAt
        }
        cashRegisters {
          id
          name
        }
      }
    }
  `,
  () => ({
    startDate: dateFromStartOfDay.value,
    endDate: dateToEndOfDay.value,
    cashRegisters: cashRegisters.value
  })
);
const markets = useResult(result);

let availableCashRegisters = computed(() => {
  return markets.value[0].cashRegisters;
});

function getTotalTransactionAmount(transactions) {
  var amount = 0;

  for (var i = 0; i < transactions.length; i++) {
    if (transactions[i].__typename === "RefundTransactionGraphType") {
      amount -= transactions[i].amount;
    } else {
      amount += transactions[i].amount;
    }
  }

  return getMoneyFormat(parseFloat(amount));
}

function onCashRegistersChecked(value) {
  cashRegisters.value.push(value);
}

function onCashRegistersUnchecked(value) {
  cashRegisters.value = cashRegisters.value.filter((x) => x !== value);
}

function onResetFilters() {
  cashRegisters.value = [];
}

async function onExportReport() {
  const timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;

  var dateFromLocal = new Date(dateFrom.value);
  dateFromLocal.setHours(0, 0, 0, 0);

  var dateToLocal = new Date(dateTo.value);
  dateToLocal.setHours(23, 59, 59, 999);

  const variables = {
    marketId: markets.value[0].id,
    startDate: dateFromLocal,
    endDate: dateToLocal,
    timeZoneId: timeZone,
    language: locale.value === LANG_EN ? LANGUAGE_FILTER_EN : LANGUAGE_FILTER_FR
  };

  let result = await client.query({
    query: gql`
      query GenerateTransactionsReportsForMarket(
        $marketId: ID!
        $startDate: DateTime!
        $endDate: DateTime!
        $timeZoneId: String!
        $language: Language!
      ) {
        generateTransactionsReportForMarket(
          marketId: $marketId
          startDate: $startDate
          endDate: $endDate
          timeZoneId: $timeZoneId
          language: $language
        )
      }
    `,
    variables
  });

  window.open(result.data.generateTransactionsReportForMarket, "_blank");
}
</script>

<style scoped lang="postcss">
.market-list-transaction-vue {
  --pf-top-header-height: 170px;
  --pf-table-header-height: 67px;
  --ui-table-height: calc(
    100dvh -
      (var(--pf-top-bar-height) + var(--pf-top-header-height) + var(--pf-table-header-height) + var(--pf-footer-height) + 2rem)
  );

  @media screen("xs") {
    --pf-top-header-height: 123px;
    --pf-table-header-height: 72px;
    --ui-table-height: calc(
      100dvh -
        (var(--pf-top-bar-height) + var(--pf-top-header-height) + var(--pf-table-header-height) + var(--pf-footer-height) + 2rem)
    );
  }

  @media screen("sm") {
    --pf-top-header-height: 139px;
    --pf-table-header-height: 61px;
    --ui-table-height: calc(
      100dvh -
        (var(--pf-top-bar-height) + var(--pf-top-header-height) + var(--pf-table-header-height) + var(--pf-footer-height) + 2rem)
    );
  }

  @media screen("lg") {
    --pf-top-header-height: 78px;
    --pf-table-header-height: 66px;
    --ui-table-height: calc(
      100dvh -
        (var(--pf-top-bar-height) + var(--pf-top-header-height) + var(--pf-table-header-height) + var(--pf-footer-height) + 2rem)
    );
  }
}
</style>
