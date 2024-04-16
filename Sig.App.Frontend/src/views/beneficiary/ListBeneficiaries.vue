<i18n>
  {
    "en": {
      "import-beneficiaries-list": "Import a list",
      "selected-organization": "Organization",
      "title": "Participants",
      "download-template-file": "Download a template",
      "available-amount-for-allocation": "Remaining budget allowance",
      "participant-count": "{count} participants",
      "empty-list": "There are no participants.",
      "import-new-list": "Import a list",
      "export-beneficiaries-list": "Export",
      "no-results": "Your search yields no results",
      "reset-search": "Reset search",
      "empty-organizations-list": "No organization is associated with the program.",
      "add-organization": "Add an organization",
      "payment-conflicts-alert": "{count} payment conflicts",
      "manage-participants": "Manage",
      "assign-subscriptions": "Assignments"
    },
    "fr": {
      "import-beneficiaries-list": "Importer une liste",
      "selected-organization": "Organisme",
      "title": "Participant-e-s",
      "download-template-file": "Télécharger un modèle",
      "available-amount-for-allocation": "Enveloppe restante",
      "participant-count": "{count} participant-e-s",
      "empty-list": "Il n'y a pas de participant-e-s.",
      "import-new-list": "Importer une liste",
      "export-beneficiaries-list": "Exporter",
      "no-results": "Votre recherche ne donne aucun résultat",
      "reset-search": "Réinitialiser la recherche",
      "empty-organizations-list": "Aucun organisme n'est associé au programme.",
      "add-organization": "Ajouter un organisme",
      "payment-conflicts-alert":"{count} conflit de versement | {count} conflits de versements",
      "manage-participants": "Gestion",
      "assign-subscriptions": "Attribution"
    }
  }
</i18n>

