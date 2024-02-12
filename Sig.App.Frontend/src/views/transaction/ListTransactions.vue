<i18n>
  {
    "en": {
      "title": "Transaction history",
      "transaction-count": "No transaction | {count} transaction | {count} transactions",   
      "export-btn": "Export report",
      "create-transaction": "New transaction"
    },
    "fr": {
      "title": "Historique des transactions",
      "transaction-count": "Aucune transaction | {count} transaction | {count} transactions",
      "export-btn": "Exporter un rapport",
      "create-transaction": "Nouvelle transaction"
    }
  }
</i18n>

<template>
  <RouterView v-slot="{ Component }">
    <AppShell :loading="loading">
      <template #title>
        <Title :title="t('title')">
          <template #subpagesCta>
            <div class="text-right">
              <TransactionFilters
                v-model="searchInput"
                :available-organizations="availableOrganizations"
                :available-beneficiary-types="availableBeneficiaryTypes"
                :available-subscriptions="availableSubscriptions"
                :selected-organizations="organizations"
                :selected-beneficiary-types="beneficiaryTypes"
                :selected-subscriptions="subscriptions"
                :selected-transaction-types="transactionTypes"
                :without-subscription-id="WITHOUT_SUBSCRIPTION"
                :date-from="dateFrom"
                :date-to="dateTo"
                :search-filter="searchText"
                :administration-subscriptions-off-platform="administrationSubscriptionsOffPlatform"
                @organizationsChecked="onOrganizationsChecked"
                @organizationsUnchecked="onOrganizationsUnchecked"
                @beneficiaryTypesUnchecked="onBeneficiaryTypesUnchecked"
                @beneficiaryTypesChecked="onBeneficiaryTypesChecked"
                @subscriptionsUnchecked="onSubscriptionsUnchecked"
                @subscriptionsChecked="onSubscriptionsChecked"
                @transactionTypesChecked="onTransactionTypesChecked"
                @transactionTypesUnchecked="onTransactionTypesUnchecked"
                @dateFromUpdated="onDateFromUpdated"
                @dateToUpdated="onDateToUpdated"
                @resetFilters="onResetFilters"
                @update:modelValue="onSearchTextUpdated" />
              <PfButtonAction class="mt-2" :label="t('export-btn')" :icon="ICON_DOWNLOAD" has-icon-left @click="onExportReport" />
            </div>
          </template>
        </Title>
      </template>
      <div v-if="transactionLogs">
        <UiTableHeader :title="t('transaction-count', transactionLogs.totalCount)" />
        <ProgramTransactionTable :transactions="transactions" />
        <UiPagination
          v-if="transactionLogs && transactionLogs.totalPages > 1"
          v-model:page="page"
          class="mb-6"
          :total-pages="transactionLogs.totalPages" />
        <div
          class="sticky bottom-4 ml-auto before:block before:absolute before:pointer-events-none before:w-[calc(100%+50px)] before:h-[calc(100%+50px)] before:-translate-y-1/2 before:right-0 before:top-1/2 before:bg-gradient-radial before:bg-white/70 before:blur-lg before:rounded-full">
          <PfButtonLink tag="routerLink" :to="{ name: URL_TRANSACTION_ADD }" btn-style="secondary" class="rounded-full">
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
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { useQuery, useResult, useApolloClient } from "@vue/apollo-composable";
import { useRouter, useRoute, onBeforeRouteUpdate } from "vue-router";

import Title from "@/components/app/title";
import TransactionFilters from "@/components/transaction/transaction-filters";
import ProgramTransactionTable from "@/components/transaction/program-transaction-table";

import { URL_TRANSACTION_ADD } from "@/lib/consts/urls";
import { WITHOUT_SUBSCRIPTION } from "@/lib/consts/enums";
import ICON_DOWNLOAD from "@/lib/icons/download.json";

import { URL_TRANSACTION_ADMIN } from "@/lib/consts/urls";

import { usePageTitle } from "@/lib/helpers/page-title";

const route = useRoute();
const router = useRouter();

let previousMonth = new Date();
previousMonth.setMonth(previousMonth.getMonth() - 1);

const dateFrom = ref(previousMonth);
const dateTo = ref(new Date(Date.now()));
const organizations = ref([]);
const beneficiaryTypes = ref([]);
const subscriptions = ref([]);
const transactionTypes = ref([]);
const searchInput = ref("");
const searchText = ref("");
const page = ref(1);

