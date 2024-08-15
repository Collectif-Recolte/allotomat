<i18n>
{
  "en": {
    "payment-dates": "→ Payment dates : <b>{paymentDates}</b>",
    "payment-moment-first-day-of-the-month": "1st",
    "payment-moment-fifteenth-day-of-the-month": "15",
    "payment-moment-first-and-fifteenth-day-of-the-month": "1st & 15",
    "max-number-of-payments": "→ Max <b>{count}</b> payments per card",
    "subscription-payment-based-card-usage": "→ Payments based on card usage",
    "expiration-date": "→ Funds expiration <b>{definition}</b>",
    "expiration-date-next-payment": "at the next payment date",
    "expiration-date-specific-date": "on {date}",
    "expiration-date-number-of-days": "{count} days after the first purchase made with the card, or at the latest on {date}",
    "dont-have-budget-allowance": "→ <b>The subscription does not have a budget allowance for this group of participants</b>",
    "dont-have-beneficiary-type": "→ <b>The subscription does not have a beneficiary type for this participant</b>",
    "budget-allowance-needed": "→ {amountByPayment}$ per payment * {remainingPaymentCount} remaining payments (out of {totalPaymentCount} in total) = {budgetAllowanceNeeded} $ required for the allocation",
    "budget-allowance-not-enought": "→ <b>The budget allowance is not sufficient to cover the remaining payments</b>",
    "select-all": "Select all",
	},
	"fr": {
		"payment-dates": "→ Dates des versements : <b>Versements le {paymentMoment} du mois entre le {startDate} et le {endDate}</b>",
    "payment-moment-first-day-of-the-month": "1er",
    "payment-moment-fifteenth-day-of-the-month": "15",
    "payment-moment-first-and-fifteenth-day-of-the-month": "1er et le 15",
    "max-number-of-payments": "→ Max <b>{count}</b> versements par carte",
    "subscription-payment-based-card-usage":"→ Versements en fonction de l'utilisation de la carte",
    "expiration-date": "→ Expiration des fonds <b>{definition}</b>",
    "expiration-date-next-payment": "à la prochaine date de paiement",
    "expiration-date-specific-date": "le {date}",
    "expiration-date-number-of-days": "{count} jours après le premier achat fait avec la carte, ou au plus tard le {date}",
    "dont-have-budget-allowance": "→ <b>L'abonnement n'a pas d'enveloppe budgétaire pour ce groupe de participant-e</b>",
    "dont-have-beneficiary-type": "→ <b>L'abonnement n'a pas de type de bénéficiaire pour ce participant-e</b>",
    "budget-allowance-needed": "→ {amountByPayment}$ par versement * {remainingPaymentCount} versements restants (sur {totalPaymentCount} au total) = {budgetAllowanceNeeded} $ requis pour l'attribution",
    "budget-allowance-not-enought": "→ <b>L'enveloppe budgétaire n'est pas suffisante pour couvrir les versements restants</b>",
    "select-all": "Tout sélectionner",
	}
}
</i18n>

