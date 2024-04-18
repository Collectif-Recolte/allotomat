<i18n>
  {
    "en": {
      "selected-organization": "Organization",
      "title": "Participants",
      "available-amount-for-allocation": "Budget allowance",
      "amount-of-payment-remaining": "Payment remaining",
      "manage-participants": "Manage",
      "assign-subscriptions": "Assignments",
      "auto-select-participants": "Auto select",
      "selected-subscription": "Subscription",
      "max-allocation": "Max allocation",
      "amount-allocated": "{amount} will be allocated",
      "budget-allowance-available": "Remaining budget allowance after allocation: {amount}",
      "no-results": "Your search yields no results",
      "reset-search": "Reset search",
      "assign-subscription-btn": "Confirm assignment",
      "title-confirm": "Confirm selection?",
      "cancel-confirmation": "Cancel",
      "submit-confirmation": "Yes, assign subscriptions",
      "subscription-count": "{participantCount} participants will be assigned the {subscriptionName} subscription",
      "usage-amount": "<b>{amount}</b> will be distributed as follows:</br> {detail}",
      "remaining-amount": "The remaining budget allowance will be: <b>{amount}</b>",
      "success-assign-beneficiaries-to-subscription": "The subscription \"{subscriptionName}\" was successfully assigned to {assignedBeneficiariesCount} participants out of the {totalBeneficiariesCount} selected.",
      "load-more-beneficiaries": "Load more participants",
      "sort": "Sort list",
      "randomize": "Randomize list",
      "chronological-order": "Chronological",
      "random-order": "Random",
      "no-participants": "No participants in the selected organization",
      "no-participants-in-subscription": "No participants found for this subscription"
    },
    "fr": {
      "selected-organization": "Organisme",
      "title": "Participant-e-s",
      "available-amount-for-allocation": "Enveloppe",
      "amount-of-payment-remaining": "Versements",
      "manage-participants": "Gestion",
      "assign-subscriptions": "Attribution",
      "auto-select-participants": "Sélection automatique",
      "selected-subscription":"Abonnement",
      "max-allocation":"Allocation max.",
      "amount-allocated": "{amount} seront alloués",
      "budget-allowance-available": "Enveloppe restante après attribution: {amount}",
      "no-results": "Votre recherche ne donne aucun résultat",
      "reset-search": "Réinitialiser la recherche",
      "assign-subscription-btn":"Confirmer l'attribution",
      "title-confirm": "On confirme la sélection ?",
      "cancel-confirmation": "Annuler",
      "submit-confirmation": "Oui, attribuer les abonnements",
      "subscription-count": "{participantCount} participant-e-s se verront attribuer l'abonnement {subscriptionName}",
      "usage-amount": "<b>{amount}</b> seront répartis de la façon suivante :</br> {detail}",	
      "remaining-amount": "L'enveloppe restante sera de : <b>{amount}</b>",
      "success-assign-beneficiaries-to-subscription": "L'abonnement «{subscriptionName}» a été assigné avec succès à {assignedBeneficiariesCount} participant-e-s.",
      "load-more-beneficiaries": "Charger plus de participants",
      "sort": "Trier la liste",
      "randomize": "Trier la liste aléatoirement",
      "chronological-order": "Chronologique",
      "random-order": "Aléatoire",
      "no-participants": "Aucun participant dans l'organisme sélectionné",
      "no-participants-in-subscription": "Aucun participant n'a été trouvé pour cet abonnement"
    }
  }
</i18n>

