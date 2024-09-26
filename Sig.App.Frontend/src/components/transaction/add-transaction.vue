/* eslint-disable @intlify/vue-i18n/no-unused-keys */
<i18n>
{
	"en": {
		"amount-label": "{productGroupName}",
    "amount-after-label": "Balance: {amountAvailable}",
    "confirmation-amount-label": "{productGroupName}",
    "amount-validation-label": "Amount",
		"amount-placeholder": "Ex. {amount}",
		"cancel": "Cancel",
    "product-groups": "Product groups",
		"product-group-fund-not-enought": "The product group does not have enough funds",
		"card-selected": "Card #{cardProgramCardId}",
		"create-transaction": "Pay",
		"title": "Transaction",
    "title-confirm": "Confirmation",
    "amount-charged": "The card will be charged ",
    "confirm": "Confirm",
    "edit": "Revise",
    "gift-card": "Gift card",
    "no-product-group-transaction":"At least one product group must have an amount to create a transaction.",
    "no-funds-message": "There are no available funds on this card.",
    "product-group-amount-isnan": "The amount must be a number.",
    "card-is-disabled": "The card is disabled.",
    "beneficiary-card-last-usage": "Last usage : {date}",
    "subscriptions-title": "Beneficiary subscriptions",
    "amount-received-each-payment": "→ Amount(s) received by this card at each payment: <b>{amountByPayment}</b>",
    "remaining-payment-on-card": "→ <b>{remainingPayment}</b> remaining payments on this card",
    "payment-dates": "→ Payment dates: <b>Payments on the {paymentMoment} of the month between {startDate} and {endDate}</b>",
    "payment-moment-first-day-of-the-month": "1st",
    "payment-moment-fifteenth-day-of-the-month": "15",
    "payment-moment-first-and-fifteenth-day-of-the-month": "1st & 15",
    "expiration-date": "→ Funds expiration <b>{definition}</b>",
    "expiration-date-next-payment": "at the next payment date",
    "expiration-date-specific-date": "on {date}",
    "expiration-date-number-of-days": "{count} days after the first purchase made with the card, or at the latest on {date}"
	},
	"fr": {
		"amount-label": "{productGroupName}",
    "amount-after-label": "Solde: {amountAvailable}",
    "confirmation-amount-label": "{productGroupName}",
    "amount-validation-label": "Solde",
		"amount-placeholder": "Ex. {amount}",
		"cancel": "Annuler",
    "product-groups": "Groupes de produits",
		"product-group-fund-not-enought": "Le groupe de produits ne possède pas assez de fonds",
		"card-selected": "{marketName} - Carte #{cardProgramCardId}",
		"create-transaction": "Payer",
		"title": "Transaction",
    "title-confirm": "Confirmation",
    "amount-charged": "La carte sera débitée de ",
    "confirm": "Confirmer",
    "edit": "Réviser",
    "gift-card": "Carte-cadeau",
    "no-product-group-transaction":"Au minimum un groupe de produit doit avoir un montant pour créer une transaction.",
    "no-funds-message": "Il n'y a pas de fonds disponibles sur cette carte.",
    "product-group-amount-isnan": "Le montant doit être un nombre.",
    "card-is-disabled": "La carte est désactivée.",
    "beneficiary-card-last-usage": "Dernier usage : {date}",
    "subscriptions-title": "Abonnements du participant-e",
    "amount-received-each-payment": "→ Montant(s) reçu(s) par cette carte à chaque versement : <b>{amountByPayment}</b>",
    "remaining-payment-on-card": "→ <b>{remainingPayment}</b> versement(s) restant(s) sur cette carte",
    "payment-dates": "→ Dates des versements : <b>Versements le {paymentMoment} du mois entre le {startDate} et le {endDate}</b>",
    "payment-moment-first-day-of-the-month": "1er",
    "payment-moment-fifteenth-day-of-the-month": "15",
    "payment-moment-first-and-fifteenth-day-of-the-month": "1er et le 15",
    "expiration-date": "→ Expiration des fonds <b>{definition}</b>",
    "expiration-date-next-payment": "à la prochaine date de paiement",
    "expiration-date-specific-date": "le {date}",
    "expiration-date-number-of-days": "{count} jours après le premier achat fait avec la carte, ou au plus tard le {date}"
	}
}
</i18n>

