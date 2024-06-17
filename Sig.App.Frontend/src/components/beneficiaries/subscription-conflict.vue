<i18n>
{
  "en": {
    "select-all": "Select all",
    "remaining-budget-allowance": "→ Remaining envelope for this subscription: <b>{amount}</b>",
    "remaining-budget-allowance-after-adjustment": "Remaining envelope: {amount} after adjustment",
    "payment-to-receive": "→ <b>One</b> payment remaining | → <b>{count}</b> payments remaining",
    "participant-category-detail": "→ The participant will move from category <b>{previousCategory} ({previousCategoryAmount})</b> to category <b>{newCategory} ({newCategoryAmount})</b> for future payments",
	},
	"fr": {
		"select-all": "Tout sélectionner",
    "remaining-budget-allowance": "→ Enveloppe restante pour cet abonnement : <b>{amount}</b>",
    "remaining-budget-allowance-after-adjustment": "→ Enveloppe restante pour cet abonnement : <b>{amount}</b> après l'ajustement",
    "payment-to-receive": "→ <b>Un</b> versement restant | → <b>{count}</b> versements restants",
    "participant-category-detail": "→ Le-a participant-e passera de la catégorie <b>{previousCategory} ({previousCategoryAmount})</b> à la catégorie <b>{newCategory} ({newCategoryAmount})</b> pour les versements futurs",
	}
}
</i18n>

<template>
  <PfFormFieldset :id="props.id" :name="props.id" :has-error-state="props.hasErrorState" :errors="props.errors">
    <div v-for="(option, index) in props.options" :key="index">
      <PfFormInputCheckbox
        :value="isChecked(option.value)"
        :label="option.label"
        :checked="isChecked(option.value)"
        :disabled="option.disabled"
        @input="(e) => updateCheckbox(option.value, e)">
        <template #description>
          <!-- eslint-disable vue/no-v-html @intlify/vue-i18n/no-v-html -->
          <p
            class="mb-2 text-p2 leading-none"
            :class="
              isChecked(option.value)
                ? option.previousPaymentAmount - option.newPaymentAmount > 0
                  ? 'text-green-300'
                  : option.previousPaymentAmount - option.newPaymentAmount < 0
                  ? 'text-red-500'
                  : ''
                : ''
            "
            v-html="remainingBudgetAllowance(option)"></p>
          <!-- eslint-disable-next-line vue/no-v-html @intlify/vue-i18n/no-v-html -->
          <p class="mb-2 text-p2 leading-none" v-html="t('payment-to-receive', option.numberOfPaymentToReceive)"></p>
          <!-- eslint-disable vue/no-v-html @intlify/vue-i18n/no-v-html -->
          <p
            class="mb-2 text-p2 leading-none"
            v-html="
              t('participant-category-detail', {
                previousCategory: option.previousCategoryName,
                previousCategoryAmount: getMoneyFormat(option.previousCategoryAmount),
                newCategory: option.newCategoryName,
                newCategoryAmount: getMoneyFormat(option.newCategoryAmount)
              })
            "></p>
          <!-- eslint-disable vue/no-v-html @intlify/vue-i18n/no-v-html -->
          <p class="mb-2 text-p2 leading-none" v-html="option.description"></p>
        </template>
      </PfFormInputCheckbox>
    </div>
    <PfFormInputCheckbox
      v-if="props.options.length > 1"
      id="select-all"
      :label="t('select-all')"
      :checked="isAllChecked"
      @input="updateCheckAll" />
  </PfFormFieldset>
</template>

<script setup>
import { computed, defineProps, defineEmits } from "vue";
import { useI18n } from "vue-i18n";

import { getMoneyFormat } from "@/lib/helpers/money";

import { commonFieldProps } from "@/../pinkflamant/components/form/field/index";

const props = defineProps({
  ...commonFieldProps,
  value: {
    type: Array,
    default() {
      return [];
    }
  },
  options: {
    type: Array,
    required: true,
    default() {
      return null;
    }
  }
});

const { t } = useI18n();
const emit = defineEmits(["input", "checkAll"]);

const isAllChecked = computed(() => props.value.length === props.options.length);
const remainingBudgetAllowance = (option) => {
  if (isChecked(option.value)) {
    return t("remaining-budget-allowance-after-adjustment", {
      amount: getMoneyFormat(
        option.budgetAllowanceAvailableFund +
          (option.previousPaymentAmount - option.newPaymentAmount) * option.numberOfPaymentToReceive
      )
    });
  }
  return t("remaining-budget-allowance", { amount: getMoneyFormat(option.budgetAllowanceAvailableFund) });
};

function isChecked(value) {
  return props.value.indexOf(value) !== -1;
}

function updateCheckbox(value, isChecked) {
  emit("input", { value, isChecked });
}

function updateCheckAll(isChecked) {
  emit("checkAll", isChecked);
}
</script>
