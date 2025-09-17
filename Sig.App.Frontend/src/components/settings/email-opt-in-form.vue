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
          <Field v-slot="{ field }" name="MONTHLY_BALANCE_REPORT_EMAIL_JANUARY">
            <PfFormInputCheckbox
              id="MONTHLY_BALANCE_REPORT_EMAIL_JANUARY"
              v-bind="field"
              :checked="field.value"
              :label="t('january')" />
          </Field>
          <Field v-slot="{ field }" name="MONTHLY_BALANCE_REPORT_EMAIL_FEBRUARY">
            <PfFormInputCheckbox
              id="MONTHLY_BALANCE_REPORT_EMAIL_FEBRUARY"
              v-bind="field"
              :checked="field.value"
              :label="t('february')" />
          </Field>
          <Field v-slot="{ field }" name="MONTHLY_BALANCE_REPORT_EMAIL_MARCH">
            <PfFormInputCheckbox
              id="MONTHLY_BALANCE_REPORT_EMAIL_MARCH"
              v-bind="field"
              :checked="field.value"
              :label="t('march')" />
          </Field>
          <Field v-slot="{ field }" name="MONTHLY_BALANCE_REPORT_EMAIL_APRIL">
            <PfFormInputCheckbox
              id="MONTHLY_BALANCE_REPORT_EMAIL_APRIL"
              v-bind="field"
              :checked="field.value"
              :label="t('april')" />
          </Field>
          <Field v-slot="{ field }" name="MONTHLY_BALANCE_REPORT_EMAIL_MAY">
            <PfFormInputCheckbox id="MONTHLY_BALANCE_REPORT_EMAIL_MAY" v-bind="field" :checked="field.value" :label="t('may')" />
          </Field>
          <Field v-slot="{ field }" name="MONTHLY_BALANCE_REPORT_EMAIL_JUNE">
            <PfFormInputCheckbox
              id="MONTHLY_BALANCE_REPORT_EMAIL_JUNE"
              v-bind="field"
              :checked="field.value"
              :label="t('june')" />
          </Field>
          <Field v-slot="{ field }" name="MONTHLY_BALANCE_REPORT_EMAIL_JULY">
            <PfFormInputCheckbox
              id="MONTHLY_BALANCE_REPORT_EMAIL_JULY"
              v-bind="field"
              :checked="field.value"
              :label="t('july')" />
          </Field>
          <Field v-slot="{ field }" name="MONTHLY_BALANCE_REPORT_EMAIL_AUGUST">
            <PfFormInputCheckbox
              id="MONTHLY_BALANCE_REPORT_EMAIL_AUGUST"
              v-bind="field"
              :checked="field.value"
              :label="t('august')" />
          </Field>
          <Field v-slot="{ field }" name="MONTHLY_BALANCE_REPORT_EMAIL_SEPTEMBER">
            <PfFormInputCheckbox
              id="MONTHLY_BALANCE_REPORT_EMAIL_SEPTEMBER"
              v-bind="field"
              :checked="field.value"
              :label="t('september')" />
          </Field>
          <Field v-slot="{ field }" name="MONTHLY_BALANCE_REPORT_EMAIL_OCTOBER">
            <PfFormInputCheckbox
              id="MONTHLY_BALANCE_REPORT_EMAIL_OCTOBER"
              v-bind="field"
              :checked="field.value"
              :label="t('october')" />
          </Field>
          <Field v-slot="{ field }" name="MONTHLY_BALANCE_REPORT_EMAIL_NOVEMBER">
            <PfFormInputCheckbox
              id="MONTHLY_BALANCE_REPORT_EMAIL_NOVEMBER"
              v-bind="field"
              :checked="field.value"
              :label="t('november')" />
          </Field>
          <Field v-slot="{ field }" name="MONTHLY_BALANCE_REPORT_EMAIL_DECEMBER">
            <PfFormInputCheckbox
              id="MONTHLY_BALANCE_REPORT_EMAIL_DECEMBER"
              v-bind="field"
              :checked="field.value"
              :label="t('december')" />
          </Field>
        </div>

        <p class="mb-0 mt-0">{{ t("monthly-card-balance-report-email") }}</p>
        <div class="inline-flex flex-col gap-x-4 mt-0">
          <Field v-slot="{ field }" name="MONTHLY_CARD_BALANCE_REPORT_EMAIL_JANUARY">
            <PfFormInputCheckbox
              id="MONTHLY_CARD_BALANCE_REPORT_EMAIL_JANUARY"
              v-bind="field"
              :checked="field.value"
              :label="t('january')" />
          </Field>
          <Field v-slot="{ field }" name="MONTHLY_CARD_BALANCE_REPORT_EMAIL_FEBRUARY">
            <PfFormInputCheckbox
              id="MONTHLY_CARD_BALANCE_REPORT_EMAIL_FEBRUARY"
              v-bind="field"
              :checked="field.value"
              :label="t('february')" />
          </Field>
          <Field v-slot="{ field }" name="MONTHLY_CARD_BALANCE_REPORT_EMAIL_MARCH">
            <PfFormInputCheckbox
              id="MONTHLY_CARD_BALANCE_REPORT_EMAIL_MARCH"
              v-bind="field"
              :checked="field.value"
              :label="t('march')" />
          </Field>
          <Field v-slot="{ field }" name="MONTHLY_CARD_BALANCE_REPORT_EMAIL_APRIL">
            <PfFormInputCheckbox
              id="MONTHLY_CARD_BALANCE_REPORT_EMAIL_APRIL"
              v-bind="field"
              :checked="field.value"
              :label="t('april')" />
          </Field>
          <Field v-slot="{ field }" name="MONTHLY_CARD_BALANCE_REPORT_EMAIL_MAY">
            <PfFormInputCheckbox
              id="MONTHLY_CARD_BALANCE_REPORT_EMAIL_MAY"
              v-bind="field"
              :checked="field.value"
              :label="t('may')" />
          </Field>
          <Field v-slot="{ field }" name="MONTHLY_CARD_BALANCE_REPORT_EMAIL_JUNE">
            <PfFormInputCheckbox
              id="MONTHLY_CARD_BALANCE_REPORT_EMAIL_JUNE"
              v-bind="field"
              :checked="field.value"
              :label="t('june')" />
          </Field>
          <Field v-slot="{ field }" name="MONTHLY_CARD_BALANCE_REPORT_EMAIL_JULY">
            <PfFormInputCheckbox
              id="MONTHLY_CARD_BALANCE_REPORT_EMAIL_JULY"
              v-bind="field"
              :checked="field.value"
              :label="t('july')" />
          </Field>
          <Field v-slot="{ field }" name="MONTHLY_CARD_BALANCE_REPORT_EMAIL_AUGUST">
            <PfFormInputCheckbox
              id="MONTHLY_CARD_BALANCE_REPORT_EMAIL_AUGUST"
              v-bind="field"
              :checked="field.value"
              :label="t('august')" />
          </Field>
          <Field v-slot="{ field }" name="MONTHLY_CARD_BALANCE_REPORT_EMAIL_SEPTEMBER">
            <PfFormInputCheckbox
              id="MONTHLY_CARD_BALANCE_REPORT_EMAIL_SEPTEMBER"
              v-bind="field"
              :checked="field.value"
              :label="t('september')" />
          </Field>
          <Field v-slot="{ field }" name="MONTHLY_CARD_BALANCE_REPORT_EMAIL_OCTOBER">
            <PfFormInputCheckbox
              id="MONTHLY_CARD_BALANCE_REPORT_EMAIL_OCTOBER"
              v-bind="field"
              :checked="field.value"
              :label="t('october')" />
          </Field>
          <Field v-slot="{ field }" name="MONTHLY_CARD_BALANCE_REPORT_EMAIL_NOVEMBER">
            <PfFormInputCheckbox
              id="MONTHLY_CARD_BALANCE_REPORT_EMAIL_NOVEMBER"
              v-bind="field"
              :checked="field.value"
              :label="t('november')" />
          </Field>
          <Field v-slot="{ field }" name="MONTHLY_CARD_BALANCE_REPORT_EMAIL_DECEMBER">
            <PfFormInputCheckbox
              id="MONTHLY_CARD_BALANCE_REPORT_EMAIL_DECEMBER"
              v-bind="field"
              :checked="field.value"
              :label="t('december')" />
          </Field>
        </div>

        <Field v-slot="{ field }" name="CREATED_CARD_PDF_EMAIL">
          <PfFormInputCheckbox
            id="CREATED_CARD_PDF_EMAIL"
            v-bind="field"
            :checked="field.value"
            :label="t('created-card-csv-email')" />
        </Field>

        <Field v-slot="{ field }" name="SUBSCRIPTION_EXPIRATION_EMAIL">
          <PfFormInputCheckbox
            id="SUBSCRIPTION_EXPIRATION_EMAIL"
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

