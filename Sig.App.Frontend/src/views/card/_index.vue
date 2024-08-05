<i18n>
{
	"en": {
		"generate-cards": "Generate new cards",
		"title": "Cards",
    "empty-card-list": "The program has not generated any cards yet.",
    "create-gift-card": "Create a gift card",
    "available-card": "{count} available",
    "total-card-count": "{count} program cards | {count} program card | {count} program cards",
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
    "search-placeholder": "Search by ID or card number",
    "beneficiary-disable-card": "Disable card",
    "beneficiary-enable-card": "Enable card",
    "card-disabled-status": "Card status",
    "card-is-disabled": "Temporarily disabled",
    "card-is-enabled": "Card is enabled",
    "sort-by-balance": "Balances",
    "sort-by-id": "ID",
    "sort-order": "Sort order",
    "sort-order-by-id": "Sort by ID",
    "sort-order-by-balance": "Sort by balance"
	},
	"fr": {
		"generate-cards": "Générer de nouvelles cartes",
		"title": "Cartes",
    "empty-card-list": "Le programme n'a pas encore généré de cartes.",
    "create-gift-card": "Créer une carte-cadeau",
    "available-card": "{count} disponible | {count} disponible | {count} disponibles",
    "total-card-count":"{count} carte du programme | {count} carte du programme | {count} cartes du programme",
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
    "search-placeholder": "Chercher par ID ou n° de carte",
    "beneficiary-disable-card": "Désactiver la carte",
    "beneficiary-enable-card": "Réactiver la carte",
    "card-disabled-status": "État de la carte",
    "card-is-disabled": "Désactivée temporairement",
    "card-is-enabled": "Carte activée",
    "sort-by-balance": "Soldes",
    "sort-by-id": "ID",
    "sort-order": "Ordre de tri",
    "sort-order-by-id": "Trier par ID",
    "sort-order-by-balance": "Trier par solde"
	}
}
</i18n>

<template>
  <RouterView v-slot="{ Component }">
    <AppShell :loading="loadingProjects" class="card-list-vue">
      <template #title>
        <Title :title="t('title')">
          <template v-if="project" #bottom>
            <div class="flex flex-col gap-y-4 sm:flex-row sm:gap-x-4 sm:justify-between sm:items-center">
              <div class="flex flex-wrap gap-x-4">
                <h2 class="my-0">{{ t("total-card-count", { count: project.cards.totalCount }) }}</h2>
                <p class="my-1">{{ t("available-card", { count: project.cardStats.cardsUnassigned }) }}</p>
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
              has-sort
              :sort-order="sortOrderDirection"
              :sort-label="sortLabel"
              :placeholder="t('search-placeholder')"
              :has-active-filters="!!searchText || activeFiltersCount > 0"
              :active-filters-count="activeFiltersCount"
              :beneficiaries-are-anonymous="project.beneficiariesAreAnonymous && canManageOrganizations"
              @resetFilters="resetSearch"
              @search="onSearch">
              <template #sortOrder>
                <PfFormInputRadioGroup
                  id="sortOrder"
                  :value="sortOrder"
                  :label="t('sort-order')"
                  :options="sortOrderOptions"
                  @input="onSortOrderChanged" />
              </template>
              <PfFormInputCheckboxGroup
                v-if="availableCardStatus.length > 0"
                id="card-status"
                is-filter
                :value="selectedCardStatus"
                :label="t('card-status')"
                :options="availableCardStatus"
                @input="onCardStatusChecked" />
              <PfFormInputCheckboxGroup
                id="cardDisabled"
                class="mt-3"
                is-filter
                :value="selectedCardDisabled"
                :label="t('card-disabled-status')"
                :options="cardDisabled"
                @input="onCardIsDisabledChecked" />
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
            :beneficiaries-are-anonymous="project.beneficiariesAreAnonymous && canManageOrganizations"
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
import { ref, computed, watch } from "vue";
import { useI18n } from "vue-i18n";
import { onBeforeRouteUpdate } from "vue-router";
import { useQuery } from "@vue/apollo-composable";
import { storeToRefs } from "pinia";

import { usePageTitle } from "@/lib/helpers/page-title";
import { useAuthStore } from "@/lib/store/auth";

import {
  URL_CARDS,
  URL_CARDS_ADD,
  URL_GIFT_CARD_ADD,
  URL_CARDS_QRCODE_PREVIEW,
  URL_CARDS_UNASSIGN,
  URL_CARDS_LOST,
  URL_CARDS_ENABLE,
  URL_CARDS_DISABLE
} from "@/lib/consts/urls";
import { GLOBAL_MANAGE_ORGANIZATIONS } from "@/lib/consts/permissions";
import {
  CARD_STATUS_ASSIGNED,
  CARD_STATUS_UNASSIGNED,
  CARD_STATUS_LOST,
  CARD_STATUS_GIFT,
  CARD_IS_DISABLED,
  CARD_IS_ENABLED
} from "@/lib/consts/enums";
import { BY_ID, BY_BALANCE, ASC, DESC } from "@/lib/consts/card-sort-order";

import Title from "@/components/app/title";
import CardSummaryTable from "@/components/card/card-summary-table.vue";

import ICON_CARD_LOST from "@/lib/icons/card-lost.json";
import ICON_QR_CODE from "@/lib/icons/qrcode.json";
import ICON_MINUS from "@/lib/icons/minus.json";
import ICON_CLOSE from "@/lib/icons/close.json";
import ICON_CARD_LINK from "@/lib/icons/card-link.json";

const { getGlobalPermissions } = storeToRefs(useAuthStore());
const { t } = useI18n();

