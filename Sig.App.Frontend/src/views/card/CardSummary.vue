<i18n>
  {
    "en": {
      "assign-cards": "Assign cards",
      "available-card-count": "Available cards",
      "participants-without-card": "Participants without a card",
      "active-cards": "{amount} active card | {amount} active card | {amount} active cards",
		  "display-qr-code": "View QR code",
      "remove-card": "Unassign",
      "empty-list": "There is no active card yet",
      "create-gift-card": "Create a gift card",
      "generate-cards": "Generate new cards",
      "lost-card": "Lost card",
      "no-results": "Your search yields no results",
      "reset-search": "Reset search",
      "switch-view": "Go to the list of participants without a card"
    },
    "fr": {
      "assign-cards": "Assigner des cartes",
      "available-card-count": "Cartes disponibles",
      "participants-without-card": "Participant-e-s sans carte",
      "active-cards": "{amount} carte active | {amount} carte active | {amount} cartes actives",   
		  "display-qr-code": "Voir le code QR",
      "remove-card": "Désassigner",
      "empty-list": "Il n'y a encore aucune carte active",
      "create-gift-card": "Créer une carte-cadeau",
      "generate-cards": "Générer de nouvelles cartes",
      "lost-card": "Carte perdue",
      "no-results": "Votre recherche ne donne aucun résultat",
      "reset-search": "Réinitialiser la recherche",
      "switch-view": "Aller à la liste des participants sans carte"
    }
  }
</i18n>

<template>
  <div v-if="beneficiaries">
    <RouterView v-slot="{ Component }">
      <div v-if="canManageOrganizations" class="flex flex-col gap-6 xs:flex-row mt-4 mb-12">
        <UiStat
          class="xs:w-1/2"
          :stat="project?.cardStats.cardsUnassigned"
          :label="t('available-card-count')"
          :primary-btn-route="showGenerateCardsBtn"
          :primary-btn-label="t('generate-cards')"
          :secondary-btn-route="showCreateGiftCardBtn"
          :secondary-btn-label="t('create-gift-card')" />
        <UiStat
          class="xs:w-1/2"
          :stat="beneficiaryStats?.beneficiariesWithoutCard"
          :label="t('participants-without-card')"
          :primary-btn-route="{
            name: URL_CARDS_ASSIGNATION,
            query: { projectId: organization?.project.id }
          }"
          :primary-btn-label="beneficiaryStats?.beneficiariesWithoutCard > 0 ? t('assign-cards') : null" />
      </div>
      <div v-if="beneficiariesPagination">
        <UiTableHeader :title="t('active-cards', { amount: beneficiariesPagination.totalCount }, beneficiaries.length)">
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
          <CardSummaryTable
            :beneficiaries="beneficiaries"
            :beneficiaries-are-anonymous="beneficiariesAreAnonymous && canManageOrganizations"
            :administration-subscriptions-off-platform="administrationSubscriptionsOffPlatform">
            <template #beforeActions="{ beneficiary }">
              <UiButtonGroup :items="getBeforeBtnGroup(beneficiary)" tooltip-position="right" />
            </template>
            <template #afterActions="{ beneficiary }">
              <UiButtonGroup :items="getAfterBtnGroup(beneficiary)" tooltip-position="left" />
            </template>
          </CardSummaryTable>
          <UiPagination
            v-if="beneficiariesPagination && beneficiariesPagination.totalPages > 1"
            v-model:page="page"
            :total-pages="beneficiariesPagination.totalPages">
          </UiPagination>

          <Component :is="Component" />
        </template>
        <div v-else class="flex items-center justify-center my-8 lg:my-16">
          <UiCta
            v-if="searchText"
            :img-src="require('@/assets/img/swan.jpg')"
            :description="t('no-results')"
            :primary-btn-label="t('reset-search')"
            primary-btn-is-action
            :secondary-btn-label="t('switch-view')"
            :secondary-btn-route="{
              name: URL_CARDS_ASSIGNATION,
              query: { projectId: organization.value?.project.id }
            }"
            @onPrimaryBtnClick="onResetSearch">
          </UiCta>
          <UiCta v-else :img-src="require('@/assets/img/cards.jpg')" :description="t('empty-list')" />
        </div>
      </div>
    </RouterView>
  </div>
</template>

<script setup>
import { ref, defineProps, computed } from "vue";
import { useI18n } from "vue-i18n";
import gql from "graphql-tag";
import { useQuery, useResult } from "@vue/apollo-composable";
import { onBeforeRouteUpdate } from "vue-router";
import { storeToRefs } from "pinia";