const CreatedCardPdfEmail = "CREATED_CARD_PDF_EMAIL";
const SubscriptionExpirationEmail = "SUBSCRIPTION_EXPIRATION_EMAIL";

const MonthlyCardBalanceReportEmailJanuary = "MONTHLY_CARD_BALANCE_REPORT_EMAIL_JANUARY";
const MonthlyCardBalanceReportEmailFebruary = "MONTHLY_CARD_BALANCE_REPORT_EMAIL_FEBRUARY";
const MonthlyCardBalanceReportEmailMarch = "MONTHLY_CARD_BALANCE_REPORT_EMAIL_MARCH";
const MonthlyCardBalanceReportEmailApril = "MONTHLY_CARD_BALANCE_REPORT_EMAIL_APRIL";
const MonthlyCardBalanceReportEmailMay = "MONTHLY_CARD_BALANCE_REPORT_EMAIL_MAY";
const MonthlyCardBalanceReportEmailJune = "MONTHLY_CARD_BALANCE_REPORT_EMAIL_JUNE";
const MonthlyCardBalanceReportEmailJuly = "MONTHLY_CARD_BALANCE_REPORT_EMAIL_JULY";
const MonthlyCardBalanceReportEmailAugust = "MONTHLY_CARD_BALANCE_REPORT_EMAIL_AUGUST";
const MonthlyCardBalanceReportEmailSeptember = "MONTHLY_CARD_BALANCE_REPORT_EMAIL_SEPTEMBER";
const MonthlyCardBalanceReportEmailOctober = "MONTHLY_CARD_BALANCE_REPORT_EMAIL_OCTOBER";
const MonthlyCardBalanceReportEmailNovember = "MONTHLY_CARD_BALANCE_REPORT_EMAIL_NOVEMBER";
const MonthlyCardBalanceReportEmailDecember = "MONTHLY_CARD_BALANCE_REPORT_EMAIL_DECEMBER";

