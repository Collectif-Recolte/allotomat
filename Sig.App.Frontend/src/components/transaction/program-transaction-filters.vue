<i18n>
{
	"en": {
    "date-selector-from": "Interval from",
    "date-selector-to": "to",
    "date-start-label": "Start date of transactions",
    "date-end-label": "End date of transactions",
    "organizations": "Groups",
		"beneficiary-type": "Categories",
    "subscription": "Subscriptions",
    "without-subscription-option": "None",
    "transaction-log-types": "Transaction types",
    "transaction-log-type-expired": "Funds expired",
    "transaction-log-type-loyalty": "Gift Card",
    "transaction-log-type-manually": "Fund added manually",
    "transaction-log-type-off-platform": "Adding Fund (Off-Platform Participant)",
    "transaction-log-type-subscription": "Subscription",
    "transaction-log-type-payment": "Payment",
    "transaction-log-type-transfer": "Fund transferred",
    "transaction-log-type-refund-budget-allowance-from-unassigned-card": "Budget allowance refund from unassigned card",
    "transaction-log-type-refund-budget-allowance-from-no-card-when-adding-fund": "Budget allowances refund from participant having no cards",
    "transaction-log-type-refund-budget-allowance-from-removed-beneficiary-from-subscription": "Budget allowance refund from participant removed from subscription",
    "transaction-log-type-refund-payment": "Payment refund",
    "market": "Markets",
    "cash-register": "Cash Registers",
    "gift-card-transaction-types": "Gift Card Transaction Types",
    "with-gift-card": "With Gift Card",
    "without-gift-card": "Without Gift Card"
	},
	"fr": {
    "date-selector-from": "Intervalle du",
    "date-selector-to": "au",
    "date-start-label": "Date de début des transactions",
    "date-end-label": "Date de fin des transactions",
    "organizations": "Groupes",
		"beneficiary-type": "Catégories",
    "subscription": "Abonnements",
    "without-subscription-option": "Aucun",
    "transaction-log-types": "Types de transactions",
    "transaction-log-type-expired": "Expiration des fonds",
    "transaction-log-type-loyalty": "Carte-cadeau",
    "transaction-log-type-manually": "Fond ajouté manuellement",
    "transaction-log-type-off-platform": "Ajout de fond (participant hors plateforme)",
    "transaction-log-type-subscription": "Abonnement",
    "transaction-log-type-payment": "Paiement",
    "transaction-log-type-transfer": "Fond transféré",
    "transaction-log-type-refund-budget-allowance-from-unassigned-card": "Enveloppe remboursées après la désassignation d'une carte",
    "transaction-log-type-refund-budget-allowance-from-no-card-when-adding-fund": "Enveloppe remboursée en raison d'un participant sans carte",
    "transaction-log-type-refund-budget-allowance-from-removed-beneficiary-from-subscription": "Enveloppe remboursée après avoir retiré un participant d'un abonnement",
    "transaction-log-type-refund-payment": "Remboursement d'un paiement",
    "market": "Commerces",
    "cash-register": "Caisses",
    "gift-card-transaction-types": "Carte-cadeaux",
    "with-gift-card": "Avec carte-cadeau",
    "without-gift-card": "Sans carte-cadeau"
	}
}
</i18n>