if (route.query.organizations) {
  organizations.value = route.query.organizations.split(",");
}
if (route.query.beneficiaryTypes) {
  beneficiaryTypes.value = route.query.beneficiaryTypes.split(",");
}
if (route.query.beneficiaryTypes) {
  beneficiaryTypes.value = route.query.beneficiaryTypes.split(",");
}
if (route.query.subscriptions) {
  subscriptions.value = route.query.subscriptions.split(",");
}
if (route.query.transactionTypes) {
  transactionTypes.value = route.query.transactionTypes.split(",");
}
if (route.query.text) {
  searchText.value = route.query.text;
}

if (route.query.dateFrom) {
  dateFrom.value = new Date(route.query.dateFrom);
}
if (route.query.dateTo) {
  dateTo.value = new Date(route.query.dateTo);
}

const { t } = useI18n();
const { resolveClient } = useApolloClient();
const client = resolveClient();

usePageTitle(t("title"));

const { result: resultProjects } = useQuery(
  gql`
    query Projects {
      projects {
        id
        name
        organizations {
          id
          name
        }
        beneficiaryTypes {
          id
          name
        }
        subscriptions {
          id
          name
        }
        administrationSubscriptionsOffPlatform
      }
    }
  `
);
const projects = useResult(resultProjects, null, (data) => {
  return data.projects;
});

const { result: resultOrganizations } = useQuery(
  gql`
    query Organizations {
      organizations {
        id
        name
        project {
          id
          name
          beneficiaryTypes {
            id
            name
          }
          subscriptions {
            id
            name
          }
          administrationSubscriptionsOffPlatform
        }
      }
    }
  `
);

const organization = useResult(resultOrganizations, null, (data) => {
  return data.organizations[0];
});

const project = computed(() => {
  return projects.value && projects.value.length > 0 ? projects.value[0] : organization.value?.project;
});

const projectId = computed(() => {
  return project?.value?.id ?? "";
});

const projectsOrOrganizationLoaded = computed(() => {
  return (
    (project.value !== undefined && project.value !== null) || (organization.value !== undefined && organization.value !== null)
  );
});

const {
  result: resultTransactionLogs,
  loading,
  refetch: refetchTransactions
} = useQuery(
  gql`
    query TransactionLogs(
      $page: Int!
      $projectId: ID!
      $startDate: DateTime!
      $endDate: DateTime!
      $organizations: [ID!]
      $categories: [ID!]
      $subscriptions: [ID!]
      $withoutSubscription: Boolean
      $transactionTypes: [String]
      $searchText: String
    ) {
      transactionLogs(
        page: $page
        limit: 30
        projectId: $projectId
        startDate: $startDate
        endDate: $endDate
        organizations: $organizations
        categories: $categories
        subscriptions: $subscriptions
        withoutSubscription: $withoutSubscription
        transactionTypes: $transactionTypes
        searchText: $searchText
      ) {
        totalCount
        pageNumber
        pageSize
        totalPages
        items {
          id
          beneficiaryFirstname
          beneficiaryLastname
          createdAt
          discriminator
          marketName
          projectName
          initiatedByProject
          totalAmount
          transaction {
            id
          }
          subscriptionName
        }
      }
    }
  `,
  () => ({
    page: page.value,
    projectId: projectId.value,
    startDate: dateFrom.value,
    endDate: dateTo.value,
    organizations: organizations.value,
    subscriptions: subscriptions.value.length > 0 ? subscriptions.value.filter((x) => x !== WITHOUT_SUBSCRIPTION) : null,
    withoutSubscription: subscriptions.value.indexOf(WITHOUT_SUBSCRIPTION) !== -1 ? true : null,
    categories: beneficiaryTypes.value,
    transactionTypes: transactionTypes.value,
    searchText: searchText.value
  }),
  {
    enabled: projectsOrOrganizationLoaded
  }
);
const transactionLogs = useResult(resultTransactionLogs);
const transactions = useResult(resultTransactionLogs, null, (data) => {
  return data.transactionLogs.items;
});

