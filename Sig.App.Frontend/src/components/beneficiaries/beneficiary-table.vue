<i18n>
  {
    "en": {
      "beneficiary-name": "Name",
      "beneficiary-contact-information": "Contact information",
      "beneficiary-notes": "Notes",
      "beneficiary-category": "Category",
      "beneficiary-id1": "Unique Identifier 1",
      "beneficiary-id2": "Unique Identifier 2",
      "beneficiary-subscription": "Subscription",
      "beneficiary-none-subscription": "None",
      "delete-beneficiary": "Delete",
      "edit-beneficiary": "Edit",
      "add-manually-money": "Manually add funds"
    },
    "fr": {
      "beneficiary-name": "Nom",
      "beneficiary-contact-information": "Coordonnées",
      "beneficiary-notes": "Notes",
      "beneficiary-category": "Catégorie",
      "beneficiary-id1": "Identifiant unique 1",
      "beneficiary-id2": "Identifiant unique 2",
      "beneficiary-subscription": "Abonnement",
      "beneficiary-none-subscription": "Aucun",
      "delete-beneficiary": "Supprimer",
      "edit-beneficiary": "Modifier",
      "add-manually-money": "Ajouter manuellement des fonds"
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
        <template v-if="haveAnySubscriptions(slotProps.item)">
          <div class="inline-flex flex-col justify-start items-start gap-y-1">
            <PfTag
              v-for="item in getBeneficiarySubscriptions(slotProps.item)"
              :key="item.id"
              :label="item.name"
              can-dismiss
              is-dark-theme
              bg-color-class="bg-primary-700"
              @dismiss="removeSubscription(slotProps.item, item)" />
          </div>
        </template>
        <PfTag v-else :label="t('beneficiary-none-subscription')" bg-color-class="bg-primary-300" />
      </td>
      <td>
        {{ getBeneficiaryCategory(slotProps.item) }}
      </td>
      <UiTableContactCell v-if="!beneficiariesAreAnonymous" :person="slotProps.item" />
      <td v-if="!beneficiariesAreAnonymous" class="text-p4 py-2">
        {{ getBeneficiaryNotes(slotProps.item) }}
      </td>
      <td>
        <UiButtonGroup v-if="!beneficiariesAreAnonymous" :items="getBtnGroup(slotProps.item)" tooltip-position="left" />
      </td>
    </template>
    <template #floatingActions>
      <slot name="floatingActions"></slot>
    </template>
  </UiTable>
</template>

<script setup>
import { defineProps, computed } from "vue";
import { useI18n } from "vue-i18n";

import { useRouter } from "vue-router";

import ICON_TRASH from "@/lib/icons/trash.json";
import ICON_PENCIL from "@/lib/icons/pencil.json";
import ICON_ADD_CASH from "@/lib/icons/add-cash.json";
import ICON_CREDIT_CARD from "@/lib/icons/credit-card.json";
import {
  URL_BENEFICIARY_EDIT,
  URL_BENEFICIARY_DELETE,
  URL_BENEFICIARY_REMOVE_SUBSCRIPTION,
  URL_BENEFICIARY_MANUALLY_ADD_FUND
} from "@/lib/consts/urls";

import { GLOBAL_MANAGE_BENEFICIARIES } from "@/lib/consts/permissions";

import { useAuthStore } from "@/lib/store/auth";

const { t } = useI18n();
const router = useRouter();
const auth = useAuthStore();

const props = defineProps({
  beneficiaries: { type: Object, default: null },
  showAssociatedCard: Boolean,
  beneficiariesAreAnonymous: {
    type: Boolean,
    default: false
  }
});

const cols = computed(() => {
  const cols = [];

  if (props.beneficiariesAreAnonymous) {
    cols.push({ label: t("beneficiary-id1") });
    cols.push({ label: t("beneficiary-id2") });
    cols.push({ label: t("beneficiary-subscription") });
    cols.push({ label: t("beneficiary-category") });
  } else {
    cols.push({ label: t("beneficiary-id1") });
    cols.push({ label: t("beneficiary-id2") });
    cols.push({ label: t("beneficiary-name") });
    cols.push({ label: t("beneficiary-subscription") });
    cols.push({ label: t("beneficiary-category") });
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
    },
    {
      icon: ICON_PENCIL,
      label: t("edit-beneficiary"),
      route: {
        name: URL_BENEFICIARY_EDIT,
        params: { beneficiaryId: beneficiary.id }
      },
      if: canEditBeneficiary()
    },
    {
      icon: ICON_TRASH,
      label: t("delete-beneficiary"),
      route: {
        name: URL_BENEFICIARY_DELETE,
        params: { beneficiaryId: beneficiary.id }
      },
      if: canDelete(beneficiary)
    }
  ];
};

function getBeneficiaryName(beneficiary) {
  return `${beneficiary.firstname} ${beneficiary.lastname}`;
}

function getBeneficiaryNotes(beneficiary) {
  return beneficiary.notes ? beneficiary.notes : "";
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

function getBeneficiarySubscriptions(beneficiary) {
  return beneficiary.subscriptions;
}

function haveAnySubscriptions(beneficiary) {
  return beneficiary.subscriptions.length > 0;
}

function removeSubscription(beneficiary, subscription) {
  router.push({
    name: URL_BENEFICIARY_REMOVE_SUBSCRIPTION,
    params: { beneficiaryId: beneficiary.id, subscriptionId: subscription.id }
  });
}

function canDelete(beneficiary) {
  return !haveAnySubscriptions(beneficiary) && !haveCard(beneficiary);
}

function haveCard(beneficiary) {
  return beneficiary.card !== null;
}

function canEditBeneficiary() {
  return auth.getGlobalPermissions.includes(GLOBAL_MANAGE_BENEFICIARIES);
}
</script>
