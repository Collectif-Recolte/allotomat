<i18n>
  {
    "en": {
      "assign-subscription-btn": "Assign subscriptions",
      "import-beneficiaries-list": "Import a list",
      "selected-organization": "Organization",
      "title": "Participants",
      "download-template-file": "Download a template file",
      "available-amount-for-allocation": "Remaining budget allowance",
      "participant-count": "{count} participants",
      "empty-list": "There are no participants.",
      "import-new-list": "Import a list",
      "export-beneficiaries-list": "Export participants",
      "no-results": "Your search yields no results",
      "reset-search": "Reset search",
      "empty-organizations-list": "No organization is associated with the program.",
      "add-organization": "Add an organization",
    },
    "fr": {
      "assign-subscription-btn": "Attribuer des abonnements",
      "import-beneficiaries-list": "Importer une liste",
      "selected-organization": "Organisme",
      "title": "Participant-e-s",
      "download-template-file": "Télécharger un fichier modèle",
      "available-amount-for-allocation": "Enveloppe restante",
      "participant-count": "{count} participant-e-s",
      "empty-list": "Il n'y a pas de participant-e-s.",
      "import-new-list": "Importer une liste",
      "export-beneficiaries-list": "Exporter les participants",
      "no-results": "Votre recherche ne donne aucun résultat",
      "reset-search": "Réinitialiser la recherche",
      "empty-organizations-list": "Aucun organisme n'est associé au programme.",
      "add-organization": "Ajouter un organisme",
    }
  }
</i18n>