<template>
  <div v-if="userType === USER_TYPE_ORGANIZATIONMANAGER && beneficiary">
    <template v-if="currentStep === 0">
      <div>
        <!-- eslint-disable-next-line @intlify/vue-i18n/no-raw-text -->
        <span class="font-bold">{{ beneficiary.firstname }} {{ beneficiary.lastname }} - {{ beneficiary.id1 }}</span> -
        {{ t("beneficiary-card-last-usage", { date: formatDate(dateUtc(card.lastTransactionDate), regularFormat) }) }}
      </div>
      <h3 class="mb-2 mt-4">{{ t("subscriptions-title") }}</h3>
      <ul class="inline-flex flex-col justify-start items-start gap-y-1 mb-4 max-w-full">
        <li class="mb-4" v-for="subscription in beneficiary.beneficiarySubscriptions" :key="subscription.subscription.id">
          <b>{{ subscription.subscription.name }}</b>
          <!-- eslint-disable-next-line vue/no-v-html @intlify/vue-i18n/no-v-html -->
          <div v-html="t('amount-received-each-payment', { amountByPayment: amountByPayment(subscription) })"></div>
          <!-- eslint-disable-next-line vue/no-v-html @intlify/vue-i18n/no-v-html -->
          <div v-html="t('remaining-payment-on-card', { remainingPayment: subscription.subscription.paymentRemaining })"></div>
          <!-- eslint-disable-next-line vue/no-v-html @intlify/vue-i18n/no-v-html -->
          <div v-html="getPaymentDates(subscription.subscription)"></div>
          <!-- eslint-disable-next-line vue/no-v-html @intlify/vue-i18n/no-v-html -->
          <div v-html="getExpirationDate(subscription.subscription)"></div>
        </li>
      </ul>
    </template>
  </div>
  <p v-else-if="card && market" class="text-1">
    {{
      t("card-selected", {
        marketName: market.name,
        cardProgramCardId: card.programCardId
      })
    }}
  </p>
  <p v-if="card && card.isDisabled" class="text-red-500 font-bold">{{ t("card-is-disabled") }}</p>
  <Form
    v-if="funds"
    v-slot="{ isSubmitting, errors: formErrors }"
    :initial-values="initialValues"
    :validation-schema="currentSchema"
    keep-values
    @submit="nextStep">
    <div v-if="funds.length > 0">
      <PfForm
        v-if="currentStep === 0"
        has-footer
        footer-alt-style
        can-cancel
        :disable-submit="Object.keys(formErrors).length > 0 || card.isDisabled"
        :submit-label="t('create-transaction')"
        :cancel-label="t('cancel')"
        :processing="isSubmitting"
        @cancel="goToAdminTransaction">
        <PfFormSection>
          <FieldArray v-slot="{ fields }" key-path="id" name="funds">
            <div
              v-for="(field, idx) in fields"
              :key="field.key"
              :class="getIsGiftCard(funds[idx].fund.productGroup.name) ? 'pt-6 border-t border-grey-100' : ''">
              <div
                class="p-4 pt-2.5 rounded-lg"
                :class="[
                  getColorBgClass(funds[idx].fund.productGroup.color),
                  getIsGiftCard(funds[idx].fund.productGroup.name) ? 'bg-diagonal-pattern' : 'dark'
                ]">
                <Field
                  :id="`funds[${idx}].amount`"
                  v-slot="{ field: inputField, errors: fieldErrors }"
                  :name="`funds[${idx}].amount`">
                  <PfFormInputText
                    :id="`funds[${idx}].amount`"
                    class="grow"
                    v-bind="inputField"
                    :label="fundLabel(funds[idx].fund, 'amount-label')"
                    :after-label="fundAfterLabel(funds[idx].fund, 'amount-after-label')"
                    :placeholder="t('amount-placeholder', { amount: 18.43 })"
                    :errors="fieldErrors"
                    input-mode="decimal">
                    <template #trailingIcon>
                      <UiDollarSign :errors="fieldErrors" />
                    </template>
                  </PfFormInputText>
                </Field>
              </div>
            </div>
          </FieldArray>
        </PfFormSection>
      </PfForm>
      <PfForm
        v-else
        has-footer
        footer-alt-style
        :submit-label="t('confirm')"
        :cancel-label="t('edit')"
        can-cancel
        :processing="isSubmitting"
        @cancel="prevStep">
        <PfFormSection>
          <p class="text-h1 font-bold text-primary-700 mb-0">
            {{ t("amount-charged") }}
            <!-- eslint-disable-next-line  @intlify/vue-i18n/no-raw-text -->
            <span class="text-primary-500">{{ getTotalTransactionAmount() }}.</span>
          </p>
          <div>
            <p class="mb-2 text-p3">{{ t("product-groups") }}</p>
            <ul class="mb-0 w-full">
              <li
                v-for="item in funds"
                :key="item.id"
                class="mb-2 last:mb-0 text-p2"
                :class="getIsGiftCard(item.fund.productGroup.name) ? 'mt-6 pt-4 border-t border-grey-100' : 'dark'">
                <div
                  class="flex items-center w-full rounded-md py-1 px-2 text-primary-900 dark:text-white"
                  :class="[
                    getColorBgClass(item.fund.productGroup.color),
                    getIsGiftCard(item.fund.productGroup.name) ? 'bg-diagonal-pattern' : ''
                  ]">
                  <span class="w-1/2 font-bold">
                    {{ fundLabel(item.fund, "confirmation-amount-label") }}
                  </span>
                  <span class="w-1/2 text-right">
                    <span class="ml-2">{{ getMoneyFormat(-Math.abs(item.amount)) }}</span>
                  </span>
                </div>
              </li>
            </ul>
          </div>
        </PfFormSection>
      </PfForm>
    </div>
    <div v-else>
      <PfForm
        can-cancel
        :disable-submit="true"
        has-footer
        footer-alt-style
        :submit-label="t('create-transaction')"
        :cancel-label="t('cancel')"
        @cancel="goToAdminTransaction">
        <div>{{ t("no-funds-message") }}</div>
      </PfForm>
    </div>
  </Form>
