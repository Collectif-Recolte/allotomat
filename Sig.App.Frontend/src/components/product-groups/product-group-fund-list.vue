<i18n>
  {
    "en": {
      "gift-card": "Gift card",
      "expiration-date": "Expires on {date}",
      "never-expired": "Never expires"
    },
    "fr": {
      "gift-card": "Carte-cadeau",
      "expiration-date": "Expire le {date}",
      "never-expired": "N'expire jamais"
    }
  }
  </i18n>

<template>
  <div v-if="props.productGroups">
    <ul class="mb-0 min-w-48 sm:min-w-36">
      <li v-for="(product, index) in props.productGroups" :key="index" class="mb-2 last:mb-0 border-b border-grey-300 pb-2">
        <div :class="displayExpirationDate ? 'items-start' : 'items-center'" class="text-p2 flex justify-between gap-x-2 w-full">
          <PfTag
            :class="{ 'mt-0.5': displayExpirationDate }"
            class="max-w-full"
            :label="getProductGroupName(product.label)"
            :bg-color-class="`${getColorBgClass(product.color)} ${getIsGiftCard(product.label) ? 'bg-diagonal-pattnern' : ''}`"
            :is-dark-theme="!getIsGiftCard(product.label)"
            is-squared />
          <div class="text-right">
            <div :class="displaySmall ? 'text-p2' : 'text-h3'" class="leading-none whitespace-nowrap">
              {{ getMoneyFormat(product.fund) }}
            </div>
          </div>
        </div>
        <div v-if="displayExpirationDate" class="text-right text-p4 mt-1.5 leading-none">
          <template v-if="product && product.expirationDate">
            {{ t("expiration-date", { date: formatDate(dateUtc(product.expirationDate), textualFormat) }) }}
          </template>
          <template v-else>
            {{ t("never-expired") }}
          </template>
        </div>
      </li>
    </ul>
  </div>
</template>

<script setup>
import { useI18n } from "vue-i18n";
import { defineProps } from "vue";
import { getColorBgClass } from "@/lib/helpers/products-color";
import { getMoneyFormat } from "@/lib/helpers/money";
import { formatDate, dateUtc, textualFormat } from "@/lib/helpers/date";

import { PRODUCT_GROUP_LOYALTY } from "@/lib/consts/enums";

const { t } = useI18n();

const props = defineProps({
  productGroups: { type: Object, default: null },
  displayExpirationDate: { type: Boolean, default: false },
  displaySmall: Boolean
});

function getProductGroupName(label) {
  if (label === PRODUCT_GROUP_LOYALTY) {
    return t("gift-card");
  }

  return label;
}

function getIsGiftCard(productGroupName) {
  if (productGroupName === PRODUCT_GROUP_LOYALTY) return true;
  else return false;
}
</script>