const MonthlyBalanceReportEmailJanuary = "MONTHLY_BALANCE_REPORT_EMAIL_JANUARY";
const MonthlyBalanceReportEmailFebruary = "MONTHLY_BALANCE_REPORT_EMAIL_FEBRUARY";
const MonthlyBalanceReportEmailMarch = "MONTHLY_BALANCE_REPORT_EMAIL_MARCH";
const MonthlyBalanceReportEmailApril = "MONTHLY_BALANCE_REPORT_EMAIL_APRIL";
const MonthlyBalanceReportEmailMay = "MONTHLY_BALANCE_REPORT_EMAIL_MAY";
const MonthlyBalanceReportEmailJune = "MONTHLY_BALANCE_REPORT_EMAIL_JUNE";
const MonthlyBalanceReportEmailJuly = "MONTHLY_BALANCE_REPORT_EMAIL_JULY";
const MonthlyBalanceReportEmailAugust = "MONTHLY_BALANCE_REPORT_EMAIL_AUGUST";
const MonthlyBalanceReportEmailSeptember = "MONTHLY_BALANCE_REPORT_EMAIL_SEPTEMBER";
const MonthlyBalanceReportEmailOctober = "MONTHLY_BALANCE_REPORT_EMAIL_OCTOBER";
const MonthlyBalanceReportEmailNovember = "MONTHLY_BALANCE_REPORT_EMAIL_NOVEMBER";
const MonthlyBalanceReportEmailDecember = "MONTHLY_BALANCE_REPORT_EMAIL_DECEMBER";

const props = defineProps({
  user: {
    type: Object,
    required: true
  }
});

