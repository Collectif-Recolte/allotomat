<i18n>
{
	"en": {
    "beneficiary": "Participant",
    "contact-info": "Contact information",
		"card-current-loyalty-balance": "Loyalty balance",
    "beneficiary-current-balance": "Balance",
    "card-status": "Status",
    "qr-code": "QR",
    "card": "Card",
    "options": "Options",
    "gift-card-label": "Gift card",
    "lost-card-label": "Lost card",
    "card-last-transaction-date": "Last use",
    "off-platform-beneficiary-active": "Active",
    "off-platform-beneficiary-inactive": "Inactive"
	},
	"fr": {
		"beneficiary": "Participant-e",
    "contact-info": "Coordonnées",
    "card-current-loyalty-balance": "Solde cadeau",
		"beneficiary-current-balance": "Solde",
    "card-status": "Statut",
    "qr-code": "QR",
    "card": "Carte",
    "options": "Options",
    "gift-card-label": "Carte-cadeau",
    "lost-card-label": "Carte perdue",
    "card-last-transaction-date": "Dernier usage",
    "off-platform-beneficiary-active": "Actif",
    "off-platform-beneficiary-inactive": "Inactif"
  }
}
</i18n>

<template>
  <UiTable ref="beneficiaryTable" :items="tableItems" :cols="cols">
    <template #default="slotProps">
      <td :class="CELL_CLASSES" :style="slotProps.item.rowPaddingBottom">
        <slot name="beforeActions" :beneficiary="slotProps.item"></slot>
      </td>
      <td :class="CELL_CLASSES" :style="slotProps.item.rowPaddingBottom">
        {{ getCardId(slotProps.item) }}
      </td>
      <td v-if="!beneficiariesAreAnonymous" :class="CELL_CLASSES" :style="slotProps.item.rowPaddingBottom">
        {{ getBeneficiaryName(slotProps.item) }}
      </td>
      <td class="text-right" :class="CELL_CLASSES" :style="slotProps.item.rowPaddingBottom">
        <UiGenericCardBalance :beneficiary="slotProps.item" show-total />
      </td>
      <td class="text-right" :class="CELL_CLASSES" :style="slotProps.item.rowPaddingBottom">
        {{ getCardLoyaltyFund(slotProps.item) }}
      </td>
      <td class="text-right" :class="CELL_CLASSES" :style="slotProps.item.rowPaddingBottom">
        {{ getCardLastUsage(slotProps.item) }}
      </td>
      <td :class="CELL_CLASSES" :style="slotProps.item.rowPaddingBottom">
        <div :class="'inline-flex flex-col justify-start items-start gap-y-1 '">
          <PfTag v-if="checkIfLost(slotProps.item)" :label="t('lost-card-label')" is-dark-theme bg-color-class="bg-grey-700" />
          <template v-else-if="administrationSubscriptionsOffPlatform">
            <PfTag
              v-if="isBeneficiaryActive(slotProps.item)"
              :label="t('off-platform-beneficiary-active')"
              is-dark-theme
              bg-color-class="bg-primary-700" />
            <PfTag v-else :label="t('off-platform-beneficiary-inactive')" bg-color-class="bg-primary-300" />
            <PfTag
              v-if="haveLoyaltyFund(slotProps.item)"
              :label="t('gift-card-label')"
              is-dark-theme
              bg-color-class="bg-grey-700" />
          </template>
          <template v-else>
            <PfTag
              v-for="item in getBeneficiarySubscriptions(slotProps.item)"
              :key="item.order"
              :label="item.name"
              is-dark-theme
              bg-color-class="bg-primary-700" />
            <PfTag
              v-if="haveLoyaltyFund(slotProps.item)"
              key="giftCard"
              :label="t('gift-card-label')"
              is-dark-theme
              bg-color-class="bg-primary-700" />
          </template>
        </div>
      </td>
      <UiTableContactCell
        v-if="!beneficiariesAreAnonymous"
        :class="CELL_CLASSES"
        :person="slotProps.item"
        :row-padding-bottom="slotProps.item.rowPaddingBottom" />
      <td :class="CELL_CLASSES" :style="slotProps.item.rowPaddingBottom">
        <slot v-if="!beneficiariesAreAnonymous" name="afterActions" :beneficiary="slotProps.item"></slot>
      </td>
    </template>
  </UiTable>
