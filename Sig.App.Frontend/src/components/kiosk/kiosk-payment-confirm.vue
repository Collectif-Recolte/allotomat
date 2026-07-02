<template>
  <div class="flex flex-col h-full min-h-0">
    <div
      class="bg-secondary-100 text-tertiary-500 text-center font-semibold text-p2 sm:text-p1 py-3 px-4 rounded-t-2xl -mx-3 xs:-mx-6 -mt-6 xs:-mt-6 mb-6">
      {{ t("verify-before-confirm") }}
    </div>

    <p v-if="cardProgramCardId !== ''" class="text-center mb-4">
      <span class="font-semibold text-h3 text-primary-900">{{ t("purchase-title") }}</span>
      <!-- eslint-disable-next-line @intlify/vue-i18n/no-raw-text -->
      <span class="font-normal text-h4 sm:text-h3 text-grey-500"> | {{ t("card-number", { cardProgramCardId }) }}</span>
    </p>

    <p class="text-center text-h3 sm:text-h2 font-bold text-primary-900 mb-6">
      {{ t("amount-charged") }}
      <span class="text-tertiary-500 text-h2 sm:text-h1">{{ totalAmount }}</span>
    </p>

    <ul class="flex-1 min-h-0 overflow-y-auto mb-6 space-y-3">
      <li
        v-for="item in fundsWithAmount"
        :key="item.id"
        class="flex items-center justify-between gap-4 py-1"
        :class="getIsGiftCard(item.fund.productGroup.name) ? 'pt-4 mt-2 border-t border-grey-200' : ''">
        <PfTag
          class="max-w-[55%] shrink-0 [&_.block]:!text-p2 sm:[&_.block]:!text-p1 [&_.block]:!font-bold !px-3 !py-1.5"
          :label="fundLabel(item.fund)"
          :bg-color-class="`${getColorBgClass(item.fund.productGroup.color)} ${
            getIsGiftCard(item.fund.productGroup.name) ? 'bg-diagonal-pattern' : ''
          }`"
          :is-dark-theme="!getIsGiftCard(item.fund.productGroup.name)"
          is-squared />
        <span class="font-bold text-h3 sm:text-h2 text-primary-900 shrink-0">
          {{ getMoneyFormat(-Math.abs(item.amount)) }}
        </span>
      </li>
    </ul>

    <div class="flex flex-row justify-between items-start gap-4 mt-auto pt-4 border-t border-grey-200">
      <PfButtonAction
        size="lg"
        btn-style="secondary"
        has-icon-left
        :icon="ICON_CLOSE"
        :label="t('cancel')"
        :processing="processing"
        @click="emit('cancel')" />
      <div class="flex flex-col items-center">
        <PfButtonAction
          size="lg"
          btn-style="primary"
          has-icon-left
          :icon="ICON_SHOPPING_CART"
          :label="t('confirm-payment')"
          :processing="processing"
          @click="emit('confirm')" />
        <p class="text-p3 sm:text-p2 text-grey-600 mt-2 text-center">{{ t("irreversible-warning") }}</p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, defineEmits, defineProps } from "vue";
import { useI18n } from "vue-i18n";

import { PRODUCT_GROUP_LOYALTY } from "@/lib/consts/enums";
import { getColorBgClass } from "@/lib/helpers/products-color";
import { getMoneyFormat } from "@/lib/helpers/money";

import ICON_CLOSE from "@/lib/icons/close.json";
import ICON_SHOPPING_CART from "@/lib/icons/shopping-cart.json";

const { t } = useI18n();

const props = defineProps({
  funds: { type: Array, required: true },
  cardProgramCardId: { type: [String, Number], default: "" },
  totalAmount: { type: String, required: true },
  processing: Boolean
});

const emit = defineEmits(["cancel", "confirm"]);

const fundsWithAmount = computed(() =>
  props.funds.filter((item) => {
    const amount = item.amount;
    return amount !== undefined && amount !== null && amount !== "" && parseFloat(amount) > 0;
  })
);

function fundLabel(fund) {
  if (fund.productGroup.name === PRODUCT_GROUP_LOYALTY) {
    return t("gift-card");
  }
  return fund.productGroup.name;
}

function getIsGiftCard(productGroupName) {
  return productGroupName === PRODUCT_GROUP_LOYALTY;
}
</script>

<i18n>
{
  "en": {
    "verify-before-confirm": "Please verify before confirming",
    "purchase-title": "Make a purchase",
    "card-number": "Card #{cardProgramCardId}",
    "amount-charged": "The card will be charged ",
    "cancel": "Cancel",
    "confirm-payment": "Confirm payment",
    "irreversible-warning": "This action is irreversible.",
    "gift-card": "Gift card"
  },
  "fr": {
    "verify-before-confirm": "Veuillez vérifier avant de confirmer",
    "purchase-title": "Faire un achat",
    "card-number": "Carte #{cardProgramCardId}",
    "amount-charged": "La carte sera débitée de ",
    "cancel": "Annuler",
    "confirm-payment": "Confirmer le paiement",
    "irreversible-warning": "Cette action est irréversible.",
    "gift-card": "Carte-cadeau"
  }
}
</i18n>
