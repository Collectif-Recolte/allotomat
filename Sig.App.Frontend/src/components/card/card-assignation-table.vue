<i18n>
  {
    "en": {
      "beneficiary-name": "Participant",
      "contact-info": "Contact information",
      "category": "Category",
      "subscriptions": "Subscriptions",
      "status": "Status",
      "options": "Options",
      "beneficiary-status-active": "Active",
      "beneficiary-status-inactive": "Inactive"
    },
    "fr": {
      "beneficiary-name": "Participant-e",
      "contact-info": "Coordonnées",
      "category": "Catégorie",
      "subscriptions": "Abonnements",
      "status": "Statut",
      "options": "Options",
      "beneficiary-status-active": "Actif",
      "beneficiary-status-inactive": "Inactif"
    }
  }
</i18n>

<template>
  <UiTable v-if="props.beneficiaries" :items="props.beneficiaries" :cols="cols">
    <template #default="slotProps">
      <td v-if="!beneficiariesAreAnonymous">
        {{ getBeneficiaryName(slotProps.item) }}
      </td>
      <UiTableContactCell v-if="!beneficiariesAreAnonymous" :person="slotProps.item" />
      <td v-if="!props.administrationSubscriptionsOffPlatform">
        {{ getBeneficiaryCategory(slotProps.item) }}
      </td>
      <td v-if="!props.administrationSubscriptionsOffPlatform">
        <div class="inline-flex flex-col justify-start items-start gap-y-1">
          <PfTag
            v-for="item in getBeneficiarySubscriptions(slotProps.item)"
            :key="item.subscription.id"
            :label="subscriptionName(item.subscription)"
            is-dark-theme
            bg-color-class="bg-primary-700" />
        </div>
      </td>
      <td v-if="props.administrationSubscriptionsOffPlatform">
        <PfTag
          :label="getBeneficiaryStatus(slotProps.item)"
          :is-dark-theme="getIsBeneficiaryActive(slotProps.item)"
          :bg-color-class="getIsBeneficiaryActive(slotProps.item) ? 'bg-primary-700' : 'bg-primary-300'" />
      </td>
      <td>
        <slot v-if="!beneficiariesAreAnonymous" name="actions" :beneficiary="slotProps.item"></slot>
      </td>
    </template>
  </UiTable>
</template>

<script setup>
import { defineProps, computed } from "vue";
import { useI18n } from "vue-i18n";
import { subscriptionName } from "@/lib/helpers/subscription";

const { t } = useI18n();

const props = defineProps({
  selectedOrganization: {
    type: String,
    default: ""
  },
  beneficiaries: { type: Object, default: null },
  showAssociatedCard: Boolean,
  beneficiariesAreAnonymous: {
    type: Boolean,
    default: false
  },
  administrationSubscriptionsOffPlatform: {
    type: Boolean,
    default: false
  }
});

const cols = computed(() => {
  const cols = [];

  if (props.beneficiariesAreAnonymous) {
    if (!props.administrationSubscriptionsOffPlatform) {
      cols.push({ label: t("category") });
      cols.push({ label: t("subscriptions") });
    } else {
      cols.push({ label: t("status") });
    }
  } else {
    cols.push({ label: t("beneficiary-name") });
    cols.push({ label: t("contact-info") });
    if (!props.administrationSubscriptionsOffPlatform) {
      cols.push({ label: t("category") });
      cols.push({ label: t("subscriptions") });
    } else {
      cols.push({ label: t("status") });
    }
    cols.push({
      label: t("options"),
      hasHiddenLabel: true
    });
  }

  return cols;
});

function getBeneficiaryName(beneficiary) {
  return `${beneficiary.firstname} ${beneficiary.lastname}`;
}

function getBeneficiaryCategory(beneficiary) {
  return beneficiary.beneficiaryType ? `${beneficiary.beneficiaryType.name}` : "";
}

function getBeneficiaryStatus(beneficiary) {
  return beneficiary.isActive ? t("beneficiary-status-active") : t("beneficiary-status-inactive");
}

function getIsBeneficiaryActive(beneficiary) {
  return beneficiary.isActive;
}

function getBeneficiarySubscriptions(beneficiary) {
  return beneficiary.beneficiarySubscriptions;
}
</script>
