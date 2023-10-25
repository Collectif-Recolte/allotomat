<i18n>
  {
    "en": {
      "beneficiary-name": "Name",
      "beneficiary-contact-information": "Contact information",
      "beneficiary-notes": "Notes",
      "beneficiary-id1": "Unique Identifier 1",
      "beneficiary-id2": "Unique Identifier 2",
      "add-manually-money": "Manually add funds",
      "fifteenth-day-of-the-month": "The 15th day of the month",
      "first-day-of-the-month": "The first day of the month",
      "first-and-fifteenth-day-of-the-month": "The first and 15th day of the month",
      "first-day-of-the-week": "Weekly",
      "beneficiary-status-active": "Active",
      "beneficiary-status-inactive": "Inactive",
      "beneficiary-start-date": "Start date",
      "beneficiary-end-date": "End date",
      "beneficiary-monthly-payment-moment": "Payment (frequency)",
      "beneficiary-status": "Status",
      "beneficiary-payment-fund": "Payment ({productGroup})"
    },
    "fr": {
      "beneficiary-name": "Nom",
      "beneficiary-contact-information": "Coordonnées",
      "beneficiary-notes": "Notes",
      "beneficiary-id1": "Identifiant unique 1",
      "beneficiary-id2": "Identifiant unique 2",
      "add-manually-money": "Ajouter manuellement des fonds",
      "fifteenth-day-of-the-month": "Le 15e jour du mois",
      "first-day-of-the-month": "Le premier jour du mois",
      "first-and-fifteenth-day-of-the-month": "Le premier et le 15e jour du mois",
      "first-day-of-the-week": "Hebdomadaire",
      "beneficiary-status-active": "Actif",
      "beneficiary-status-inactive": "Inactif",
      "beneficiary-start-date": "Date de début",
      "beneficiary-end-date": "Date de fin",
      "beneficiary-monthly-payment-moment": "Versement (fréquence)",
      "beneficiary-status": "Statut",
      "beneficiary-payment-fund": "Versement ({productGroup})"
    }
  }
</i18n>

<template>
  <UiTable v-if="props.beneficiaries" class="mb-8" :items="props.beneficiaries" :cols="cols">
    <template #default="slotProps">
      <td>
        {{ getBeneficiaryId1(slotProps.item) }}
      </td>
      <td>
        {{ getBeneficiaryId2(slotProps.item) }}
      </td>
      <td v-if="!beneficiariesAreAnonymous">
        <div class="inline-flex items-center">
          <PfIcon class="mr-2" :class="{ 'opacity-50': !haveCard(slotProps.item) }" :icon="ICON_CREDIT_CARD" />
          {{ getBeneficiaryName(slotProps.item) }}
        </div>
      </td>
      <td>
        {{ getBeneficiaryStartDate(slotProps.item) }}
      </td>
      <td>
        {{ getBeneficiaryEndDate(slotProps.item) }}
      </td>
      <td>
        {{ getBeneficiaryMonthlyPaymentMoment(slotProps.item) }}
      </td>
      <td v-for="productGroup in productGroups" :key="productGroup.id">
        {{ getFundForProductGroup(slotProps.item, productGroup) }}
      </td>
      <td>
        <PfTag
          :label="getBeneficiaryStatus(slotProps.item)"
          :is-dark-theme="getIsBeneficiaryActive(slotProps.item)"
          :bg-color-class="getIsBeneficiaryActive(slotProps.item) ? 'bg-primary-700' : 'bg-primary-300'" />
      </td>
      <UiTableContactCell v-if="!beneficiariesAreAnonymous" :person="slotProps.item" />
      <td v-if="!beneficiariesAreAnonymous" class="text-p4 py-2">
        {{ getBeneficiaryNotes(slotProps.item) }}
      </td>
      <td>
        <UiButtonGroup v-if="!beneficiariesAreAnonymous" :items="getBtnGroup(slotProps.item)" tooltip-position="left" />
      </td>
    </template>
  </UiTable>
</template>

<script setup>
import { defineProps, computed } from "vue";
import { useI18n } from "vue-i18n";

import ICON_ADD_CASH from "@/lib/icons/add-cash.json";
import ICON_CREDIT_CARD from "@/lib/icons/credit-card.json";
import { URL_BENEFICIARY_MANUALLY_ADD_FUND } from "@/lib/consts/urls";
import {
  FIRST_DAY_OF_THE_MONTH,
  FIFTEENTH_DAY_OF_THE_MONTH,
  FIRST_AND_FIFTEENTH_DAY_OF_THE_MONTH,
  FIRST_DAY_OF_THE_WEEK
} from "@/lib/consts/monthly-payment-moment";

