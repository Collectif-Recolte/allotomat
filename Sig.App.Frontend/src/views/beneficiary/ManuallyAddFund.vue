<i18n>
{
	"en": {
		"title": "Manually add funds",
		"select-subscription-label": "Subscription",
		"cancel": "Cancel",
		"add-amount": "Add amount",
		"amount-label": "Amount",
		"amount-placeholder": "Ex. {amount}",
		"subscription-label": "{name} - {monthlyPaymentMoment}",
		"manually-add-fund-success-notification": "{name} has received {amount} on their card. The funds will expire on {expirationDate}.",
    "select-product-group-label": "Product group",
    "available-fund-cant-be-less-than-zero-error-notification": "The card balance cannot be negative.",
    "card-current-balance": "Current balance: {amount}",
    "select-subscription-description": "Subscriptions that have expired or subscriptions that are not assigned to the participant are not displayed and selectable for manual fund addition.",
    "select-expiration-date-label": "Expiration date",
    "first-day-of-the-month": "payment on the 1st day of the month",
    "first-and-fifteenth-day-of-the-month": "payment on the 1st and 15th day of the month",
    "fifteenth-day-of-the-month": "payment on the 15th day of the month"
	},
	"fr": {
		"title": "Ajouter manuellement des fonds",
		"select-subscription-label": "Période d'abonnement",
		"cancel": "Annuler",
		"add-amount": "Ajouter le montant",
		"amount-label": "Montant",
		"amount-placeholder": "Ex. {amount}",
		"subscription-label": "{name} - {monthlyPaymentMoment}",
		"manually-add-fund-success-notification": "{name} a reçu {amount} sur sa carte. Les fonds vont expirer le {expirationDate}.",
    "select-product-group-label": "Groupe de produits",
    "available-fund-cant-be-less-than-zero-error-notification": "Le solde de la carte ne peut pas être négatif.",
    "card-current-balance": "Solde actuel: {amount}",
    "select-subscription-description": "Les abonnements qui ont expiré ou les abonnements qui ne sont pas assignés au participant ne sont pas affichés et sélectionnables pour l'ajout manuel de fonds.",
    "select-expiration-date-label": "Date d'expiration",
    "first-day-of-the-month": "versement le 1er jour du mois",
    "first-and-fifteenth-day-of-the-month": "versement le 1er et 15ème jour du mois",
    "fifteenth-day-of-the-month": "versement le 15ème jour du mois"
	}
}
</i18n>

<template>
  <UiDialogModal
    v-if="!loading"
    v-slot="{ closeModal }"
    :title="t('title')"
    :has-footer="false"
    :return-route="{ name: URL_BENEFICIARY_ADMIN }">
    <Form v-slot="{ isSubmitting, errors: formErrors }" :validation-schema="validationSchema" @submit="onSubmit">
      <PfForm
        has-footer
        can-cancel
        :disable-submit="Object.keys(formErrors).length > 0"
        :submit-label="t('add-amount')"
        :cancel-label="t('cancel')"
        :processing="isSubmitting"
        @cancel="closeModal">
        <PfFormSection>
          <Field
            v-if="!project.administrationSubscriptionsOffPlatform"
            v-slot="{ field, errors: fieldErrors }"
            name="subscription">
            <PfFormInputSelect
              id="subscription"
              v-bind="field"
              :label="t('select-subscription-label')"
              :options="subscriptionOptions"
              :description="t('select-subscription-description')"
              :errors="fieldErrors"
              @input="onSubscriptionSelected" />
          </Field>
          <Field v-slot="{ field, errors: fieldErrors }" name="expirationDate">
            <PfFormInputSelect
              id="expirationDate"
              v-bind="field"
              :disabled="expirationDateOptions.length === 0"
              :label="t('select-expiration-date-label')"
              :options="expirationDateOptions"
              :errors="fieldErrors" />
          </Field>
          <Field v-slot="{ field, errors: fieldErrors }" name="productGroup">
            <PfFormInputSelect
              id="productGroup"
              v-bind="field"
              :disabled="productGroupOptions.length === 0"
              :label="t('select-product-group-label')"
              :options="productGroupOptions"
              :errors="fieldErrors" />
          </Field>
        </PfFormSection>
        <Field v-slot="{ field, errors: fieldErrors }" name="amount">
          <PfFormInputText
            id="amount"
            v-bind="field"
            :label="t('amount-label')"
            :placeholder="t('amount-placeholder', { amount: getMoneyFormat(18.43) })"
            :errors="fieldErrors"
            input-type="number"
            min="0">
            <template #trailingIcon>
              <UiDollarSign :errors="fieldErrors" />
            </template>
          </PfFormInputText>
        </Field>
        <span class="py-8 text-sm">{{ t("card-current-balance", { amount: availableCardFund }) }}</span>
      </PfForm>
    </Form>
  </UiDialogModal>
</template>