</template>

<script setup>
import { defineProps, computed, ref, onMounted, watch } from "vue";
import { useI18n } from "vue-i18n";

import { formatDate, dateUtc, regularFormat } from "@/lib/helpers/date";
import { getMoneyFormat } from "@/lib/helpers/money";

import { CARD_STATUS_LOST } from "@/lib/consts/enums";

const { t } = useI18n();

const CELL_CLASSES = "py-1 transition-padding ease-in-out duration-300";

const props = defineProps({
  selectedOrganization: {
    type: String,
    default: ""
  },
  beneficiaries: {
    type: Array,
    default: null
  },
  beneficiariesAreAnonymous: {
    type: Boolean,
    default: false
  },
  administrationSubscriptionsOffPlatform: {
    type: Boolean,
    default: false
  }
});

const tableItems = ref([]);

onMounted(() => {
  const items = [];

  for (let i = 0; i < props.beneficiaries.length; i++) {
    items.push({
      ...props.beneficiaries[i],
      dropdownIsOpen: false,
      dropdownMaxHeight: null,
      rowPaddingBottom: null
    });
  }
  tableItems.value = items;
});

watch(
  () => props.beneficiaries,
  (beneficiaries) => {
    const items = [];

    for (let i = 0; i < beneficiaries.length; i++) {
      items.push({
        ...beneficiaries[i],
        dropdownIsOpen: false,
        dropdownMaxHeight: null,
        rowPaddingBottom: null
      });
    }
    tableItems.value = items;
  }
);

const cols = computed(() => {
  const cols = [];

  if (props.beneficiariesAreAnonymous) {
    cols.push({ label: t("qr-code") });
    cols.push({ label: t("card") });
    cols.push({ label: t("beneficiary-current-balance"), isRight: true });
    cols.push({ label: t("card-current-loyalty-balance"), isRight: true });
    cols.push({ label: t("card-last-transaction-date"), isRight: true });
    cols.push({ label: t("card-status") });
  } else {
    cols.push({ label: t("qr-code") });
    cols.push({ label: t("card") });
    cols.push({ label: t("beneficiary") });
    cols.push({ label: t("beneficiary-current-balance"), isRight: true });
    cols.push({ label: t("card-current-loyalty-balance"), isRight: true });
    cols.push({ label: t("card-last-transaction-date"), isRight: true });
    cols.push({ label: t("card-status") });
    cols.push({ label: t("contact-info") });
    cols.push({
      label: t("options"),
      hasHiddenLabel: true
    });
  }

  return cols;
});

function getCardId(beneficiary) {
  return `${beneficiary.card.programCardId}`;
}

function getBeneficiaryName(beneficiary) {
  return `${beneficiary.firstname} ${beneficiary.lastname}`;
}

function haveLoyaltyFund(beneficiary) {
  return beneficiary.card ? beneficiary.card.loyaltyFund !== null : false;
}

function isBeneficiaryActive(beneficiary) {
  return beneficiary.isActive;
}

function getCardLoyaltyFund(beneficiary) {
  return beneficiary.card ? getMoneyFormat(haveLoyaltyFund(beneficiary) ? beneficiary.card.loyaltyFund.amount : 0) : "";
}

function getCardLastUsage(beneficiary) {
  return beneficiary.card && beneficiary.card.lastTransactionDate
    ? formatDate(dateUtc(beneficiary.card.lastTransactionDate), regularFormat)
    : "";
}

function getBeneficiarySubscriptions(beneficiary) {
  return beneficiary.subscriptions;
}

function checkIfLost(beneficiary) {
  return beneficiary.card.status === CARD_STATUS_LOST;
}
</script>
