<i18n>
  {
    "en": {
      "display-qr-code": "View QR code",
      "remove-card": "Unassign",
      "empty-list": "There is no active card yet",
      "lost-card": "Lost card",
      "no-results": "Your search yields no results",
      "reset-search": "Reset search",
      "switch-view": "Go to the list of participants without a card",
      "card-status": "Status",
      "gift-card-label": "Gift card",
      "lost-card-label": "Lost card",
      "card-assigned": "Assigned",
      "card-unassigned": "Unassigned",
      "card-deactivated": "Deactivated"
    },
    "fr": {
		  "display-qr-code": "Voir le code QR",
      "remove-card": "Désassigner",
      "empty-list": "Il n'y a encore aucune carte active",
      "lost-card": "Carte perdue",
      "no-results": "Votre recherche ne donne aucun résultat",
      "reset-search": "Réinitialiser la recherche",
      "switch-view": "Aller à la liste des participants sans carte",
      "card-status": "Statut",
      "gift-card-label": "Carte cadeau",
      "lost-card-label": "Carte perdue",
      "card-assigned": "Assignée",
      "card-unassigned": "Non assignée",
      "card-deactivated": "Désactivée"
    }
  }
</i18n>

<template>
  <div v-if="cards">
    <RouterView v-slot="{ Component }">
      <div v-if="cardsPagination">
        <UiTableHeader>
          <template #right>
            <UiFilter
              v-model="searchInput"
              has-search
              has-filters
              :has-active-filters="!!searchText"
              :beneficiaries-are-anonymous="beneficiariesAreAnonymous && canManageOrganizations"
              @resetFilters="onResetSearch"
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

        <Component :is="Component" />
      </div>
    </RouterView>
  </div>
</template>

<script setup>
import { ref, computed } from "vue";
import { useI18n } from "vue-i18n";
import gql from "graphql-tag";
import { useQuery, useResult } from "@vue/apollo-composable";
import { storeToRefs } from "pinia";

import { URL_CARDS_QRCODE_PREVIEW, URL_CARDS_UNASSIGN, URL_CARDS_LOST } from "@/lib/consts/urls";
import { GLOBAL_MANAGE_ORGANIZATIONS } from "@/lib/consts/permissions";
import {
  CARD_STATUS_ASSIGNED,
  CARD_STATUS_UNASSIGNED,
  CARD_STATUS_DEACTIVATED,
  CARD_STATUS_LOST,
  CARD_STATUS_GIFT
} from "@/lib/consts/enums";

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
const selectedCardStatus = ref([]);

const availableCardStatus = [
  { value: CARD_STATUS_ASSIGNED, label: t("card-assigned") },
  { value: CARD_STATUS_UNASSIGNED, label: t("card-unassigned") },
  { value: CARD_STATUS_DEACTIVATED, label: t("card-deactivated") },
  { value: CARD_STATUS_LOST, label: t("lost-card-label") },
  { value: CARD_STATUS_GIFT, label: t("gift-card-label") }
];

const { result } = useQuery(
  gql`
    query Projects($page: Int!, $status: [CardStatus!]) {
      projects {
        id
        name
        beneficiariesAreAnonymous
        administrationSubscriptionsOffPlatform
        cardStats {
          cardsUnassigned
        }
        cards(page: $page, limit: 30, status: $status) {
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
  projectsVariables
);

function projectsVariables() {
  return {
    page: page.value,
    status: selectedCardStatus.value
  };
}

const cards = useResult(result, null, (data) => {
  return data.projects[0]?.cards.items;
});

const cardsPagination = useResult(result, null, (data) => {
  return data.projects[0]?.cards;
});

const canManageOrganizations = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_ORGANIZATIONS);
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
}

function onResetSearch() {
  page.value = 1;
}

function onCardStatusChecked(input) {
  if (input.isChecked) {
    selectedCardStatus.value.push(input.value);
  } else {
    selectedCardStatus.value = selectedCardStatus.value.filter((x) => x !== input.value);
  }
}
</script>