<template>
  <Title :title="t('title')" :subpages="subpages">
    <template v-if="organizations && !administrationSubscriptionsOffPlatform" #left>
      <div class="text-left flex flex-col gap-x-4 text-primary-700">
        <span class="text-md">{{ t("available-amount-for-allocation") }}</span>
        <span class="text-4xl font-bold text-center">{{ budgetAllowanceBySubscription }}</span>
      </div>
    </template>
    <template v-if="organizations && !administrationSubscriptionsOffPlatform" #center>
      <div class="text-left flex flex-col gap-x-4 text-primary-700">
        <span class="text-md">{{ t("amount-of-payment-remaining") }}</span>
        <span class="text-4xl font-bold text-center">{{ subscriptionPaymentRemainingCount }}</span>
      </div>
    </template>
    <template v-if="organizations && manageOrganizations" #right>
      <div class="flex items-center gap-x-4">
        <span class="text-sm text-primary-700" aria-hidden>{{ t("selected-organization") }}</span>
        <PfFormInputSelect
          id="selectedOrganization"
          has-hidden-label
          col-span-class="sm:col-span-3"
          :label="t('selected-organization')"
          :value="selectedOrganization"
          :options="organizations"
          @input="onOrganizationSelected" />
      </div>
    </template>
    <template v-if="organizations" #subpagesCta>
      <div class="sm:ml-6 flex flex-right gap-x-4 gap-y-3 justify-end">
        <div class="flex items-center gap-x-4">
          <span class="text-sm text-primary-700" aria-hidden>{{ t("selected-subscription") }}</span>
          <PfFormInputSelect
            id="selectedSubscription"
            has-hidden-label
            col-span-class="sm:col-span-3"
            :label="t('selected-subscription')"
            :value="selectedSubscription"
            :options="subscriptions"
            @input="onSubscriptionSelected" />
        </div>
        <div class="flex items-center gap-x-4">
          <span class="text-sm text-primary-700" aria-hidden>{{ t("max-allocation") }}</span>
          <PfFormInputText
            id="maxAllocation"
            has-hidden-label
            input-type="number"
            input-mode="decimal"
            col-span-class="sm:col-span-3"
            :disabled="isMaxAllocationInputDisabled"
            :label="t('max-allocation')"
            :value="maxAllocation"
            @input="updateMaxAllocation">
            <template #trailingIcon>
              <UiDollarSign />
            </template>
          </PfFormInputText>
        </div>
        <div class="flex items-center">
          <template v-if="isMaxAllocationInputDisabled">
            <PfButtonAction
              class="pf-button px-0 border-primary-700 border rounded-r-none"
              :class="!isRandomized ? 'cursor-default bg-green-300 text-white' : 'hover:bg-primary-700 hover:text-white'"
              type="button"
              :disabled="isMaxAllocationInputDisabled"
              :title="t('chronological-order')"
              @click="isRandomized = false">
              <PfIcon :icon="SortIcon" size="lg" />
              <span class="sr-only">{{ t("sort") }}</span>
            </PfButtonAction>
            <PfButtonAction
              class="pf-button px-0 border-primary-700 border rounded-l-none border-l-0"
              :class="isRandomized ? 'cursor-default bg-green-300 text-white' : 'hover:bg-primary-700 hover:text-white'"
              type="button"
              :disabled="isMaxAllocationInputDisabled"
              :title="t('random-order')"
              @click="isRandomized = true">
              <PfIcon :icon="RandomIcon" size="lg" />
              <span class="sr-only">{{ t("randomize") }}</span>
            </PfButtonAction>
          </template>
          <template v-else>
            <button
              class="pf-button px-0 border-primary-700 border rounded-r-none"
              :class="!isRandomized ? 'cursor-default bg-green-300 text-white' : 'hover:bg-primary-700 hover:text-white'"
              type="button"
              :title="t('chronological-order')"
              @click="isRandomized = false">
              <PfIcon :icon="SortIcon" size="lg" />
              <span class="sr-only">{{ t("sort") }}</span>
            </button>
            <button
              class="pf-button px-0 border-primary-700 border rounded-l-none border-l-0"
              :class="isRandomized ? 'cursor-default bg-green-300 text-white' : 'hover:bg-primary-700 hover:text-white'"
              type="button"
              :title="t('random-order')"
              @click="isRandomized = true">
              <PfIcon :icon="RandomIcon" size="lg" />
              <span class="sr-only">{{ t("randomize") }}</span>
            </button>
          </template>
        </div>
        <PfButtonAction
          btn-style="primary"
          :disabled="isAutoSelectBtnDisabled"
          :label="t('auto-select-participants')"
          @click="onAutoSelect" />
      </div>
    </template>
  </Title>

  <div v-if="beneficiaries">
    <div class="flex flex-col gap-y-4 sm:flex-row sm:gap-x-4 sm:justify-between sm:items-center pb-5">
      <div class="flex flex-wrap gap-x-4">
        <h2 class="my-0">{{ t("amount-allocated", { amount: amountThatWillBeAllocatedMoneyFormat }) }}</h2>
        <p class="my-1" :class="{ 'text-red-500 font-bold': budgetAllowanceAvailableAfterAllocation < 0 }">
          {{ t("budget-allowance-available", { amount: budgetAllowanceAvailableAfterAllocationMoneyFormat }) }}
        </p>
      </div>
      <div class="lg:flex lg:items-center">
        <BeneficiaryFilters
          v-if="selectedOrganization !== ''"
          v-model="searchInput"
          hide-conflict-filter
          :available-beneficiary-types="availableBeneficiaryTypes"
          :available-subscriptions="availableSubscriptions"
          :selected-beneficiary-types="beneficiaryTypesFilter"
          :selected-subscriptions="subscriptionsFilter"
          :selected-status="status"
          :selected-card-status="cardStatus"
          :without-subscription-id="WITHOUT_SUBSCRIPTION"
          :beneficiary-status-inactive="BENEFICIARY_STATUS_INACTIVE"
          :beneficiary-status-active="BENEFICIARY_STATUS_ACTIVE"
          :card-status-with="BENEFICIARY_WITH_CARD"
          :card-status-without="BENEFICIARY_WITHOUT_CARD"
          :search-filter="searchText"
          :administration-subscriptions-off-platform="administrationSubscriptionsOffPlatform"
          :beneficiaries-are-anonymous="beneficiariesAreAnonymous"
          @beneficiaryTypesUnchecked="onBeneficiaryTypesUnchecked"
          @beneficiaryTypesChecked="onBeneficiaryTypesChecked"
          @subscriptionsUnchecked="onSubscriptionsUnchecked"
          @subscriptionsChecked="onSubscriptionsChecked"
          @statusChecked="onStatusChecked"
          @statusUnchecked="onStatusUnchecked"
          @cardStatusChecked="onCardStatusChecked"
          @cardStatusUnchecked="onCardStatusUnchecked"
          @resetFilters="onResetFilters"
          @search="onSearch" />
      </div>
    </div>

    <template v-if="selectedOrganization !== '' && beneficiaries.length > 0">
      <BeneficiaryTable
        show-associated-card
        :beneficiaries="beneficiaries"
        :beneficiaries-are-anonymous="beneficiariesAreAnonymous"
        :subscriptions="subscriptions"
        :selected-subscription="selectedSubscription"
        @beneficiarySelectedChecked="onSelectedBeneficiaryChecked"
        @beneficiarySelectedUnchecked="onSelectedBeneficiaryUnchecked">
      </BeneficiaryTable>
      <div
        class="sticky bottom-4 ml-auto before:block before:absolute before:pointer-events-none before:w-[calc(100%+50px)] before:h-[calc(100%+50px)] before:-translate-y-1/2 before:right-0 before:top-1/2 before:bg-gradient-radial before:bg-white/70 before:blur-lg before:rounded-full">
        <PfButtonAction
          tag="routerLink"
          btn-style="secondary"
          class="rounded-full"
          :disabled="isConfirmButtonDisabled"
          @click="onConfirmSubscription">
          <span class="inline-flex items-center">
            {{ t("assign-subscription-btn") }}
            <span class="bg-primary-700 w-6 h-6 flex items-center justify-center rounded-full text-p3 leading-none ml-2 -mr-2">{{
              selectedBeneficiaries.length
            }}</span>
          </span>
        </PfButtonAction>
      </div>
      <div v-if="displayLoadMoreBeneficiaries" class="sticky items-center justify-center py-4 px-4 text-center sm:block sm:p-0">
        <PfButtonAction tag="routerLink" btn-style="primary" class="rounded-full" @click="onFetchMoreBeneficiaries">
          <span class="inline-flex items-center">
            {{ t("load-more-beneficiaries") }}
          </span>
        </PfButtonAction>
      </div>
    </template>
    <UiEmptyPage v-else-if="selectedSubscription && anyFiltersActive">
      <UiCta
        :img-src="require('@/assets/img/participants.jpg')"
        :description="t('no-results')"
        :primary-btn-label="t('reset-search')"
        primary-btn-is-action
        @onPrimaryBtnClick="onResetFilters">
      </UiCta>
    </UiEmptyPage>
    <UiEmptyPage v-else-if="selectedSubscription">
      <UiCta :img-src="require('@/assets/img/participants.jpg')" :description="t('no-participants-in-subscription')"> </UiCta>
    </UiEmptyPage>
    <UiEmptyPage v-else>
      <UiCta :img-src="require('@/assets/img/participants.jpg')" :description="t('no-participants')"> </UiCta>
    </UiEmptyPage>
  </div>

  <UiDialogWarningModal
    v-if="displayConfirmDialog"
    :title="t('title-confirm')"
    :cancel-button-label="t('cancel-confirmation')"
    :confirm-button-label="t('submit-confirmation')"
    @goBack="closeConfirmDialog"
    @confirm="confirmAssignation">
    <template #description>
      <div>
        <p class="text-h1 font-bold text-primary-900">
          {{
            t("subscription-count", {
              participantCount: selectedBeneficiaries.length,
              subscriptionName: selectedSubscriptionName
            })
          }}
        </p>
        <!-- eslint-disable vue/no-v-html @intlify/vue-i18n/no-v-html -->
        <p
          class="text-primary-700"
          v-html="t('usage-amount', { amount: amountThatWillBeAllocatedMoneyFormat, detail: usageAmountDetail })"></p>
        <!-- eslint-disable vue/no-v-html @intlify/vue-i18n/no-v-html -->
        <p class="text-primary-700" v-html="t('remaining-amount', { amount: budgetAllowanceAvailableAfterAllocation })"></p>
      </div>
    </template>
  </UiDialogWarningModal>