const initialFormValues = computed(() => {
  return {
    CREATED_CARD_PDF_EMAIL: props.user.emailOptIn.includes(CreatedCardPdfEmail),
    MONTHLY_BALANCE_REPORT_EMAIL_JANUARY: props.user.emailOptIn.includes(MonthlyBalanceReportEmailJanuary),
    MONTHLY_BALANCE_REPORT_EMAIL_FEBRUARY: props.user.emailOptIn.includes(MonthlyBalanceReportEmailFebruary),
    MONTHLY_BALANCE_REPORT_EMAIL_MARCH: props.user.emailOptIn.includes(MonthlyBalanceReportEmailMarch),
    MONTHLY_BALANCE_REPORT_EMAIL_APRIL: props.user.emailOptIn.includes(MonthlyBalanceReportEmailApril),
    MONTHLY_BALANCE_REPORT_EMAIL_MAY: props.user.emailOptIn.includes(MonthlyBalanceReportEmailMay),
    MONTHLY_BALANCE_REPORT_EMAIL_JUNE: props.user.emailOptIn.includes(MonthlyBalanceReportEmailJune),
    MONTHLY_BALANCE_REPORT_EMAIL_JULY: props.user.emailOptIn.includes(MonthlyBalanceReportEmailJuly),
    MONTHLY_BALANCE_REPORT_EMAIL_AUGUST: props.user.emailOptIn.includes(MonthlyBalanceReportEmailAugust),
    MONTHLY_BALANCE_REPORT_EMAIL_SEPTEMBER: props.user.emailOptIn.includes(MonthlyBalanceReportEmailSeptember),
    MONTHLY_BALANCE_REPORT_EMAIL_OCTOBER: props.user.emailOptIn.includes(MonthlyBalanceReportEmailOctober),
    MONTHLY_BALANCE_REPORT_EMAIL_NOVEMBER: props.user.emailOptIn.includes(MonthlyBalanceReportEmailNovember),
    MONTHLY_BALANCE_REPORT_EMAIL_DECEMBER: props.user.emailOptIn.includes(MonthlyBalanceReportEmailDecember),
    MONTHLY_CARD_BALANCE_REPORT_EMAIL_JANUARY: props.user.emailOptIn.includes(MonthlyCardBalanceReportEmailJanuary),
    MONTHLY_CARD_BALANCE_REPORT_EMAIL_FEBRUARY: props.user.emailOptIn.includes(MonthlyCardBalanceReportEmailFebruary),
    MONTHLY_CARD_BALANCE_REPORT_EMAIL_MARCH: props.user.emailOptIn.includes(MonthlyCardBalanceReportEmailMarch),
    MONTHLY_CARD_BALANCE_REPORT_EMAIL_APRIL: props.user.emailOptIn.includes(MonthlyCardBalanceReportEmailApril),
    MONTHLY_CARD_BALANCE_REPORT_EMAIL_MAY: props.user.emailOptIn.includes(MonthlyCardBalanceReportEmailMay),
    MONTHLY_CARD_BALANCE_REPORT_EMAIL_JUNE: props.user.emailOptIn.includes(MonthlyCardBalanceReportEmailJune),
    MONTHLY_CARD_BALANCE_REPORT_EMAIL_JULY: props.user.emailOptIn.includes(MonthlyCardBalanceReportEmailJuly),
    MONTHLY_CARD_BALANCE_REPORT_EMAIL_AUGUST: props.user.emailOptIn.includes(MonthlyCardBalanceReportEmailAugust),
    MONTHLY_CARD_BALANCE_REPORT_EMAIL_SEPTEMBER: props.user.emailOptIn.includes(MonthlyCardBalanceReportEmailSeptember),
    MONTHLY_CARD_BALANCE_REPORT_EMAIL_OCTOBER: props.user.emailOptIn.includes(MonthlyCardBalanceReportEmailOctober),
    MONTHLY_CARD_BALANCE_REPORT_EMAIL_NOVEMBER: props.user.emailOptIn.includes(MonthlyCardBalanceReportEmailNovember),
    MONTHLY_CARD_BALANCE_REPORT_EMAIL_DECEMBER: props.user.emailOptIn.includes(MonthlyCardBalanceReportEmailDecember),
    SUBSCRIPTION_EXPIRATION_EMAIL: props.user.emailOptIn.includes(SubscriptionExpirationEmail)
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
  const result = Object.entries(input)
    .filter(([, value]) => value === true)
    .map(([key]) => key);

  await changeEmailOptIn({
    input: {
      emailOptIns: result
    }
  });

  addSuccess(t("change-email-opt-in-standby-notification"));
  router.push({ name: URL_ROOT });
}
</script>