import {
  URL_CARDS_SUMMARY,
  URL_CARDS_ASSIGNATION,
  URL_CARDS_ADD,
  URL_GIFT_CARD_ADD,
  URL_CARDS_QRCODE_PREVIEW,
  URL_CARDS_UNASSIGN,
  URL_CARDS_LOST
} from "@/lib/consts/urls";
import { GLOBAL_MANAGE_ORGANIZATIONS } from "@/lib/consts/permissions";

import { useAuthStore } from "@/lib/store/auth";

import ICON_CARD_LOST from "@/lib/icons/card-lost.json";
import ICON_QR_CODE from "@/lib/icons/qrcode.json";
import ICON_MINUS from "@/lib/icons/minus.json";

import CardSummaryTable from "@/components/card/card-summary-table.vue";

const { getGlobalPermissions } = storeToRefs(useAuthStore());
const { t } = useI18n();

const page = ref(1);
const searchInput = ref("");
const searchText = ref("");

const props = defineProps({
  selectedOrganization: {
    type: String,
    default: ""
  }
});

const { result: resultBeneficiaries, refetch } = useQuery(
  gql`
    query Organization($id: ID!, $page: Int!, $searchText: String) {
      organization(id: $id) {
        id
        project {
          id
          beneficiariesAreAnonymous
          administrationSubscriptionsOffPlatform
        }
        beneficiaries(
          page: $page
          limit: 30
          withCard: true
          sort: { field: BY_FUND_AVAILABLE_ON_CARD, order: DESC }
          searchText: $searchText
        ) {
          totalPages
          totalCount
          items {
            id
            firstname
            lastname
            email
            phone
            card {
              id
              programCardId
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
              addedFund
              spentFund
              lastTransactionDate
            }
            ... on BeneficiaryGraphType {
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
        beneficiaryStats {
          beneficiariesWithoutCard
        }
      }
    }
  `,
  beneficiariesVariables
);

function beneficiariesVariables() {
  return { id: props.selectedOrganization, page: page.value, searchText: searchText.value };
}

const organization = useResult(resultBeneficiaries, null, (data) => {
  return data.organization;
});

const beneficiaryStats = useResult(resultBeneficiaries, null, (data) => {
  return data.organization?.beneficiaryStats;
});

const beneficiariesAreAnonymous = useResult(resultBeneficiaries, null, (data) => {
  return data.organization?.project.beneficiariesAreAnonymous;
});

const administrationSubscriptionsOffPlatform = useResult(resultBeneficiaries, null, (data) => {
  return data.organization?.project.administrationSubscriptionsOffPlatform;
});

const beneficiariesPagination = useResult(resultBeneficiaries, null, (data) => {
  return data.organization?.beneficiaries;
});

const beneficiaries = computed(() => beneficiariesPagination.value?.items);

const canManageOrganizations = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_ORGANIZATIONS);
});

const { result: resultProjects } = useQuery(
  gql`
    query Projects {
      projects {
        id
        cardStats {
          cardsUnassigned
          cardsAssigned
        }
      }
    }
  `
);
const project = useResult(resultProjects, null, (data) => {
  return data.projects[0];
});

const showCreateGiftCardBtn = computed(() => {
  return project.value ? { name: URL_GIFT_CARD_ADD, query: { projectId: project.value.id } } : null;
});

const showGenerateCardsBtn = computed(() =>
  project.value?.cardStats.cardsUnassigned <= 0 ? { name: URL_CARDS_ADD, query: { projectId: project.value.id } } : null
);

const getBeforeBtnGroup = (beneficiary) => [
  {
    label: t("display-qr-code"),
    icon: ICON_QR_CODE,
    route: {
      name: URL_CARDS_QRCODE_PREVIEW,
      params: { cardId: beneficiary.card.id }
    }
  }
];

const getAfterBtnGroup = (beneficiary) => [
  {
    label: t("lost-card"),
    icon: ICON_CARD_LOST,
    route: {
      name: URL_CARDS_LOST,
      params: { beneficiaryId: beneficiary.id, cardId: beneficiary.card.id }
    }
  },
  {
    label: t("remove-card"),
    icon: ICON_MINUS,
    route: {
      name: URL_CARDS_UNASSIGN,
      params: { beneficiaryId: beneficiary.id, cardId: beneficiary.card.id }
    }
  }
];

function onSearch() {
  page.value = 1;
  searchText.value = searchInput.value;
}

function onResetSearch() {
  page.value = 1;
  searchText.value = "";
  searchInput.value = "";
}

onBeforeRouteUpdate((to) => {
  if (to.name === URL_CARDS_SUMMARY) {
    refetch();
  }
});
</script>
