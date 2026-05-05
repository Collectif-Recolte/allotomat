<i18n>
  {
    "en": {
      "title": "Transaction History",
      "transaction-count": "{count} transaction / {money} | {count} transactions / {money}",
      "date-selector-from": "Interval from",
      "date-selector-to": "to",
      "date-start-label": "Start date of transactions",
      "date-end-label": "End date of transactions",
      "empty-list": "There are no transactions for the chosen dates.",
      "create-transaction": "New transaction"
    },
    "fr": {
      "title": "Historique des transactions",
      "transaction-count": "{count} transaction / {money} | {count} transactions / {money}",
      "date-selector-from": "Intervalle du",
      "date-selector-to": "au",
      "date-start-label": "Date de début des transactions",
      "date-end-label": "Date de fin des transactions",
      "empty-list": "Il n'existe aucune transaction pour les dates choisies.",
      "create-transaction": "Nouvelle transaction"
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
            <div v-if="marketGroups && marketGroups.length > 0" class="flex flex-wrap justify-end items-center gap-2">
              <TransactionFilters
                :available-cash-register="availableCashRegisters"
                :selected-cash-registers="cashRegisters"
                :has-search="false"
                :items-can-wrap="false"
                hide-date
                hide-transaction-type
                hide-gift-card-transaction-type
                @cashRegistersUnchecked="onCashRegistersUnchecked"
                @cashRegistersChecked="onCashRegistersChecked"
                @resetFilters="onResetFilters" />
            </div>
          </template>
        </Title>
      </template>

      <div v-if="marketGroups && marketGroups.length > 0" class="flex flex-col relative mb-6">
        <template v-if="marketGroups[0].transactions.length > 0">
          <UiTableHeader
            :title="
              t('transaction-count', {
                count: marketGroups[0].transactions.length,
                money: getTotalTransactionAmount(marketGroups[0].transactions)
              })
            " />
          <MarketTransactionTable :transactions="marketGroups[0].transactions" />
        </template>
        <UiEmptyPage v-else>
          <UiCta :img-src="require('@/assets/img/cards.jpg')" :description="t('empty-list')" />
        </UiEmptyPage>
        <div
          class="sticky bottom-4 ml-auto before:block before:absolute before:pointer-events-none before:w-[calc(100%+50px)] before:h-[calc(100%+50px)] before:-translate-y-1/2 before:right-0 before:top-1/2 before:bg-gradient-radial before:bg-white/70 before:blur-lg before:rounded-full">
          <PfButtonLink
            tag="routerLink"
            :to="{ name: URL_MARKETGROUP_TRANSACTION_ADD }"
            btn-style="secondary"
            class="rounded-full">
            <span class="inline-flex items-center">
              {{ t("create-transaction") }}
            </span>
          </PfButtonLink>
        </div>
      </div>

      <Component :is="Component" />
    </AppShell>
  </RouterView>
</template>

<script setup>
import { ref, computed } from "vue";
import { useI18n } from "vue-i18n";
import { useQuery, useResult } from "@vue/apollo-composable";
import gql from "graphql-tag";

import TransactionFilters from "@/components/transaction/transaction-filters";
import MarketTransactionTable from "@/components/transaction/market-transaction-table";
import Title from "@/components/app/title";

import { getMoneyFormat } from "@/lib/helpers/money";
import { usePageTitle } from "@/lib/helpers/page-title";
import { useCashRegisterStore } from "@/lib/store/cash-register";
import { URL_MARKETGROUP_TRANSACTION_ADD } from "@/lib/consts/urls";

const dateFrom = ref(new Date(Date.now()));
const dateTo = ref(new Date(Date.now()));
const { currentCashRegister } = useCashRegisterStore();
const cashRegisters = ref(currentCashRegister !== null ? [currentCashRegister] : []);

const { t } = useI18n();

usePageTitle(t("title"));

const dateFromStartOfDay = computed(
  () => new Date(dateFrom.value.getFullYear(), dateFrom.value.getMonth(), dateFrom.value.getDate(), 0, 0, 0, 0)
);
const dateToEndOfDay = computed(
  () => new Date(dateTo.value.getFullYear(), dateTo.value.getMonth(), dateTo.value.getDate(), 23, 59, 59, 999)
);

const { result, loading } = useQuery(
  gql`
    query MarketGroups($startDate: DateTime!, $endDate: DateTime!, $cashRegisters: [ID!]) {
      marketGroups {
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
const marketGroups = useResult(result);

const availableCashRegisters = computed(() => {
  if (!marketGroups.value || marketGroups.value.length === 0) {
    return [];
  }

  return marketGroups.value[0].cashRegisters ?? [];
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