<template>
  <UiFilter
    :model-value="modelValue"
    :has-search="props.hasSearch"
    :items-can-wrap="itemsCanWrap"
    :has-active-filters="hasActiveFilters"
    :active-filters-count="activeFiltersCount"
    @resetFilters="onResetFilters"
    @search="onSearch"
    @update:modelValue="(e) => emit('update:modelValue', e)">
    <template v-if="!props.hideDate" #prependElements>
      <div class="text-right mb-2 w-full xs:flex xs:gap-x-4 xs:justify-end sm:mb-0 xl:w-auto">
        <div class="flex items-center justify-end gap-x-4 mb-2 xs:mb-0">
          <span class="text-sm text-primary-700">{{ t("date-selector-from") }}</span>
          <UiDatePicker
            id="datefrom"
            :model-value="dateFrom"
            class="sm:col-span-6"
            :label="t('date-start-label')"
            has-hidden-label
            :can-clear="false"
            @update:modelValue="(e) => emit('dateFromUpdated', e)" />
        </div>
        <div class="flex items-center justify-end gap-x-4">
          <span class="text-sm text-primary-700">{{ t("date-selector-to") }}</span>
          <UiDatePicker
            id="dateTo"
            :model-value="dateTo"
            :min-date="dateFrom"
            class="sm:col-span-6"
            :label="t('date-end-label')"
            has-hidden-label
            :can-clear="false"
            @update:modelValue="(e) => emit('dateToUpdated', e)" />
        </div>
        <slot name="prependElements" />
      </div>
    </template>
    <template #appendElements>
      <div class="flex flex-row flex-wrap gap-2 justify-end">
        <UiFilterSelect :label="t('organizations')" :active-filters-count="organizationActiveFiltersCount">
          <PfFormInputCheckboxGroup
            v-if="availableOrganizations.length > 0"
            id="organizations"
            is-filter
            :value="selectedOrganizations"
            :options="availableOrganizations"
            @input="onOrganizationsChecked" />
        </UiFilterSelect>
        <UiFilterSelect :label="t('beneficiary-type')" :active-filters-count="beneficiaryTypeActiveFiltersCount">
          <PfFormInputCheckboxGroup
            v-if="availableBeneficiaryTypes.length > 0"
            id="beneficiary-types"
            class="mt-3"
            is-filter
            :value="selectedBeneficiaryTypes"
            :options="availableBeneficiaryTypes"
            @input="onBeneficiaryTypesChecked" />
        </UiFilterSelect>
        <UiFilterSelect :label="t('subscription')" :active-filters-count="subscriptionActiveFiltersCount">
          <PfFormInputCheckboxGroup
            v-if="availableSubscriptions.length > 0"
            id="subscriptions"
            class="mt-3"
            is-filter
            :value="selectedSubscriptions"
            :options="availableSubscriptions"
            @input="onSubscriptionsChecked" />
        </UiFilterSelect>
        <UiFilterSelect :label="t('market')" :active-filters-count="marketActiveFiltersCount">
          <PfFormInputCheckboxGroup
            v-if="availableMarkets.length > 0"
            id="markets"
            class="mt-3"
            is-filter
            :value="selectedMarkets"
            :options="availableMarkets"
            @input="onMarketsChecked" />
        </UiFilterSelect>
        <UiFilterSelect :label="t('cash-register')" :active-filters-count="cashRegisterActiveFiltersCount">
          <PfFormInputCheckboxGroup
            v-if="availableCashRegister.length > 0"
            id="cashRegisters"
            class="mt-3"
            is-filter
            :value="selectedCashRegisters"
            :options="availableCashRegister"
            @input="onCashRegistersChecked" />
        </UiFilterSelect>
        <UiFilterSelect :label="t('transaction-log-types')" :active-filters-count="transactionTypeActiveFiltersCount">
          <PfFormInputCheckboxGroup
            v-if="availableTransactionTypes.length > 0 && !props.hideTransactionType"
            id="transactionTypes"
            class="mt-3"
            is-filter
            :value="selectedTransactionTypes"
            :options="availableTransactionTypes"
            @input="onTransactionTypesChecked" />
        </UiFilterSelect>
        <UiFilterSelect
          :label="t('gift-card-transaction-types')"
          :active-filters-count="giftCardTransactionTypeActiveFiltersCount">
          <PfFormInputCheckboxGroup
            v-if="availableGiftCardTransactionTypes.length > 0 && !props.hideGiftCardTransactionType"
            id="giftCardTransactionTypes"
            class="mt-3"
            is-filter
            :value="selectedGiftCardTransactionTypes"
            :options="availableGiftCardTransactionTypes"
            @input="onGiftCardTransactionTypesChecked" />
        </UiFilterSelect>
      </div>
    </template>
  </UiFilter>
</template>

<script setup>
import { defineProps, defineEmits, computed } from "vue";
import { useI18n } from "vue-i18n";
import { subscriptionName } from "@/lib/helpers/subscription";

const { t } = useI18n();

const emit = defineEmits([
  "organizationsChecked",
  "organizationsUnchecked",
  "subscriptionsChecked",
  "subscriptionsUnchecked",
  "beneficiaryTypesChecked",
  "beneficiaryTypesUnchecked",
  "resetFilters",
  "search",
  "update:modelValue",
  "transactionTypesChecked",
  "transactionTypesUnchecked",
  "dateFromUpdated",
  "dateToUpdated",
  "marketsChecked",
  "marketsUnchecked",
  "cashRegistersChecked",
  "cashRegistersUnchecked",
  "giftCardTransactionTypesChecked",
  "giftCardTransactionTypesUnchecked"
]);

