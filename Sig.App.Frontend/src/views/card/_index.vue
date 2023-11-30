<i18n>
{
	"en": {
		"generate-cards": "Generate new cards",
		"title": "Cards",
    "empty-card-list": "The program has not generated any cards yet.",
    "create-gift-card": "Create a gift card",
    "available-card": "{n} available",
    "total-card-count": "{n} program cards",
    "display-qr-code": "View QR code",
    "remove-card": "Unassign",
    "lost-card": "Lost card",
    "no-results": "Your search yields no results",
    "reset-search": "Reset search",
    "card-status": "Status",
    "gift-card-label": "Gift card",
    "lost-card-label": "Lost card",
    "card-assigned": "Assigned",
    "card-unassigned": "Unassigned",
    "card-deactivated": "Deactivated",
    "search-placeholder": "Search by ID or card number"
	},
	"fr": {
		"generate-cards": "Générer de nouvelles cartes",
		"title": "Cartes",
    "empty-card-list": "Le programme n'a pas encore généré de cartes.",
    "create-gift-card": "Créer une carte-cadeau",
    "available-card": "{n} disponibles",
    "total-card-count":"{n} cartes du programme",
    "display-qr-code": "Voir le code QR",
    "remove-card": "Désassigner",
    "lost-card": "Carte perdue",
    "no-results": "Votre recherche ne donne aucun résultat",
    "reset-search": "Réinitialiser la recherche",
    "card-status": "Statut",
    "gift-card-label": "Carte cadeau",
    "lost-card-label": "Carte perdue",
    "card-assigned": "Assignée",
    "card-unassigned": "Non assignée",
    "card-deactivated": "Désactivée",
    "search-placeholder": "Chercher par ID ou n° de carte"
	}
}
</i18n>

<template>
  <RouterView v-slot="{ Component }">
    <AppShell :loading="loadingProjects">
      <template #title>
        <Title :title="t('title')">
          <template v-if="project" #bottom>
            <div class="flex flex-col gap-y-4 sm:flex-row sm:gap-x-4 sm:justify-between sm:items-center">
              <div class="flex flex-wrap gap-x-4">
                <h2 class="my-0">{{ t("total-card-count", project.cards.totalCount) }}</h2>
                <p class="my-1">{{ t("available-card", project.cardStats.cardsUnassigned) }}</p>
              </div>
              <div class="flex flex-wrap gap-x-4 gap-y-3">
                <PfButtonLink
                  v-if="project"
                  btn-style="outline"
                  tag="routerLink"
                  :label="t('create-gift-card')"
                  :to="showCreateGiftCardBtn" />
                <PfButtonLink
                  v-if="project"
                  tag="routerLink"
                  :label="t('generate-cards')"
                  :to="{ name: URL_CARDS_ADD, query: { projectId: project.id } }" />
              </div>
            </div>
          </template>
        </Title>
      </template>
      <div v-if="cards && cardsPagination">
        <UiTableHeader>
          <template #right>
            <UiFilter
              v-model="searchInput"
              has-search
              has-filters
              :placeholder="t('search-placeholder')"
              :has-active-filters="!!searchText || activeFiltersCount > 0"
              :active-filters-count="activeFiltersCount"
              :beneficiaries-are-anonymous="beneficiariesAreAnonymous && canManageOrganizations"
              @resetFilters="resetSearch"
              @search="onSearch">
              <PfFormInputCheckboxGroup
                v-if="availableCardStatus.length > 0"
                id="card-status"
                is-filter
                :value="selectedCardStatus"
                :label="t('card-status')"
                :options="availableCardStatus"
                @input="onCardStatusChecked" />
            </UiFilter>
          </template>
        </UiTableHeader>
        <UiEmptyPage v-if="cardsPagination.totalCount === 0 && activeFiltersCount === 0 && searchText === ''">
          <UiCta
            :img-src="require('@/assets/img/cards.jpg')"
            :description="t('empty-card-list')"
            :primary-btn-label="t('generate-cards')"
            :primary-btn-route="{ name: URL_CARDS_ADD, query: { projectId: project.id } }"
            @onPrimaryBtnClick="resetSearch">
          </UiCta>
        </UiEmptyPage>
        <UiEmptyPage v-else-if="cardsPagination.totalCount === 0">
          <UiCta
            :img-src="require('@/assets/img/cards.jpg')"
            :description="t('no-results')"
            :primary-btn-label="t('reset-search')"
            primary-btn-is-action
            @onPrimaryBtnClick="resetSearch">
          </UiCta>
        </UiEmptyPage>
        <div v-else>
          <CardSummaryTable
            :cards="cards"
            :beneficiaries-are-anonymous="beneficiariesAreAnonymous && canManageOrganizations"
            :administration-subscriptions-off-platform="administrationSubscriptionsOffPlatform">
            <template #beforeActions="{ card }">
              <UiButtonGroup :items="getBeforeBtnGroup(card)" tooltip-position="right" />
            </template>
            <template #afterActions="{ card }">
              <UiButtonGroup :items="getAfterBtnGroup(card)" tooltip-position="left" />
            </template>
          </CardSummaryTable>
          <UiPagination
            v-if="cardsPagination && cardsPagination.totalPages > 1"
            v-model:page="page"
            :total-pages="cardsPagination.totalPages">
          </UiPagination>
        </div>
        <Component :is="Component" />
      </div>
    </AppShell>
  </RouterView>
