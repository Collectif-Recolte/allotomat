<i18n>
  {
    "en": {
      "beneficiary-edit": "Edit details",
      "beneficiary-add-funds": "Add funds",
      "beneficiary-display-qrcode": "Show QR code",
      "beneficiary-lost-card": "Lost card",
      "beneficiary-assign-card": "Assign a card",
      "beneficiary-unassign-card": "Unassign card",
      "beneficiary-delete": "Delete {firstname}",
      "beneficiary-delete-participant-anonymous": "the participant",
      "beneficiary-transactions-history": "Transactions history",
      "beneficiary-edit-disabled": "You don't have the permission to edit this beneficiary",
      "beneficiary-add-funds-disabled": "You can't add funds if the beneficiary doesn't have a card",
      "beneficiary-display-qrcode-disabled": "You can't display a QR code if the beneficiary doesn't have a card",
      "beneficiary-lost-card-disabled": "You can't declare a card lost if the beneficiary doesn't have a card",
      "beneficiary-delete-disabled": "You can't delete a beneficiary with a card assigned",
      "beneficiary-delete-disabled-anonymous": "You can't delete an anonymous beneficiary",
      "beneficiary-add-funds-disabled-anonymous": "You can't add funds if the beneficiary is anonymous",
      "beneficiary-add-funds-disabled-no-subscription": "You can't add funds if the beneficiary doesn't have a subscription",
      "beneficiary-add-funds-disabled-all-group": "You can't add funds if all groups are selected",
      "beneficiary-disable-card": "Deactivate card",
      "beneficiary-enable-card": "Re-enable card",
      "beneficiary-payment-conflict": "Fix conflicts",
      "beneficiary-payment-conflict-disabled": "The beneficiary doesn't have a payment conflict",
      "beneficiary-payment-conflict-all-group-selected": "You can't fix a conflict if all groups are selected",
      "beneficiary-assign-subscription": "Assign subscription",
      "beneficiary-assign-subscription-disabled": "You can't assign a subscription if all groups are selected",
      "beneficiary-add-missed-payment": "Add missed payment",
      "beneficiary-add-missed-payment-no-card": "You can't add a missed payment if the beneficiary doesn't have a card",
      "beneficiary-add-missed-payment-no-subscription": "You can't add a missed payment if the beneficiary doesn't have a subscription",
      "beneficiary-add-missed-payment-no-missed-payment": "You can't add a missed payment if the beneficiary doesn't have a missed payment",
      "beneficiary-add-missed-payment-all-group-selected": "You can't add a missed payment if all groups are selected",
      "beneficiary-create-transaction": "Create transaction",
      "beneficiary-create-transaction-no-market": "You can't create a transaction if the group doesn't have a market",
      "beneficiary-create-transaction-no-card": "You can't create a transaction if the beneficiary doesn't have a card",
      "beneficiary-create-transaction-card-disabled": "You can't create a transaction if the beneficiary's card is disabled",
      "beneficiary-create-transaction-all-group": "You can't create a transaction if all groups are selected"
    },
    "fr": {
      "beneficiary-edit": "Modifier les détails",
      "beneficiary-add-funds": "Ajouter des fonds",
      "beneficiary-display-qrcode": "Afficher le code QR",
      "beneficiary-lost-card": "Carte perdue",
      "beneficiary-assign-card": "Assigner une carte",
      "beneficiary-unassign-card": "Désassigner la carte",
      "beneficiary-delete": "Supprimer {firstname}",
      "beneficiary-delete-participant-anonymous": "le participant",
      "beneficiary-transactions-history": "Historique de transactions",
      "beneficiary-edit-disabled": "Vous n'avez pas la permission de modifier ce participant-e",
      "beneficiary-add-funds-disabled": "Vous ne pouvez pas ajouter des fonds si le participant-e n'a pas de carte",
      "beneficiary-display-qrcode-disabled": "Vous ne pouvez pas afficher un code QR si le participant-e n'a pas de carte",
      "beneficiary-lost-card-disabled": "Vous ne pouvez pas déclarer une carte perdue si le participant-e n'a pas de carte",
      "beneficiary-delete-disabled": "Vous ne pouvez pas supprimer un participant-e avec une carte assignée",
      "beneficiary-delete-disabled-anonymous": "Vous ne pouvez pas supprimer un participant-e anonyme",
      "beneficiary-add-funds-disabled-anonymous": "Vous ne pouvez pas ajouter des fonds si le participant-e est anonyme",
      "beneficiary-add-funds-disabled-no-subscription": "Vous ne pouvez pas ajouter des fonds si le participant-e n'a pas d'abonnement",
      "beneficiary-add-funds-disabled-all-group": "Vous ne pouvez pas ajouter des fonds si tous les groupes sont sélectionnés",
      "beneficiary-disable-card": "Désactiver la carte",
      "beneficiary-enable-card": "Réactiver la carte",
      "beneficiary-payment-conflict": "Corriger les conflits",
      "beneficiary-payment-conflict-disabled": "Le participant-e n'a pas de conflit de paiement",
      "beneficiary-payment-conflict-all-group-selected": "Vous ne pouvez pas corriger un conflit si tous les groupes sont sélectionnés",
      "beneficiary-assign-subscription": "Attribuer un abonnement",
      "beneficiary-assign-subscription-disabled": "Vous ne pouvez pas attribuer un abonnement si tous les groupes sont sélectionnés",
      "beneficiary-add-missed-payment": "Versement d’un paiement manqué",
      "beneficiary-add-missed-payment-no-card": "Vous ne pouvez pas ajouter un paiement manqué si le participant-e n'a pas de carte",
      "beneficiary-add-missed-payment-no-subscription": "Vous ne pouvez pas ajouter un paiement manqué si le participant-e n'a pas d'abonnement",
      "beneficiary-add-missed-payment-no-missed-payment": "Vous ne pouvez pas ajouter un paiement manqué si le participant-e n'a pas de paiement manqué",
      "beneficiary-add-missed-payment-all-group-selected": "Vous ne pouvez pas ajouter un paiement manqué si tous les groupes sont sélectionnés",
      "beneficiary-create-transaction": "Créer une transaction",
      "beneficiary-create-transaction-no-market": "Vous ne pouvez pas créer une transaction si le groupe n'a pas de commerce",
      "beneficiary-create-transaction-no-card": "Vous ne pouvez pas créer une transaction si le participant-e n'a pas de carte",
      "beneficiary-create-transaction-card-disabled": "Vous ne pouvez pas créer une transaction si la carte du participant-e est désactivée",
      "beneficiary-create-transaction-all-group": "Vous ne pouvez pas créer une transaction si tous les groupes sont sélectionnés"
    }
  }
