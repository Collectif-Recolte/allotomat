<i18n>
  {
    "en": {
      "title": "Reconciliation report",
      "no-results": "No transactions for the selected period"
    },
    "fr": {
      "title": "Rapport de réconciliation",
      "no-results": "Aucune transaction pour la période sélectionnée"
    }
  }
</i18n>

<template>
  <RouterView v-slot="{ Component }">
    <AppShell :loading="loadingProjects || loadingMarketGroups" class="markets-list-vue">
      <template #title>
        <Title :title="t('title')">
          <template #subpagesCta>
            <div class="text-right">
              <ReportFilters
                v-model="searchInput"
                :date-from="dateFrom"
                :date-to="dateTo"
                @dateFromUpdated="onDateFromUpdated"
                @dateToUpdated="onDateToUpdated" />
            </div>
          </template>
        </Title>
      </template>
      <div v-if="marketsAmountOwed">
        <div class="flex flex-col relative mb-6">
          <MarketAmountOwedTable v-if="marketsAmountOwed.totalCount > 0" :markets="marketsAmountOwed.items" />
          <UiEmptyPage v-else>
            <UiCta :img-src="require('@/assets/img/swan.jpg')" :description="t('no-results')"> </UiCta>
          </UiEmptyPage>
          <UiPagination
            v-if="marketsAmountOwed.totalPages > 1"
            v-model:page="page"
            class="mb-6"
            :total-pages="marketsAmountOwed.totalPages" />
        </div>
      </div>
      <Component :is="Component" />
    </AppShell>
  </RouterView>
</template>

<script setup>
import { ref, computed } from "vue";
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { useQuery, useResult } from "@vue/apollo-composable";
import { useRoute, useRouter } from "vue-router";
import { storeToRefs } from "pinia";

import Title from "@/components/app/title";
import ReportFilters from "@/components/report/report-filters";
import MarketAmountOwedTable from "@/components/report/market-amount-owed-table";

import { formatDate, serverFormat } from "@/lib/helpers/date";
import { useAuthStore } from "@/lib/store/auth";
import { usePageTitle } from "@/lib/helpers/page-title";

import { URL_RECONCILIATION_REPORT } from "@/lib/consts/urls";
import { USER_TYPE_PROJECTMANAGER, USER_TYPE_MARKETGROUPMANAGER } from "@/lib/consts/enums";

const { userType } = storeToRefs(useAuthStore());
const route = useRoute();
const router = useRouter();

let previousMonth = new Date();
previousMonth.setMonth(previousMonth.getMonth() - 1);

const dateFrom = ref(previousMonth);
const dateTo = ref(new Date(Date.now()));
const searchInput = ref("");
const page = ref(1);

if (route.query.dateFrom) {
  dateFrom.value = new Date(route.query.dateFrom);
}
if (route.query.dateTo) {
  dateTo.value = new Date(route.query.dateTo);
}

const { t } = useI18n();

usePageTitle(t("title"));

const dateFromStartOfDay = computed(
  () => new Date(dateFrom.value.getFullYear(), dateFrom.value.getMonth(), dateFrom.value.getDate(), 0, 0, 0, 0)
);
const dateToEndOfDay = computed(
  () => new Date(dateTo.value.getFullYear(), dateTo.value.getMonth(), dateTo.value.getDate(), 23, 59, 59, 999)
);

function marketsAmountOwedVariables() {
  return {
    page: page.value,
    dateFrom: dateFromStartOfDay.value,
    dateTo: dateToEndOfDay.value
  };
}

const { result: resultProjects, loading: loadingProjects } = useQuery(
  gql`
    query Projects($page: Int!, $dateFrom: DateTime!, $dateTo: DateTime!) {
      projects {
        id
        reconciliationReportDate
        name
        marketsAmountOwed(page: $page, limit: 30, startDate: $dateFrom, endDate: $dateTo) {
          totalCount
          totalPages
          items {
            market {
              id
              name
            }
            amount
            amountByCashRegister {
              cashRegister {
                id
                name
              }
              amount
            }
          }
        }
      }
    }
  `,
  marketsAmountOwedVariables,
  () => ({
    enabled: userType.value === USER_TYPE_PROJECTMANAGER
  })
);

