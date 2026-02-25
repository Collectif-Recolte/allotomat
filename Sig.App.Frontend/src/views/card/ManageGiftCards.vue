<i18n>
  {
    "en": {
      "generate-cards": "Generate new cards",
      "title": "Cards",
      "empty-card-list": "The program has not generated any cards yet.",
      "create-gift-card": "Create a gift card",
      "available-card": "{count} available",
      "total-card-count": "{count} program gift cards | {count} program gift card | {count} program gift cards",
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
      "sort-order-by-balance": "Sort by balance",
      "unlock-card": "Mark card as found",
      "export-cards-list": "Export",
      "manage-cards": "Manage cards",
      "manage-gift-cards": "Manage gift cards",
      "card-transactions-history": "Transactions history",
      "card-create-transaction": "Create a transaction"
    },
    "fr": {
      "generate-cards": "Générer de nouvelles cartes",
      "title": "Cartes",
      "empty-card-list": "Le programme n'a pas encore généré de cartes.",
      "create-gift-card": "Créer une carte-cadeau",
      "available-card": "{count} disponible | {count} disponible | {count} disponibles",
      "total-card-count":"{count} carte-cadeau du programme | {count} carte-cadeau du programme | {count} cartes-cadeaux du programme",
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
      "sort-order-by-balance": "Trier par solde",
      "unlock-card": "Marquer la carte comme retrouvée",
      "export-cards-list": "Exporter",
      "manage-cards": "Gérer les cartes",
      "manage-gift-cards": "Gérer les cartes-cadeaux",
      "card-transactions-history": "Historique des transactions",
      "card-create-transaction": "Créer une transaction"
    }
  }
  </i18n>

<template>
  <Loading :loading="loadingProjects" is-full-height>
    <RouterView v-slot="{ Component }">
      <Title :title="t('title')" :subpages="subpages">
        <template v-if="project" #bottom>
          <div class="flex flex-col gap-y-4 sm:flex-row sm:gap-x-4 sm:justify-between sm:items-center">
            <div class="flex flex-wrap gap-x-4">
              <h2 class="my-0">{{ t("total-card-count", { count: project.giftCards.totalCount }) }}</h2>
              <p class="my-1">{{ t("available-card", { count: project.cardStats.cardsUnassigned }) }}</p>
            </div>
            <div class="flex flex-wrap gap-x-4 gap-y-3">
              <PfButtonLink
                v-if="project"
                btn-style="outline"
                tag="routerLink"
                :label="t('create-gift-card')"
                :to="showCreateGiftCardBtn" />
            </div>
          </div>
        </template>
      </Title>
      <div v-if="giftCards && giftCardsPagination" class="px-section md:px-8 py-5">
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
        <UiEmptyPage v-if="giftCardsPagination.totalCount === 0 && activeFiltersCount === 0 && searchText === ''">
          <UiCta
            :img-src="require('@/assets/img/cards.jpg')"
            :description="t('empty-card-list')"
            :primary-btn-label="t('create-gift-card')"
            :primary-btn-route="{ name: URL_CARDS_GIFT_CARD_ADD, query: { projectId: project.id } }"
            @onPrimaryBtnClick="resetSearch">
          </UiCta>
        </UiEmptyPage>
        <UiEmptyPage v-else-if="giftCardsPagination.totalCount === 0">
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
            :cards="giftCards"
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
            v-if="giftCardsPagination && giftCardsPagination.totalPages > 1"
            v-model:page="page"
            :total-pages="giftCardsPagination.totalPages">
          </UiPagination>
        </div>
        <Component :is="Component" />
      </div>
    </RouterView>
  </Loading>
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
  URL_CARDS_GIFT_CARD_ADD,
  URL_CARDS_GIFT_CARD_QRCODE_PREVIEW,
  URL_CARDS_UNASSIGN,
  URL_CARDS_GIFT_CARD_LOST,
  URL_CARDS_GIFT_CARD_DISABLE,
  URL_CARDS_UNLOCK,
  URL_CARDS_GIFT_CARD_ENABLE,
  URL_CARDS_MANAGE,
  URL_CARDS_MANAGE_GIFT_CARDS,
  URL_TRANSACTION_ADMIN,
  URL_CARD_TRANSACTION_ADD
} from "@/lib/consts/urls";
import { GLOBAL_MANAGE_ORGANIZATIONS } from "@/lib/consts/permissions";
import { CARD_STATUS_ASSIGNED, CARD_STATUS_LOST, CARD_STATUS_GIFT, CARD_IS_DISABLED, CARD_IS_ENABLED } from "@/lib/consts/enums";
import { BY_ID, BY_BALANCE, ASC, DESC } from "@/lib/consts/card-sort-order";

