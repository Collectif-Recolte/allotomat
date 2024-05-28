<i18n>
  {
    "en": {
      "beneficiary-id1": "ID 1",
      "beneficiary-id2": "ID 2",
      "beneficiary-category": "CAT",
      "beneficiary-balance": "Balance",
      "beneficiary-without-subscription": "No subscription",
      "beneficiary-no-card": "No card",
      "beneficiary-card-last-usage": "Last use"
    },
    "fr": {
      "beneficiary-id1": "ID 1",
      "beneficiary-id2": "ID 2",
      "beneficiary-category": "CAT",
      "beneficiary-balance": "Solde",
      "beneficiary-without-subscription": "Pas d'abonnement",
      "beneficiary-no-card": "Pas de carte",
      "beneficiary-card-last-usage": "Dernier usage"
    }
  }
</i18n>

<template>
  <div v-if="beneficiary" class="relative border border-primary-300 rounded-lg px-5 pt-3 pb-6 mb-4 last:mb-0">
    <h3 class="text-h4 font-semibold text-primary-900 mt-0 mb-2">
      <span v-if="!beneficiariesAreAnonymous">{{ beneficiary.firstname }} {{ beneficiary.lastname }}</span>
    </h3>
    <div class="sm:grid sm:grid-cols-12 sm:gap-y-2 sm:gap-x-12 w-sm max-w-full">
      <div class="col-span-4">
        <dl class="flex flex-wrap gap-y-1.5 mb-2.5">
          <div :class="dlGroupClasses" class="w-1/2">
            <dt :class="dtClasses">{{ t("beneficiary-id1") }}</dt>
            <dd :class="ddClasses">{{ beneficiary.id1 ?? "‒" }}</dd>
          </div>
          <div :class="dlGroupClasses" class="w-1/2">
            <dt :class="dtClasses">{{ t("beneficiary-id2") }}</dt>
            <dd :class="ddClasses">{{ beneficiary.id2 ?? "‒" }}</dd>
          </div>
          <div :class="dlGroupClasses" class="w-full">
            <dt :class="isBeneficiaryPaymentConflict() ? dtClassesError : dtClasses">
              {{ t("beneficiary-category") }}
            </dt>
            <dd :class="isBeneficiaryPaymentConflict() ? ddClassesError : ddClasses">
              <div class="flex">
                <span>{{ getBeneficiaryCategory() }}</span>
                <PfIcon
                  v-if="isBeneficiaryPaymentConflict()"
                  :icon="ICON_INFO"
                  class="text-red-500 shrink-0 mt-1 ml-1"
                  size="xs" />
              </div>
            </dd>
          </div>
        </dl>
        <template v-if="haveAnySubscriptions()">
          <ul class="inline-flex flex-col justify-start items-start gap-y-1 mb-4 sm:mb-0 max-w-full">
            <li v-for="item in getBeneficiarySubscriptions()" :key="item.subscription.id" class="max-w-full">
              <PfTag
                class="max-w-full"
                :label="item.subscription.name"
                is-dark-theme
                :bg-color-class="isSubscriptionPaymentConflict(item) ? 'bg-red-500' : 'bg-primary-700'"
                can-dismiss
                @dismiss="removeSubscription(beneficiary, item.subscription)" />
            </li>
          </ul>
        </template>
        <PfTag v-else :label="t('beneficiary-without-subscription')" bg-color-class="bg-primary-300" />
      </div>
      <div class="col-span-4 text-p4 text-primary-900 mb-4 sm:mb-0">
        <UiGenericContactInfo v-if="!beneficiariesAreAnonymous" :person="beneficiary" />
        <p v-if="!beneficiariesAreAnonymous && beneficiary.notes" class="mb-0 mt-2.5 flex items-center gap-x-1">
          <PfIcon :icon="ICON_INFO" class="text-primary-900 shrink-0" size="xs" />
          <span class="truncate">
            {{ beneficiary.notes }}
          </span>
        </p>
      </div>
      <div class="col-span-4">
        <dl class="mb-0">
          <div class="flex gap-x-2 mb-2.5 sm:block sm:relative">
            <dt class="sm:absolute sm:-left-7 sm:top-0">
              <PfIcon :icon="ICON_CREDIT_CARD" aria-label="card" />
            </dt>
            <dd>
              <template v-if="getCardProgramId() || getCardNumber()">
                <span :class="ddClasses" class="block">
                  {{ getCardProgramId() }}
                </span>
                <span class="block text-p4">
                  {{ getCardNumber() }}
                </span>
              </template>
              <PfTag
                v-else
                :label="t('beneficiary-no-card')"
                :is-dark-theme="haveAnySubscriptions()"
                :bg-color-class="haveAnySubscriptions() ? 'bg-red-500' : 'bg-primary-300'" />
            </dd>
          </div>
          <div v-if="getCardProgramId() || getCardNumber()" :class="dlGroupClasses" class="mb-2.5">
            <dt :class="dtClasses">{{ t("beneficiary-balance") }}</dt>
            <dd
              class="transition-padding ease-in-out duration-300 relative -left-11 -mr-11"
              :class="ddClasses"
              :style="beneficiary.rowPaddingBottom">
              <UiGenericCardBalance v-if="beneficiary.card" :beneficiary="beneficiary" />
            </dd>
          </div>
          <div v-if="getCardProgramId() || getCardNumber()" :class="dlGroupClasses">
            <dt :class="dtClasses">{{ t("beneficiary-card-last-usage") }}</dt>
            <dd :class="ddClasses">{{ getCardLastUsage() ?? "‒" }}</dd>
          </div>
        </dl>
      </div>
    </div>
    <div class="absolute right-3 top-3">
      <BeneficiaryActions :beneficiary="beneficiary" :beneficiaries-are-anonymous="props.beneficiariesAreAnonymous" />
    </div>
  </div>
