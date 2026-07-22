<i18n>
{
  "en": {
    "balance-label": "Balance:",
    "gift-card": "Gift card"
  },
  "fr": {
    "balance-label": "Solde:",
    "gift-card": "Carte-cadeau"
  }
}
</i18n>

<template>
  <div
    class="rounded-2xl border-2 p-5 flex flex-row items-center gap-6 min-h-[104px] h-full"
    :class="[cardStyles.border, cardStyles.bg]">
    <div class="flex-1 min-w-0">
      <p class="font-bold line-clamp-2 text-d6 mb-1" :class="cardStyles.text" :title="productGroupLabel">
        {{ productGroupLabel }}
      </p>
      <p class="text-d7 text-primary-700 mb-0 leading-tight">
        {{ t("balance-label") }}
        <span class="font-medium text-d5 tabular-nums ml-1.5">{{ getMoneyFormat(props.fund.amount) }}</span>
      </p>
    </div>
    <div class="flex-1 min-w-0">
      <slot />
    </div>
  </div>
</template>

<script setup>
import { computed, defineProps } from "vue";
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
</script>
