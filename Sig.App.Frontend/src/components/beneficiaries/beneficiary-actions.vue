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
      "beneficiary-edit-disabled": "You don't have the permission to edit this beneficiary",
      "beneficiary-add-funds-disabled": "You can't add funds if the beneficiary doesn't have a card",
      "beneficiary-display-qrcode-disabled": "You can't display a QR code if the beneficiary doesn't have a card",
      "beneficiary-lost-card-disabled": "You can't declare a card lost if the beneficiary doesn't have a card",
      "beneficiary-delete-disabled": "You can't delete a beneficiary with a card assigned",
      "beneficiary-delete-disabled-anonymous": "You can't delete an anonymous beneficiary",
      "beneficiary-add-funds-disabled-anonymous": "You can't add funds if the beneficiary is anonymous",
      "beneficiary-add-funds-disabled-no-subscription": "You can't add funds if the beneficiary doesn't have a subscription"
    },
    "fr": {
      "beneficiary-edit": "Modifier les détails",
      "beneficiary-add-funds": "Ajouter des fonds",
      "beneficiary-display-qrcode": "Afficher le code QR",
      "beneficiary-lost-card": "Carte perdue",
      "beneficiary-assign-card": "Assigner une carte",
      "beneficiary-unassign-card": "Désassigner la carte",
      "beneficiary-delete": "Supprimer {firstname}",
      "beneficiary-edit-disabled": "Vous n'avez pas la permission de modifier ce participant-e-",
      "beneficiary-add-funds-disabled": "Vous ne pouvez pas ajouter des fonds si le participant-e n'a pas de carte",
      "beneficiary-display-qrcode-disabled": "Vous ne pouvez pas afficher un code QR si le participant-e n'a pas de carte",
      "beneficiary-lost-card-disabled": "Vous ne pouvez pas déclarer une carte perdue si le participant-e n'a pas de carte",
      "beneficiary-delete-disabled": "Vous ne pouvez pas supprimer un participant-e avec une carte assignée",
      "beneficiary-delete-disabled-anonymous": "Vous ne pouvez pas supprimer un participant-e anonyme",
      "beneficiary-add-funds-disabled-anonymous": "Vous ne pouvez pas ajouter des fonds si le participant-e est anonyme",
      "beneficiary-add-funds-disabled-no-subscription": "Vous ne pouvez pas ajouter des fonds si le participant-e n'a pas d'abonnement"
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

import {
  URL_BENEFICIARY_EDIT,
  URL_BENEFICIARY_MANUALLY_ADD_FUND,
  URL_BENEFICIARY_QRCODE_PREVIEW,
  URL_BENEFICIARY_CARD_LOST,
  URL_BENEFICIARY_CARD_UNASSIGN,
  URL_BENEFICIARY_DELETE,
  URL_BENEFICIARY_CARD_ASSIGN
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
        icon: ICON_TRASH,
        label: t("beneficiary-delete", { firstname: props.beneficiary.firstname }),
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
        icon: ICON_ADD_CASH,
        label: t("beneficiary-add-funds"),
        route: { name: URL_BENEFICIARY_MANUALLY_ADD_FUND, params: { beneficiaryId: props.beneficiary.id } },
        disabled: !haveCard() || props.beneficiariesAreAnonymous || !haveSubscriptions(),
        reason: !haveCard()
          ? t("beneficiary-add-funds-disabled")
          : !haveSubscriptions()
          ? t("beneficiary-add-funds-disabled-no-subscription")
          : t("beneficiary-add-funds-disabled-anonymous")
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
  }
});

function haveCard() {
  return props.beneficiary.card !== null;
}

function haveSubscriptions() {
  return props.beneficiary.subscriptions.length > 0;
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
</script>

<style scoped>
.disable_link {
  pointer-events: none;
  opacity: 0.5;
}
</style>
