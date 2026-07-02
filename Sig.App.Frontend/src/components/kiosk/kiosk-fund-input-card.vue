<template>
  <div
    class="rounded-xl border-2 p-4 flex flex-row items-center gap-4 min-h-[100px] cursor-text"
    :class="[cardStyles.border, cardStyles.bg]"
    @click="focusInput">
    <div class="w-[42%] shrink-0 min-w-0 flex flex-col gap-0.5">
      <p class="font-bold truncate text-p1 sm:text-h4 mb-0" :class="cardStyles.text" :title="productGroupLabel">
        {{ productGroupLabel }}
      </p>
      <p class="text-p2 sm:text-p1 text-grey-700 mt-0 leading-tight">
        {{ t("balance-label", { amount: getMoneyFormat(props.fund.amount) }) }}
      </p>
    </div>
    <div ref="slotContainer" class="flex-1 min-w-0">
      <slot />
    </div>
  </div>
</template>

<script setup>
import { computed, defineProps, ref } from "vue";
import { useI18n } from "vue-i18n";

import { PRODUCT_GROUP_LOYALTY } from "@/lib/consts/enums";
import { getKioskProductGroupCardClasses } from "@/lib/helpers/products-color";
import { getMoneyFormat } from "@/lib/helpers/money";

const { t } = useI18n();

const props = defineProps({
  fund: { type: Object, required: true },
  isGiftCard: Boolean
});

const productGroupLabel = computed(() => {
  if (props.fund.productGroup.name === PRODUCT_GROUP_LOYALTY) {
    return t("gift-card");
  }
  return props.fund.productGroup.name;
});

const cardStyles = computed(() => getKioskProductGroupCardClasses(props.fund.productGroup.color, props.isGiftCard));

const slotContainer = ref(null);

function focusInput() {
  slotContainer.value?.querySelector("input")?.focus();
}
</script>

<i18n>
{
  "en": {
    "balance-label": "Balance: {amount}",
    "gift-card": "Gift card"
  },
  "fr": {
    "balance-label": "Solde: {amount}",
    "gift-card": "Carte-cadeau"
  }
}
</i18n>