<script setup>
import { computed, ref } from "vue";
import { useI18n } from "vue-i18n";
import gql from "graphql-tag";
import { useQuery, useResult, useMutation } from "@vue/apollo-composable";
import { useRoute, useRouter } from "vue-router";
import { object, string, number, lazy, mixed } from "yup";
import { useGraphQLErrorMessages } from "@/lib/helpers/error-handler";

import { formatDate, dateUtc, textualFormat, serverFormat, formattedDate } from "@/lib/helpers/date";
import { getMoneyFormat } from "@/lib/helpers/money";
import { subscriptionName } from "@/lib/helpers/subscription";

import { useOrganizationStore } from "@/lib/store/organization";
import { useNotificationsStore } from "@/lib/store/notifications";

import {
  FIRST_DAY_OF_THE_MONTH,
  FIRST_AND_FIFTEENTH_DAY_OF_THE_MONTH,
  FIFTEENTH_DAY_OF_THE_MONTH
} from "@/lib/consts/monthly-payment-moment";
import { URL_BENEFICIARY_ADMIN } from "@/lib/consts/urls";
import { PRODUCT_GROUP_LOYALTY } from "@/lib/consts/enums";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { addSuccess } = useNotificationsStore();
const { currentOrganization } = useOrganizationStore();

const selectedSubscription = ref("");

// Configure les messages en lien avec les erreurs graphql susceptibles d'être lancées par ce composant
useGraphQLErrorMessages({
  // Ce code est lancé quand le montant est plus petit que 0
  AVAILABLE_FUND_CANT_BE_LESS_THAN_ZERO: () => {
    return t("available-fund-cant-be-less-than-zero-error-notification");
  }
});

const { result: resultBeneficiary } = useQuery(
  gql`
    query Beneficiary($id: ID!) {
      beneficiary(id: $id) {
        id
        card {
          id
          totalFund
        }
        ... on BeneficiaryGraphType {
          beneficiarySubscriptions {
            subscription {
              id
            }
          }
        }
        ... on OffPlatformBeneficiaryGraphType {
          beneficiarySubscriptions {
            subscription {
              id
            }
          }
        }
      }
    }
  `,
  {
    id: route.params.beneficiaryId
  }
);
const beneficiary = useResult(resultBeneficiary);

const { result: resultOrganization, loading } = useQuery(
  gql`
    query Organization($id: ID!) {
      organization(id: $id) {
        id
        project {
          id
          administrationSubscriptionsOffPlatform
          productGroups {
            id
            name
            color
            orderOfAppearance
          }
        }
        budgetAllowances {
          id
          availableFund
          subscription {
            id
            isArchived
            name
            fundsExpirationDate
            monthlyPaymentMoment
            isFundsAccumulable
            types {
              id
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
    }
  `,
  {
    id: currentOrganization
  },
  () => ({
    enabled: currentOrganization !== null
  })
);

const budgetAllowances = useResult(resultOrganization, null, (data) => {
  return data.organization.budgetAllowances;
});

const project = useResult(resultOrganization, null, (data) => {
  return data.organization.project;
});

const subscriptionOptions = useResult(resultOrganization, null, (data) => {
  const availableSubscriptionIds = beneficiary.value.beneficiarySubscriptions.map((x) => x.subscription.id);
  return data.organization.budgetAllowances
    .filter(
      (x) =>
        availableSubscriptionIds.includes(x.subscription.id) &&
        (!x.subscription.isFundsAccumulable || dateUtc(x.subscription.fundsExpirationDate) > Date.now())
    )
    .map((x) => {
      let label = "";

      let monthlyPaymentMoment = "";
      switch (x.subscription.monthlyPaymentMoment) {
        case FIRST_DAY_OF_THE_MONTH:
          monthlyPaymentMoment = t("first-day-of-the-month");
          break;
        case FIRST_AND_FIFTEENTH_DAY_OF_THE_MONTH:
          monthlyPaymentMoment = t("first-and-fifteenth-day-of-the-month");
          break;
        case FIFTEENTH_DAY_OF_THE_MONTH:
          monthlyPaymentMoment = t("fifteenth-day-of-the-month");
          break;
      }
      label = t("subscription-label", {
        name: subscriptionName(x.subscription),
        monthlyPaymentMoment
      });

      return {
        label: label,
        value: x.subscription.id
      };
    })
    .reduce(function (a, b) {
      return a.concat(b);
    }, []);
});

const subscriptions = useResult(resultOrganization, null, (data) => {
  return data.organization.budgetAllowances
    .filter((x) => dateUtc(x.subscription.fundsExpirationDate) > Date.now() || !x.subscription.isFundsAccumulable)
    .map((x) => x.subscription);
});

const { mutate: createManuallyAddingFundTransaction } = useMutation(
  gql`
    mutation CreateManuallyAddingFundTransaction($input: CreateManuallyAddingFundTransactionInput!) {
      createManuallyAddingFundTransaction(input: $input) {
        transaction {
          id
          amount
          expirationDate
          card {
            id
            beneficiary {
              id
              firstname
              lastname
            }
          }
        }
      }
    }
  `
);