const props = defineProps({
  availableOrganizations: {
    type: Array,
    default() {
      return [];
    }
  },
  selectedOrganizations: {
    type: Array,
    default() {
      return [];
    }
  },
  availableBeneficiaryTypes: {
    type: Array,
    default() {
      return [];
    }
  },
  selectedBeneficiaryTypes: {
    type: Array,
    default() {
      return [];
    }
  },
  availableSubscriptions: {
    type: Array,
    default() {
      return [];
    }
  },
  selectedSubscriptions: {
    type: Array,
    default() {
      return [];
    }
  },
  availableMarkets: {
    type: Array,
    default() {
      return [];
    }
  },
  availableCashRegister: {
    type: Array,
    default() {
      return [];
    }
  },
  selectedMarkets: {
    type: Array,
    default() {
      return [];
    }
  },
  selectedCashRegisters: {
    type: Array,
    default() {
      return [];
    }
  },
  selectedTransactionTypes: {
    type: Array,
    default() {
      return [];
    }
  },
  selectedGiftCardTransactionTypes: {
    type: Array,
    default() {
      return [];
    }
  },
  withoutSubscriptionId: {
    type: String,
    default: ""
  },
  withCashRegisterId: {
    type: String,
    default: ""
  },
  beneficiaryStatusActive: {
    type: String,
    default: ""
  },
  beneficiaryStatusInactive: {
    type: String,
    default: ""
  },
  modelValue: {
    type: String,
    default: ""
  },
  searchFilter: {
    type: String,
    default: ""
  },
  dateFrom: {
    type: Date,
    default: undefined
  },
  dateTo: {
    type: Date,
    default: undefined
  },
  administrationSubscriptionsOffPlatform: {
    type: Boolean,
    default: false
  },
  hideDate: {
    type: Boolean,
    default: false
  },
  hasSearch: {
    type: Boolean,
    default: true
  },
  hideTransactionType: {
    type: Boolean,
    default: false
  },
  hideGiftCardTransactionType: {
    type: Boolean,
    default: false
  },
  withGiftCardId: {
    type: String,
    default: ""
  },
  withoutGiftCardId: {
    type: String,
    default: ""
  },
  itemsCanWrap: {
    type: Boolean,
    default: true
  }
});

const hasActiveFilters = computed(() => {
  return (
    props.selectedOrganizations?.length > 0 ||
    props.selectedBeneficiaryTypes?.length > 0 ||
    props.selectedSubscriptions?.length > 0 ||
    props.selectedTransactionTypes?.length > 0 ||
    props.selectedCashRegisters?.length > 0 ||
    props.selectedMarkets?.length > 0 ||
    !!props.searchFilter
  );
});

const activeFiltersCount = computed(() => {
  const selectedOrganizationsCount = props.selectedOrganizations?.length ?? 0;
  const selectedBeneficiariesCount = props.selectedBeneficiaryTypes?.length ?? 0;
  const selectedSubscritionsCount = props.selectedSubscriptions?.length ?? 0;
  const selectedTransactionTypesCount = props.selectedTransactionTypes?.length ?? 0;
  const selectedCashRegistersCount = props.selectedCashRegisters?.length ?? 0;
  const selectedMarketsCount = props.selectedMarkets?.length ?? 0;

  return (
    selectedOrganizationsCount +
    selectedBeneficiariesCount +
    selectedSubscritionsCount +
    selectedTransactionTypesCount +
    selectedCashRegistersCount +
    selectedMarketsCount
  );
});

const organizationActiveFiltersCount = computed(() => {
  return props.selectedOrganizations?.length ?? 0;
});

const beneficiaryTypeActiveFiltersCount = computed(() => {
  return props.selectedBeneficiaryTypes?.length ?? 0;
});

const subscriptionActiveFiltersCount = computed(() => {
  return props.selectedSubscriptions?.length ?? 0;
});

const marketActiveFiltersCount = computed(() => {
  return props.selectedMarkets?.length ?? 0;
});

const cashRegisterActiveFiltersCount = computed(() => {
  return props.selectedCashRegisters?.length ?? 0;
});

const transactionTypeActiveFiltersCount = computed(() => {
  return props.selectedTransactionTypes?.length ?? 0;
});

const giftCardTransactionTypeActiveFiltersCount = computed(() => {
  return props.selectedGiftCardTransactionTypes?.length ?? 0;
});

const availableOrganizations = computed(() => {
  if (!props.availableOrganizations || props.availableOrganizations?.length <= 0) return [];
  return props.availableOrganizations.map((x) => ({ value: x.id, label: x.name }));
});