const project = useResult(resultProjects, null, (data) => {
  if (!route.query.dateFrom && !route.query.dateTo) {
    setDateFrom(data.projects[0].reconciliationReportDate);
  }

  return data.projects[0];
});

const { result: resultMarketGroups, loading: loadingMarketGroups } = useQuery(
  gql`
    query MarketGroups($page: Int!, $dateFrom: DateTime!, $dateTo: DateTime!) {
      marketGroups {
        id
        name
        project {
          id
          reconciliationReportDate
        }
        marketsAmountOwed(page: $page, limit: 30, startDate: $dateFrom, endDate: $dateTo) {
          totalCount
          totalPages
          items {
            market {
              id
              name
            }
            amount
            amountByCashRegister {
              cashRegister {
                id
                name
              }
              amount
            }
          }
        }
      }
    }
  `,
  marketsAmountOwedVariables,
  () => ({
    enabled: userType.value === USER_TYPE_MARKETGROUPMANAGER
  })
);

const marketGroup = useResult(resultMarketGroups, null, (data) => {
  if (!route.query.dateFrom && !route.query.dateTo) {
    setDateFrom(data.marketGroups[0].project.reconciliationReportDate);
  }

  return data.marketGroups[0];
});

function setDateFrom(reconciliationReportDate) {
  switch (reconciliationReportDate) {
    case "ONE_MONTH": {
      let previousMonth = new Date();
      previousMonth.setMonth(previousMonth.getMonth() - 1);
      dateFrom.value = previousMonth;
      break;
    }
    case "ONE_WEEK": {
      let previousWeek = new Date();
      previousWeek.setDate(previousWeek.getDate() - 7);
      dateFrom.value = previousWeek;
      break;
    }
    case "ONE_DAY": {
      dateFrom.value = new Date();
      break;
    }
  }
}

const marketsAmountOwed = computed(() => {
  var marketsAmountOwed = project.value
    ? project.value.marketsAmountOwed
    : marketGroup.value
    ? marketGroup.value.marketsAmountOwed
    : null;
  let items = [];
  let totalCount = 0;

  if (marketsAmountOwed) {
    totalCount = marketsAmountOwed.totalCount;
    marketsAmountOwed.items.forEach((item) => {
      item.amountByCashRegister.forEach((cashRegisterItem) => {
        let marketData = {
          market: item.market,
          cashRegister: cashRegisterItem.cashRegister,
          amount: cashRegisterItem.amount
        };
        items.push(marketData);
      });
    });
  }

  return { items, totalCount };
});

function updateUrl() {
  router.replace({
    name: URL_RECONCILIATION_REPORT,
    query: {
      dateFrom: formatDate(new Date(dateFrom.value), serverFormat),
      dateTo: formatDate(new Date(dateTo.value), serverFormat)
    }
  });
}

function onDateFromUpdated(value) {
  page.value = 1;
  dateFrom.value = value;
  updateUrl();
}

function onDateToUpdated(value) {
  page.value = 1;
  dateTo.value = value;
  updateUrl();
}
</script>

<style scoped lang="postcss">
.markets-list-vue {
  --pf-top-header-height: 170px;
  --pf-table-header-height: 67px;
  --ui-table-height: calc(
    100dvh -
      (var(--pf-top-bar-height) + var(--pf-top-header-height) + var(--pf-table-header-height) + var(--pf-footer-height) + 3rem)
  );

  @media screen("xs") {
    --pf-top-header-height: 123px;
    --pf-table-header-height: 72px;
    --ui-table-height: calc(
      100dvh -
        (var(--pf-top-bar-height) + var(--pf-top-header-height) + var(--pf-table-header-height) + var(--pf-footer-height) + 3rem)
    );
  }

  @media screen("sm") {
    --pf-top-header-height: 139px;
    --pf-table-header-height: 61px;
    --ui-table-height: calc(
      100dvh -
        (var(--pf-top-bar-height) + var(--pf-top-header-height) + var(--pf-table-header-height) + var(--pf-footer-height) + 3rem)
    );
  }

  @media screen("lg") {
    --pf-top-header-height: 244px;
    --pf-table-header-height: 66px;
    --ui-table-height: calc(
      100dvh -
        (
          var(--pf-top-bar-height) + var(--pf-top-header-height) + var(--pf-table-header-height) + var(--pf-footer-height) +
            2.2rem
        )
    );
  }
}
</style>