const page = ref(1);
const searchInput = ref("");
const searchText = ref("");
const selectedCardStatus = ref([]);
const selectedCardDisabled = ref([]);
const sortOrder = ref(BY_ID);
const project = ref(null);
const cardsPagination = ref(null);
const cards = ref(null);

const availableCardStatus = [
  { value: CARD_STATUS_ASSIGNED, label: t("card-assigned") },
  { value: CARD_STATUS_UNASSIGNED, label: t("card-unassigned") },
  { value: CARD_STATUS_LOST, label: t("lost-card-label") },
  { value: CARD_STATUS_GIFT, label: t("gift-card-label") }
];

const cardDisabled = ref([
  { value: CARD_IS_DISABLED, label: t("card-is-disabled") },
  { value: CARD_IS_ENABLED, label: t("card-is-enabled") }
]);

const sortOrderOptions = [
  {
    id: "by-id",
    value: BY_ID,
    label: t("sort-by-id")
  },
  {
    id: "by-balance",
    value: BY_BALANCE,
    label: t("sort-by-balance")
  }
];

usePageTitle(t("title"));

const canManageOrganizations = () => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_ORGANIZATIONS);
};

const {
  result: resultProjects,
  loading: loadingProjects,
  refetch: refetchCards
} = useQuery(
  gql`
    query Projects($page: Int!, $status: [CardStatus!], $searchText: String, $withCardDisabled: Boolean, $sort: CardSortSort) {
      projects {
        id
        name
        beneficiariesAreAnonymous
        administrationSubscriptionsOffPlatform
        cardStats {
          cardsUnassigned
        }
        cards(
          page: $page
          limit: 30
          status: $status
          searchText: $searchText
          withCardDisabled: $withCardDisabled
          sort: $sort
        ) {
          pageNumber
          pageSize
          totalCount
          totalPages
          items {
            id
            isDisabled
            programCardId
            cardNumber
            status
            totalFund
            loyaltyFund {
              id
              amount
            }
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
    withCardDisabled:
      selectedCardDisabled.value.length === 1 ? selectedCardDisabled.value.indexOf(CARD_IS_DISABLED) !== -1 : null,
    searchText: searchText.value,
    sort: { field: sortOrder.value, order: ASC }
  };
}

watch(resultProjects, (value) => {
  if (value === undefined) return;

  project.value = value.projects[0];
  cardsPagination.value = value.projects[0]?.cards;
  cards.value = value.projects[0]?.cards.items;
});

const showCreateGiftCardBtn = computed(() => {
  return project.value ? { name: URL_GIFT_CARD_ADD, query: { projectId: project.value.id } } : null;
});

const sortLabel = computed(() => {
  return sortOrder.value === BY_ID ? t("sort-order-by-id") : t("sort-order-by-balance");
});

const sortOrderDirection = computed(() => {
  return sortOrder.value === BY_ID ? ASC : DESC;
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
    if (card.isDisabled) {
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
          label: t("beneficiary-enable-card"),
          icon: ICON_CARD_LINK,
          route: {
            name: URL_CARDS_ENABLE,
            params: { cardId: card.id }
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
    } else {
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
          label: t("beneficiary-disable-card"),
          icon: ICON_CLOSE,
          route: {
            name: URL_CARDS_DISABLE,
            params: { cardId: card.id }
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
  }
  if (card.status === CARD_STATUS_GIFT) {
    if (card.isDisabled) {
      return [
        {
          label: t("beneficiary-enable-card"),
          icon: ICON_CARD_LINK,
          route: {
            name: URL_CARDS_ENABLE,
            params: { cardId: card.id }
          }
        }
      ];
    }
    return [
      {
        label: t("beneficiary-disable-card"),
        icon: ICON_CLOSE,
        route: {
          name: URL_CARDS_DISABLE,
          params: { cardId: card.id }
        }
      }
    ];
  }
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
  selectedCardDisabled.value = [];
}

function onCardStatusChecked(input) {
  if (input.isChecked) {
    selectedCardStatus.value.push(input.value);
  } else {
    selectedCardStatus.value = selectedCardStatus.value.filter((x) => x !== input.value);
  }
}

function onCardIsDisabledChecked(input) {
  if (input.isChecked) {
    selectedCardDisabled.value.push(input.value);
  } else {
    selectedCardDisabled.value = selectedCardDisabled.value.filter((x) => x !== input.value);
  }
}

function onSortOrderChanged(input) {
  sortOrder.value = input;
}

const activeFiltersCount = computed(() => {
  return selectedCardStatus.value.length + selectedCardDisabled.value.length;
});

onBeforeRouteUpdate((to) => {
  if (to.name === URL_CARDS) {
    refetchCards();
  }
});
</script>

<style scoped lang="postcss">
.card-list-vue {
  --pf-top-header-height: 170px;
  --pf-table-header-height: 67px;
  --ui-table-height: calc(
    100dvh -
      (var(--pf-top-bar-height) + var(--pf-top-header-height) + var(--pf-table-header-height) + var(--pf-footer-height) + 2rem)
  );

  @media screen("xs") {
    --pf-top-header-height: 123px;
    --pf-table-header-height: 72px;
    --ui-table-height: calc(
      100dvh -
        (var(--pf-top-bar-height) + var(--pf-top-header-height) + var(--pf-table-header-height) + var(--pf-footer-height) + 2rem)
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
    --pf-top-header-height: 156px;
    --pf-table-header-height: 84px;
    --ui-table-height: calc(
      100dvh -
        (
          var(--pf-top-bar-height) + var(--pf-top-header-height) + var(--pf-table-header-height) + var(--pf-footer-height) +
            3.6rem
        )
    );
  }
}
</style>
