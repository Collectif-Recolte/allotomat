<i18n>
{
  "en": {
    "select-all": "Select all",
    "remaining-budget-allowance": "Remaining envelope: {amount}",
    "remaining-budget-allowance-after-adjustment": "Remaining envelope: {amount} after adjustment",
    "payment-to-receive": "→ <b>One</b> payment remaining | → <b>{count}</b> payments remaining",
    "participant-miss-payment": "→ Participant will <b>not receive</b> this amount",
    "participant-get-payment": "→ Participant will <b>receive</b> this amount",
    "participant-category-detail": "→ Participant moves from category <b>{previousCategory} ({previousCategoryAmount})</b> to category <b>{newCategory} ({newCategoryAmount})</b>",
	},
	"fr": {
		"select-all": "Tout sélectionner",
    "remaining-budget-allowance": "Enveloppe restante : {amount}",
    "remaining-budget-allowance-after-adjustment": "Enveloppe restante : {amount} après l'ajustement",
    "payment-to-receive": "→ <b>Un</b> versement restant | → <b>{count}</b> versements restants",
    "participant-miss-payment": "→ Le participant ne va <b>pas recevoir</b> ce montant",
    "participant-get-payment": "→ Le participant va <b>recevoir</b> ce montant",
    "participant-category-detail": "→ Le participant passe de la catégorie <b>{previousCategory} ({previousCategoryAmount})</b> à la catégorie <b>{newCategory} ({newCategoryAmount})</b>",
	}
}
</i18n>

<template>
  <PfFormFieldset :id="props.id" :name="props.id" :has-error-state="props.hasErrorState" :errors="props.errors">
    <div v-for="(option, index) in props.options" :key="index">
      <PfFormInputCheckbox
        :id="option.value"
        :value="isChecked(option.value)"
        :label="option.label"
        :checked="isChecked(option.value)"
        :disabled="option.disabled"
        @input="(e) => updateCheckbox(option.value, e)">
        <template #description>
          <!-- eslint-disable-next-line vue/no-v-html @intlify/vue-i18n/no-v-html -->
          <p class="mb-2 text-p2 leading-none" v-html="t('payment-to-receive', option.numberOfPaymentToReceive)"></p>
          <!-- eslint-disable vue/no-v-html @intlify/vue-i18n/no-v-html -->
          <p class="mb-2 text-p2 leading-none" v-html="option.description"></p>

          <p
            v-if="option.newPaymentAmount > option.previousPaymentAmount"
            class="mb-2 text-p2 leading-none"
            v-html="t('participant-get-payment')"></p>
          <!-- eslint-disable-next-line vue/no-v-html @intlify/vue-i18n/no-v-html -->
          <p v-else class="mb-2 text-p2 leading-none" v-html="t('participant-miss-payment')"></p>
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
          <b
            class="pb-2 text-p2 leading-none"
            :class="
              isChecked(option.value)
                ? option.previousPaymentAmount - option.newPaymentAmount > 0
                  ? 'text-green-300'
                  : option.previousPaymentAmount - option.newPaymentAmount < 0
                  ? 'text-red-500'
                  : ''
                : ''
            ">
            <template v-if="isChecked(option.value)">
              {{
                t("remaining-budget-allowance-after-adjustment", {
                  amount: getMoneyFormat(
                    option.budgetAllowanceAvailableFund +
                      (option.previousPaymentAmount - option.newPaymentAmount) * option.numberOfPaymentToReceive
                  )
                })
              }}
            </template>
            <template v-else>
              {{ t("remaining-budget-allowance", { amount: getMoneyFormat(option.budgetAllowanceAvailableFund) }) }}
            </template>
          </b>
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