const availableBeneficiaryTypes = computed(() => {
  if (!props.availableBeneficiaryTypes || props.availableBeneficiaryTypes?.length <= 0) return [];
  return props.availableBeneficiaryTypes.map((x) => ({ value: x.id, label: x.name }));
});

const availableSubscriptions = computed(() => {
  if (props.administrationSubscriptionsOffPlatform) {
    return [];
  }
  let subscriptions = [];
  if (props.availableSubscriptions && props.availableSubscriptions?.length > 0) {
    subscriptions = props.availableSubscriptions.map((x) => ({ value: x.id, label: subscriptionName(x) }));
  }

  if (props.withoutSubscriptionId !== "") {
    subscriptions.push({ value: props.withoutSubscriptionId, label: t("without-subscription-option") });
  }

  return subscriptions;
});

const availableMarkets = computed(() => {
  if (!props.availableMarkets || props.availableMarkets?.length <= 0) return [];
  return props.availableMarkets.map((x) => ({ value: x.id, label: x.name })).sort((a, b) => a.label.localeCompare(b.label));
});

const availableCashRegister = computed(() => {
  if (!props.availableCashRegister || props.availableCashRegister?.length <= 0) return [];
  return props.availableCashRegister.map((x) => ({ value: x.id, label: x.name }));
});

const availableTransactionTypes = computed(() => {
  return [
    { value: "ExpireFundTransactionLog", label: t("transaction-log-type-expired") },
    { value: "LoyaltyAddingFundTransactionLog", label: t("transaction-log-type-loyalty") },
    { value: "ManuallyAddingFundTransactionLog", label: t("transaction-log-type-manually") },
    { value: "OffPlatformAddingFundTransactionLog", label: t("transaction-log-type-off-platform") },
    { value: "SubscriptionAddingFundTransactionLog", label: t("transaction-log-type-subscription") },
    { value: "PaymentTransactionLog", label: t("transaction-log-type-payment") },
    { value: "TransferFundTransactionLog", label: t("transaction-log-type-transfer") },
    {
      value: "RefundBudgetAllowanceFromUnassignedCardTransactionLog",
      label: t("transaction-log-type-refund-budget-allowance-from-unassigned-card")
    },
    {
      value: "RefundBudgetAllowanceFromNoCardWhenAddingFundTransactionLog",
      label: t("transaction-log-type-refund-budget-allowance-from-no-card-when-adding-fund")
    },
    {
      value: "RefundBudgetAllowanceFromRemovedBeneficiaryFromSubscriptionTransactionLog",
      label: t("transaction-log-type-refund-budget-allowance-from-removed-beneficiary-from-subscription")
    },
    { value: "RefundPaymentTransactionLog", label: t("transaction-log-type-refund-payment") }
  ];
});

const availableGiftCardTransactionTypes = computed(() => {
  return [
    { value: props.withGiftCardId, label: t("with-gift-card") },
    { value: props.withoutGiftCardId, label: t("without-gift-card") }
  ];
});

function onOrganizationsChecked(input) {
  if (input.isChecked) {
    emit("organizationsChecked", input.value);
  } else {
    emit("organizationsUnchecked", input.value);
  }
}

function onSubscriptionsChecked(input) {
  if (input.isChecked) {
    emit("subscriptionsChecked", input.value);
  } else {
    emit("subscriptionsUnchecked", input.value);
  }
}

function onMarketsChecked(input) {
  if (input.isChecked) {
    emit("marketsChecked", input.value);
  } else {
    emit("marketsUnchecked", input.value);
  }
}

function onCashRegistersChecked(input) {
  if (input.isChecked) {
    emit("cashRegistersChecked", input.value);
  } else {
    emit("cashRegistersUnchecked", input.value);
  }
}

function onBeneficiaryTypesChecked(input) {
  if (input.isChecked) {
    emit("beneficiaryTypesChecked", input.value);
  } else {
    emit("beneficiaryTypesUnchecked", input.value);
  }
}

function onTransactionTypesChecked(input) {
  if (input.isChecked) {
    emit("transactionTypesChecked", input.value);
  } else {
    emit("transactionTypesUnchecked", input.value);
  }
}

function onGiftCardTransactionTypesChecked(input) {
  if (input.isChecked) {
    emit("giftCardTransactionTypesChecked", input.value);
  } else {
    emit("giftCardTransactionTypesUnchecked", input.value);
  }
}

function onResetFilters() {
  emit("resetFilters");
}

function onSearch() {
  emit("search");
}
</script>