</template>

<script setup>
import gql from "graphql-tag";
import { ref, computed, reactive } from "vue";
import { useI18n } from "vue-i18n";
import { onBeforeRouteUpdate } from "vue-router";
import { storeToRefs } from "pinia";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useRoute, useRouter } from "vue-router";

import { useNotificationsStore } from "@/lib/store/notifications";
import { useOrganizationStore } from "@/lib/store/organization";
import { useAuthStore } from "@/lib/store/auth";
import { usePageTitle } from "@/lib/helpers/page-title";

import { getShortMoneyFormat } from "@/lib/helpers/money";

import {
  WITHOUT_SUBSCRIPTION,
  BENEFICIARY_STATUS_INACTIVE,
  BENEFICIARY_STATUS_ACTIVE,
  BENEFICIARY_WITH_CARD,
  BENEFICIARY_WITHOUT_CARD,
  USER_TYPE_PROJECTMANAGER
} from "@/lib/consts/enums";
import { URL_BENEFICIARY_ADMIN, URL_BENEFICIARY_ASSIGN_SUBSCRIPTIONS } from "@/lib/consts/urls";
import { GLOBAL_MANAGE_ORGANIZATIONS } from "@/lib/consts/permissions";

import SortIcon from "@/lib/icons/sort.json";
import RandomIcon from "@/lib/icons/random.json";