<template>
  <RouterView v-slot="{ Component }">
    <AppShell :loading="loading || projectsLoading || organizationsLoading">
      <template #title>
        <Title :title="t('title')" :subpages="subpages">
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
          <template v-if="organizations" #subpagesCta>
            <div class="flex flex-right gap-x-4 gap-y-3 justify-end">
              <PfButtonLink
                v-if="!beneficiariesAreAnonymous"
                :label="t('import-beneficiaries-list')"
                tag="routerLink"
                :to="importBeneficiariesListRoute"
                :icon="ICON_UPLOAD"
                has-icon-left />
              <PfButtonAction
                btn-style="outline"
                :label="t('export-beneficiaries-list')"
                :icon="ICON_DOWNLOAD"
                has-icon-left
                @click="onExportReport" />
              <template v-if="!beneficiariesAreAnonymous">
                <PfButtonLink
                  v-if="!administrationSubscriptionsOffPlatform"
                  btn-style="outline"
                  :label="t('download-template-file')"
                  href="/files/Template_Upload.xlsx"
                  file-name="Template_Upload.xlsx"
                  is-downloadable
                  :icon="ICON_TABLE"
                  has-icon-left />
                <PfButtonAction
                  v-else
                  btn-style="outline"
                  :label="t('download-template-file')"
                  :icon="ICON_TABLE"
                  has-icon-left
                  @click="onDownloadTemplateFile" />
              </template>
            </div>
          </template>
        </Title>
      </template>
      <div v-if="beneficiariesPagination">
        <div class="flex flex-col gap-y-4 sm:flex-row sm:gap-x-4 sm:justify-between sm:items-center pb-5">
          <div class="flex flex-wrap gap-x-4">
            <h2 class="my-0">{{ t("participant-count", { count: beneficiariesPagination.totalCount }) }}</h2>
            <PfButtonLink
              v-if="beneficiariesPagination.conflictPaymentCount > 0"
              class="ml-2 text-red-500"
              btn-style="link"
              tag="routerLink"
              :to="{
                name: URL_BENEFICIARY_ADMIN,
                query: { paymentConflict: BENEFICIARY_WITH_PAYMENT_CONFLICT }
              }">
              <span class="inline-flex items-center underline mt-1">
                <PfIcon :icon="ICON_INFO" class="shrink-0 mr-1" size="xs" />
                {{ t("payment-conflicts-alert", { count: beneficiariesPagination.conflictPaymentCount }) }}
              </span>
            </PfButtonLink>
          </div>
          <div class="lg:flex lg:items-center">
            <BeneficiaryFilters
              v-if="selectedOrganization !== ''"
              v-model="searchInput"
              :available-beneficiary-types="availableBeneficiaryTypes"
              :available-subscriptions="availableSubscriptions"
              :selected-beneficiary-types="beneficiaryTypes"
              :selected-subscriptions="subscriptions"
              :selected-status="status"
              :selected-card-status="cardStatus"
              :selected-payment-conflict-status="conflictPaymentStatus"
              :without-subscription-id="WITHOUT_SUBSCRIPTION"
              :beneficiary-status-inactive="BENEFICIARY_STATUS_INACTIVE"
              :beneficiary-status-active="BENEFICIARY_STATUS_ACTIVE"
              :card-status-with="BENEFICIARY_WITH_CARD"
              :card-status-without="BENEFICIARY_WITHOUT_CARD"
              :payment-conflict-status-with="BENEFICIARY_WITH_PAYMENT_CONFLICT"
              :payment-conflict-status-without="BENEFICIARY_WITHOUT_PAYMENT_CONFLICT"
              :search-filter="searchText"
              :administration-subscriptions-off-platform="administrationSubscriptionsOffPlatform"
              :beneficiaries-are-anonymous="beneficiariesAreAnonymous"
              :card-is-disabled="CARD_IS_DISABLED"
              :card-is-enabled="CARD_IS_ENABLED"
              :selected-card-disabled="cardDisabled"
              @beneficiaryTypesUnchecked="onBeneficiaryTypesUnchecked"
              @beneficiaryTypesChecked="onBeneficiaryTypesChecked"
              @subscriptionsUnchecked="onSubscriptionsUnchecked"
              @subscriptionsChecked="onSubscriptionsChecked"
              @statusChecked="onStatusChecked"
              @statusUnchecked="onStatusUnchecked"
              @cardStatusChecked="onCardStatusChecked"
              @cardStatusUnchecked="onCardStatusUnchecked"
              @paymentConflictStatusChecked="onPaymentConflictStatusChecked"
              @paymentConflictStatusUnchecked="onPaymentConflictStatusUnchecked"
              @cardIsDisabledChecked="onCardDisabledChecked"
              @cardIsDisabledUnchecked="onCardDisabledUnchecked"
              @resetFilters="onResetFilters"
              @search="onSearch" />
          </div>
        </div>

        <template v-if="selectedOrganization !== '' && beneficiariesPagination.items.length > 0">
          <ListBeneficiariesWithDetailed
            :product-groups="productGroups"
            :administration-subscriptions-off-platform="administrationSubscriptionsOffPlatform"
            :beneficiaries-pagination="beneficiariesPagination"
            :beneficiaries-are-anonymous="beneficiariesAreAnonymous"
            :filtered-query="filteredQuery" />
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
  URL_BENEFICIARY_OFF_PLATFORM_IMPORT_LIST,
  URL_ORGANIZATION_ADD,
  URL_BENEFICIARY_ASSIGN_SUBSCRIPTIONS
} from "@/lib/consts/urls";
import { GLOBAL_MANAGE_ORGANIZATIONS } from "@/lib/consts/permissions";
import {
  WITHOUT_SUBSCRIPTION,
  USER_TYPE_PROJECTMANAGER,
  BENEFICIARY_STATUS_INACTIVE,
  BENEFICIARY_STATUS_ACTIVE,
  BENEFICIARY_WITH_CARD,
  BENEFICIARY_WITHOUT_CARD,
  BENEFICIARY_WITH_PAYMENT_CONFLICT,
  BENEFICIARY_WITHOUT_PAYMENT_CONFLICT,
  LANGUAGE_FILTER_EN,
  LANGUAGE_FILTER_FR,
  CARD_IS_ENABLED,
  CARD_IS_DISABLED
} from "@/lib/consts/enums";
import { PRODUCT_GROUP_LOYALTY } from "@/lib/consts/enums";
import { LANG_EN } from "@/lib/consts/langs";

import ICON_UPLOAD from "@/lib/icons/upload-file.json";
import ICON_TABLE from "@/lib/icons/table.json";
import ICON_DOWNLOAD from "@/lib/icons/download.json";
import ICON_INFO from "@/lib/icons/info.json";

import Title from "@/components/app/title";
import BeneficiaryFilters from "@/components/beneficiaries/beneficiary-filters";
import ListBeneficiariesWithDetailed from "@/components/beneficiaries/list-beneficiaries-detailed";

const { t, locale } = useI18n();
const route = useRoute();
const router = useRouter();
const { resolveClient } = useApolloClient();
const client = resolveClient();
const { currentOrganization, changeOrganization } = useOrganizationStore();

usePageTitle(t("title"));

const subpages = computed(() => {
  return [
    {
      route: { name: URL_BENEFICIARY_ADMIN },
      label: t("manage-participants"),
      isActive: true
    },
    {
      route: { name: URL_BENEFICIARY_ASSIGN_SUBSCRIPTIONS },
      label: t("assign-subscriptions"),
      isActive: false
    }
  ];
});

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
const cardStatus = ref([]);
const conflictPaymentStatus = ref([]);
const cardDisabled = ref([]);
const selectedOrganization = ref(currentOrganization);
const searchInput = ref("");
const searchText = ref("");

