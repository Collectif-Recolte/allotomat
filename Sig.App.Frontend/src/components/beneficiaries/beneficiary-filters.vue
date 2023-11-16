<i18n>
{
	"en": {
		"beneficiary-type": "Categories",
    "subscription": "Subscriptions",
    "cards": "Card",
    "without-subscription-option": "None",
    "status": "Status",
    "status-active": "Active",
    "status-inactive": "Inactive",
    "with-card": "With card",
    "without-card": "Without card",
	},
	"fr": {
		"beneficiary-type": "Catégories",
    "subscription": "Abonnements",
    "cards": "Carte",
    "without-subscription-option": "Aucun",
    "status": "État",
    "status-active": "Actif",
    "status-inactive": "Inactif",
    "with-card": "Avec carte",
    "without-card": "Sans carte",
	}
}
</i18n>

<template>
  <UiFilter
    :model-value="modelValue"
    has-search
    has-filters
    :has-active-filters="hasActiveFilters"
    :active-filters-count="activeFiltersCount"
    :beneficiaries-are-anonymous="props.beneficiariesAreAnonymous"
    @resetFilters="onResetFilters"
    @search="onSearch"
    @update:modelValue="(e) => emit('update:modelValue', e)">
    <PfFormInputCheckboxGroup
      v-if="availableBeneficiaryTypes.length > 0"
      id="beneficiary-types"
      is-filter
      :value="selectedBeneficiaryTypes"
      :label="t('beneficiary-type')"
      :options="availableBeneficiaryTypes"
      @input="onBeneficiaryTypesChecked" />
    <PfFormInputCheckboxGroup
      v-if="availableSubscriptions.length > 0"
      id="subscriptions"
      class="mt-3"
      is-filter
      :value="selectedSubscriptions"
      :label="t('subscription')"
      :options="availableSubscriptions"
      @input="onSubscriptionsChecked" />
    <PfFormInputCheckboxGroup
      v-if="availableStatus.length > 0"
      id="status"
      class="mt-3"
      is-filter
      :value="selectedStatus"
      :label="t('status')"
      :options="availableStatus"
      @input="onStatusChecked" />
    <PfFormInputCheckboxGroup
      id="cardStatus"
      class="mt-3"
      is-filter
      :value="selectedCardStatus"
      :label="t('cards')"
      :options="cardStatus"
      @input="onCardStatusChecked" />
  </UiFilter>
</template>

<script setup>
import { defineProps, defineEmits, computed, ref } from "vue";
import { useI18n } from "vue-i18n";

const { t } = useI18n();

const emit = defineEmits([
  "subscriptionsChecked",
  "subscriptionsUnchecked",
  "beneficiaryTypesChecked",
  "beneficiaryTypesUnchecked",
  "resetFilters",
  "search",
  "update:modelValue",
  "statusChecked",
  "statusUnchecked",
  "cardStatusChecked",
  "cardStatusUnchecked"
]);

const props = defineProps({
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
  selectedStatus: {
    type: Array,
    default() {
      return [];
    }
  },
  selectedCardStatus: {
    type: Array,
    default() {
      return [];
    }
  },
  withoutSubscriptionId: {
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
  administrationSubscriptionsOffPlatform: {
    type: Boolean,
    default: false
  },
  beneficiariesAreAnonymous: {
    type: Boolean,
    default: false
  },
  cardStatusWith: {
    type: String,
    default: ""
  },
  cardStatusWithout: {
    type: String,
    default: ""
  }
});

const hasActiveFilters = computed(() => {
  return (
    props.selectedBeneficiaryTypes?.length > 0 ||
    props.selectedSubscriptions?.length > 0 ||
    !!props.searchFilter ||
    props.selectedCardStatus?.length > 0
  );
});

const activeFiltersCount = computed(() => {
  const selectedBeneficiariesCount = props.selectedBeneficiaryTypes?.length ?? 0;
  const selectedSubscritionsCount = props.selectedSubscriptions?.length ?? 0;
  const selectedCardStatusCount = props.selectedCardStatus?.length ?? 0;
  return selectedBeneficiariesCount + selectedSubscritionsCount + selectedCardStatusCount;
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
    subscriptions = props.availableSubscriptions.map((x) => ({ value: x.id, label: x.name }));
  }

  if (props.withoutSubscriptionId !== "") {
    subscriptions.push({ value: props.withoutSubscriptionId, label: t("without-subscription-option") });
  }

  return subscriptions;
});

const availableStatus = computed(() => {
  if (props.administrationSubscriptionsOffPlatform) {
    return [
      { value: props.beneficiaryStatusActive, label: t("status-active") },
      { value: props.beneficiaryStatusInactive, label: t("status-inactive") }
    ];
  }
  return [];
});

const cardStatus = ref([
  { value: props.cardStatusWith, label: t("with-card") },
  { value: props.cardStatusWithout, label: t("without-card") }
]);

function onSubscriptionsChecked(input) {
  if (input.isChecked) {
    emit("subscriptionsChecked", input.value);
  } else {
    emit("subscriptionsUnchecked", input.value);
  }
}

function onBeneficiaryTypesChecked(input) {
  if (input.isChecked) {
    emit("beneficiaryTypesChecked", input.value);
  } else {
    emit("beneficiaryTypesUnchecked", input.value);
  }
}

function onStatusChecked(input) {
  if (input.isChecked) {
    emit("statusChecked", input.value);
  } else {
    emit("statusUnchecked", input.value);
  }
}

function onCardStatusChecked(input) {
  if (input.isChecked) {
    emit("cardStatusChecked", input.value);
  } else {
    emit("cardStatusUnchecked", input.value);
  }
}

function onResetFilters() {
  emit("resetFilters");
}

function onSearch() {
  emit("search");
}
</script>
