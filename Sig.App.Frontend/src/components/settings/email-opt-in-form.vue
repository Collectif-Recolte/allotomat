<i18n>
  {
    "en": {
      "submit": "Submit",
      "created-card-csv-email": "Copy of generated card data (.csv file)",
      "monthly-balance-report-email": "Monthly reconciliation report (amounts owing to merchants)",
      "monthly-card-balance-report-email": "Monthly card status report",
      "subscription-expiration-email": "Subscription expiration email",
      "change-email-opt-in-standby-notification": "Your email preferences have been updated",
      "january": "January",
      "february": "February",
      "march": "March",
      "april": "April",
      "may": "May",
      "june": "June",
      "july": "July",
      "august": "August",
      "september": "September",
      "october": "October",
      "november": "November",
      "december": "December"
    },
    "fr": {
      "submit": "Mettre à jour",
      "created-card-csv-email": "Copie des données de cartes générées (fichier .csv)",
      "monthly-balance-report-email": "Rapport de réconciliation mensuel (montants dûs aux commerces)",
      "monthly-card-balance-report-email": "Rapport de solde mensuel des cartes",
      "subscription-expiration-email": "Rappel d’expiration d’abonnement",
      "change-email-opt-in-standby-notification": "Vos préférences d'email ont été mises à jour",
      "january": "Janvier",
      "february": "Février",
      "march": "Mars",
      "april": "Avril",
      "may": "Mai",
      "june": "Juin",
      "july": "Juillet",
      "august": "Août",
      "september": "Septembre",
      "october": "Octobre",
      "november": "Novembre",
      "december": "Décembre"
    }
  }
</i18n>

