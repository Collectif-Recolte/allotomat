<i18n>
  {
    "en": {
      "beneficiary-name": "Name",
      "beneficiary-category": "Category",
      "beneficiary-id1": "ID 1",
      "beneficiary-id2": "ID 2",
      "beneficiary-payment": "Payment",
      "beneficiary-order-random": "Random order",
    },
    "fr": {
      "beneficiary-name": "Nom",
      "beneficiary-category": "Catégorie",
      "beneficiary-id1": "ID 1",
      "beneficiary-id2": "ID 2",
      "beneficiary-payment": "Versement",
      "beneficiary-order-random": "Ordre aléatoire",
    }
  }
</i18n>

<template>
  <UiTable v-if="props.beneficiaries" class="mb-8" :items="props.beneficiaries" :cols="cols">
    <template #default="slotProps">
      <td>
        <PfFormInputCheckbox
          id="selected"
          has-hidden-label
          :label="t('beneficiary-order-random')"
          :checked="isChecked(slotProps.item)"
          @input="(e) => onSelectedBeneficiaryChecked(slotProps.item, e)" />
      </td>
      <td>
        {{ getBeneficiaryId1(slotProps.item) }}
      </td>
      <td class="min-w-20">
        {{ getBeneficiaryId2(slotProps.item) }}
      </td>
      <td v-if="!beneficiariesAreAnonymous">
        <div class="inline-flex items-center">
          <PfIcon class="mr-2" :class="{ 'opacity-50': !haveCard(slotProps.item) }" :icon="ICON_CREDIT_CARD" />
          {{ getBeneficiaryName(slotProps.item) }}
        </div>
      </td>
      <td>
        {{ getBeneficiaryCategory(slotProps.item) }}
      </td>
      <td>
        {{ getBeneficiaryPayment(slotProps.item) }}
      </td>
    </template>
  </UiTable>
</template>

<script setup>
import { defineProps, defineEmits, computed } from "vue";
import { useI18n } from "vue-i18n";

import ICON_CREDIT_CARD from "@/lib/icons/credit-card.json";

const { t } = useI18n();

const props = defineProps({
  beneficiaries: { type: Object, default: null },
  showAssociatedCard: Boolean,
  beneficiariesAreAnonymous: {
    type: Boolean,
    default: false
  },
  subscriptions: {
    type: Array,
    default() {
      return [];
    }
  },
  selectedSubscription: {
    type: String,
    default: ""
  }
});

const emit = defineEmits(["beneficiarySelectedChecked", "beneficiarySelectedUnchecked"]);

const cols = computed(() => {
  const cols = [];

  cols.push({ label: "" });
  cols.push({ label: t("beneficiary-id1") });
  cols.push({ label: t("beneficiary-id2") });

  if (!props.beneficiariesAreAnonymous) {
    cols.push({ label: t("beneficiary-name") });
  }

  cols.push({ label: t("beneficiary-category") });
  cols.push({ label: t("beneficiary-payment"), isRight: true });

  return cols;
});

function isChecked(beneficiary) {
  return beneficiary.isSelected;
}

function getBeneficiaryName(beneficiary) {
  return `${beneficiary.firstname} ${beneficiary.lastname}`;
}

function getBeneficiaryCategory(beneficiary) {
  return beneficiary.beneficiaryType ? beneficiary.beneficiaryType.name : "";
}

function getBeneficiaryId1(beneficiary) {
  return beneficiary.id1 ? beneficiary.id1 : "";
}

function getBeneficiaryId2(beneficiary) {
  return beneficiary.id2 ? beneficiary.id2 : "";
}

function getBeneficiaryPayment(beneficiary) {
  if (props.selectedSubscription === "" || props.selectedSubscription === null) return "-$";

  return `${props.subscriptions
    .find((subscription) => subscription.value === props.selectedSubscription)
    .types.filter((type) => type.beneficiaryType.id === beneficiary.beneficiaryType.id)
    .reduce((accumulator, type) => accumulator + type.amount, 0)}$`;
}

function haveCard(beneficiary) {
  return beneficiary.card !== null;
}

function onSelectedBeneficiaryChecked(beneficiary, input) {
  if (input) {
    emit("beneficiarySelectedChecked", beneficiary);
  } else {
    emit("beneficiarySelectedUnchecked", beneficiary);
  }
}
</script>