async function onSubmit({ amount, expirationDate, subscription, productGroup }) {
  var result = await createManuallyAddingFundTransaction({
    input: {
      amount: parseFloat(amount),
      expirationDate: formatDate(expirationDate, serverFormat),
      subscriptionId: subscription,
      beneficiaryId: route.params.beneficiaryId,
      productGroupId: productGroup
    }
  });
  router.push({ name: URL_BENEFICIARY_ADMIN });

  addSuccess(
    t("manually-add-fund-success-notification", {
      name: `${result.data.createManuallyAddingFundTransaction.transaction.card.beneficiary.firstname} ${result.data.createManuallyAddingFundTransaction.transaction.card.beneficiary.lastname}`,
      amount: getMoneyFormat(parseFloat(amount)),
      expirationDate: formatDate(
        dateUtc(result.data.createManuallyAddingFundTransaction.transaction.expirationDate),
        textualFormat
      )
    })
  );
}

function onSubscriptionSelected(e) {
  selectedSubscription.value = e;
}

const validationSchema = computed(() =>
  object({
    subscription: lazy(() => {
      if (project.value !== undefined && project.value.administrationSubscriptionsOffPlatform) {
        return mixed().test({
          test: function () {
            return true;
          }
        });
      }

      return string().label(t("select-subscription-label")).required();
    }),
    expirationDate: string().label(t("select-expiration-date-label")).required(),
    productGroup: string().label(t("select-product-group-label")).required(),
    amount: lazy((value) => {
      if (value === "") {
        return string().label(t("amount-label")).required();
      }

      return number().label(t("amount-label")).required().max(maxBudgetAllowance.value);
    })
  })
);

const getNextPaymentDates = (currentDate, paymentMoment) => {
  const results = [];
  const addDate = (date) => {
    results.push({
      label: formatDate(date, textualFormat),
      value: new Date(date.getTime())
    });
  };

  const nextDate = new Date(currentDate.getTime());

  switch (paymentMoment) {
    case FIRST_DAY_OF_THE_MONTH:
      nextDate.setMonth(nextDate.getMonth() + 1, 1);
      addDate(nextDate);
      nextDate.setMonth(nextDate.getMonth() + 1);
      addDate(nextDate);
      break;
    case FIRST_AND_FIFTEENTH_DAY_OF_THE_MONTH:
      if (currentDate.getDate() >= 15) {
        nextDate.setMonth(nextDate.getMonth() + 1, 1);
        addDate(nextDate);
        nextDate.setDate(15);
        addDate(nextDate);
      } else {
        nextDate.setDate(15);
        addDate(nextDate);
        nextDate.setMonth(nextDate.getMonth() + 1, 1);
        addDate(nextDate);
      }
      break;
    default:
      if (currentDate.getDate() >= 15) {
        nextDate.setMonth(nextDate.getMonth() + 1);
      }
      nextDate.setDate(15);
      addDate(nextDate);
      nextDate.setMonth(nextDate.getMonth() + 1);
      addDate(nextDate);
  }

  return results;
};

const expirationDateOptions = computed(() => {
  if (selectedSubscription.value == "") return [];

  var subscription = subscriptions.value.find((x) => x.id === selectedSubscription.value);

  if (subscription.isFundsAccumulable) {
    return [
      {
        label: formatDate(dateUtc(subscription.fundsExpirationDate), textualFormat),
        value: formattedDate(subscription.fundsExpirationDate)
      }
    ];
  }

  let currentDate = new Date();

  return getNextPaymentDates(currentDate, subscription.monthlyPaymentMoment);
});

const productGroupOptions = computed(() => {
  if (project.value === undefined) {
    return [];
  }
  if (project.value.administrationSubscriptionsOffPlatform) {
    return project.value.productGroups
      .filter((x) => x.name !== PRODUCT_GROUP_LOYALTY)
      .map((x) => {
        return {
          label: x.name,
          value: x.id
        };
      });
  }
  if (selectedSubscription.value == "") return [];

  var subscription = subscriptions.value.find((x) => x.id === selectedSubscription.value);

  var results = subscription.types.map((x) => {
    return {
      label: x.productGroup.name,
      value: x.productGroup.id
    };
  });

  return [...new Map(results.map((item) => [item["value"], item])).values()];
});

const maxBudgetAllowance = computed(() =>
  selectedSubscription.value === ""
    ? Number.MAX_SAFE_INTEGER
    : budgetAllowances.value.find((x) => x.subscription.id === selectedSubscription.value).availableFund
);

const availableCardFund = computed(() => {
  if (
    beneficiary.value === null ||
    beneficiary.value === undefined ||
    beneficiary.value.card === null ||
    beneficiary.value.card === undefined
  ) {
    return 0;
  }
  return getMoneyFormat(beneficiary.value.card.totalFund);
});
</script>