<template>
  <Form
    v-slot="{ isSubmitting }"
    :validation-schema="validationSchemaEmail"
    :initial-values="initialFormValues"
    @submit="onSubmitChangeEmailOptIn">
    <PfForm has-footer :submit-label="t('submit')" :loading-label="t('loading')" :processing="isSubmitting">
      <PfFormSection>
        <p class="mb-0 mt-0">{{ t("monthly-balance-report-email") }}</p>
        <div class="inline-flex flex-col gap-x-4 mt-0">
          <Field v-slot="{ field }" name="monthlyBalanceReportEmailJanuary">
            <PfFormInputCheckbox
              id="monthlyBalanceReportEmailJanuary"
              v-bind="field"
              :checked="field.value"
              :label="t('january')" />
          </Field>
          <Field v-slot="{ field }" name="monthlyBalanceReportEmailFebruary">
            <PfFormInputCheckbox
              id="monthlyBalanceReportEmailFebruary"
              v-bind="field"
              :checked="field.value"
              :label="t('february')" />
          </Field>
          <Field v-slot="{ field }" name="monthlyBalanceReportEmailMarch">
            <PfFormInputCheckbox id="monthlyBalanceReportEmailMarch" v-bind="field" :checked="field.value" :label="t('march')" />
          </Field>
          <Field v-slot="{ field }" name="monthlyBalanceReportEmailApril">
            <PfFormInputCheckbox id="monthlyBalanceReportEmailApril" v-bind="field" :checked="field.value" :label="t('april')" />
          </Field>
          <Field v-slot="{ field }" name="monthlyBalanceReportEmailMay">
            <PfFormInputCheckbox id="monthlyBalanceReportEmailMay" v-bind="field" :checked="field.value" :label="t('may')" />
          </Field>
          <Field v-slot="{ field }" name="monthlyBalanceReportEmailJune">
            <PfFormInputCheckbox id="monthlyBalanceReportEmailJune" v-bind="field" :checked="field.value" :label="t('june')" />
          </Field>
          <Field v-slot="{ field }" name="monthlyBalanceReportEmailJuly">
            <PfFormInputCheckbox id="monthlyBalanceReportEmailJuly" v-bind="field" :checked="field.value" :label="t('july')" />
          </Field>
          <Field v-slot="{ field }" name="monthlyBalanceReportEmailAugust">
            <PfFormInputCheckbox
              id="monthlyBalanceReportEmailAugust"
              v-bind="field"
              :checked="field.value"
              :label="t('august')" />
          </Field>
          <Field v-slot="{ field }" name="monthlyBalanceReportEmailSeptember">
            <PfFormInputCheckbox
              id="monthlyBalanceReportEmailSeptember"
              v-bind="field"
              :checked="field.value"
              :label="t('september')" />
          </Field>
          <Field v-slot="{ field }" name="monthlyBalanceReportEmailOctober">
            <PfFormInputCheckbox
              id="monthlyBalanceReportEmailOctober"
              v-bind="field"
              :checked="field.value"
              :label="t('october')" />
          </Field>
          <Field v-slot="{ field }" name="monthlyBalanceReportEmailNovember">
            <PfFormInputCheckbox
              id="monthlyBalanceReportEmailNovember"
              v-bind="field"
              :checked="field.value"
              :label="t('november')" />
          </Field>
          <Field v-slot="{ field }" name="monthlyBalanceReportEmailDecember">
            <PfFormInputCheckbox
              id="monthlyBalanceReportEmailDecember"
              v-bind="field"
              :checked="field.value"
              :label="t('december')" />
          </Field>
        </div>

        <p class="mb-0 mt-0">{{ t("monthly-card-balance-report-email") }}</p>
        <div class="inline-flex flex-col gap-x-4 mt-0">
          <Field v-slot="{ field }" name="monthlyCardBalanceReportEmailJanuary">
            <PfFormInputCheckbox
              id="monthlyCardBalanceReportEmailJanuary"
              v-bind="field"
              :checked="field.value"
              :label="t('january')" />
          </Field>
          <Field v-slot="{ field }" name="monthlyCardBalanceReportEmailFebruary">
            <PfFormInputCheckbox
              id="monthlyCardBalanceReportEmailFebruary"
              v-bind="field"
              :checked="field.value"
              :label="t('february')" />
          </Field>
          <Field v-slot="{ field }" name="monthlyCardBalanceReportEmailMarch">
            <PfFormInputCheckbox
              id="monthlyCardBalanceReportEmailMarch"
              v-bind="field"
              :checked="field.value"
              :label="t('march')" />
          </Field>
          <Field v-slot="{ field }" name="monthlyCardBalanceReportEmailApril">
            <PfFormInputCheckbox
              id="monthlyCardBalanceReportEmailApril"
              v-bind="field"
              :checked="field.value"
              :label="t('april')" />
          </Field>
          <Field v-slot="{ field }" name="monthlyCardBalanceReportEmailMay">
            <PfFormInputCheckbox id="monthlyCardBalanceReportEmailMay" v-bind="field" :checked="field.value" :label="t('may')" />
          </Field>
          <Field v-slot="{ field }" name="monthlyCardBalanceReportEmailJune">
            <PfFormInputCheckbox
              id="monthlyCardBalanceReportEmailJune"
              v-bind="field"
              :checked="field.value"
              :label="t('june')" />
          </Field>
          <Field v-slot="{ field }" name="monthlyCardBalanceReportEmailJuly">
            <PfFormInputCheckbox
              id="monthlyCardBalanceReportEmailJuly"
              v-bind="field"
              :checked="field.value"
              :label="t('july')" />
          </Field>
          <Field v-slot="{ field }" name="monthlyCardBalanceReportEmailAugust">
            <PfFormInputCheckbox
              id="monthlyCardBalanceReportEmailAugust"
              v-bind="field"
              :checked="field.value"
              :label="t('august')" />
          </Field>
          <Field v-slot="{ field }" name="monthlyCardBalanceReportEmailSeptember">
            <PfFormInputCheckbox
              id="monthlyCardBalanceReportEmailSeptember"
              v-bind="field"
              :checked="field.value"
              :label="t('september')" />
          </Field>
          <Field v-slot="{ field }" name="monthlyCardBalanceReportEmailOctober">
            <PfFormInputCheckbox
              id="monthlyCardBalanceReportEmailOctober"
              v-bind="field"
              :checked="field.value"
              :label="t('october')" />
          </Field>
          <Field v-slot="{ field }" name="monthlyCardBalanceReportEmailNovember">
            <PfFormInputCheckbox
              id="monthlyCardBalanceReportEmailNovember"
              v-bind="field"
              :checked="field.value"
              :label="t('november')" />
          </Field>
          <Field v-slot="{ field }" name="monthlyCardBalanceReportEmailDecember">
            <PfFormInputCheckbox
              id="monthlyCardBalanceReportEmailDecember"
              v-bind="field"
              :checked="field.value"
              :label="t('december')" />
          </Field>
        </div>

        <Field v-slot="{ field }" name="createdCardPdfEmail">
          <PfFormInputCheckbox
            id="createdCardPdfEmail"
            v-bind="field"
            :checked="field.value"
            :label="t('created-card-csv-email')" />
        </Field>

        <Field v-slot="{ field }" name="subscriptionExpirationEmail">
          <PfFormInputCheckbox
            id="subscriptionExpirationEmail"
            v-bind="field"
            :checked="field.value"
            :label="t('subscription-expiration-email')" />
        </Field>
      </PfFormSection>
    </PfForm>
  </Form>