<template>
  <RouterView v-slot="{ Component }">
    <AppShell :loading="loading">
      <template #title>
        <Title :title="t('title')">
          <template v-if="organizations && !administrationSubscriptionsOffPlatform" #center>
            <div class="xs:text-right flex items-center gap-x-4 text-primary-700">
              <span class="text-sm">{{ t("available-amount-for-allocation") }}</span>
              <span class="text-4xl font-bold">{{ getShortMoneyFormat(budgetAllowancesTotal) }}</span>
            </div>
          </template>
          <template v-if="organizations && manageOrganizations" #right>
            <div class="flex items-center gap-x-4">
              <span class="text-sm text-primary-700" aria-hidden>{{ t("selected-organization") }}</span>
              <PfFormInputSelect
                id="selectedOrganization"
                has-hidden-label
                :label="t('selected-organization')"
                :value="selectedOrganization"
                :options="organizations"
                col-span-class="sm:col-span-3"
                @input="onOrganizationSelected" />
            </div>
          </template>
          <template v-if="organizations" #bottom>
            <div class="flex flex-wrap gap-x-4 gap-y-3">
              <PfButtonLink
                v-if="!beneficiariesAreAnonymous"
                :label="t('import-beneficiaries-list')"
                tag="routerLink"
                :to="importBeneficiariesListRoute"
                :icon="ICON_UPLOAD"
                has-icon-left />
              <PfButtonAction
                :label="t('export-beneficiaries-list')"
                :icon="ICON_DOWNLOAD"
                has-icon-left
                @click="onExportReport" />
              <PfButtonLink
                v-if="!beneficiariesAreAnonymous && !administrationSubscriptionsOffPlatform"
                btn-style="outline"
                :label="t('download-template-file')"
                href="/files/Template_Upload.xlsx"
                file-name="Template_Upload.xlsx"
                is-downloadable
                :icon="ICON_TABLE"
                has-icon-left />
              <PfButtonAction
                v-if="!beneficiariesAreAnonymous && administrationSubscriptionsOffPlatform"
                btn-style="outline"
                :label="t('download-template-file')"
                :icon="ICON_TABLE"
                has-icon-left
                @click="onDownloadTemplateFile" />
            </div>
          </template>
        </Title>
      </template>
      <div v-if="beneficiariesPagination">
        <UiTableHeader :title="t('participant-count', { count: beneficiariesPagination.totalCount })">
          <template v-if="selectedOrganization !== '' && beneficiariesPagination.items.length > 0" #right>
            <BeneficiaryFilters
              v-model="searchInput"
              :available-beneficiary-types="availableBeneficiaryTypes"
              :available-subscriptions="availableSubscriptions"
              :selected-beneficiary-types="beneficiaryTypes"
              :selected-subscriptions="subscriptions"
              :selected-status="status"
              :without-subscription-id="WITHOUT_SUBSCRIPTION"
              :beneficiary-status-inactive="BENEFICIARY_STATUS_INACTIVE"
              :beneficiary-status-active="BENEFICIARY_STATUS_ACTIVE"
              :search-filter="searchText"
              :administration-subscriptions-off-platform="administrationSubscriptionsOffPlatform"
              :beneficiaries-are-anonymous="beneficiariesAreAnonymous"
              @beneficiaryTypesUnchecked="onBeneficiaryTypesUnchecked"
              @beneficiaryTypesChecked="onBeneficiaryTypesChecked"
              @subscriptionsUnchecked="onSubscriptionsUnchecked"
              @subscriptionsChecked="onSubscriptionsChecked"
              @statusChecked="onStatusChecked"
              @statusUnchecked="onStatusUnchecked"
              @resetFilters="onResetFilters"
              @search="onSearch" />
          </template>
        </UiTableHeader>

        <template v-if="selectedOrganization !== '' && beneficiariesPagination.items.length > 0">
          <BeneficiaryTable
            v-if="!administrationSubscriptionsOffPlatform"
            :beneficiaries="beneficiariesPagination.items"
            :selected-organization="selectedOrganization"
            :beneficiaries-are-anonymous="beneficiariesAreAnonymous"
            show-associated-card>
            <template #floatingActions>
              <PfButtonLink
                tag="routerLink"
                :to="{ name: URL_BENEFICIARY_ASSIGN_SUBSCRIPTIONS, query: filteredQuery }"
                btn-style="secondary"
                class="rounded-full">
                <span class="inline-flex items-center">
                  {{ t("assign-subscription-btn") }}
                  <span
                    class="bg-primary-700 w-6 h-6 flex items-center justify-center rounded-full text-p3 leading-none ml-2 -mr-2"
                    >{{ beneficiariesPagination.totalCount }}</span
                  >
                </span>
              </PfButtonLink>
            </template>
          </BeneficiaryTable>
          <OffPlatformBeneficiaryTable
            v-else
            :beneficiaries="beneficiariesPagination.items"
            :selected-organization="selectedOrganization"
            :beneficiaries-are-anonymous="beneficiariesAreAnonymous"
            :product-groups="productGroups"
            show-associated-card />
          <UiPagination
            v-if="beneficiariesPagination.totalPages > 1"
            v-model:page="page"
            :total-pages="beneficiariesPagination.totalPages">
          </UiPagination>
        </template>

        <UiEmptyPage v-else>
          <UiCta
            v-if="searchText"
            :img-src="require('@/assets/img/swan.jpg')"
            :description="t('no-results')"
            :primary-btn-label="t('reset-search')"
            primary-btn-is-action
            @onPrimaryBtnClick="onResetSearch">
          </UiCta>
          <UiCta
            v-else-if="beneficiariesAreAnonymous"
            :img-src="require('@/assets/img/participants.jpg')"
            :description="t('empty-list')" />
          <UiCta
            v-else
            :img-src="require('@/assets/img/participants.jpg')"
            :description="t('empty-list')"
            :primary-btn-icon="ICON_UPLOAD"
            :primary-btn-label="t('import-new-list')"
            :primary-btn-route="importBeneficiariesListRoute">
            <template #secondaryBtn>
              <PfButtonLink
                v-if="!administrationSubscriptionsOffPlatform"
                btn-style="link"
                :label="t('download-template-file')"
                href="/files/Template_Upload.xlsx"
                file-name="Template_Upload.xlsx" />
              <PfButtonAction v-else btn-style="link" :label="t('download-template-file')" @click="onDownloadTemplateFile" />
            </template>
          </UiCta>
        </UiEmptyPage>
      </div>
      <UiEmptyPage v-else-if="!loading && !projectsLoading && !organizationsLoading">
        <UiCta
          :img-src="require('@/assets/img/organismes.jpg')"
          :description="t('empty-organizations-list')"
          :primary-btn-label="t('add-organization')"
          :primary-btn-route="addOrganizationRoute">
        </UiCta>
      </UiEmptyPage>

      <Component :is="Component" />
    </AppShell>
  </RouterView>
</template>

<script setup>
import gql from "graphql-tag";
import { ref, computed } from "vue";
import { useI18n } from "vue-i18n";
import { onBeforeRouteUpdate } from "vue-router";
import { storeToRefs } from "pinia";
import { useQuery, useResult, useApolloClient } from "@vue/apollo-composable";
import { useRoute, useRouter } from "vue-router";
import { useOrganizationStore } from "@/lib/store/organization";

import { useAuthStore } from "@/lib/store/auth";
import { usePageTitle } from "@/lib/helpers/page-title";
import { getShortMoneyFormat } from "@/lib/helpers/money";