</i18n>

<template>
  <UiButtonGroup :items="items" />
</template>

<script setup>
import { defineProps, ref, onMounted, watch, computed } from "vue";
import { useI18n } from "vue-i18n";
import { storeToRefs } from "pinia";

import { canEditBeneficiary } from "@/lib/helpers/beneficiary";

import { useAuthStore } from "@/lib/store/auth";

import ICON_PENCIL from "@/lib/icons/pencil.json";
import ICON_ADD_CASH from "@/lib/icons/add-cash.json";
import ICON_QR_CODE from "@/lib/icons/qrcode.json";
import ICON_CARD_LOST from "@/lib/icons/card-lost.json";
import ICON_MINUS from "@/lib/icons/minus.json";
import ICON_TRASH from "@/lib/icons/trash.json";
import ICON_CARD_LINK from "@/lib/icons/card-link.json";
import ICON_CLOCK from "@/lib/icons/clock.json";
import ICON_CLOSE from "@/lib/icons/close.json";
import ICON_CONFLICT from "@/lib/icons/exclamation-circle.json";
import ICON_IDENTIFICATION from "@/lib/icons/identification.json";
import ICON_TRANSACTION from "@/lib/icons/add-square.json";
import ICON_MISSED_PAYMENT from "@/lib/icons/arrow-ricochet.json";