</template>

<script setup>
import { defineProps, computed } from "vue";
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { useMutation } from "@vue/apollo-composable";
import { useRouter } from "vue-router";

import { URL_ROOT } from "@/lib/consts/urls";
import { useNotificationsStore } from "@/lib/store/notifications";

const { t } = useI18n();
const { addSuccess } = useNotificationsStore();
const router = useRouter();

const CreatedCardPdfEmail = "CreatedCardPdfEmail";
const SubscriptionExpirationEmail = "SubscriptionExpirationEmail";

const MonthlyCardBalanceReportEmailJanuary = "MonthlyCardBalanceReportEmailJanuary";
const MonthlyCardBalanceReportEmailFebruary = "MonthlyCardBalanceReportEmailFebruary";
const MonthlyCardBalanceReportEmailMarch = "MonthlyCardBalanceReportEmailMarch";
const MonthlyCardBalanceReportEmailApril = "MonthlyCardBalanceReportEmailApril";
const MonthlyCardBalanceReportEmailMay = "MonthlyCardBalanceReportEmailMay";
const MonthlyCardBalanceReportEmailJune = "MonthlyCardBalanceReportEmailJune";
const MonthlyCardBalanceReportEmailJuly = "MonthlyCardBalanceReportEmailJuly";
const MonthlyCardBalanceReportEmailAugust = "MonthlyCardBalanceReportEmailAugust";
const MonthlyCardBalanceReportEmailSeptember = "MonthlyCardBalanceReportEmailSeptember";
const MonthlyCardBalanceReportEmailOctober = "MonthlyCardBalanceReportEmailOctober";
const MonthlyCardBalanceReportEmailNovember = "MonthlyCardBalanceReportEmailNovember";
const MonthlyCardBalanceReportEmailDecember = "MonthlyCardBalanceReportEmailDecember";

const MonthlyBalanceReportEmailJanuary = "MonthlyBalanceReportEmailJanuary";
const MonthlyBalanceReportEmailFebruary = "MonthlyBalanceReportEmailFebruary";
const MonthlyBalanceReportEmailMarch = "MonthlyBalanceReportEmailMarch";
const MonthlyBalanceReportEmailApril = "MonthlyBalanceReportEmailApril";
const MonthlyBalanceReportEmailMay = "MonthlyBalanceReportEmailMay";
const MonthlyBalanceReportEmailJune = "MonthlyBalanceReportEmailJune";
const MonthlyBalanceReportEmailJuly = "MonthlyBalanceReportEmailJuly";
const MonthlyBalanceReportEmailAugust = "MonthlyBalanceReportEmailAugust";
const MonthlyBalanceReportEmailSeptember = "MonthlyBalanceReportEmailSeptember";
const MonthlyBalanceReportEmailOctober = "MonthlyBalanceReportEmailOctober";
const MonthlyBalanceReportEmailNovember = "MonthlyBalanceReportEmailNovember";
const MonthlyBalanceReportEmailDecember = "MonthlyBalanceReportEmailDecember";

const props = defineProps({
  user: {
    type: Object,
    required: true
  }
});

