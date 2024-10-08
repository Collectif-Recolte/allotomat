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
    <AppShell :loading="loading" class="markets-list-vue">
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
      <div v-if="project">
        <div class="flex flex-col relative mb-6">
          <MarketAmountOwedTable v-if="project.marketsAmountOwed.totalCount > 0" :markets="project.marketsAmountOwed.items" />
          <UiEmptyPage v-else>
            <UiCta :img-src="require('@/assets/img/swan.jpg')" :description="t('no-results')"> </UiCta>
          </UiEmptyPage>
          <UiPagination
            v-if="project.marketsAmountOwed.totalPages > 1"
            v-model:page="page"
            class="mb-6"
            :total-pages="project.marketsAmountOwed.totalPages" />
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
import { useRoute } from "vue-router";

import Title from "@/components/app/title";
import ReportFilters from "@/components/report/report-filters";
import MarketAmountOwedTable from "@/components/report/market-amount-owed-table";

import { usePageTitle } from "@/lib/helpers/page-title";

const route = useRoute();

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

const { result, loading } = useQuery(
  gql`
    query Projects($page: Int!, $dateFrom: DateTime!, $dateTo: DateTime!) {
      projects {
        id
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
          }
        }
      }
    }
  `,
  marketsAmountOwedVariables
);

function marketsAmountOwedVariables() {
  return {
    page: page.value,
    dateFrom: dateFromStartOfDay.value,
    dateTo: dateToEndOfDay.value
  };
}

const project = useResult(result, null, (data) => {
  return data.projects[0];
});

function onDateFromUpdated(value) {
  page.value = 1;
  dateFrom.value = value;
}

function onDateToUpdated(value) {
  page.value = 1;
  dateTo.value = value;
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