import {
  URL_BENEFICIARY_ADMIN,
  URL_BENEFICIARY_IMPORT_LIST,
  URL_BENEFICIARY_ASSIGN_SUBSCRIPTIONS,
  URL_BENEFICIARY_OFF_PLATFORM_IMPORT_LIST,
  URL_ORGANIZATION_ADD
} from "@/lib/consts/urls";
import { GLOBAL_MANAGE_ORGANIZATIONS } from "@/lib/consts/permissions";
import {
  WITHOUT_SUBSCRIPTION,
  USER_TYPE_PROJECTMANAGER,
  BENEFICIARY_STATUS_INACTIVE,
  BENEFICIARY_STATUS_ACTIVE
} from "@/lib/consts/enums";
import ICON_UPLOAD from "@/lib/icons/upload-file.json";
import ICON_TABLE from "@/lib/icons/table.json";
import ICON_DOWNLOAD from "@/lib/icons/download.json";
import { PRODUCT_GROUP_LOYALTY } from "@/lib/consts/enums";

import Title from "@/components/app/title";
import BeneficiaryTable from "@/components/beneficiaries/beneficiary-table";
import OffPlatformBeneficiaryTable from "@/components/beneficiaries/off-platform-beneficiary-table";
import BeneficiaryFilters from "@/components/beneficiaries/beneficiary-filters";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { resolveClient } = useApolloClient();
const client = resolveClient();
const { currentOrganization, changeOrganization } = useOrganizationStore();

usePageTitle(t("title"));

const importBeneficiariesListRoute = computed(() => {
  if (administrationSubscriptionsOffPlatform.value) {
    return {
      name: URL_BENEFICIARY_OFF_PLATFORM_IMPORT_LIST
    };
  } else {
    return {
      name: URL_BENEFICIARY_IMPORT_LIST
    };
  }
});

const addOrganizationRoute = computed(() => {
  if (projects.value && projects.value.length > 0) {
    return {
      name: URL_ORGANIZATION_ADD,
      query: { projectId: projects.value[0].id }
    };
  }
  return "";
});

const { getGlobalPermissions, userType } = storeToRefs(useAuthStore());
const manageOrganizations = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_ORGANIZATIONS);
});

const page = ref(1);
const beneficiaryTypes = ref([]);
const subscriptions = ref([]);
const status = ref([]);
const selectedOrganization = ref(currentOrganization);
const searchInput = ref("");
const searchText = ref("");

const filteredQuery = computed(() => {
  return {
    organizationId: selectedOrganization.value,
    subscriptions: subscriptions.value.length > 0 ? subscriptions.value.toString() : undefined,
    beneficiaryTypes: beneficiaryTypes.value.length > 0 ? beneficiaryTypes.value.toString() : undefined,
    status: status.value.length > 0 ? status.value.toString() : undefined,
    text: searchText.value ? searchText.value : undefined
  };
});

if (route.query.beneficiaryTypes) {
  beneficiaryTypes.value = route.query.beneficiaryTypes.split(",");
}
if (route.query.subscriptions) {
  subscriptions.value = route.query.subscriptions.split(",");
}
if (route.query.status) {
  status.value = route.query.status.split(",");
}

const { result, loading: projectsLoading } = useQuery(
  gql`
    query Projects {
      projects {
        id
        name
      }
    }
  `
);
const projects = useResult(result);

const {
  result: resultOrganizations,
  loading: organizationsLoading,
  refetch: refetchOrganizations
} = useQuery(
  gql`
    query Organizations {
      organizations {
        id
        name
        budgetAllowancesTotal
        project {
          id
          beneficiariesAreAnonymous
          administrationSubscriptionsOffPlatform
          productGroups {
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
        }
      }
    }
  `
);
const organizations = useResult(resultOrganizations, null, (data) => {
  if (!selectedOrganization.value) {
    selectedOrganization.value = data.organizations[0].id;
    changeOrganization(data.organizations[0].id);
  }
  return data.organizations.map((x) => ({
    label: x.name,
    value: x.id,
    budgetAllowancesTotal: x.budgetAllowancesTotal,
    beneficiariesAreAnonymous: x.project.beneficiariesAreAnonymous
  }));
});

let administrationSubscriptionsOffPlatform = useResult(resultOrganizations, null, (data) => {
  return data.organizations[0].project.administrationSubscriptionsOffPlatform;
});

let availableBeneficiaryTypes = useResult(resultOrganizations, null, (data) => {
  return data.organizations[0].project.beneficiaryTypes;
});

let availableSubscriptions = useResult(resultOrganizations, null, (data) => {
  return data.organizations[0].project.subscriptions;
});

let productGroups = useResult(resultOrganizations, null, (data) => {
  return data.organizations[0].project.productGroups.filter((x) => x.name !== PRODUCT_GROUP_LOYALTY);
});

