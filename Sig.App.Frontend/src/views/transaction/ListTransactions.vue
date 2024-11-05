<i18n>
  {
    "en": {
      "title": "Transaction history",
      "transaction-count": "No transaction | {count} transaction / Amount due to markets {amount} | {count} transactions / Amount due to markets {amount}",   
      "export-btn": "Export report",
      "create-transaction": "New transaction"
    },
    "fr": {
      "title": "Historique des transactions",
      "transaction-count": "Aucune transaction | {count} transaction / Montant dû aux marché(s) : {amount} | {count} transactions / Montant dû aux marchés : {amount}",
      "export-btn": "Exporter un rapport",
      "create-transaction": "Nouvelle transaction"
    }
  }
</i18n>

<template>
  <RouterView v-slot="{ Component }">
    <AppShell :loading="loading || loadingProjects" class="transactions-list-vue">
      <template #title>
        <Title :title="t('title')">
          <template #subpagesCta>
            <div class="text-right">
              <TransactionFilters
                v-model="searchInput"
                :available-organizations="availableOrganizations"
                :available-beneficiary-types="availableBeneficiaryTypes"
                :available-subscriptions="availableSubscriptions"
                :available-markets="availableMarkets"
                :available-cash-register="availableCashRegisters"
                :selected-organizations="organizations"
                :selected-beneficiary-types="beneficiaryTypes"
                :selected-subscriptions="subscriptions"
                :selected-markets="markets"
                :selected-cash-registers="cashRegisters"
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
                @marketsUnchecked="onMarketsUnchecked"
                @marketsChecked="onMarketsChecked"
                @cashRegistersUnchecked="onCashRegistersUnchecked"
                @cashRegistersChecked="onCashRegistersChecked"
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
        <UiTableHeader
          :title="
            t('transaction-count', {
              count: transactionLogs.totalCount,
              amount: getMoneyFormat(transactionLogs.amountDueToMarkets)
            })
          " />
        <div class="flex flex-col relative mb-6">
          <ProgramTransactionTable
            :transactions="transactions"
            :beneficiaries-are-anonymous="project.beneficiariesAreAnonymous" />
          <UiPagination
            v-if="transactionLogs && transactionLogs.totalPages > 1"
            v-model:page="page"
            class="mb-6"
            :total-pages="transactionLogs.totalPages" />
          <div
            v-if="canCreateTransaction"
            class="sticky bottom-4 ml-auto before:block before:absolute before:pointer-events-none before:w-[calc(100%+50px)] before:h-[calc(100%+50px)] before:-translate-y-1/2 before:right-0 before:top-1/2 before:bg-gradient-radial before:bg-white/70 before:blur-lg before:rounded-full">
            <PfButtonLink tag="routerLink" :to="{ name: URL_TRANSACTION_ADD }" btn-style="secondary" class="rounded-full">
              <span class="inline-flex items-center">
                {{ t("create-transaction") }}
              </span>
            </PfButtonLink>
          </div>
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
import { storeToRefs } from "pinia";

import Title from "@/components/app/title";
import TransactionFilters from "@/components/transaction/transaction-filters";
import ProgramTransactionTable from "@/components/transaction/program-transaction-table";

import { URL_TRANSACTION_ADD } from "@/lib/consts/urls";
import { WITHOUT_SUBSCRIPTION, LANGUAGE_FILTER_EN, LANGUAGE_FILTER_FR } from "@/lib/consts/enums";
import { LANG_EN } from "@/lib/consts/langs";
import { GLOBAL_CREATE_TRANSACTION } from "@/lib/consts/permissions";
import { URL_TRANSACTION_ADMIN } from "@/lib/consts/urls";

import ICON_DOWNLOAD from "@/lib/icons/download.json";

import { useAuthStore } from "@/lib/store/auth";

import { usePageTitle } from "@/lib/helpers/page-title";
import { getMoneyFormat } from "@/lib/helpers/money";

const route = useRoute();
const router = useRouter();

const { getGlobalPermissions } = storeToRefs(useAuthStore());

let previousMonth = new Date();
previousMonth.setMonth(previousMonth.getMonth() - 1);

const dateFrom = ref(previousMonth);
const dateTo = ref(new Date(Date.now()));
const organizations = ref([]);
const beneficiaryTypes = ref([]);
const subscriptions = ref([]);
const markets = ref([]);
const cashRegisters = ref([]);
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
if (route.query.markets) {
  markets.value = route.query.markets.split(",");
}
if (route.query.cashRegisters) {
  cashRegisters.value = route.query.cashRegisters.split(",");
}
if (route.query.transactionTypes) {
  transactionTypes.value = route.query.transactionTypes.split(",");
}
if (route.query.text) {
  searchText.value = route.query.text;
  searchInput.value = route.query.text;
}

if (route.query.dateFrom) {
  dateFrom.value = new Date(route.query.dateFrom);
}
if (route.query.dateTo) {
  dateTo.value = new Date(route.query.dateTo);
}

const { t, locale } = useI18n();
const { resolveClient } = useApolloClient();
const client = resolveClient();

usePageTitle(t("title"));

