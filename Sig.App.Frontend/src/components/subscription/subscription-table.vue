<i18n>
  {
      "en": {
          "date-separator": " to ",
          "options": "Options",
          "subscription-delete": "Delete",
          "subscription-edit": "Edit configuration",
          "subscription-edit-budget-allowance": "Manage budgets allowances",
          "subscription-name": "Subscription period name",
          "subscription-period": "Active interval",
          "subscription-type": "Type",
          "subscription-budget-allowance-total": "Budget allowance total"
      },
      "fr": {
          "date-separator": " au ",
          "options": "Options",
          "subscription-delete": "Supprimer",
          "subscription-edit": "Modifier la configuration",
          "subscription-edit-budget-allowance": "Configurer les enveloppes",
          "subscription-name": "Nom de la période",
          "subscription-period": "Intervalle actif",
          "subscription-type": "Type",
          "subscription-budget-allowance-total": "Total des enveloppes"
      }
  }
</i18n>

<template>
  <UiTable v-if="props.subscriptions" :items="props.subscriptions" :cols="cols">
    <template #default="slotProps">
      <td>
        {{ getSubscriptionName(slotProps.item) }}
      </td>
      <td v-if="showSubscriptionPeriod">
        {{ getSubscriptionPeriod(slotProps.item) }}
      </td>
      <td v-if="showSubscriptionType">
        {{ getSubscriptionType(slotProps.item) }}
      </td>
      <td v-if="showBudgetAllowanceTotal">
        {{ getBudgetAllowanceTotal(slotProps.item) }}
      </td>
      <td>
        <UiButtonGroup :items="getBtnGroup(slotProps.item.id)" tooltip-position="left" />
      </td>
    </template>
  </UiTable>
</template>

<script setup>
import { defineProps, computed } from "vue";
import { useI18n } from "vue-i18n";

import { formatDate, dateUtc, textualFormat } from "@/lib/helpers/date";

import ICON_TRASH from "@/lib/icons/trash.json";
import ICON_PENCIL from "@/lib/icons/pencil.json";
import ICON_ADD_ENVELOPPE from "@/lib/icons/add-enveloppe.json";
import { URL_SUBSCRIPTION_EDIT, URL_SUBSCRIPTION_MANAGE_BUDGET_ALLOWANCE, URL_SUBSCRIPTION_DELETE } from "@/lib/consts/urls";
import { getMoneyFormat } from "@/lib/helpers/money";

const { t } = useI18n();

const props = defineProps({
  subscriptions: { type: Object, default: null },
  canEdit: Boolean,
  showSubscriptionPeriod: Boolean,
  showSubscriptionType: Boolean,
  showBudgetAllowanceTotal: Boolean
});

const cols = computed(() => {
  let cols = [];
  cols.push({ label: t("subscription-name") });
  if (props.showSubscriptionPeriod) cols.push({ label: t("subscription-period") });
  if (props.showSubscriptionType) cols.push({ label: t("subscription-type") });
  if (props.showBudgetAllowanceTotal) cols.push({ label: t("subscription-budget-allowance-total") });
  cols.push({
    label: t("options"),
    hasHiddenLabel: true
  });
  return cols;
});

const getBtnGroup = (subscriptionId) => [
  {
    icon: ICON_ADD_ENVELOPPE,
    label: t("subscription-edit-budget-allowance"),
    route: { name: URL_SUBSCRIPTION_MANAGE_BUDGET_ALLOWANCE, params: { subscriptionId: subscriptionId } }
  },
  {
    icon: ICON_PENCIL,
    label: t("subscription-edit"),
    route: { name: URL_SUBSCRIPTION_EDIT, params: { subscriptionId: subscriptionId } },
    if: props.canEdit
  },
  {
    icon: ICON_TRASH,
    label: t("subscription-delete"),
    route: { name: URL_SUBSCRIPTION_DELETE, params: { subscriptionId: subscriptionId } }
  }
];

function getSubscriptionName(item) {
  return `${item.name}`;
}

function getSubscriptionType(item) {
  return `${item.type}`;
}

function getBudgetAllowanceTotal(item) {
  return getMoneyFormat(item.budgetAllowances.reduce((partialSum, a) => partialSum + a.originalFund, 0));
}

function getSubscriptionPeriod(item) {
  return `${formatDate(dateUtc(item.startDate), textualFormat)}${t("date-separator")}${formatDate(
    dateUtc(item.endDate),
    textualFormat
  )}`;
}
</script>
