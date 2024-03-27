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
    <ul class="mb-0 w-36">
      <li v-for="(product, index) in props.productGroups" :key="index" class="mb-1 last:mb-0 mt-3 first:mt-0">
        <div class="text-p2 flex items-center w-full">
          <div class="w-7/12 text-right">
            <PfTag
              class="max-w-full"
              :label="getProductGroupName(product.label)"
              :bg-color-class="`${getColorBgClass(product.color)} ${getIsGiftCard(product.label) ? 'bg-diagonal-pattnern' : ''}`"
              :is-dark-theme="!getIsGiftCard(product.label)"
              is-squared />
          </div>
          <div class="w-5/12 text-right">
            <div class="ml-2">{{ getMoneyFormat(product.fund) }}</div>
          </div>
        </div>
        <div class="text-p2 align-middle w-full mb-1 last:mb-0">
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
  productGroups: { type: Object, default: null }
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