</template>

<script setup>
import { defineProps, defineEmits, ref, computed } from "vue";
import { useI18n } from "vue-i18n";
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { number, object, lazy, array, string } from "yup";
import { FieldArray } from "vee-validate";
import { storeToRefs } from "pinia";

import { PRODUCT_GROUP_LOYALTY, TRANSACTION_STEPS_COMPLETE } from "@/lib/consts/enums";
import { USER_TYPE_ORGANIZATIONMANAGER } from "@/lib/consts/enums";
import {
  FIRST_DAY_OF_THE_MONTH,
  FIFTEENTH_DAY_OF_THE_MONTH,
  FIRST_AND_FIFTEENTH_DAY_OF_THE_MONTH
} from "@/lib/consts/monthly-payment-moment";

import { getMoneyFormat } from "@/lib/helpers/money";
import { usePageTitle } from "@/lib/helpers/page-title";
import { getColorBgClass } from "@/lib/helpers/products-color";
import { formatDate, dateUtc, regularFormat, textualFormat } from "@/lib/helpers/date";

import { useAuthStore } from "@/lib/store/auth";
import { useNotificationsStore } from "@/lib/store/notifications";

const { userType } = storeToRefs(useAuthStore());
const { t } = useI18n();
const { addError } = useNotificationsStore();

const audio = new Audio(require("@/assets/audio/confirmation.mp3"));
usePageTitle(t("title"));
const currentStep = ref(0);
const beneficiary = ref(null);

const props = defineProps({
  cardId: {
    type: String,
    required: true
  },
  marketId: {
    type: String,
    required: true
  }
});

const emit = defineEmits(["onUpdateStep", "onUpdateLoadingState", "onCloseModal"]);

const { result } = useQuery(
  gql`
    query Card($id: ID!) {
      card(id: $id) {
        id
        isDisabled
        totalFund
        programCardId
        lastTransactionDate
        beneficiary {
          id
          firstname
          lastname
          id1
          ... on BeneficiaryGraphType {
            beneficiarySubscriptions {
              subscription {
                id
                name
                paymentRemaining
                monthlyPaymentMoment
                endDate
                startDate
                isFundsAccumulable
                numberDaysUntilFundsExpire
                fundsExpirationDate
                types {
                  id
                  amount
                  beneficiaryType {
                    id
                  }
                }
              }
              beneficiaryType {
                id
              }
            }
          }
        }
        funds {
          id
          amount
          productGroup {
            id
            name
            orderOfAppearance
            color
          }
        }
        loyaltyFund {
          id
          amount
          productGroup {
            id
            name
            orderOfAppearance
            color
          }
        }
      }
    }
  `,
  {
    id: props.cardId
  }
);
const card = useResult(result, null, (data) => {
  beneficiary.value = data.card.beneficiary;
  return data.card;
});

const funds = useResult(result, null, (data) => {
  var results = data.card.funds.map((x) => {
    return {
      amount: 0,
      id: x.id,
      fund: x
    };
  });

  if (data.card.loyaltyFund !== null) {
    results.push({
      amount: 0,
      id: data.card.loyaltyFund.id,
      fund: data.card.loyaltyFund
    });
  }

  initialValues.funds = results.map((x) => ({ amount: "", fundId: x.id }));
  return results;
});

