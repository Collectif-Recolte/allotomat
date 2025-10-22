<i18n>
  {
    "en": {
      "title": "Budget allowance report",
      "no-results": "No transactions for the selected period"
    },
    "fr": {
      "title": "Rapport d'enveloppe",
      "no-results": "Aucune transaction pour la période sélectionnée"
    }
  }
</i18n>

<template>
  <RouterView v-slot="{ Component }">
    <AppShell :loading="loadingProjects || loadingOrganizations" class="markets-list-vue">
      <template #title>
        <Title :title="t('title')">
          <template #subpagesCta>
            <div class="text-right">
              <ReportFilters
                v-model="searchInput"
                :date-from="dateFrom"
                :date-to="dateTo"
                :available-organizations="availableOrganizations"
                :selected-organizations="organizations"
                :available-subscriptions="availableSubscriptions"
                :selected-subscriptions="subscriptions"
                @dateFromUpdated="onDateFromUpdated"
                @dateToUpdated="onDateToUpdated"
                @organizationsChecked="onOrganizationsChecked"
                @organizationsUnchecked="onOrganizationsUnchecked"
                @subscriptionsUnchecked="onSubscriptionsUnchecked"
                @subscriptionsChecked="onSubscriptionsChecked"
                @resetFilters="onResetFilters" />
            </div>
          </template>
        </Title>
      </template>
      <div v-if="budgetAllowanceReport">
        <div class="flex flex-col relative mb-6">
          <BudgetAllowanceReportTable
            v-if="budgetAllowanceReport.totalCount > 0"
            :budget-allowance-logs="budgetAllowanceReport.items" />
          <UiEmptyPage v-else>
            <UiCta :img-src="require('@/assets/img/swan.jpg')" :description="t('no-results')"> </UiCta>
          </UiEmptyPage>
          <UiPagination
            v-if="budgetAllowanceReport.totalPages > 1"
            v-model:page="page"
            class="mb-6"
            :total-pages="budgetAllowanceReport.totalPages" />
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
import BudgetAllowanceReportTable from "@/components/report/budget-allowance-report-table";

import { formatDate, serverFormat } from "@/lib/helpers/date";
import { useAuthStore } from "@/lib/store/auth";
import { usePageTitle } from "@/lib/helpers/page-title";

import { URL_BUDGET_ALLOWANCE_REPORT } from "@/lib/consts/urls";
import { USER_TYPE_PROJECTMANAGER } from "@/lib/consts/enums";

const { userType } = storeToRefs(useAuthStore());
const route = useRoute();
const router = useRouter();

let previousMonth = new Date();
previousMonth.setMonth(previousMonth.getMonth() - 1);

const dateFrom = ref(previousMonth);
const dateTo = ref(new Date(Date.now()));
const searchInput = ref("");
const page = ref(1);
const organizations = ref([]);
const subscriptions = ref([]);
const availableSubscriptions = ref([]);
const availableOrganizations = ref([]);

if (route.query.dateFrom) {
  dateFrom.value = new Date(route.query.dateFrom);
}
if (route.query.dateTo) {
  dateTo.value = new Date(route.query.dateTo);
}
if (route.query.subscriptions) {
  subscriptions.value = route.query.subscriptions.split(",");
}
if (route.query.organizations) {
  organizations.value = route.query.organizations.split(",");
}

const { t } = useI18n();

usePageTitle(t("title"));

const dateFromStartOfDay = computed(
  () => new Date(dateFrom.value.getFullYear(), dateFrom.value.getMonth(), dateFrom.value.getDate(), 0, 0, 0, 0)
);
const dateToEndOfDay = computed(
  () => new Date(dateTo.value.getFullYear(), dateTo.value.getMonth(), dateTo.value.getDate(), 23, 59, 59, 999)
);

function budgetAllowanceReportVariables() {
  return {
    page: page.value,
    dateFrom: dateFromStartOfDay.value,
    dateTo: dateToEndOfDay.value,
    organizations: organizations.value,
    subscriptions: subscriptions.value
  };
}

const { result: resultProjects, loading: loadingProjects } = useQuery(
  gql`
    query Projects($page: Int!, $dateFrom: DateTime!, $dateTo: DateTime!, $organizations: [ID!]!, $subscriptions: [ID!]!) {
      projects {
        id
        reconciliationReportDate
        organizations {
          id
          name
        }
        subscriptions {
          id
          name
        }
        budgetAllowanceReport(
          page: $page
          limit: 30
          startDate: $dateFrom
          endDate: $dateTo
          withSpecificOrganizations: $organizations
          withSpecificSubscriptions: $subscriptions
        ) {
          totalCount
          totalPages
          items {
            id
            discriminator
            createdAt
            amount
            organizationName
            subscriptionName
            targetOrganizationName
            targetSubscriptionName
          }
        }
      }
    }
  `,
  budgetAllowanceReportVariables,
  () => ({
    enabled: userType.value === USER_TYPE_PROJECTMANAGER
  })
);

const project = useResult(resultProjects, null, (data) => {
  if (!route.query.dateFrom && !route.query.dateTo) {
    setDateFrom(data.projects[0].reconciliationReportDate);
  }

  availableOrganizations.value = data.projects[0].organizations;
  availableSubscriptions.value = data.projects[0].subscriptions;
  return data.projects[0];
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

const budgetAllowanceReport = computed(() => {
  return project.value ? project.value.budgetAllowanceReport : null;
});

function updateUrl() {
  router.replace({
    name: URL_BUDGET_ALLOWANCE_REPORT,
    query: {
      dateFrom: formatDate(new Date(dateFrom.value), serverFormat),
      dateTo: formatDate(new Date(dateTo.value), serverFormat),
      organizations: organizations.value.length > 0 ? organizations.value.toString() : undefined,
      subscriptions: subscriptions.value.length > 0 ? subscriptions.value.toString() : undefined
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

function onOrganizationsChecked(value) {
  page.value = 1;
  organizations.value.push(value);
  updateUrl();
}

function onOrganizationsUnchecked(value) {
  page.value = 1;
  organizations.value = organizations.value.filter((x) => x !== value);
  updateUrl();
}

function onSubscriptionsChecked(value) {
  page.value = 1;
  subscriptions.value.push(value);
  updateUrl();
}

function onSubscriptionsUnchecked(value) {
  page.value = 1;
  subscriptions.value = subscriptions.value.filter((x) => x !== value);
  updateUrl();
}

function onResetFilters() {
  page.value = 1;
  organizations.value = [];
  subscriptions.value = [];
  dateFrom.value = previousMonth;
  dateTo.value = new Date(Date.now());
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