const filteredQuery = computed(() => {
  return {
    organizationId: selectedOrganization.value,
    subscriptions: subscriptions.value.length > 0 ? subscriptions.value.toString() : undefined,
    beneficiaryTypes: beneficiaryTypes.value.length > 0 ? beneficiaryTypes.value.toString() : undefined,
    status: status.value.length > 0 ? status.value.toString() : undefined,
    cardStatus: cardStatus.value.length > 0 ? cardStatus.value.toString() : undefined,
    paymentConflict: conflictPaymentStatus.value.length > 0 ? conflictPaymentStatus.value.toString() : undefined,
    cardDisabled: cardDisabled.value.length > 0 ? cardDisabled.value.toString() : undefined,
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

if (route.query.cardStatus) {
  cardStatus.value = route.query.cardStatus.split(",");
}

if (route.query.paymentConflict) {
  conflictPaymentStatus.value = route.query.paymentConflict.split(",");
}

if (route.query.cardDisabled) {
  cardDisabled.value = route.query.cardDisabled.split(",");
}

if (route.query.text) {
  searchText.value = route.query.text;
}

if (route.query.organizationId) {
  selectedOrganization.value = route.query.organizationId;
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
      $withCard: Boolean
      $withConflictPayment: Boolean
      $withCardDisabled: Boolean
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
          withCard: $withCard
          withConflictPayment: $withConflictPayment
          withCardDisabled: $withCardDisabled
        ) {
          conflictPaymentCount
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
              cardNumber
              programCardId
              isDisabled
              funds {
                id
                amount
                productGroup {
                  id
                  name
                  orderOfAppearance
                  color
                }
              }
              loyaltyFund {
                id
                amount
                productGroup {
                  id
                  name
                  orderOfAppearance
                  color
                }
              }
              totalFund
              lastTransactionDate
            }
            ... on BeneficiaryGraphType {
              beneficiaryType {
                id
                name
              }
              subscriptions {
                id
                name
                endDate
                types {
                  id
                  beneficiaryType {
                    id
                  }
                }
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
              subscriptions {
                id
                name
              }
            }
          }
        }
      }
    }
  `,
  beneficiariesVariables,
  () => ({
    enabled: selectedOrganization.value !== null
  })
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

function onCardStatusChecked(value) {
  cardStatus.value.push(value);
  updateUrl();
}

function onCardStatusUnchecked(value) {
  cardStatus.value = cardStatus.value.filter((x) => x !== value);
  updateUrl();
}

function onPaymentConflictStatusChecked(value) {
  conflictPaymentStatus.value.push(value);
  updateUrl();
}

function onPaymentConflictStatusUnchecked(value) {
  conflictPaymentStatus.value = conflictPaymentStatus.value.filter((x) => x !== value);
  updateUrl();
}

function onCardDisabledChecked(value) {
  cardDisabled.value.push(value);
  updateUrl();
}

function onCardDisabledUnchecked(value) {
  cardDisabled.value = cardDisabled.value.filter((x) => x !== value);
  updateUrl();
}

function onSubscriptionsUnchecked(value) {
  subscriptions.value = subscriptions.value.filter((x) => x !== value);
  updateUrl();
}

function onResetFilters() {
  subscriptions.value = [];
  beneficiaryTypes.value = [];
  cardStatus.value = [];
  conflictPaymentStatus.value = [];
  cardDisabled.value = [];
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
    withCard: cardStatus.value.length === 1 ? cardStatus.value.indexOf(BENEFICIARY_WITH_CARD) !== -1 : null,
    withConflictPayment:
      conflictPaymentStatus.value.length === 1
        ? conflictPaymentStatus.value.indexOf(BENEFICIARY_WITH_PAYMENT_CONFLICT) !== -1
        : null,
    withCardDisabled: cardDisabled.value.length === 1 ? cardDisabled.value.indexOf(CARD_IS_DISABLED) !== -1 : null,
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
        query ExportBeneficiariesList($organizationId: ID!, $timeZoneId: String!, $language: Language!) {
          exportBeneficiariesList(id: $organizationId, timeZoneId: $timeZoneId, language: $language)
        }
      `,
      variables: {
        organizationId: selectedOrganization.value,
        timeZoneId: timeZone,
        language: locale.value === LANG_EN ? LANGUAGE_FILTER_EN : LANGUAGE_FILTER_FR
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
    if (to.query.beneficiaryTypes) {
      beneficiaryTypes.value = to.query.beneficiaryTypes.split(",");
    }
    if (to.query.subscriptions) {
      subscriptions.value = to.query.subscriptions.split(",");
    }
    if (to.query.status) {
      status.value = to.query.status.split(",");
    }
    if (to.query.cardStatus) {
      cardStatus.value = to.query.cardStatus.split(",");
    }
    if (to.query.paymentConflict) {
      conflictPaymentStatus.value = to.query.paymentConflict.split(",");
    }
    if (to.query.text) {
      searchText.value = to.query.text;
    }
    if (to.query.organizationId) {
      selectedOrganization.value = to.query.organizationId;
    }

    refetchBeneficiaries();
    refetchOrganizations();
  }
});
</script>