const { result: resultMarket } = useQuery(
  gql`
    query Market($id: ID!) {
      market(id: $id) {
        id
        name
      }
    }
  `,
  {
    id: props.marketId
  }
);
const market = useResult(resultMarket, null, (data) => {
  return data.market;
});

const { mutate: createTransaction } = useMutation(
  gql`
    mutation CreateTransaction($input: CreateTransactionInput!) {
      createTransaction(input: $input) {
        transaction {
          id
          amount
          transactionByProductGroups {
            id
            amount
            productGroup {
              id
              name
              color
              orderOfAppearance
            }
          }
        }
      }
    }
  `
);

const initialValues = {
  funds: []
};

// Form validation & steps management
const validationSchemas = computed(() => {
  return [
    object({
      funds: array().of(
        object({
          amount: lazy((value) => {
            if (value === undefined || value === "" || value === null) return string().notRequired();
            value = value.toString().replace(/,/, ".");
            if (isNaN(value)) {
              return string().test({
                name: "productGroupAmountMustBeNumber",
                exclusive: false,
                params: {},
                message: t("product-group-amount-isnan"),
                test: function () {
                  return false;
                }
              });
            }
            return number()
              .label(t("amount-validation-label"))
              .transform((_, value) => {
                return +value.toString().replace(/,/, ".");
              })
              .test({
                name: "maxProductGroupAmount",
                exclusive: false,
                params: {},
                message: t("product-group-fund-not-enought"),
                test: function (value, context) {
                  var fundId = context.parent.fundId;
                  var fund = funds.value.find((x) => x.id === fundId).fund;
                  if (fund) return value <= parseFloat(fund.amount);
                  return false;
                }
              })
              .min(0.01)
              .required();
          })
        })
      )
    })
  ];
});

const currentSchema = computed(() => {
  return validationSchemas.value[currentStep.value];
});

const getPaymentDates = (subscription) => {
  let paymentMoment = "";
  switch (subscription.monthlyPaymentMoment) {
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
    startDate: formatDate(new Date(subscription.startDate), textualFormat),
    endDate: formatDate(new Date(subscription.endDate), textualFormat)
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

function fundLabel(fund, key) {
  let productGroupName = fund.productGroup.name;
  if (productGroupName === PRODUCT_GROUP_LOYALTY) {
    productGroupName = t("gift-card");
  }

  return t(key, { productGroupName });
}

function fundAfterLabel(fund, key) {
  return t(key, { amountAvailable: getMoneyFormat(fund.amount) });
}

function getTotalTransactionAmount() {
  let result = 0;
  for (var i = 0; i < funds.value.length; i++) {
    result += parseFloat(funds.value[i].amount);
  }

  return getMoneyFormat(result);
}

function getIsGiftCard(productGroupName) {
  if (productGroupName === PRODUCT_GROUP_LOYALTY) return true;
  else return false;
}

function nextStep(values) {
  let haveAtLeastOneProductGroup = false;
  for (var i = 0; i < values.funds.length; i++) {
    const amount = values.funds[i].amount;
    if (amount !== undefined && amount !== null && amount !== "") {
      haveAtLeastOneProductGroup = true;
      // Convert numbers with comma as decimal separator
      funds.value[i].amount = amount.replace(/,/, ".");
    }
  }

  if (!haveAtLeastOneProductGroup) {
    addError(t("no-product-group-transaction"));
    return;
  }

  if (currentStep.value === 1) {
    onSubmit(values);
    return;
  }
  currentStep.value++;
}

function prevStep() {
  if (currentStep.value <= 0) {
    return;
  }

  currentStep.value--;
}

function amountByPayment(subscription) {
  return getMoneyFormat(subscription.subscription.types.map((x) => x.amount).reduce((total, arg) => total + arg, 0));
}

async function onSubmit() {
  emit("onUpdateLoadingState", true);
  var result = await createTransaction({
    input: {
      transactions: funds.value
        .filter((x) => parseFloat(x.amount) > 0)
        .map((x) => ({ amount: parseFloat(x.amount), productGroupId: x.fund.productGroup.id })),
      cardId: props.cardId,
      marketId: props.marketId
    }
  });

  audio.play();
  setTimeout(() => {
    emit("onUpdateStep", TRANSACTION_STEPS_COMPLETE, { transactionId: result.data.createTransaction.transaction.id });
  }, 200);
}

const goToAdminTransaction = () => {
  emit("onCloseModal");
};
</script>