const { result: resultProjects, loading: loadingProjects } = useQuery(
  gql`
    query Projects {
      projects {
        id
        name
        organizations {
          id
          name
        }
        markets {
          id
          name
        }
        marketGroups {
          id
          name
          cashRegisters {
            id
            name
          }
        }
        beneficiaryTypes {
          id
          name
        }
        subscriptions {
          id
          name
          startDate
          endDate
          fundsExpirationDate
          isFundsAccumulable
        }
        administrationSubscriptionsOffPlatform
        beneficiariesAreAnonymous
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
            startDate
            endDate
            fundsExpirationDate
            isFundsAccumulable
          }
          markets {
            id
            name
          }
          marketGroups {
            id
            name
            cashRegisters {
              id
              name
            }
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

const canCreateTransaction = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_CREATE_TRANSACTION);
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
      $markets: [ID!]
      $cashRegisters: [ID!]
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
        markets: $markets
        cashRegisters: $cashRegisters
        withoutSubscription: $withoutSubscription
        transactionTypes: $transactionTypes
        searchText: $searchText
      ) {
        totalCount
        pageNumber
        pageSize
        totalPages
        amountDueToMarkets
        items {
          id
          beneficiaryFirstname
          beneficiaryLastname
          createdAt
          discriminator
          marketName
          projectName
          organizationName
          initiatedByProject
          initiatedByOrganization
          totalAmount
          transaction {
            id
            ... on PaymentTransactionGraphType {
              paymentTransactionAddingFundTransactions {
                id
                amount
                refundAmount
                addingFundTransaction {
                  ... on ManuallyAddingFundTransactionGraphType {
                    id
                    subscription {
                      id
                    }
                  }
                  ... on SubscriptionAddingFundTransactionGraphType {
                    id
                    subscription {
                      id
                      subscription {
                        id
                      }
                    }
                  }
                  expirationDate
                  productGroup {
                    id
                  }
                  status
                }
              }
            }
          }
          subscriptionName
          subscriptionId
        }
      }
    }
  `,
  () => {
    var dateFromLocal = new Date(dateFrom.value);
    dateFromLocal.setHours(0, 0, 0, 0);

    var dateToLocal = new Date(dateTo.value);
    dateToLocal.setHours(23, 59, 59, 999);

    return {
      page: page.value,
      projectId: projectId.value,
      startDate: dateFromLocal,
      endDate: dateToLocal,
      organizations: organizations.value,
      subscriptions: subscriptions.value.length > 0 ? subscriptions.value.filter((x) => x !== WITHOUT_SUBSCRIPTION) : null,
      markets: markets.value,
      cashRegisters: cashRegisters.value,
      withoutSubscription: subscriptions.value.indexOf(WITHOUT_SUBSCRIPTION) !== -1 ? true : null,
      categories: beneficiaryTypes.value,
      transactionTypes: transactionTypes.value,
      searchText: searchText.value
    };
  },
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
    markets: markets.value.length > 0 ? markets.value.toString() : undefined,
    cashRegisters: cashRegisters.value.length > 0 ? cashRegisters.value.toString() : undefined,
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
  if (project.value === undefined) return [];
  var subscriptions = [...project.value?.subscriptions];
  subscriptions = subscriptions.sort((a, b) => {
    const dateA = a.isFundsAccumulable ? new Date(a.fundsExpirationDate) : new Date(a.endDate);
    const dateB = b.isFundsAccumulable ? new Date(b.fundsExpirationDate) : new Date(b.endDate);
    return dateB - dateA;
  });

  return subscriptions;
});

let availableMarkets = computed(() => {
  return project.value?.markets;
});

let availableCashRegisters = computed(() => {
  var cashRegister = project.value?.marketGroups.flatMap((x) => x.cashRegisters) ?? [];
  return cashRegister.filter((x, index) => cashRegister.findIndex((y) => y.id === x.id) === index);
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

function onMarketsChecked(value) {
  page.value = 1;
  markets.value.push(value);
}

function onMarketsUnchecked(value) {
  page.value = 1;
  markets.value = markets.value.filter((x) => x !== value);
}

function onCashRegistersChecked(value) {
  page.value = 1;
  cashRegisters.value.push(value);
}

function onCashRegistersUnchecked(value) {
  page.value = 1;
  cashRegisters.value = cashRegisters.value.filter((x) => x !== value);
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
  markets.value = [];
  cashRegisters.value = [];
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
        $markets: [ID!]
        $cashRegisters: [ID!]
        $withoutSubscription: Boolean
        $transactionTypes: [String]
        $searchText: String
        $language: Language!
      ) {
        generateTransactionsReport(
          projectId: $projectId
          startDate: $startDate
          endDate: $endDate
          timeZoneId: $timeZoneId
          organizations: $organizations
          categories: $categories
          subscriptions: $subscriptions
          markets: $markets
          cashRegisters: $cashRegisters
          withoutSubscription: $withoutSubscription
          transactionTypes: $transactionTypes
          searchText: $searchText
          language: $language
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
      markets: markets.value,
      cashRegisters: cashRegisters.value,
      withoutSubscription: subscriptions.value.indexOf(WITHOUT_SUBSCRIPTION) !== -1 ? true : null,
      categories: beneficiaryTypes.value,
      transactionTypes: transactionTypes.value,
      searchText: searchText.value,
      language: locale.value === LANG_EN ? LANGUAGE_FILTER_EN : LANGUAGE_FILTER_FR
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

<style scoped lang="postcss">
.transactions-list-vue {
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