import {
  URL_BENEFICIARY_EDIT,
  URL_BENEFICIARY_MANUALLY_ADD_FUND,
  URL_BENEFICIARY_QRCODE_PREVIEW,
  URL_BENEFICIARY_CARD_LOST,
  URL_BENEFICIARY_CARD_UNASSIGN,
  URL_BENEFICIARY_DELETE,
  URL_BENEFICIARY_CARD_ASSIGN,
  URL_TRANSACTION_ADMIN,
  URL_BENEFICIARY_CARD_DISABLE,
  URL_BENEFICIARY_CARD_ENABLE,
  URL_BENEFICIARY_MANAGE_CONFLICT,
  URL_BENEFICIARY_ASSIGN_SUBSCRIPTIONS,
  URL_BENEFICIARY_ADD_MISSED_PAYMENT,
  URL_BENEFICIARY_TRANSACTION_ADD
} from "@/lib/consts/urls";

import { GLOBAL_MANAGE_CARDS } from "@/lib/consts/permissions";

const { t } = useI18n();
const { getGlobalPermissions } = storeToRefs(useAuthStore());

const items = ref([]);

onMounted(() => {
  updateItems();
});

watch(
  () => props.beneficiary,
  () => {
    updateItems();
  }
);

const manageCards = computed(() => {
  return getGlobalPermissions.value.includes(GLOBAL_MANAGE_CARDS);
});