<template>
  <PfFormFieldset :id="props.id" :name="props.id" :has-error-state="props.hasErrorState" :errors="props.errors">
    {{ props.options }}
    <div v-for="(option, index) in props.options" :key="index">
      <PfFormInputCheckbox
        :value="isChecked(option.id)"
        :label="option.name"
        :checked="isChecked(option.id)"
        :disabled="option.dontHaveBudgetAllowance || option.dontHaveBeneficiaryType || !option.isBudgetAllowanceIsEnough"
        @input="(e) => updateCheckbox(option.id, e)">
        <template #description>
          <!-- eslint-disable vue/no-v-html @intlify/vue-i18n/no-v-html -->
          <p
            v-if="option.dontHaveBudgetAllowance"
            class="mb-2 text-p2 leading-none text-red-500"
            v-html="t('dont-have-budget-allowance')"></p>
          <!-- eslint-disable vue/no-v-html @intlify/vue-i18n/no-v-html -->
          <p
            v-if="option.dontHaveBeneficiaryType"
            class="mb-2 text-p2 leading-none text-red-500"
            v-html="t('dont-have-beneficiary-type')"></p>
          <!-- eslint-disable vue/no-v-html @intlify/vue-i18n/no-v-html -->
          <p class="mb-2 text-p2 leading-none" v-html="getPaymentDates(option)"></p>
          <!-- eslint-disable-next-line vue/no-v-html @intlify/vue-i18n/no-v-html -->
          <p
            v-if="option.maxNumberOfPayments > 0"
            class="mb-2 text-p2 leading-none"
            v-html="t('max-number-of-payments', { count: option.maxNumberOfPayments })"></p>
          <p v-if="option.isSubscriptionPaymentBasedCardUsage" class="mb-2 text-p2 leading-none">
            {{ t("subscription-payment-based-card-usage") }}
          </p>
          <!-- eslint-disable vue/no-v-html @intlify/vue-i18n/no-v-html -->
          <p class="mb-2 text-p2 leading-none" v-html="getExpirationDate(option)"></p>

          <!-- eslint-disable vue/no-v-html @intlify/vue-i18n/no-v-html -->
          <p
            v-if="!option.dontHaveBudgetAllowance && !option.isBudgetAllowanceIsEnough"
            class="mb-2 text-p2 leading-none text-red-500"
            v-html="t('budget-allowance-not-enought')"></p>
          <!-- eslint-disable vue/no-v-html @intlify/vue-i18n/no-v-html -->
          <p v-else class="mb-2 text-p2 leading-none" v-html="getBudgetAllowanceNeeded(option)"></p>
        </template>
      </PfFormInputCheckbox>
    </div>
    <PfFormInputCheckbox
      v-if="props.options.length > 1"
      id="select-all"
      :disabled="!anyOptionEnabled(props.options)"
      :label="t('select-all')"
      :checked="isAllChecked"
      @input="updateCheckAll" />
  </PfFormFieldset>
</template>

<script setup>
import { computed, defineProps, defineEmits } from "vue";
import { useI18n } from "vue-i18n";

import { formatDate, textualFormat } from "@/lib/helpers/date";

import {
  FIRST_DAY_OF_THE_MONTH,
  FIFTEENTH_DAY_OF_THE_MONTH,
  FIRST_AND_FIFTEENTH_DAY_OF_THE_MONTH
} from "@/lib/consts/monthly-payment-moment";

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
  },
  beneficiary: {
    type: Object,
    required: true
  }
});

const { t } = useI18n();
const emit = defineEmits(["input", "checkAll"]);

const isAllChecked = computed(() => props.value.length === props.options.length);
const getPaymentDates = (option) => {
  let paymentMoment = "";
  switch (option.monthlyPaymentMoment) {
    case FIRST_DAY_OF_THE_MONTH:
      paymentMoment = t("payment-moment-first-day-of-the-month");
      break;
    case FIFTEENTH_DAY_OF_THE_MONTH:
      paymentMoment = t("payment-moment-fifteenth-day-of-the-month");
      break;
    case FIRST_AND_FIFTEENTH_DAY_OF_THE_MONTH:
      paymentMoment = t("payment-moment-first-and-fifteenth-day-of-the-month");
      break;
  }

  return t("payment-dates", {
    paymentMoment,
    startDate: formatDate(new Date(option.startDate), textualFormat),
    endDate: formatDate(new Date(option.endDate), textualFormat)
  });
};

const getExpirationDate = (option) => {
  let definition = "";

  if (!option.isFundsAccumulable) definition = t("expiration-date-next-payment");
  else if (option.numberDaysUntilFundsExpire > 0)
    definition = t("expiration-date-number-of-days", {
      count: option.numberDaysUntilFundsExpire,
      date: formatDate(new Date(option.fundsExpirationDate), textualFormat)
    });
  else definition = t("expiration-date-specific-date", { date: formatDate(new Date(option.fundsExpirationDate), textualFormat) });

  return t("expiration-date", { definition });
};

const getBudgetAllowanceNeeded = (option) => {
  if (option.dontHaveBudgetAllowance) return "";

  const amountByPayment = option.types
    .filter((x) => x.beneficiaryType.id === props.beneficiary.beneficiaryType.id)
    .reduce((acc, x) => acc + x.amount, 0);
  const remainingPaymentCount = option.paymentRemaining;
  const totalPaymentCount = option.totalPayment;
  const budgetAllowanceNeeded = amountByPayment * remainingPaymentCount;

  return t("budget-allowance-needed", { amountByPayment, remainingPaymentCount, totalPaymentCount, budgetAllowanceNeeded });
};

const anyOptionEnabled = (options) => {
  return options.some(
    (option) => !option.dontHaveBudgetAllowance && !option.dontHaveBeneficiaryType && option.isBudgetAllowanceIsEnough
  );
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