import { formatDate, textualFormat, dateUtc } from "@/lib/helpers/date";
import { getMoneyFormat } from "@/lib/helpers/money";

const { t } = useI18n();

const props = defineProps({
  beneficiaries: { type: Array, default: () => [] },
  productGroups: { type: Array, default: () => [] },
  showAssociatedCard: Boolean,
  beneficiariesAreAnonymous: {
    type: Boolean,
    default: false
  }
});

const cols = computed(() => {
  const cols = [];
  let i = 0;

  if (props.beneficiariesAreAnonymous) {
    cols.push({ label: t("beneficiary-id1") });
    cols.push({ label: t("beneficiary-id2") });
    cols.push({ label: t("beneficiary-start-date") });
    cols.push({ label: t("beneficiary-end-date") });
    cols.push({ label: t("beneficiary-monthly-payment-moment") });
    for (i = 0; i < props.productGroups.length; i++) {
      cols.push({ label: t("beneficiary-payment-fund", { productGroup: props.productGroups[i].name }) });
    }
    cols.push({ label: t("beneficiary-status") });
  } else {
    cols.push({ label: t("beneficiary-id1") });
    cols.push({ label: t("beneficiary-id2") });
    cols.push({ label: t("beneficiary-name") });
    cols.push({ label: t("beneficiary-start-date") });
    cols.push({ label: t("beneficiary-end-date") });
    cols.push({ label: t("beneficiary-monthly-payment-moment") });
    for (i = 0; i < props.productGroups.length; i++) {
      cols.push({ label: t("beneficiary-payment-fund", { productGroup: props.productGroups[i].name }) });
    }
    cols.push({ label: t("beneficiary-status") });
    cols.push({ label: t("beneficiary-contact-information") });
    cols.push({ label: t("beneficiary-notes") });
    cols.push({
      label: "",
      hasHiddenLabel: true
    });
  }
  return cols;
});

const getBtnGroup = (beneficiary) => {
  return [
    {
      icon: ICON_ADD_CASH,
      label: t("add-manually-money"),
      route: {
        name: URL_BENEFICIARY_MANUALLY_ADD_FUND,
        params: { beneficiaryId: beneficiary.id }
      },
      if: haveCard(beneficiary)
    }
  ];
};

function getBeneficiaryName(beneficiary) {
  return `${beneficiary.firstname} ${beneficiary.lastname}`;
}

function getBeneficiaryStartDate(beneficiary) {
  return formatDate(dateUtc(beneficiary.startDate), textualFormat);
}

function getBeneficiaryEndDate(beneficiary) {
  return formatDate(dateUtc(beneficiary.endDate), textualFormat);
}

function getBeneficiaryMonthlyPaymentMoment(beneficiary) {
  switch (beneficiary.monthlyPaymentMoment) {
    case FIRST_DAY_OF_THE_MONTH:
      return t("first-day-of-the-month");
    case FIFTEENTH_DAY_OF_THE_MONTH:
      return t("fifteenth-day-of-the-month");
    case FIRST_AND_FIFTEENTH_DAY_OF_THE_MONTH:
      return t("first-and-fifteenth-day-of-the-month");
    case FIRST_DAY_OF_THE_WEEK:
      return t("first-day-of-the-week");
  }
  return "";
}

function getFundForProductGroup(beneficiary, productGroup) {
  const fund = beneficiary.funds.find((x) => x.productGroup.id === productGroup.id);
  if (fund === undefined) {
    return "";
  }
  return getMoneyFormat(fund.amount);
}

function getBeneficiaryStatus(beneficiary) {
  return beneficiary.isActive ? t("beneficiary-status-active") : t("beneficiary-status-inactive");
}

function getIsBeneficiaryActive(beneficiary) {
  return beneficiary.isActive;
}

function getBeneficiaryNotes(beneficiary) {
  return beneficiary.notes ? beneficiary.notes : "";
}

function getBeneficiaryId1(beneficiary) {
  return beneficiary.id1 ? beneficiary.id1 : "";
}

function getBeneficiaryId2(beneficiary) {
  return beneficiary.id2 ? beneficiary.id2 : "";
}
function haveCard(beneficiary) {
  return beneficiary.card !== null;
}
</script>
