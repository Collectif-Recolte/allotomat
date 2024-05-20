<i18n>
  {
    "en": {
      "total": "Total",
      "open-product-group": "See balance by product group",
    },
    "fr": {
      "total": "Total",
      "open-product-group": "Voir le solde par groupe de produits",
    }
  }
</i18n>

<template>
  <div class="relative">
    <div class="flex items-center min-w-48 sm:min-w-36 ml-auto">
      <div class="w-7/12 text-right">
        <PfTag
          v-if="showTotal && props.beneficiary.card.funds?.length > 0"
          class="uppercase"
          :label="t('total')"
          border-color-class="border-primary-900"
          is-squared />
      </div>
      <div class="w-5/12 text-right">
        <div class="ml-2">{{ getCardFund() }}</div>
      </div>
      <div v-if="props.beneficiary.card.funds?.length > 0" class="absolute -top-0.5 -right-9">
        <button class="pf-button pf-button--outline min-h-7 min-w-7 p-0" @click="() => toggleDropdown()">
          <span class="sr-only">{{ t("open-product-group") }}</span>
          <PfIcon :class="props.beneficiary.dropdownIsOpen ? 'rotate-180' : 'rotate-0'" :icon="ICON_CHEVRON" size="sm" />
        </button>
      </div>
    </div>
    <div
      v-if="props.beneficiary.card.funds?.length > 0"
      :style="props.beneficiary.dropdownIsOpen ? { maxHeight: `${props.beneficiary.dropdownMaxHeight}px` } : null"
      class="absolute -bottom-1 translate-y-full right-0 overflow-hidden transition-max-height ease-in-out duration-300"
      :class="props.beneficiary.dropdownIsOpen ? 'max-h-full' : 'max-h-0'">
      <div class="h-full">
        <ProductGroupFundList :product-groups="getProductGroups()" display-small />
      </div>
    </div>
  </div>
</template>

<script setup>
import { defineProps } from "vue";
import { useI18n } from "vue-i18n";

import ICON_CHEVRON from "@/lib/icons/chevron-down.json";
import ProductGroupFundList from "@/components/product-groups/product-group-fund-list";

import { getMoneyFormat } from "@/lib/helpers/money";

const { t } = useI18n();

const props = defineProps({
  beneficiary: {
    type: Object,
    required: true
  },
  showTotal: {
    type: Boolean,
    default: false
  }
});

const elInsideDropdownHeight = 31;
const getProductGroups = () => {
  const productGroups = [];

  if (props.beneficiary.card.loyaltyFund !== null) {
    productGroups.push({
      color: props.beneficiary.card.loyaltyFund.productGroup.color,
      label: props.beneficiary.card.loyaltyFund.productGroup.name,
      fund: props.beneficiary.card.loyaltyFund.amount
    });
  }

  for (let fund of props.beneficiary.card.funds) {
    productGroups.push({
      color: fund.productGroup.color,
      label: fund.productGroup.name,
      fund: fund.amount
    });
  }

  return productGroups;
};

const getDropdownMaxHeight = () => {
  let productGroupNb = props.beneficiary.card.funds.length;
  if (props.beneficiary.card.loyaltyFund !== null) {
    productGroupNb++;
  }
  return (productGroupNb + 1) * elInsideDropdownHeight;
};

const getRowPaddingBottom = () => {
  return props.beneficiary.dropdownIsOpen ? { paddingBottom: `${props.beneficiary.dropdownMaxHeight}px` } : null;
};

function toggleDropdown() {
  // eslint-disable-next-line vue/no-mutating-props
  props.beneficiary.dropdownIsOpen = !props.beneficiary.dropdownIsOpen;
  if (props.beneficiary.dropdownIsOpen) {
    // eslint-disable-next-line vue/no-mutating-props
    props.beneficiary.dropdownMaxHeight = getDropdownMaxHeight();
    // eslint-disable-next-line vue/no-mutating-props
    props.beneficiary.rowPaddingBottom = getRowPaddingBottom();
  } else {
    // eslint-disable-next-line vue/no-mutating-props
    props.beneficiary.dropdownMaxHeight = null;
    // eslint-disable-next-line vue/no-mutating-props
    props.beneficiary.rowPaddingBottom = null;
  }
}

function getCardFund() {
  let fund = props.beneficiary.card.totalFund;

  if (props.beneficiary.card.loyaltyFund !== null) {
    fund += props.beneficiary.card.loyaltyFund.amount;
  }

  return props.beneficiary.card ? getMoneyFormat(fund) : "";
}
</script>