function updateItems() {
  if (manageCards.value) {
    items.value = [
      {
        isExtra: true,
        icon: ICON_PENCIL,
        label: t("beneficiary-edit"),
        route: { name: URL_BENEFICIARY_EDIT, params: { beneficiaryId: props.beneficiary.id } },
        disabled: !canEditBeneficiary() || props.beneficiariesAreAnonymous,
        reason: t("beneficiary-edit-disabled")
      },
      {
        isExtra: true,
        icon: ICON_IDENTIFICATION,
        label: t("beneficiary-assign-subscription"),
        route: { name: URL_BENEFICIARY_ASSIGN_SUBSCRIPTIONS, query: { text: props.beneficiary.id1 } },
        disabled: props.isAllGroupSelected,
        reason: t("beneficiary-assign-subscription-disabled")
      },
      {
        isExtra: true,
        icon: ICON_CLOCK,
        label: t("beneficiary-transactions-history"),
        route: { name: URL_TRANSACTION_ADMIN, query: { text: props.beneficiary.id1, dateFrom: "2023-01-01" } } // Set datefrom to a farthest date to get all transactions
      },
      {
        isExtra: true,
        icon: ICON_TRANSACTION,
        label: t("beneficiary-create-transaction"),
        route: { name: URL_BENEFICIARY_TRANSACTION_ADD, params: { beneficiaryId: props.beneficiary.id } },
        disabled: !haveCard() || isCardDisabled() || props.isAllGroupSelected,
        reason: !haveCard()
          ? t("beneficiary-create-transaction-no-card")
          : isCardDisabled()
          ? t("beneficiary-create-transaction-card-disabled")
          : t("beneficiary-create-transaction-all-group")
      },
      {
        isExtra: true,
        icon: ICON_ADD_CASH,
        label: t("beneficiary-add-funds"),
        route: { name: URL_BENEFICIARY_MANUALLY_ADD_FUND, params: { beneficiaryId: props.beneficiary.id } },
        disabled: !haveCard() || props.beneficiariesAreAnonymous || !haveSubscriptions() || props.isAllGroupSelected,
        reason: !haveCard()
          ? t("beneficiary-add-funds-disabled")
          : !haveSubscriptions()
          ? t("beneficiary-add-funds-disabled-no-subscription")
          : props.isAllGroupSelected
          ? t("beneficiary-add-funds-disabled-all-group")
          : t("beneficiary-add-funds-disabled-anonymous")
      },
      {
        isExtra: true,
        icon: ICON_MISSED_PAYMENT,
        label: t("beneficiary-add-missed-payment"),
        route: { name: URL_BENEFICIARY_ADD_MISSED_PAYMENT, params: { beneficiaryId: props.beneficiary.id } },
        disabled: !haveCard() || !haveSubscriptions() || !haveMissedPayment() || props.isAllGroupSelected,
        reason: !haveCard()
          ? t("beneficiary-add-missed-payment-no-card")
          : !haveSubscriptions()
          ? t("beneficiary-add-missed-payment-no-subscription")
          : !haveMissedPayment()
          ? t("beneficiary-add-missed-payment-no-missed-payment")
          : t("beneficiary-add-missed-payment-all-group-selected")
      },
      {
        isExtra: true,
        icon: ICON_QR_CODE,
        label: t("beneficiary-display-qrcode"),
        route: qrCodeLink(),
        disabled: !haveCard(),
        reason: t("beneficiary-display-qrcode-disabled")
      },
      {
        isExtra: true,
        icon: ICON_CARD_LOST,
        label: t("beneficiary-lost-card"),
        route: lostCardLink(),
        disabled: !haveCard(),
        reason: t("beneficiary-lost-card-disabled")
      },
      {
        isExtra: true,
        icon: ICON_CARD_LINK,
        label: t("beneficiary-assign-card"),
        route: { name: URL_BENEFICIARY_CARD_ASSIGN, params: { beneficiaryId: props.beneficiary.id } },
        if: !haveCard()
      },
      {
        isExtra: true,
        icon: ICON_MINUS,
        label: t("beneficiary-unassign-card"),
        route: unassignCardLink(),
        if: haveCard()
      },
      {
        isExtra: true,
        icon: ICON_CLOSE,
        label: t("beneficiary-disable-card"),
        route: disableCardLink(),
        if: haveCard() && !isCardDisabled()
      },
      {
        isExtra: true,
        icon: ICON_CARD_LINK,
        label: t("beneficiary-enable-card"),
        route: enableCardLink(),
        if: haveCard() && isCardDisabled()
      },
      {
        isExtra: true,
        icon: ICON_CONFLICT,
        label: t("beneficiary-payment-conflict"),
        disabled: !props.haveSubscriptionConflict || props.isAllGroupSelected,
        route: { name: URL_BENEFICIARY_MANAGE_CONFLICT, params: { beneficiaryId: props.beneficiary.id } },
        reason: !props.haveSubscriptionConflict
          ? t("beneficiary-payment-conflict-disabled")
          : t("beneficiary-payment-conflict-all-group-selected")
      },
      {
        isExtra: true,
        icon: ICON_TRASH,
        label: t("beneficiary-delete", {
          firstname: props.beneficiariesAreAnonymous ? t("beneficiary-delete-participant-anonymous") : props.beneficiary.firstname
        }),
        route: { name: URL_BENEFICIARY_DELETE, params: { beneficiaryId: props.beneficiary.id } },
        disabled: haveCard() || props.beneficiariesAreAnonymous,
        reason: haveCard() ? t("beneficiary-delete-disabled") : t("beneficiary-delete-disabled-anonymous")
      }
    ];
  } else {
    items.value = [
      {
        isExtra: true,
        icon: ICON_PENCIL,
        label: t("beneficiary-edit"),
        route: { name: URL_BENEFICIARY_EDIT, params: { beneficiaryId: props.beneficiary.id } },
        disabled: !canEditBeneficiary() || props.beneficiariesAreAnonymous,
        reason: t("beneficiary-edit-disabled")
      },
      {
        isExtra: true,
        icon: ICON_CLOCK,
        label: t("beneficiary-transactions-history"),
        route: { name: URL_TRANSACTION_ADMIN, query: { text: props.beneficiary.id1, dateFrom: "2023-01-01" } } // Set datefrom to a farthest date to get all transactions
      },
      {
        isExtra: true,
        icon: ICON_ADD_CASH,
        label: t("beneficiary-add-funds"),
        route: { name: URL_BENEFICIARY_MANUALLY_ADD_FUND, params: { beneficiaryId: props.beneficiary.id } },
        disabled: !haveCard() || props.beneficiariesAreAnonymous || !haveSubscriptions(),
        reason: !haveCard()
          ? t("beneficiary-add-funds-disabled")
          : !haveSubscriptions()
          ? t("beneficiary-add-funds-disabled-no-subscription")
          : t("beneficiary-add-funds-disabled-anonymous")
      },
      {
        isExtra: true,
        icon: ICON_MISSED_PAYMENT,
        label: t("beneficiary-add-missed-payment"),
        route: { name: URL_BENEFICIARY_ADD_MISSED_PAYMENT, params: { beneficiaryId: props.beneficiary.id } },
        disabled: !haveCard() || !haveSubscriptions() || !haveMissedPayment(),
        reason: !haveCard()
          ? t("beneficiary-add-missed-payment-no-card")
          : !haveSubscriptions()
          ? t("beneficiary-add-missed-payment-no-subscription")
          : t("beneficiary-add-missed-payment-no-missed-payment")
      },
      {
        isExtra: true,
        icon: ICON_TRANSACTION,
        label: t("beneficiary-create-transaction"),
        route: { name: URL_BENEFICIARY_TRANSACTION_ADD, params: { beneficiaryId: props.beneficiary.id } },
        disabled: !haveCard() || !haveMarketsInOrganization() || isCardDisabled(),
        reason: !haveCard()
          ? t("beneficiary-create-transaction-no-card")
          : !haveMarketsInOrganization()
          ? t("beneficiary-create-transaction-no-market")
          : t("beneficiary-create-transaction-card-disabled")
      }
    ];
  }
}

