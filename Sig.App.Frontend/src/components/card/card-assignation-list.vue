<i18n>
{
	"en": {
    "participants-without-card": "{amount} participants without a card",
    "empty-list": "Here will be displayed the participants without a card",
    "no-results": "Your search yields no results",
    "reset-search": "Reset search",
    "switch-view": "Go to list of active cards"
	},
	"fr": {
		"participants-without-card": "{amount} participant-e-s sans carte",
    "empty-list": "Ici s'afficheront les participant-e-s sans carte",
    "no-results": "Votre recherche ne donne aucun résultat",
    "reset-search": "Réinitialiser la recherche",
    "switch-view": "Aller à la liste des cartes actives"
	}
}
</i18n>

<template>
  <div v-if="beneficiariesPagination">
    <UiTableHeader class="mt-6" :title="t('participants-without-card', { amount: beneficiariesPagination.totalCount })">
      <template v-if="beneficiaries.length > 0" #right>
        <UiFilter
          v-model="searchInput"
          has-search
          :has-active-filters="!!searchText"
          :beneficiaries-are-anonymous="beneficiariesAreAnonymous && canManageOrganizations"
          @resetFilters="onResetSearch"
          @search="onSearch" />
      </template>
    </UiTableHeader>
    <template v-if="beneficiaries.length > 0">
      <CardAssignationTable
        v-if="beneficiaries"
        :beneficiaries="beneficiaries"
        :selected-organization="selectedOrganization"
        :beneficiaries-are-anonymous="beneficiariesAreAnonymous && canManageOrganizations"
        :administration-subscriptions-off-platform="administrationSubscriptionsOffPlatform">
        <template #actions="{ beneficiary }">
          <slot name="actions" :beneficiary="beneficiary"></slot>
        </template>
      </CardAssignationTable>
      <UiPagination
        v-if="beneficiariesPagination && beneficiariesPagination.totalPages > 1"
        v-model:page="page"
        :total-pages="beneficiariesPagination.totalPages">
      </UiPagination>
    </template>

    <div v-else class="flex items-center justify-center my-8 lg:my-12">
      <UiCta
        v-if="searchText"
        :img-src="require('@/assets/img/swan.jpg')"
        :description="t('no-results')"
        :primary-btn-label="t('reset-search')"
        primary-btn-is-action
        :secondary-btn-label="t('switch-view')"
        :secondary-btn-route="{ name: URL_CARDS }"
        @onPrimaryBtnClick="onResetSearch">
      </UiCta>
      <UiCta v-else :img-src="require('@/assets/img/cards.jpg')" :description="t('empty-list')" />
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from "vue";
import gql from "graphql-tag";
import { defineProps, defineExpose } from "vue";
import { useQuery, useResult } from "@vue/apollo-composable";
import { useI18n } from "vue-i18n";
import { storeToRefs } from "pinia";

import { URL_CARDS } from "@/lib/consts/urls";
import { GLOBAL_MANAGE_ORGANIZATIONS } from "@/lib/consts/permissions";
import { BENEFICIARY_STATUS_ACTIVE } from "@/lib/consts/enums";

import CardAssignationTable from "@/components/card/card-assignation-table";

import { useAuthStore } from "@/lib/store/auth";

const { t } = useI18n();
const { getGlobalPermissions } = storeToRefs(useAuthStore());

const page = ref(1);
const searchInput = ref("");
const searchText = ref("");

const props = defineProps({
  selectedOrganization: {
    type: String,
    default: ""
  },
  administrationSubscriptionsOffPlatform: {
    type: Boolean,
    default: false
  }
});

const { result: resultBeneficiaries, refetch } = useQuery(
  gql`
    query Organization($id: ID!, $page: Int!, $searchText: String, $withoutSubscription: Boolean, $status: [BeneficiaryStatus!]) {
      organization(id: $id) {
        id
        project {
          id
          beneficiariesAreAnonymous
        }
        beneficiaries(
          page: $page
          limit: 30
          withoutSubscription: $withoutSubscription
          withCard: false
          status: $status
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
              isActive
            }
          }
        }
      }
    }
  `,
  beneficiariesVariables,
  () => ({
    enabled: props.selectedOrganization !== null
  })
);

function beneficiariesVariables() {
  return {
    id: props.selectedOrganization,
    page: page.value,
    searchText: searchText.value,
    status: props.administrationSubscriptionsOffPlatform ? [BENEFICIARY_STATUS_ACTIVE] : null,
    withoutSubscription: !props.administrationSubscriptionsOffPlatform ? false : null
  };
}

const beneficiariesPagination = useResult(resultBeneficiaries, null, (data) => {
  return data.organization.beneficiaries;
});

const beneficiariesAreAnonymous = useResult(resultBeneficiaries, null, (data) => {
  return data.organization.project.beneficiariesAreAnonymous;
});

const canManageOrganizations = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_ORGANIZATIONS);
});

const beneficiaries = computed(() => beneficiariesPagination.value?.items);

function refetchBeneficiaries() {
  refetch();
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

defineExpose({
  refetchBeneficiaries
});
</script>
