<template>
  <div class="flex flex-col h-full">
    <div class="bg-yellow-100 text-yellow-800 text-center font-bold text-d6 py-4 px-8 rounded-t-4xl">
      {{ t("verify-before-confirm") }}
    </div>

    <div class="px-8 xs:px-12 pt-5 pb-4">
      <p v-if="cardProgramCardId !== ''" class="text-center mb-2">
        <span class="font-bold text-h3 text-primary-700">{{ t("purchase-title") }}</span>
        <!-- eslint-disable-next-line @intlify/vue-i18n/no-raw-text -->
        <span class="font-normal text-h3 text-grey-500"> | {{ t("card-number", { cardProgramCardId }) }}</span>
      </p>

      <p class="text-center text-d2 font-bold text-primary-900 mb-10">
        {{ t("amount-charged") }}
        <span class="text-tertiary-500">{{ totalAmount }}</span>
      </p>

      <ul class="flex-1 min-h-0 overflow-y-auto mb-10 space-y-3">
        <li
          v-for="item in fundsWithAmount"
          :key="item.id"
          class="flex items-center justify-between gap-8 mb-4 last:mb-0 first:border-t first:border-grey-200 first:pt-4"
          :class="getIsGiftCard(item.fund.productGroup.name) ? 'pt-4 border-t border-grey-200' : ''">
          <div class="shrink-0 text-d7 font-bold px-4 py-2 rounded-lg" :class="getKioskCategoryClasses(item.fund)">
            {{ fundLabel(item.fund) }}
          </div>
          <span class="font-bold text-h1 text-primary-900 shrink-0">
            {{ getMoneyFormat(-Math.abs(item.amount)) }}
          </span>
        </li>
      </ul>

      <div class="flex flex-col xs:flex-row justify-between items-stretch xs:items-start gap-4 mt-auto">
        <PfButtonAction
          size="lg"
          class="min-h-20 rounded-2xl text-d6 w-full xs:w-auto"
          btn-style="secondary"
          has-icon-left
          :icon="ICON_CLOSE"
          :label="t('cancel')"
          :is-disabled="processing"
          @click="emit('cancel')" />
        <div class="flex flex-col items-center">
          <PfButtonAction
            size="lg"
            class="min-h-20 rounded-2xl text-d6 w-full xs:w-auto"
            btn-style="primary"
            has-icon-left
            :icon="ICON_SHOPPING_CART"
            :label="t('confirm-payment')"
            :is-disabled="processing"
            @click="emit('confirm')" />
          <p class="text-p1 text-grey-600 mt-3 mb-0 text-center">{{ t("irreversible-warning") }}</p>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, defineEmits, defineProps } from "vue";
import { useI18n } from "vue-i18n";

import { PRODUCT_GROUP_LOYALTY } from "@/lib/consts/enums";
import { getKioskProductGroupCardClasses } from "@/lib/helpers/products-color";
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

function getKioskCategoryClasses(item) {
  const styles = getKioskProductGroupCardClasses(item.productGroup.color, getIsGiftCard(item.productGroup.name), true);
  return [styles.border, styles.bg, styles.text];
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