const initialFormValues = computed(() => {
  return {
    createdCardPdfEmail: props.user.emailOptIn.includes(CreatedCardPdfEmail),
    monthlyBalanceReportEmailJanuary: props.user.emailOptIn.includes(MonthlyBalanceReportEmailJanuary),
    monthlyBalanceReportEmailFebruary: props.user.emailOptIn.includes(MonthlyBalanceReportEmailFebruary),
    monthlyBalanceReportEmailMarch: props.user.emailOptIn.includes(MonthlyBalanceReportEmailMarch),
    monthlyBalanceReportEmailApril: props.user.emailOptIn.includes(MonthlyBalanceReportEmailApril),
    monthlyBalanceReportEmailMay: props.user.emailOptIn.includes(MonthlyBalanceReportEmailMay),
    monthlyBalanceReportEmailJune: props.user.emailOptIn.includes(MonthlyBalanceReportEmailJune),
    monthlyBalanceReportEmailJuly: props.user.emailOptIn.includes(MonthlyBalanceReportEmailJuly),
    monthlyBalanceReportEmailAugust: props.user.emailOptIn.includes(MonthlyBalanceReportEmailAugust),
    monthlyBalanceReportEmailSeptember: props.user.emailOptIn.includes(MonthlyBalanceReportEmailSeptember),
    monthlyBalanceReportEmailOctober: props.user.emailOptIn.includes(MonthlyBalanceReportEmailOctober),
    monthlyBalanceReportEmailNovember: props.user.emailOptIn.includes(MonthlyBalanceReportEmailNovember),
    monthlyBalanceReportEmailDecember: props.user.emailOptIn.includes(MonthlyBalanceReportEmailDecember),
    monthlyCardBalanceReportEmailJanuary: props.user.emailOptIn.includes(MonthlyCardBalanceReportEmailJanuary),
    monthlyCardBalanceReportEmailFebruary: props.user.emailOptIn.includes(MonthlyCardBalanceReportEmailFebruary),
    monthlyCardBalanceReportEmailMarch: props.user.emailOptIn.includes(MonthlyCardBalanceReportEmailMarch),
    monthlyCardBalanceReportEmailApril: props.user.emailOptIn.includes(MonthlyCardBalanceReportEmailApril),
    monthlyCardBalanceReportEmailMay: props.user.emailOptIn.includes(MonthlyCardBalanceReportEmailMay),
    monthlyCardBalanceReportEmailJune: props.user.emailOptIn.includes(MonthlyCardBalanceReportEmailJune),
    monthlyCardBalanceReportEmailJuly: props.user.emailOptIn.includes(MonthlyCardBalanceReportEmailJuly),
    monthlyCardBalanceReportEmailAugust: props.user.emailOptIn.includes(MonthlyCardBalanceReportEmailAugust),
    monthlyCardBalanceReportEmailSeptember: props.user.emailOptIn.includes(MonthlyCardBalanceReportEmailSeptember),
    monthlyCardBalanceReportEmailOctober: props.user.emailOptIn.includes(MonthlyCardBalanceReportEmailOctober),
    monthlyCardBalanceReportEmailNovember: props.user.emailOptIn.includes(MonthlyCardBalanceReportEmailNovember),
    monthlyCardBalanceReportEmailDecember: props.user.emailOptIn.includes(MonthlyCardBalanceReportEmailDecember),
    subscriptionExpirationEmail: props.user.emailOptIn.includes(SubscriptionExpirationEmail)
  };
});

const { mutate: changeEmailOptIn } = useMutation(
  gql`
    mutation ChangeEmailOptIn($input: ChangeEmailOptInInput!) {
      changeEmailOptIn(input: $input) {
        user {
          id
          emailOptIn
        }
      }
    }
  `
);

async function onSubmitChangeEmailOptIn(input) {
  const toPascalCase = (str) => str.charAt(0).toUpperCase() + str.slice(1);

  const result = Object.entries(input)
    .filter(([, value]) => value === true) // garde uniquement les "true"
    .map(([key]) => toPascalCase(key)) // transforme en PascalCase
    .join(";");

  await changeEmailOptIn({
    input: {
      emailOptIn: result
    }
  });

  addSuccess(t("change-email-opt-in-standby-notification"));
  router.push({ name: URL_ROOT });
}
</script>
