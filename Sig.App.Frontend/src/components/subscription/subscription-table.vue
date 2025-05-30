<i18n>
  {
      "en": {
          "date-separator": " to ",
          "options": "Options",
          "subscription-delete": "Delete",
          "subscription-archive": "Archived",
          "subscription-archived": "(Archived)",
          "subscription-unarchive": "Unarchived",
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
          "subscription-archive": "Archiver",
          "subscription-archived": "Archivé",
          "subscription-unarchive": "Désarchiver",
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
      <td :class="{ 'text-primary-500': slotProps.item.isArchived }">
        <span
          v-if="slotProps.item.isArchived"
          class="bg-grey-50 border border-grey-100 rounded-md px-1.5 py-0.5 text-xs font-semibold mr-1">
          {{ t("subscription-archived") }}
        </span>
        {{ getSubscriptionName(slotProps.item) }}
      </td>
      <td v-if="showSubscriptionPeriod" :class="{ 'text-primary-500': slotProps.item.isArchived }">
        {{ getSubscriptionPeriod(slotProps.item) }}
      </td>
      <td v-if="showSubscriptionType">
        {{ getSubscriptionType(slotProps.item) }}
      </td>
      <td v-if="showBudgetAllowanceTotal" class="text-right" :class="{ 'text-primary-500': slotProps.item.isArchived }">
        {{ getBudgetAllowanceTotal(slotProps.item) }}
      </td>
      <td>
        <div class="inline-flex items-center gap-x-2">
          <template v-if="props.canEdit">
            <PfButtonLink
              v-if="!slotProps.item.isArchived"
              tag="routerLink"
              btn-style="outline"
              size="sm"
              :label="t('subscription-archive')"
              :to="{ name: URL_SUBSCRIPTION_ARCHIVE, params: { subscriptionId: slotProps.item.id } }" />
            <PfButtonLink
              v-else
              tag="routerLink"
              btn-style="outline"
              size="sm"
              :label="t('subscription-unarchive')"
              :to="{ name: URL_SUBSCRIPTION_UNARCHIVE, params: { subscriptionId: slotProps.item.id } }" />
          </template>
          <UiButtonGroup :items="getBtnGroup(slotProps.item)" tooltip-position="left" />
        </div>
      </td>
    </template>
  </UiTable>
</template>

<script setup>
import { defineProps, computed } from "vue";
import { useI18n } from "vue-i18n";

import { formatDate, dateUtc, textualFormat } from "@/lib/helpers/date";
import { subscriptionName } from "@/lib/helpers/subscription";

import ICON_TRASH from "@/lib/icons/trash.json";
import ICON_PENCIL from "@/lib/icons/pencil.json";
import ICON_ADD_ENVELOPPE from "@/lib/icons/add-enveloppe.json";

import {
  URL_SUBSCRIPTION_EDIT,
  URL_SUBSCRIPTION_MANAGE_BUDGET_ALLOWANCE,
  URL_SUBSCRIPTION_DELETE,
  URL_SUBSCRIPTION_ARCHIVE,
  URL_SUBSCRIPTION_UNARCHIVE
} from "@/lib/consts/urls";
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
  if (props.showBudgetAllowanceTotal) cols.push({ label: t("subscription-budget-allowance-total"), isRight: true });
  cols.push({
    label: t("options"),
    hasHiddenLabel: true
  });
  return cols;
});

const getBtnGroup = (subscription) => [
  {
    icon: ICON_ADD_ENVELOPPE,
    label: t("subscription-edit-budget-allowance"),
    route: { name: URL_SUBSCRIPTION_MANAGE_BUDGET_ALLOWANCE, params: { subscriptionId: subscription.id } },
    if: !subscription.isArchived
  },
  {
    icon: ICON_PENCIL,
    label: t("subscription-edit"),
    route: { name: URL_SUBSCRIPTION_EDIT, params: { subscriptionId: subscription.id } },
    if: props.canEdit && !subscription.isArchived
  },
  {
    icon: ICON_TRASH,
    label: t("subscription-delete"),
    route: { name: URL_SUBSCRIPTION_DELETE, params: { subscriptionId: subscription.id } }
  }
];

function getSubscriptionName(item) {
  return subscriptionName(item);
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