import { getMoneyFormat } from "@/lib/helpers/money";

import Title from "@/components/app/title";
import BeneficiaryFilters from "@/components/beneficiaries/beneficiary-filters";
import BeneficiaryTable from "@/components/beneficiaries/beneficiary-table";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { addSuccess } = useNotificationsStore();
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

const { getGlobalPermissions, userType } = storeToRefs(useAuthStore());
const manageOrganizations = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_ORGANIZATIONS);
});

const page = ref(1);
const beneficiaryTypesFilter = ref([]);
const subscriptionsFilter = ref([]);
const status = ref([]);
const cardStatus = ref([]);
const selectedOrganization = ref(currentOrganization);
const selectedSubscription = ref(null);
const searchInput = ref("");
const searchText = ref("");
const isRandomized = ref(false);
const maxAllocation = ref(null);
const displayConfirmDialog = ref(false);
const loadMoreBeneficiaries = ref(false);
const displayLoadMoreBeneficiaries = ref(false);

if (route.query.beneficiaryTypes) {
  beneficiaryTypesFilter.value = route.query.beneficiaryTypes.split(",");
}
if (route.query.subscriptions) {
  subscriptionsFilter.value = route.query.subscriptions.split(",");
}
if (route.query.status) {
  status.value = route.query.status.split(",");
}