const filteredQuery = computed(() => {
  return {
    organizations: organizations.value.length > 0 ? organizations.value.toString() : undefined,
    subscriptions: subscriptions.value.length > 0 ? subscriptions.value.toString() : undefined,
    beneficiaryTypes: beneficiaryTypes.value.length > 0 ? beneficiaryTypes.value.toString() : undefined,
    transactionTypes: transactionTypes.value.length > 0 ? transactionTypes.value.toString() : undefined,
    text: searchText.value ? searchText.value : undefined,
    dateFrom: dateFrom.value ? dateFrom.value.toISOString() : undefined,
    dateTo: dateTo.value ? dateTo.value.toISOString() : undefined
  };
});

let availableOrganizations = computed(() => {
  return project.value?.organizations ?? [];
});

let availableBeneficiaryTypes = computed(() => {
  return project.value?.beneficiaryTypes;
});

let availableSubscriptions = computed(() => {
  return project.value?.subscriptions;
});

let administrationSubscriptionsOffPlatform = computed(() => {
  return project.value?.administrationSubscriptionsOffPlatform;
});

function onOrganizationsChecked(value) {
  page.value = 1;
  organizations.value.push(value);
}

function onOrganizationsUnchecked(value) {
  page.value = 1;
  organizations.value = organizations.value.filter((x) => x !== value);
}

function onBeneficiaryTypesChecked(value) {
  page.value = 1;
  beneficiaryTypes.value.push(value);
}

function onBeneficiaryTypesUnchecked(value) {
  page.value = 1;
  beneficiaryTypes.value = beneficiaryTypes.value.filter((x) => x !== value);
}

function onSubscriptionsChecked(value) {
  page.value = 1;
  subscriptions.value.push(value);
}

function onSubscriptionsUnchecked(value) {
  page.value = 1;
  subscriptions.value = subscriptions.value.filter((x) => x !== value);
}

function onTransactionTypesChecked(value) {
  page.value = 1;
  transactionTypes.value.push(value);
}

function onTransactionTypesUnchecked(value) {
  page.value = 1;
  transactionTypes.value = transactionTypes.value.filter((x) => x !== value);
}

function onDateFromUpdated(value) {
  page.value = 1;
  dateFrom.value = value;
}

function onDateToUpdated(value) {
  page.value = 1;
  dateTo.value = value;
}

function onSearchTextUpdated(value) {
  page.value = 1;
  searchText.value = value;
}

function onResetFilters() {
  page.value = 1;
  organizations.value = [];
  subscriptions.value = [];
  beneficiaryTypes.value = [];
  transactionTypes.value = [];
  searchText.value = "";
  searchInput.value = "";
  dateFrom.value = previousMonth;
  dateTo.value = new Date(Date.now());
  updateUrl();
}

function updateUrl() {
  router.push({
    name: URL_TRANSACTION_ADMIN,
    query: filteredQuery.value
  });
}

async function onExportReport() {
  updateUrl();
  const timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;

  let result = await client.query({
    query: gql`
      query GenerateTransactionsReports(
        $projectId: ID!
        $startDate: DateTime!
        $endDate: DateTime!
        $timeZoneId: String!
        $organizations: [ID!]
        $categories: [ID!]
        $subscriptions: [ID!]
        $withoutSubscription: Boolean
        $transactionTypes: [String]
        $searchText: String
      ) {
        generateTransactionsReport(
          projectId: $projectId
          startDate: $startDate
          endDate: $endDate
          timeZoneId: $timeZoneId
          organizations: $organizations
          categories: $categories
          subscriptions: $subscriptions
          withoutSubscription: $withoutSubscription
          transactionTypes: $transactionTypes
          searchText: $searchText
        )
      }
    `,
    variables: {
      projectId: projectId.value,
      startDate: dateFrom.value,
      endDate: dateTo.value,
      timeZoneId: timeZone,
      organizations: organizations.value,
      subscriptions: subscriptions.value.length > 0 ? subscriptions.value.filter((x) => x !== WITHOUT_SUBSCRIPTION) : null,
      withoutSubscription: subscriptions.value.indexOf(WITHOUT_SUBSCRIPTION) !== -1 ? true : null,
      categories: beneficiaryTypes.value,
      transactionTypes: transactionTypes.value,
      searchText: searchText.value
    }
  });

  window.open(result.data.generateTransactionsReport, "_blank");
}

onBeforeRouteUpdate((to) => {
  if (to.name === URL_TRANSACTION_ADMIN) {
    refetchTransactions();
  }
});
</script>
