<i18n>
{
  "en": {
    "title": "Transfer funds",
    "add-missing-payment-title": "Add a missed subscription payment",
    "add-missing-payment-desc": "Funds will be added to the participant's card according to the subscription settings.",
    "add-missing-payment": "Add a missed payment",
    "manually-add-funds-title": "Manually add funds",
    "manually-add-funds-desc": "Add a specific amount to the participant's card with a chosen expiration date.",
    "manually-add-funds": "Manually add funds",
    "add-gift-card-funds-title": "Add gift card funds to the card",
    "add-gift-card-funds-desc": "Add a gift card amount to the participant's card without an expiration date.",
    "add-gift-card-funds": "Add gift card funds to the card"
  },
  "fr": {
    "title": "Transférer des fonds",
    "add-missing-payment-title": "Versement d'un paiement manqué",
    "add-missing-payment-desc": "Selon les paramètres de l'abonnement, les fonds seront ajoutés à la carte du ou de la participant·e.",
    "add-missing-payment": "Ajouter un versement manqué",
    "manually-add-funds-title": "Versement sur mesure",
    "manually-add-funds-desc": "Ajouter un montant sur la carte du ou de la participant·e avec une date d'expiration au choix.",
    "manually-add-funds": "Versement sur mesure",
    "add-gift-card-funds-title": "Ajouter des fonds carte-cadeau",
    "add-gift-card-funds-desc": "Ajouter un montant carte-cadeau à la carte du ou de la participant·e sans date d'expiration.",
    "add-gift-card-funds": "Ajouter des fonds carte-cadeau"
  }
}
</i18n>

<template>
  <UiDialogModal v-if="!loading" :title="t('title')" :has-footer="true" :return-route="returnRoute">
    <div class="flex flex-col gap-6">
      <div class="grid gap-4 sm:grid-cols-[minmax(0,1fr)_auto] sm:items-center sm:gap-6">
        <UiCallout class="min-w-0" :variant="CALLOUT_INFO">
          <p class="font-medium mb-1 m-0">{{ t("add-missing-payment-title") }}</p>
          <p class="m-0">{{ t("add-missing-payment-desc") }}</p>
        </UiCallout>
        <PfButtonLink tag="routerLink" class="w-full sm:w-auto sm:min-w-[11rem] px-6 py-3 text-base shrink-0" :to="{
          name: URL_BENEFICIARY_ADD_MISSED_PAYMENT,
          params: { beneficiaryId: route.params.beneficiaryId }
        }" :label="t('add-missing-payment')" :is-disabled="!haveMissedPayment" />
      </div>
      <div class="grid gap-4 sm:grid-cols-[minmax(0,1fr)_auto] sm:items-center sm:gap-6">
        <UiCallout class="min-w-0" :variant="CALLOUT_INFO">
          <p class="font-medium mb-1 m-0">{{ t("manually-add-funds-title") }}</p>
          <p class="m-0">{{ t("manually-add-funds-desc") }}</p>
        </UiCallout>
        <PfButtonLink tag="routerLink" class="w-full sm:w-auto sm:min-w-[11rem] px-6 py-3 text-base shrink-0" :to="{
          name: URL_BENEFICIARY_MANUALLY_ADD_FUND,
          params: { beneficiaryId: route.params.beneficiaryId }
        }" :label="t('manually-add-funds')" :is-disabled="!haveActiveSubscription" />
      </div>
      <div class="grid gap-4 sm:grid-cols-[minmax(0,1fr)_auto] sm:items-center sm:gap-6">
        <UiCallout class="min-w-0" :variant="CALLOUT_INFO">
          <p class="font-medium mb-1 m-0">{{ t("add-gift-card-funds-title") }}</p>
          <p class="m-0">{{ t("add-gift-card-funds-desc") }}</p>
        </UiCallout>
        <PfButtonLink tag="routerLink" class="w-full sm:w-auto sm:min-w-[11rem] px-6 py-3 text-base shrink-0" :to="{
          name: URL_BENEFICIARY_EDIT_GIFT_CARD,
          params: { cardId: beneficiary?.card?.id }
        }" :label="t('add-gift-card-funds')" :is-disabled="!beneficiary?.card?.id" />
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

import {
  URL_BENEFICIARY_ADMIN,
  URL_BENEFICIARY_ADD_MISSED_PAYMENT,
  URL_BENEFICIARY_MANUALLY_ADD_FUND,
  URL_BENEFICIARY_EDIT_GIFT_CARD
} from "@/lib/consts/urls";
import { CALLOUT_INFO } from "@/lib/consts/callout";

import UiCallout from "@/components/ui/callout.vue";

import { dateUtc } from "@/lib/helpers/date";

const { t } = useI18n();
const route = useRoute();

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
            hasMissedPayment
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

const haveMissedPayment = computed(() => {
  return beneficiary.value?.beneficiarySubscriptions?.some((x) => x?.hasMissedPayment ?? false) ?? false;
});

const haveActiveSubscription = computed(() => {
  return (
    beneficiary.value?.beneficiarySubscriptions?.some(
      (x) => !x.subscription.isFundsAccumulable || dateUtc(x.subscription.fundsExpirationDate) > Date.now()
    ) ?? false
  );
});
</script>
