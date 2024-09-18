<i18n>
{
	"en": {
		"user-type": "User type",
    "search-placeholder": "Search by email or name",
    "user-type-admin": "Administrator",
    "user-type-projectmanager": "Program manager",
    "user-type-organizationmanager": "Group manager",
    "user-type-merchant": "Merchant",
    "user-type-marketmanager": "Market group manager"
	},
	"fr": {
		"user-type": "Type d'utilisateur",
    "search-placeholder": "Chercher par courriel ou nom",
    "user-type-admin": "Administrateur",
    "user-type-projectmanager": "Gestionnaire de projet",
    "user-type-organizationmanager": "Gestionnaire de groupe",
    "user-type-merchant": "Marchand",
    "user-type-marketmanager": "Gestionnaire de groupe de commerce"
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
    :placeholder="t('search-placeholder')"
    @resetFilters="onResetFilters"
    @search="onSearch"
    @update:modelValue="(e) => emit('update:modelValue', e)">
    <PfFormInputCheckboxGroup
      v-if="availableUserTypes.length > 0"
      id="user-types"
      is-filter
      :value="selectedUserTypes"
      :label="t('user-type')"
      :options="availableUserTypes"
      @input="onUserTypesChecked" />
  </UiFilter>
</template>

<script setup>
import { defineProps, defineEmits, computed, ref } from "vue";
import { useI18n } from "vue-i18n";

import {
  USER_TYPE_MERCHANT,
  USER_TYPE_ORGANIZATIONMANAGER,
  USER_TYPE_PROJECTMANAGER,
  USER_TYPE_PCAADMIN,
  USER_TYPE_MARKETGROUPMANAGER
} from "@/lib/consts/enums";

const { t } = useI18n();

const emit = defineEmits(["userTypesChecked", "userTypesUnchecked", "resetFilters", "search", "update:modelValue"]);

const props = defineProps({
  selectedUserTypes: {
    type: Array,
    default() {
      return [];
    }
  },
  searchFilter: {
    type: String,
    default: ""
  }
});

const hasActiveFilters = computed(() => {
  return props.selectedUserTypes?.length > 0 || props.searchFilter !== "";
});

const activeFiltersCount = computed(() => {
  const selectedUserTypesCount = props.selectedUserTypes?.length ?? 0;
  return selectedUserTypesCount;
});

const availableUserTypes = ref([
  { value: USER_TYPE_PCAADMIN, label: t("user-type-admin") },
  { value: USER_TYPE_PROJECTMANAGER, label: t("user-type-projectmanager") },
  { value: USER_TYPE_ORGANIZATIONMANAGER, label: t("user-type-organizationmanager") },
  { value: USER_TYPE_MERCHANT, label: t("user-type-merchant") },
  { value: USER_TYPE_MARKETGROUPMANAGER, label: t("user-type-marketmanager") }
]);

function onUserTypesChecked(input) {
  if (input.isChecked) {
    emit("userTypesChecked", input.value);
  } else {
    emit("userTypesUnchecked", input.value);
  }
}

function onResetFilters() {
  emit("resetFilters");
}

function onSearch() {
  emit("search");
}
</script>