</template>

<script setup>
import gql from "graphql-tag";
import { ref, computed } from "vue";
import { useI18n } from "vue-i18n";
import { useQuery, useResult } from "@vue/apollo-composable";
import { storeToRefs } from "pinia";

import { usePageTitle } from "@/lib/helpers/page-title";
import { useAuthStore } from "@/lib/store/auth";

import {
  URL_CARDS_ADD,
  URL_GIFT_CARD_ADD,
  URL_CARDS_QRCODE_PREVIEW,
  URL_CARDS_UNASSIGN,
  URL_CARDS_LOST
} from "@/lib/consts/urls";
import { GLOBAL_MANAGE_ORGANIZATIONS } from "@/lib/consts/permissions";
import {
  CARD_STATUS_ASSIGNED,
  CARD_STATUS_UNASSIGNED,
  CARD_STATUS_DEACTIVATED,
  CARD_STATUS_LOST,
  CARD_STATUS_GIFT
} from "@/lib/consts/enums";

import Title from "@/components/app/title";
import CardSummaryTable from "@/components/card/card-summary-table.vue";

import ICON_CARD_LOST from "@/lib/icons/card-lost.json";
import ICON_QR_CODE from "@/lib/icons/qrcode.json";
import ICON_MINUS from "@/lib/icons/minus.json";

const { getGlobalPermissions } = storeToRefs(useAuthStore());
const { t } = useI18n();

const page = ref(1);
const searchInput = ref("");
const searchText = ref("");
const selectedCardStatus = ref([]);

const availableCardStatus = [
  { value: CARD_STATUS_ASSIGNED, label: t("card-assigned") },
  { value: CARD_STATUS_UNASSIGNED, label: t("card-unassigned") },
  { value: CARD_STATUS_DEACTIVATED, label: t("card-deactivated") },
  { value: CARD_STATUS_LOST, label: t("lost-card-label") },
  { value: CARD_STATUS_GIFT, label: t("gift-card-label") }
];

usePageTitle(t("title"));

const canManageOrganizations = () => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_ORGANIZATIONS);
};

const { result: resultProjects, loading: loadingProjects } = useQuery(
  gql`
    query Projects($page: Int!, $status: [CardStatus!], $searchText: String) {
      projects {
        id
        name
        beneficiariesAreAnonymous
        administrationSubscriptionsOffPlatform
        cardStats {
          cardsUnassigned
        }
        cards(page: $page, limit: 30, status: $status, searchText: $searchText) {
          pageNumber
          pageSize
          totalCount
          totalPages
          items {
            id
            programCardId
            cardNumber
            status
            beneficiary {
              id
              id1
              firstname
              lastname
              organization {
                id
                name
              }
            }
          }
        }
      }
    }
  `,
  projectsVariables,
  {
    enabled: canManageOrganizations
  }
);

function projectsVariables() {
  return {
    page: page.value,
    status: selectedCardStatus.value,
    searchText: searchText.value
  };
}

const project = useResult(resultProjects, null, (data) => {
  return data.projects[0];
});

const cardsPagination = useResult(resultProjects, null, (data) => {
  return data.projects[0]?.cards;
});

const cards = useResult(resultProjects, null, (data) => {
  return data.projects[0]?.cards.items;
});

const showCreateGiftCardBtn = computed(() => {
  return project.value ? { name: URL_GIFT_CARD_ADD, query: { projectId: project.value.id } } : null;
});

const getBeforeBtnGroup = (card) => [
  {
    label: t("display-qr-code"),
    icon: ICON_QR_CODE,
    route: {
      name: URL_CARDS_QRCODE_PREVIEW,
      params: { cardId: card.id }
    }
  }
];

const getAfterBtnGroup = (card) => {
  if (card.beneficiary !== null) {
    return [
      {
        label: t("lost-card"),
        icon: ICON_CARD_LOST,
        route: {
          name: URL_CARDS_LOST,
          params: { beneficiaryId: card.beneficiary.id, cardId: card.id }
        }
      },
      {
        label: t("remove-card"),
        icon: ICON_MINUS,
        route: {
          name: URL_CARDS_UNASSIGN,
          params: { beneficiaryId: card.beneficiary.id, cardId: card.id }
        }
      }
    ];
  }
  return [];
};

function onSearch() {
  page.value = 1;
  searchText.value = searchInput.value;
}

function resetSearch() {
  page.value = 1;
  searchText.value = "";
  searchInput.value = "";
  selectedCardStatus.value = [];
}

function onCardStatusChecked(input) {
  if (input.isChecked) {
    selectedCardStatus.value.push(input.value);
  } else {
    selectedCardStatus.value = selectedCardStatus.value.filter((x) => x !== input.value);
  }
}

const activeFiltersCount = computed(() => {
  return selectedCardStatus.value.length;
});
</script>