import Title from "@/components/app/title";
import CardSummaryTable from "@/components/card/card-summary-table.vue";

import ICON_CARD_LOST from "@/lib/icons/card-lost.json";
import ICON_QR_CODE from "@/lib/icons/qrcode.json";
import ICON_MINUS from "@/lib/icons/minus.json";
import ICON_CLOSE from "@/lib/icons/close.json";
import ICON_CARD_LINK from "@/lib/icons/card-link.json";
import ICON_CLOCK from "@/lib/icons/clock.json";
import ICON_TRANSACTION from "@/lib/icons/add-square.json";

const { getGlobalPermissions } = storeToRefs(useAuthStore());
const { t } = useI18n();

const TRANSACTION_HISTORY_DATE_FROM = "2023-01-01";

const page = ref(1);
const searchInput = ref("");
const searchText = ref("");
const selectedCardStatus = ref([]);
const selectedCardDisabled = ref([]);
const sortOrder = ref(BY_ID);
const project = ref(null);
const giftCardsPagination = ref(null);
const giftCards = ref(null);

const availableCardStatus = [
  { value: CARD_STATUS_ASSIGNED, label: t("card-assigned") },
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

const subpages = computed(() => {
  return [
    {
      route: { name: URL_CARDS_MANAGE },
      label: t("manage-cards")
    },
    {
      route: { name: URL_CARDS_MANAGE_GIFT_CARDS },
      label: t("manage-gift-cards")
    }
  ];
});

const canManageOrganizations = () => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_ORGANIZATIONS);
};

const {
  result: resultProjects,
  refetch: refetchCards,
  loading: loadingProjects
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
        giftCards(
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
    enabled: computed(() => canManageOrganizations())
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
  giftCardsPagination.value = value.projects[0]?.giftCards;
  giftCards.value = value.projects[0]?.giftCards.items;
});

const administrationSubscriptionsOffPlatform = computed(() => project.value?.administrationSubscriptionsOffPlatform);

const showCreateGiftCardBtn = computed(() => {
  return project.value ? { name: URL_CARDS_GIFT_CARD_ADD, query: { projectId: project.value.id } } : null;
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
      name: URL_CARDS_GIFT_CARD_QRCODE_PREVIEW,
      params: { cardId: card.id }
    }
  }
];

const getAfterBtnGroup = (card) => {
  let buttons = [];

  if (card.status === CARD_STATUS_LOST) {
    buttons.push({
      label: t("unlock-card"),
      icon: ICON_CARD_LINK,
      route: {
        name: URL_CARDS_UNLOCK,
        params: { cardId: card.id }
      }
    });
  } else {
    buttons.push({
      label: t("lost-card"),
      icon: ICON_CARD_LOST,
      route: {
        name: URL_CARDS_GIFT_CARD_LOST,
        params: { cardId: card.id }
      }
    });
  }

  if (card.isDisabled) {
    buttons.push({
      label: t("beneficiary-enable-card"),
      icon: ICON_CARD_LINK,
      route: {
        name: URL_CARDS_GIFT_CARD_ENABLE,
        params: { cardId: card.id }
      }
    });
  } else {
    buttons.push({
      label: t("beneficiary-disable-card"),
      icon: ICON_CLOSE,
      route: {
        name: URL_CARDS_GIFT_CARD_DISABLE,
        params: { cardId: card.id }
      }
    });
  }

  if (card.beneficiary !== null) {
    buttons.push({
      label: t("remove-card"),
      icon: ICON_MINUS,
      route: {
        name: URL_CARDS_UNASSIGN,
        params: { beneficiaryId: card.beneficiary.id, cardId: card.id }
      }
    });
  }

  buttons.push({
    icon: ICON_CLOCK,
    label: t("card-transactions-history"),
    route: { name: URL_TRANSACTION_ADMIN, query: { text: card.cardNumber, dateFrom: TRANSACTION_HISTORY_DATE_FROM } } // Set datefrom to a farthest date to get all transactions
  });

  buttons.push({
    icon: ICON_TRANSACTION,
    label: t("card-create-transaction"),
    route: { name: URL_CARD_TRANSACTION_ADD, params: { cardId: card.id } }
  });

  return buttons;
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