</template>

<script setup>
import { defineProps, onMounted, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";

import BeneficiaryActions from "@/components/beneficiaries/beneficiary-actions";
import ICON_CREDIT_CARD from "@/lib/icons/credit-card.json";
import ICON_INFO from "@/lib/icons/info.json";

import { URL_BENEFICIARY_REMOVE_SUBSCRIPTION } from "@/lib/consts/urls";

import { formatDate, dateUtc, regularFormat } from "@/lib/helpers/date";

const { t } = useI18n();
const router = useRouter();

const beneficiary = ref(null);

const dlGroupClasses = "flex items-start gap-x-3";
const dtClasses = "text-primary-500 text-p4 uppercase font-semibold tracking-tight mt-px sm:mt-[3px]";
const dtClassesError = "text-red-500 text-p4 uppercase font-semibold tracking-tight mt-px sm:mt-[3px]";
const ddClasses = "text-primary-900 text-p2";
const ddClassesError = "text-red-500 text-p2";

onMounted(() => {
  beneficiary.value = {
    ...props.beneficiary,
    dropdownIsOpen: false,
    dropdownMaxHeight: null,
    rowPaddingBottom: null
  };
});

watch(
  () => props.beneficiary,
  (item) => {
    beneficiary.value = {
      ...item,
      dropdownIsOpen: false,
      dropdownMaxHeight: null,
      rowPaddingBottom: null
    };
  }
);

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

function isBeneficiaryPaymentConflict() {
  for (var i = 0; i < beneficiary.value.beneficiarySubscriptions.length; i++) {
    if (isSubscriptionPaymentConflict(beneficiary.value.beneficiarySubscriptions[i])) {
      return true;
    }
  }
  return false;
}

function getBeneficiaryCategory() {
  return beneficiary.value.beneficiaryType ? beneficiary.value.beneficiaryType.name : "";
}

function getBeneficiarySubscriptions() {
  return beneficiary.value.beneficiarySubscriptions;
}

function isSubscriptionPaymentConflict(beneficiarySubscription) {
  for (var j = 0; j < beneficiarySubscription.subscription.types.length; j++) {
    if (
      dateUtc(beneficiarySubscription.subscription.endDate) < new Date() ||
      beneficiarySubscription.beneficiaryType.id === beneficiary.value.beneficiaryType.id
    ) {
      return false;
    }
  }
  return true;
}

function haveAnySubscriptions() {
  return beneficiary.value.beneficiarySubscriptions.length > 0;
}

function getCardProgramId() {
  return beneficiary.value.card && beneficiary.value.card.programCardId ? beneficiary.value.card.programCardId : "";
}

function getCardNumber() {
  return beneficiary.value.card && beneficiary.value.card.cardNumber ? beneficiary.value.card.cardNumber : "";
}

function getCardLastUsage() {
  return beneficiary.value.card && beneficiary.value.card.lastTransactionDate
    ? formatDate(dateUtc(beneficiary.value.card.lastTransactionDate), regularFormat)
    : "";
}

function removeSubscription(beneficiary, subscription) {
  router.push({
    name: URL_BENEFICIARY_REMOVE_SUBSCRIPTION,
    params: { beneficiaryId: beneficiary.id, subscriptionId: subscription.id }
  });
}
</script>
