<i18n>
{
	"en": {
    "card-status": "Status",
    "qr-code": "QR",
    "card": "ID",
    "gift-card-label": "Gift card",
    "lost-card-label": "Lost card",
    "card-beneficiary-organization": "Organization",
    "card-number":"N°",
    "card-assigned": "Assigned",
    "card-unassigned": "Unassigned",
    "card-deactivated": "Deactivated"
	},
	"fr": {
    "card-status": "Statut",
    "qr-code": "QR",
    "card": "ID",
    "gift-card-label": "Carte cadeau",
    "lost-card-label": "Carte perdue",
    "card-beneficiary-organization": "Organisme",
    "card-number":"N°",
    "card-assigned": "Assignée",
    "card-unassigned": "Non assignée",
    "card-deactivated": "Désactivée",
  }
}
</i18n>

<template>
  <UiTable ref="beneficiaryTable" :items="tableItems" :cols="cols">
    <template #default="slotProps">
      <td :class="CELL_CLASSES" :style="slotProps.item.rowPaddingBottom">
        <slot name="beforeActions" :card="slotProps.item"></slot>
      </td>
      <td :class="CELL_CLASSES" :style="slotProps.item.rowPaddingBottom">
        {{ getCardId(slotProps.item) }}
      </td>
      <td :class="CELL_CLASSES" :style="slotProps.item.rowPaddingBottom">
        {{ getCardNumber(slotProps.item) }}
      </td>
      <td :class="CELL_CLASSES" class="" :style="slotProps.item.rowPaddingBottom">
        <PfTag
          :label="getCardStatus(slotProps.item)"
          :is-dark-theme="isCardTagDarkTheme(slotProps.item)"
          :bg-color-class="getCardTagBgColor(slotProps.item)" />
        <PfButtonLink
          v-if="!beneficiariesAreAnonymous && haveBeneficiary(slotProps.item)"
          class="ml-2"
          btn-style="link"
          tag="routerLink"
          :label="getBeneficiaryName(slotProps.item)"
          :to="{
            name: URL_BENEFICIARY_ADMIN,
            query: { text: getBeneficiaryID1(slotProps.item), organizationId: getBeneficiaryOrganizationId(slotProps.item) }
          }" />
      </td>
      <td :class="CELL_CLASSES" :style="slotProps.item.rowPaddingBottom">
        {{ getBeneficiaryOrganization(slotProps.item) }}
      </td>
      <td :class="CELL_CLASSES" :style="slotProps.item.rowPaddingBottom">
        <slot v-if="!beneficiariesAreAnonymous" name="afterActions" :card="slotProps.item"></slot>
      </td>
    </template>
  </UiTable>
</template>

<script setup>
import { defineProps, computed, ref, onMounted, watch } from "vue";
import { useI18n } from "vue-i18n";

import { URL_BENEFICIARY_ADMIN } from "@/lib/consts/urls";
import {
  CARD_STATUS_ASSIGNED,
  CARD_STATUS_UNASSIGNED,
  CARD_STATUS_DEACTIVATED,
  CARD_STATUS_LOST,
  CARD_STATUS_GIFT
} from "@/lib/consts/enums";

const { t } = useI18n();

const CELL_CLASSES = "py-1 transition-padding ease-in-out duration-300";

const props = defineProps({
  cards: {
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

  for (let i = 0; i < props.cards.length; i++) {
    items.push({
      ...props.cards[i],
      dropdownIsOpen: false,
      dropdownMaxHeight: null,
      rowPaddingBottom: null
    });
  }
  tableItems.value = items;
});

watch(
  () => props.cards,
  (cards) => {
    const items = [];

    for (let i = 0; i < cards.length; i++) {
      items.push({
        ...cards[i],
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

  cols.push({ label: t("qr-code") });
  cols.push({ label: t("card") });
  cols.push({ label: t("card-number") });
  cols.push({ label: t("card-status") });
  cols.push({ label: t("card-beneficiary-organization") });
  cols.push({ label: "" });

  return cols;
});

function getCardId(card) {
  return `${card.programCardId}`;
}

function getCardNumber(card) {
  return `${card.cardNumber}`;
}

function getCardStatus(card) {
  if (card.status === CARD_STATUS_LOST) {
    return t("lost-card-label");
  }
  if (card.status === CARD_STATUS_GIFT) {
    return t("gift-card-label");
  }
  if (card.status === CARD_STATUS_ASSIGNED) {
    return t("card-assigned");
  }
  if (card.status === CARD_STATUS_UNASSIGNED) {
    return t("card-unassigned");
  }
  if (card.status === CARD_STATUS_DEACTIVATED) {
    return t("card-deactivated");
  }
  return "";
}

function isCardTagDarkTheme(card) {
  return (
    card.status === CARD_STATUS_ASSIGNED ||
    card.status === CARD_STATUS_GIFT ||
    card.status === CARD_STATUS_DEACTIVATED ||
    card.status === CARD_STATUS_LOST
  );
}

function getCardTagBgColor(card) {
  if (card.status === CARD_STATUS_ASSIGNED) return "bg-primary-700";
  if (card.status === CARD_STATUS_UNASSIGNED) return "bg-primary-300";
  if (card.status === CARD_STATUS_LOST || card.status === CARD_STATUS_DEACTIVATED) return "bg-red-500";
  if (card.status === CARD_STATUS_GIFT) return "bg-yellow-500";
  return "";
}

function getBeneficiaryName(card) {
  if (card.beneficiary !== null) {
    return `${card.beneficiary.firstname} ${card.beneficiary.lastname}`;
  }
  return "";
}

function getBeneficiaryID1(card) {
  if (card.beneficiary !== null) {
    return `${card.beneficiary.id1}`;
  }
  return "";
}

function getBeneficiaryOrganizationId(card) {
  if (card.beneficiary !== null) {
    return `${card.beneficiary.organization.id}`;
  }
  return "";
}

function haveBeneficiary(card) {
  return card.beneficiary !== null;
}

function getBeneficiaryOrganization(card) {
  if (card.beneficiary !== null) {
    return `${card.beneficiary.organization.name}`;
  }
  return "";
}
</script>