const props = defineProps({
  beneficiary: {
    type: Object,
    required: true
  },
  beneficiariesAreAnonymous: {
    type: Boolean,
    default: false
  },
  haveSubscriptionConflict: {
    type: Boolean,
    default: false
  },
  organization: {
    type: Object,
    required: true
  },
  isAllGroupSelected: {
    type: Boolean,
    default: false
  }
});

function haveCard() {
  return props.beneficiary.card !== null;
}

function isCardDisabled() {
  return props.beneficiary.card !== null && props.beneficiary.card.isDisabled;
}

function haveSubscriptions() {
  return props.beneficiary.beneficiarySubscriptions.length > 0;
}

function haveMissedPayment() {
  return props.beneficiary.beneficiarySubscriptions.some((x) => x.hasMissedPayment);
}

function haveMarketsInOrganization() {
  return props.organization.markets.length > 0;
}

function qrCodeLink() {
  if (haveCard()) {
    return {
      name: URL_BENEFICIARY_QRCODE_PREVIEW,
      params: {
        cardId: props.beneficiary.card.id
      }
    };
  }
  return {
    name: URL_BENEFICIARY_QRCODE_PREVIEW,
    params: {
      cardId: 0
    }
  };
}

function lostCardLink() {
  if (haveCard()) {
    return {
      name: URL_BENEFICIARY_CARD_LOST,
      params: {
        beneficiaryId: props.beneficiary.id,
        cardId: props.beneficiary.card.id
      }
    };
  }
  return {
    name: URL_BENEFICIARY_CARD_LOST,
    params: {
      beneficiaryId: props.beneficiary.id,
      cardId: 0
    }
  };
}

function unassignCardLink() {
  if (haveCard()) {
    return {
      name: URL_BENEFICIARY_CARD_UNASSIGN,
      params: {
        beneficiaryId: props.beneficiary.id,
        cardId: props.beneficiary.card.id
      }
    };
  }
  return {
    name: URL_BENEFICIARY_CARD_UNASSIGN,
    params: {
      beneficiaryId: props.beneficiary.id,
      cardId: 0
    }
  };
}

function disableCardLink() {
  if (haveCard()) {
    return {
      name: URL_BENEFICIARY_CARD_DISABLE,
      params: { cardId: props.beneficiary.card.id, beneficiaryId: props.beneficiary.id }
    };
  } else {
    return null;
  }
}

function enableCardLink() {
  if (haveCard()) {
    return {
      name: URL_BENEFICIARY_CARD_ENABLE,
      params: { cardId: props.beneficiary.card.id, beneficiaryId: props.beneficiary.id }
    };
  } else {
    return null;
  }
}
</script>

<style scoped>
.disable_link {
  pointer-events: none;
  opacity: 0.5;
}
</style>
