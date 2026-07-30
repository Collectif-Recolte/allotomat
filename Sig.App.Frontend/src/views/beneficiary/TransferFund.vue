<i18n>
{
  "en": {
    "title": "Transfer Funds",
    "add-subscription-payment-title": "Replicate a scheduled payment",
    "add-subscription-payment-desc": "Add the proper funds to the participant's card according to the subscription settings.",
    "add-subscription-payment": "Replicate a scheduled payment",
    "manually-add-funds-title": "Transfer a custom amount",
    "manually-add-funds-desc": "Add a custom amount to the participant's card with a chosen expiration date.",
    "manually-add-funds": "Transfer a custom amount",
    "add-gift-card-funds-title": "Add gift card funds to the card",
    "add-gift-card-funds-desc": "Add a gift card amount to the participant's card without an expiration date.",
    "add-gift-card-funds": "Add gift card funds to the card",
    "add-subscription-payment-tooltip": "No subscription payment available per rules",
    "manually-add-funds-tooltip": "No active subscription found"
  },
  "fr": {
    "title": "Transférer des fonds",
    "add-subscription-payment-title": "Reproduire un versement automatisé",
    "add-subscription-payment-desc": "Ajoutez les fonds appropriés sur la carte, conformément aux paramètres de l'abonnement.",
    "add-subscription-payment": "Reproduire un versement automatisé",
    "manually-add-funds-title": "Versement sur mesure",
    "manually-add-funds-desc": "Ajouter un montant customisé sur la carte avec une date d'expiration au choix.",
    "manually-add-funds": "Versement sur mesure",
    "add-gift-card-funds-title": "Ajouter des fonds carte-cadeau",
    "add-gift-card-funds-desc": "Ajouter un montant carte-cadeau à la carte sans date d'expiration.",
    "add-gift-card-funds": "Ajouter des fonds carte-cadeau",
    "add-subscription-payment-tooltip": "Aucun versement possible selon les règles de l'abonnement",
    "manually-add-funds-tooltip": "Aucun abonnement actif trouvé"
  }
}
</i18n>

<template>
  <UiDialogModal v-if="!loading" :title="t('title')" :has-footer="true" :return-route="returnRoute">
    <div class="flex flex-col gap-6">
      <div class="grid gap-4 sm:grid-cols-[minmax(0,1fr)_auto] sm:items-center sm:gap-6">
        <UiCallout class="min-w-0" :variant="CALLOUT_INFO">
          <!-- <p class="font-medium mb-1 m-0">{{ t("add-subscription-payment-title") }}</p> -->
          <p class="m-0">{{ t("add-subscription-payment-desc") }}</p>
        </UiCallout>
        <PfTooltip :hide-tooltip="canAddSubscriptionPayment" :label="!canAddSubscriptionPayment ? t('add-subscription-payment-tooltip') : undefined">
          <PfButtonLink
            tag="routerLink"
            class="w-full sm:w-auto sm:min-w-[11rem] px-6 py-3 text-base shrink-0"
            :to="{
              name: URL_BENEFICIARY_ADD_SUBSCRIPTION_PAYMENT,
              params: { beneficiaryId: route.params.beneficiaryId }
            }"
            :label="t('add-subscription-payment')"
            :is-disabled="!canAddSubscriptionPayment" />
        </PfTooltip>
      </div>
      <div class="grid gap-4 sm:grid-cols-[minmax(0,1fr)_auto] sm:items-center sm:gap-6">
        <UiCallout class="min-w-0" :variant="CALLOUT_INFO">
          <!-- <p class="font-medium mb-1 m-0">{{ t("manually-add-funds-title") }}</p> -->
          <p class="m-0">{{ t("manually-add-funds-desc") }}</p>
        </UiCallout>
        <PfTooltip
          :hide-tooltip="haveActiveSubscription"
          :label="!haveActiveSubscription ? t('manually-add-funds-tooltip') : undefined">
          <PfButtonLink
            tag="routerLink"
            class="w-full sm:w-auto sm:min-w-[11rem] px-6 py-3 text-base shrink-0"
            :to="{
              name: URL_BENEFICIARY_MANUALLY_ADD_FUND,
              params: { beneficiaryId: route.params.beneficiaryId }
            }"
            :label="t('manually-add-funds')"
            :is-disabled="!haveActiveSubscription" />
        </PfTooltip>
      </div>
      <div
        v-if="userType === USER_TYPE_PROJECTMANAGER"
        class="grid gap-4 sm:grid-cols-[minmax(0,1fr)_auto] sm:items-center sm:gap-6">
        <UiCallout class="min-w-0" :variant="CALLOUT_INFO">
          <!-- <p class="font-medium mb-1 m-0">{{ t("add-gift-card-funds-title") }}</p> -->
          <p class="m-0">{{ t("add-gift-card-funds-desc") }}</p>
        </UiCallout>
        <PfButtonLink
          tag="routerLink"
          class="w-full sm:w-auto sm:min-w-[11rem] px-6 py-3 text-base shrink-0"
          :to="{
            name: URL_BENEFICIARY_EDIT_GIFT_CARD,
            params: { cardId: beneficiary?.card?.id }
          }"
          :label="t('add-gift-card-funds')"
          :is-disabled="!beneficiary?.card?.id" />
      </div>
    </div>
  </UiDialogModal>
</template>

<script setup>
import { computed } from "vue";
import { useRoute } from "vue-router";
import { useI18n } from "vue-i18n";
import { useQuery, useResult } from "@vue/apollo-composable";
import gql from "graphql-tag";
import { storeToRefs } from "pinia";

import {
  URL_BENEFICIARY_ADMIN,
  URL_BENEFICIARY_ADD_SUBSCRIPTION_PAYMENT,
  URL_BENEFICIARY_MANUALLY_ADD_FUND,
  URL_BENEFICIARY_EDIT_GIFT_CARD
} from "@/lib/consts/urls";
import { CALLOUT_INFO } from "@/lib/consts/callout";
import { USER_TYPE_PROJECTMANAGER } from "@/lib/consts/enums";

import UiCallout from "@/components/ui/callout.vue";

import { useAuthStore } from "@/lib/store/auth";
import { dateUtc } from "@/lib/helpers/date";

const { t } = useI18n();
const route = useRoute();

const { userType } = storeToRefs(useAuthStore());

const returnRoute = computed(() => ({
  name: URL_BENEFICIARY_ADMIN,
  params: { beneficiaryId: route.params.beneficiaryId }
}));

const { result: resultBeneficiary, loading } = useQuery(
  gql`
    query Beneficiary($id: ID!) {
      beneficiary(id: $id) {
        id
        card {
          id
        }
        ... on BeneficiaryGraphType {
          beneficiarySubscriptions {
            canAddSubscriptionPayment
            subscription {
              id
              fundsExpirationDate
              isFundsAccumulable
            }
          }
        }
      }
    }
  `,
  {
    id: route.params.beneficiaryId
  }
);

const beneficiary = useResult(resultBeneficiary, null, (data) => data.beneficiary);

const canAddSubscriptionPayment = computed(() => {
  return beneficiary.value?.beneficiarySubscriptions?.some((x) => x?.canAddSubscriptionPayment ?? false) ?? false;
});

const haveActiveSubscription = computed(() => {
  return (
    beneficiary.value?.beneficiarySubscriptions?.some(
      (x) => !x.subscription.isFundsAccumulable || dateUtc(x.subscription.fundsExpirationDate) > Date.now()
    ) ?? false
  );
});
</script>