if (route.query.cardStatus) {
  cardStatus.value = route.query.cardStatus.split(",");
}

if (route.query.text) {
  searchText.value = route.query.text;
}

if (route.query.organizationId) {
  selectedOrganization.value = route.query.organizationId;
}

const { result: resultOrganizations, refetch: refetchOrganizations } = useQuery(
  gql`
    query Organizations {
      organizations {
        id
        name
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
            budgetAllowancesTotal
            totalPayment
            paymentRemaining
            types {
              id
              amount
              beneficiaryType {
                id
                name
              }
              productGroup {
                id
              }
            }
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
    beneficiariesAreAnonymous: x.project.beneficiariesAreAnonymous
  }));
});

let beneficiaryTypes = useResult(resultOrganizations, null, (data) => {
  return data.organizations[0].project.beneficiaryTypes;
});

let organizationSubscriptions = useResult(resultOrganizations, null, (data) => {
  return data.organizations[0].project.subscriptions;
});

const subscriptions = useResult(resultOrganizations, null, (data) => {
  return data.organizations[0].project.subscriptions.map((x) => ({
    label: x.name,
    value: x.id,
    budgetAllowance: x.budgetAllowancesTotal,
    totalPayment: x.totalPayment,
    paymentRemaining: x.paymentRemaining,
    types: x.types
  }));
});

const {
  result: resultBeneficiaries,
  refetch: refetchBeneficiaries,
  fetchMore: fetchMoreBeneficiaries
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
      $withoutSpecificSubscriptions: [ID!]
      $withoutSpecificCategories: [ID!]
    ) {
      organization(id: $id) {
        id
        beneficiaries(
          page: $page
          categories: $categories
          withoutSpecificCategories: $withoutSpecificCategories
          subscriptions: $subscriptions
          withoutSubscription: $withoutSubscription
          withoutSpecificSubscriptions: $withoutSpecificSubscriptions
          status: $status
          limit: 500
          searchText: $searchText
          withCard: $withCard
          withConflictPayment: $withConflictPayment
        ) {
          totalCount
          totalPages
          pageNumber
          items {
            id
            firstname
            lastname
            id1
            id2
            ... on BeneficiaryGraphType {
              beneficiaryType {
                id
                name
              }
              subscriptions {
                id
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

const beneficiaries = useResult(resultBeneficiaries, null, (data) => {
  if (
    data.organization.beneficiaries.totalPages > 1 &&
    data.organization.beneficiaries.pageNumber < data.organization.beneficiaries.totalPages
  ) {
    displayLoadMoreBeneficiaries.value = true;
  } else if (
    data.organization.beneficiaries.totalPages === 1 ||
    data.organization.beneficiaries.pageNumber === data.organization.beneficiaries.totalPages
  ) {
    displayLoadMoreBeneficiaries.value = false;
  }

  loadMoreBeneficiaries.value = false;

  return data.organization.beneficiaries.items.map((x) => reactive({ ...x, isSelected: false }));
});

const beneficiariesPagination = useResult(resultBeneficiaries, null, (data) => {
  return {
    totalCount: data.organization.beneficiaries.totalCount,
    totalPages: data.organization.beneficiaries.totalPages,
    pageNumber: data.organization.beneficiaries.pageNumber
  };
});

let beneficiariesAreAnonymous = computed(() => {
  return (
    userType.value === USER_TYPE_PROJECTMANAGER &&
    organizations.value?.find((x) => x.value === selectedOrganization.value)?.beneficiariesAreAnonymous
  );
});

let administrationSubscriptionsOffPlatform = useResult(resultOrganizations, null, (data) => {
  return data.organizations[0].project.administrationSubscriptionsOffPlatform;
});

const budgetAllowanceBySubscription = computed(() => {
  if (selectedSubscription.value === null) return "-";
  var selectedSubscriptionData = subscriptions.value.find((x) => x.value === selectedSubscription.value);
  return getShortMoneyFormat(selectedSubscriptionData.budgetAllowance);
});

const subscriptionPaymentRemainingCount = computed(() => {
  if (selectedSubscription.value === null) return "-";
  var selectedSubscriptionData = subscriptions.value.find((x) => x.value === selectedSubscription.value);
  return `${selectedSubscriptionData.paymentRemaining}/${selectedSubscriptionData.totalPayment}`;
});

const isMaxAllocationInputDisabled = computed(() => {
  return selectedSubscription.value === null;
});

const isAutoSelectBtnDisabled = computed(() => {
  return selectedSubscription.value === null || maxAllocation.value === null;
});

const selectedBeneficiaries = computed(() => {
  return beneficiaries.value.filter((x) => x.isSelected);
});

const amountThatWillBeAllocatedMoneyFormat = computed(() => {
  let amount = amountThatWillBeAllocated.value;
  return amount !== 0 ? getMoneyFormat(amount) : "-";
});

const amountThatWillBeAllocated = computed(() => {
  if (selectedSubscription.value === null) return 0;

  var selectedSubscriptionData = subscriptions.value.find((x) => x.value === selectedSubscription.value);

  var amount = 0;

  selectedBeneficiaries.value.forEach((x) => {
    var beneficiaryPaymentAmount = selectedSubscriptionData.types
      .filter((y) => y.beneficiaryType.id === x.beneficiaryType.id)
      .reduce((accumulator, type) => accumulator + type.amount, 0);

    amount += beneficiaryPaymentAmount;
  });
  return amount * selectedSubscriptionData.paymentRemaining;
});

const budgetAllowanceAvailableAfterAllocationMoneyFormat = computed(() => {
  var amount = budgetAllowanceAvailableAfterAllocation.value;
  return amount !== 0 ? getMoneyFormat(amount) : "-";
});

const budgetAllowanceAvailableAfterAllocation = computed(() => {
  if (selectedSubscription.value === null) return 0;
  var selectedSubscriptionData = subscriptions.value.find((x) => x.value === selectedSubscription.value);
  return selectedSubscriptionData.budgetAllowance - amountThatWillBeAllocated.value;
});

const availableSubscriptions = computed(() => {
  if (selectedSubscription.value === null) return organizationSubscriptions.value;
  return organizationSubscriptions.value.filter((x) => x.id !== selectedSubscription.value);
});

const availableBeneficiaryTypes = computed(() => {
  if (selectedSubscription.value === null) {
    return beneficiaryTypes != null ? beneficiaryTypes.value : [];
  }
  return subscriptions.value
    .find((x) => x.value === selectedSubscription.value)
    .types.map((x) => x.beneficiaryType)
    .filter((value, index, array) => array.indexOf(value) === index);
});

const isConfirmButtonDisabled = computed(() => {
  return (
    selectedBeneficiaries.value.length === 0 ||
    budgetAllowanceAvailableAfterAllocation.value < 0 ||
    selectedSubscription.value === null
  );
});

const selectedSubscriptionName = computed(() => {
  if (selectedSubscription.value === null) return "-";
  return subscriptions.value.find((x) => x.value === selectedSubscription.value).label;
});

const usageAmountDetail = computed(() => {
  var selectedSubscriptionData = subscriptions.value.find((x) => x.value === selectedSubscription.value);
  return selectedSubscriptionData.types
    .map(
      (x) =>
        `${x.beneficiaryType.name}: <b>${
          selectedBeneficiaries.value.filter((y) => y.beneficiaryType.id === x.beneficiaryType.id).length
        }</b>`
    )
    .join(", ");
});

const anyFiltersActive = computed(() => {
  return (
    beneficiaryTypesFilter.value.length > 0 ||
    subscriptionsFilter.value.length > 0 ||
    status.value.length > 0 ||
    cardStatus.value.length > 0 ||
    searchText.value !== ""
  );
});

function onOrganizationSelected(e) {
  selectedOrganization.value = e;
  changeOrganization(e);
}

function onBeneficiaryTypesChecked(value) {
  beneficiaryTypesFilter.value.push(value);
  updateUrl();
}

function onBeneficiaryTypesUnchecked(value) {
  beneficiaryTypesFilter.value = beneficiaryTypesFilter.value.filter((x) => x !== value);
  updateUrl();
}

function onSubscriptionsChecked(value) {
  subscriptionsFilter.value.push(value);
  updateUrl();
}

function onSubscriptionsUnchecked(value) {
  subscriptionsFilter.value = subscriptionsFilter.value.filter((x) => x !== value);
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

function onSelectedBeneficiaryChecked(beneficiary) {
  beneficiary.isSelected = true;
}

function onSelectedBeneficiaryUnchecked(beneficiary) {
  beneficiary.isSelected = false;
}

function onSubscriptionSelected(e) {
  selectedSubscription.value = e;

  var availableBeneficiaryType = subscriptions.value
    .find((x) => x.value === selectedSubscription.value)
    .types.map((x) => x.beneficiaryType);
  beneficiaryTypesFilter.value = beneficiaryTypesFilter.value.filter((x) =>
    availableBeneficiaryType.map((y) => y.id).includes(x)
  );
  subscriptionsFilter.value = subscriptionsFilter.value.filter((x) => x !== e);
  maxAllocation.value = subscriptions.value.find((x) => x.value === selectedSubscription.value).budgetAllowance;

  updateUrl();
}

function updateMaxAllocation(e) {
  maxAllocation.value = e;
}

function onAutoSelect() {
  if (selectedSubscription.value === null || maxAllocation.value === null) return;

  var selectedSubscriptionData = subscriptions.value.find((x) => x.value === selectedSubscription.value);

  var beneficiariesToSelect = [...beneficiaries.value];

  if (isRandomized.value) {
    beneficiariesToSelect = beneficiariesToSelect.sort(() => Math.random() - 0.5);
  }

  var amount = 0;
  var selectedBeneficiaries = [];

  beneficiariesToSelect.forEach((x) => {
    var beneficiaryPaymentAmount = selectedSubscriptionData.types
      .filter((y) => y.beneficiaryType.id === x.beneficiaryType.id)
      .reduce((accumulator, type) => accumulator + type.amount, 0);
    if (amount + beneficiaryPaymentAmount * selectedSubscriptionData.paymentRemaining <= maxAllocation.value) {
      amount += beneficiaryPaymentAmount * selectedSubscriptionData.paymentRemaining;
      selectedBeneficiaries.push(x);
    }
  });

  beneficiaries.value.forEach((x) => {
    x.isSelected = selectedBeneficiaries.map((y) => y.id).includes(x.id);
  });
}

function onConfirmSubscription() {
  displayConfirmDialog.value = true;
}

function closeConfirmDialog() {
  displayConfirmDialog.value = false;
}

// Send data to backend
const { mutate: assignBeneficiariesToSubscription } = useMutation(
  gql`
    mutation AssignBeneficiariesToSubscription($input: AssignBeneficiariesToSubscriptionInput!) {
      assignBeneficiariesToSubscription(input: $input) {
        availableBudgetAfter
        beneficiariesWhoGetSubscriptions
        totalBeneficiaries
        organization {
          id
          budgetAllowancesTotal
        }
      }
    }
  `
);

async function confirmAssignation() {
  await assignBeneficiariesToSubscription({
    input: {
      organizationId: selectedOrganization.value,
      subscriptionId: selectedSubscription.value,
      beneficiaries: selectedBeneficiaries.value.map((x) => x.id)
    }
  });

  addSuccess(
    t("success-assign-beneficiaries-to-subscription", {
      assignedBeneficiariesCount: selectedBeneficiaries.value.length,
      subscriptionName: selectedSubscriptionName.value
    })
  );

  router.push({
    name: URL_BENEFICIARY_ADMIN,
    query: {
      organizationId: selectedOrganization.value
    }
  });
}

function onFetchMoreBeneficiaries() {
  loadMoreBeneficiaries.value = true;
  fetchMoreBeneficiaries({
    variables: {
      page: beneficiariesPagination.value.pageNumber + 1
    },
    updateQuery: (previousResult, { fetchMoreResult }) => {
      if (!fetchMoreResult) return previousResult;

      return {
        organization: {
          ...previousResult.organization,
          beneficiaries: {
            ...previousResult.organization.beneficiaries,
            pageNumber: fetchMoreResult.organization.beneficiaries.pageNumber,
            items: [...previousResult.organization.beneficiaries.items, ...fetchMoreResult.organization.beneficiaries.items]
          }
        }
      };
    }
  });
}

function onResetFilters() {
  subscriptionsFilter.value = [];
  beneficiaryTypesFilter.value = [];
  cardStatus.value = [];
  onResetSearch();
  updateUrl();
}

function updateUrl() {
  router.push({
    name: URL_BENEFICIARY_ASSIGN_SUBSCRIPTIONS,
    query: {
      organizationId: selectedOrganization.value,
      subscriptions: subscriptionsFilter.value.length > 0 ? subscriptionsFilter.value.toString() : undefined,
      beneficiaryTypes: beneficiaryTypesFilter.value.length > 0 ? beneficiaryTypesFilter.value.toString() : undefined,
      status: status.value.length > 0 ? status.value.toString() : undefined,
      cardStatus: cardStatus.value.length > 0 ? cardStatus.value.toString() : undefined,
      text: searchText.value ? searchText.value : undefined
    }
  });
}

function beneficiariesVariables() {
  return {
    id: selectedOrganization.value,
    page: page.value,
    subscriptions:
      subscriptionsFilter.value.length > 0 ? subscriptionsFilter.value.filter((x) => x !== WITHOUT_SUBSCRIPTION) : null,
    withoutSubscription: subscriptionsFilter.value.indexOf(WITHOUT_SUBSCRIPTION) !== -1 ? true : null,
    withoutSpecificSubscriptions: selectedSubscription.value != null ? [selectedSubscription.value] : [],
    categories: beneficiaryTypesFilter.value.length > 0 ? beneficiaryTypesFilter.value : null,
    withoutSpecificCategories:
      beneficiaryTypes.value !== null && availableBeneficiaryTypes.value !== null
        ? beneficiaryTypes.value.filter((x) => !availableBeneficiaryTypes.value.includes(x)).map((x) => x.id)
        : [],
    status: status.value.length > 0 ? status.value : null,
    withCard: cardStatus.value.length === 1 ? cardStatus.value.indexOf(BENEFICIARY_WITH_CARD) !== -1 : null,
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
  beneficiaryTypesFilter.value = [];
  subscriptionsFilter.value = [];
  status.value = [];
  cardStatus.value = [];
}

onBeforeRouteUpdate((to) => {
  if (to.name === URL_BENEFICIARY_ASSIGN_SUBSCRIPTIONS) {
    if (to.query.beneficiaryTypes) {
      beneficiaryTypesFilter.value = to.query.beneficiaryTypes.split(",");
    }
    if (to.query.subscriptions) {
      subscriptionsFilter.value = to.query.subscriptions.split(",");
    }
    if (to.query.status) {
      status.value = to.query.status.split(",");
    }
    if (to.query.cardStatus) {
      cardStatus.value = to.query.cardStatus.split(",");
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