const {
  result: resultBeneficiaries,
  loading,
  refetch: refetchBeneficiaries
} = useQuery(
  gql`
    query Organization(
      $id: ID!
      $page: Int!
      $categories: [ID!]
      $subscriptions: [ID!]
      $status: [BeneficiaryStatus!]
      $withoutSubscription: Boolean
      $searchText: String
    ) {
      organization(id: $id) {
        id
        beneficiaries(
          page: $page
          categories: $categories
          subscriptions: $subscriptions
          withoutSubscription: $withoutSubscription
          status: $status
          limit: 30
          searchText: $searchText
        ) {
          totalCount
          totalPages
          items {
            id
            firstname
            lastname
            email
            phone
            address
            postalCode
            notes
            id1
            id2
            card {
              id
            }
            ... on BeneficiaryGraphType {
              beneficiaryType {
                id
                name
              }
              subscriptions {
                id
                name
              }
            }
            ... on OffPlatformBeneficiaryGraphType {
              startDate
              endDate
              funds {
                id
                amount
                productGroup {
                  id
                  name
                }
              }
              isActive
              monthlyPaymentMoment
            }
          }
        }
      }
    }
  `,
  beneficiariesVariables
);

let beneficiariesPagination = useResult(resultBeneficiaries, null, (data) => {
  return data.organization.beneficiaries;
});

let beneficiariesAreAnonymous = computed(() => {
  return (
    userType.value === USER_TYPE_PROJECTMANAGER &&
    organizations.value?.find((x) => x.value === selectedOrganization.value)?.beneficiariesAreAnonymous
  );
});

const budgetAllowancesTotal = computed(() => {
  return organizations.value?.find((x) => x.value === selectedOrganization.value)?.budgetAllowancesTotal;
});

function onOrganizationSelected(e) {
  selectedOrganization.value = e;
  changeOrganization(e);
}

function onBeneficiaryTypesChecked(value) {
  beneficiaryTypes.value.push(value);
  updateUrl();
}

function onBeneficiaryTypesUnchecked(value) {
  beneficiaryTypes.value = beneficiaryTypes.value.filter((x) => x !== value);
  updateUrl();
}

function onSubscriptionsChecked(value) {
  subscriptions.value.push(value);
  updateUrl();
}

function onStatusChecked(value) {
  status.value.push(value);
  updateUrl();
}

function onStatusUnchecked(value) {
  status.value = status.value.filter((x) => x !== value);
  updateUrl();
}

function onSubscriptionsUnchecked(value) {
  subscriptions.value = subscriptions.value.filter((x) => x !== value);
  updateUrl();
}

function onResetFilters() {
  subscriptions.value = [];
  beneficiaryTypes.value = [];
  onResetSearch();
  updateUrl();
}

function updateUrl() {
  router.push({
    name: URL_BENEFICIARY_ADMIN,
    query: filteredQuery.value
  });
}

function beneficiariesVariables() {
  return {
    id: selectedOrganization.value,
    page: page.value,
    subscriptions: subscriptions.value.length > 0 ? subscriptions.value.filter((x) => x !== WITHOUT_SUBSCRIPTION) : null,
    withoutSubscription: subscriptions.value.indexOf(WITHOUT_SUBSCRIPTION) !== -1 ? true : null,
    categories: beneficiaryTypes.value.length > 0 ? beneficiaryTypes.value : null,
    status: status.value.length > 0 ? status.value : null,
    searchText: searchText.value
  };
}

function onSearch() {
  page.value = 1;
  searchText.value = searchInput.value;
}

function onResetSearch() {
  page.value = 1;
  searchText.value = "";
  searchInput.value = "";
}

async function onExportReport() {
  let result = null;
  let timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
  if (administrationSubscriptionsOffPlatform.value) {
    result = await client.query({
      query: gql`
        query ExportOffPlatformBeneficiariesList($organizationId: ID!, $timeZoneId: String!) {
          exportOffPlatformBeneficiariesList(id: $organizationId, timeZoneId: $timeZoneId)
        }
      `,
      variables: {
        organizationId: selectedOrganization.value,
        timeZoneId: timeZone
      }
    });
    window.open(result.data.exportOffPlatformBeneficiariesList, "_blank");
  } else {
    result = await client.query({
      query: gql`
        query ExportBeneficiariesList($organizationId: ID!, $timeZoneId: String!) {
          exportBeneficiariesList(id: $organizationId, timeZoneId: $timeZoneId)
        }
      `,
      variables: {
        organizationId: selectedOrganization.value,
        timeZoneId: timeZone
      }
    });
    window.open(result.data.exportBeneficiariesList, "_blank");
  }
}

async function onDownloadTemplateFile() {
  let result = await client.query({
    query: gql`
      query DownloadBeneficiariesTemplateFile($organizationId: ID!) {
        downloadBeneficiariesTemplateFile(organizationId: $organizationId)
      }
    `,
    variables: {
      organizationId: selectedOrganization.value
    }
  });

  window.open(result.data.downloadBeneficiariesTemplateFile, "_blank");
}

onBeforeRouteUpdate((to) => {
  if (to.name === URL_BENEFICIARY_ADMIN) {
    refetchBeneficiaries();
    refetchOrganizations();
  }
});
</script>
