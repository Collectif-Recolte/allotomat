<i18n>
{
	"en": {
    "create-transaction": "Create a transaction",
    "manage-beneficiaries": "Participants",
    "manage-cards": "Cards",
    "manage-markets": "Markets",
    "manage-market-groups": "Market Groups",
    "manage-organizations": "Groups",
    "manage-programs": "Programs",
    "manage-subscriptions": "Subscriptions",
    "dashboard": "Dashboard",
    "primary-menu": "Main",
    "check-balance": "Check card balance",
    "manage-transactions": "Transactions"
	},
	"fr": {
    "create-transaction": "Créer une transaction",
    "manage-beneficiaries": "Participant-e-s",
    "manage-cards": "Cartes",
    "manage-markets": "Commerces",
    "manage-market-groups": "Groupes de commerces",
    "manage-organizations": "Groupes",
    "manage-programs": "Programmes",
    "manage-subscriptions": "Abonnements",
    "dashboard": "Tableau de bord",
    "primary-menu": "Principal",
    "check-balance": "Vérifier le solde d'une carte",
    "manage-transactions": "Transactions"
	}
}
</i18n>

<template>
  <nav class="px-2 space-y-1" :aria-label="t('primary-menu')">
    <MenuItem
      v-if="manageAllPrograms"
      :router-link="{ name: $consts.urls.URL_PROJECT_ADMIN }"
      :label="t('manage-programs')"
      :icon="RECEIPT_TAX" />
    <MenuItem
      v-if="manageAllMarkets"
      :router-link="{ name: $consts.urls.URL_MARKET_ADMIN }"
      :label="t('manage-markets')"
      :icon="OFFICE_BUILDING" />
    <MenuItem
      v-if="manageBeneficiaries && isProjectManager"
      :router-link="{ name: $consts.urls.URL_PROJECT_ADMIN_DASHBOARD }"
      :label="t('dashboard')"
      :icon="DASHBOARD" />
    <MenuItem
      v-if="manageOrganizations"
      :router-link="{ name: $consts.urls.URL_ORGANIZATION_ADMIN }"
      :label="t('manage-organizations')"
      :icon="BRIEFCASE" />
    <MenuItem
      v-if="manageSubscriptions"
      :router-link="{ name: $consts.urls.URL_SUBSCRIPTION_ADMIN }"
      :label="t('manage-subscriptions')"
      :icon="IDENTIFICATION" />
    <MenuItem
      v-if="manageBeneficiaries && isProjectManager"
      :router-link="{ name: $consts.urls.URL_BENEFICIARY_MANAGE }"
      :label="t('manage-beneficiaries')"
      :icon="USER_GROUP" />
    <MenuItem
      v-if="manageCards && isProjectManager"
      :router-link="{ name: $consts.urls.URL_CARDS }"
      :label="t('manage-cards')"
      :icon="CREDIT_CARD" />
    <MenuItem
      v-if="canCreateTransaction && !isProjectManager"
      :router-link="{ name: $consts.urls.URL_TRANSACTION }"
      :label="t('create-transaction')"
      :icon="QRCODE" />
    <MenuItem
      v-if="canCreateTransaction && !isProjectManager"
      :router-link="{ name: $consts.urls.URL_TRANSACTION_LIST }"
      :label="t('manage-transactions')"
      :icon="CLOCK" />
    <MenuItem
      v-if="route.meta.anonymous || (canCreateTransaction && !isProjectManager)"
      :router-link="{ name: $consts.urls.URL_CARD_CHECK }"
      :label="t('check-balance')"
      :icon="HAND_CARD" />
    <!-- TODO: Change global permission -->
    <MenuItem
      v-if="manageOrganizations"
      :router-link="{ name: $consts.urls.URL_MARKET_OVERVIEW }"
      :label="t('manage-markets')"
      :icon="OFFICE_BUILDING" />
    <MenuItem
      v-if="manageMarketGroups"
      :router-link="{ name: $consts.urls.URL_MARKET_GROUPS_OVERVIEW }"
      :label="t('manage-market-groups')"
      :icon="OFFICE_BUILDING" />
    <MenuItem
      v-if="manageTransactions && !isMarketGroupManager"
      :router-link="{ name: $consts.urls.URL_TRANSACTION_ADMIN }"
      :label="t('manage-transactions')"
      :icon="CLOCK" />
  </nav>
</template>

<script setup>
import { computed } from "vue";
import { storeToRefs } from "pinia";
import { useI18n } from "vue-i18n";
import { useRoute } from "vue-router";

import { useAuthStore } from "@/lib/store/auth";

import MenuItem from "@/components/app/menu-item";

import {
  GLOBAL_MANAGE_ALL_PROJECTS,
  GLOBAL_MANAGE_ALL_MARKETS,
  GLOBAL_MANAGE_ORGANIZATIONS,
  GLOBAL_MANAGE_BENEFICIARIES,
  GLOBAL_MANAGE_SUBSCRIPTIONS,
  GLOBAL_MANAGE_CARDS,
  GLOBAL_MANAGE_TRANSACTIONS,
  GLOBAL_CREATE_TRANSACTION,
  GLOBAL_MANAGE_MARKET_GROUPS
} from "@/lib/consts/permissions";

import { USER_TYPE_PROJECTMANAGER, USER_TYPE_MARKETGROUPMANAGER } from "@/lib/consts/enums";

import BRIEFCASE from "@/lib/icons/briefcase.json";
import CREDIT_CARD from "@/lib/icons/credit-card.json";
import IDENTIFICATION from "@/lib/icons/identification.json";
import HAND_CARD from "@/lib/icons/hand-card.json";
import OFFICE_BUILDING from "@/lib/icons/office-building.json";
import RECEIPT_TAX from "@/lib/icons/receipt-tax.json";
import QRCODE from "@/lib/icons/qrcode.json";
import USER_GROUP from "@/lib/icons/user-group.json";
import CLOCK from "@/lib/icons/clock.json";
import DASHBOARD from "@/lib/icons/dashboard.json";

const { getGlobalPermissions, userType } = storeToRefs(useAuthStore());

const route = useRoute();

const manageAllPrograms = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_ALL_PROJECTS);
});

const manageAllMarkets = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_ALL_MARKETS);
});

const manageOrganizations = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_ORGANIZATIONS);
});

const manageBeneficiaries = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_BENEFICIARIES);
});

const manageSubscriptions = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_SUBSCRIPTIONS);
});

const manageCards = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_CARDS);
});

const isProjectManager = computed(() => {
  return userType.value === USER_TYPE_PROJECTMANAGER;
});

const isMarketGroupManager = computed(() => {
  return userType.value === USER_TYPE_MARKETGROUPMANAGER;
});

const manageTransactions = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_TRANSACTIONS);
});

const canCreateTransaction = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_CREATE_TRANSACTION);
});

const manageMarketGroups = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_MARKET_GROUPS);
});

const { t } = useI18n();
</script>
