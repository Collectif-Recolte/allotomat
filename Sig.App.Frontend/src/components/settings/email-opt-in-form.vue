<i18n>
  {
    "en": {
      "submit": "Submit",
      "email-created-card-pdf": "Email created card pdf",
      "monthly-balance-report-email": "Monthly balance report email",
      "monthly-card-balance-report-email": "Monthly card balance report email",
      "subscription-expiration-email": "Subscription expiration email",
      "change-email-opt-in-standby-notification": "Your email preferences have been updated"
    },
    "fr": {
      "submit": "Mettre à jour",
      "created-card-pdf-email": "Email créé lors de la création d'une carte PDF",
      "monthly-balance-report-email": "Email de rapport de solde mensuel",
      "monthly-card-balance-report-email": "Email de rapport de solde mensuel de carte",
      "subscription-expiration-email": "Email de fin d'abonnement",
      "change-email-opt-in-standby-notification": "Vos préférences d'email ont été mises à jour"
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
        <Field v-slot="{ field }" name="createdCardPdfEmail">
          <PfFormInputCheckbox
            id="createdCardPdfEmail"
            v-bind="field"
            :checked="field.value"
            :label="t('created-card-pdf-email')" />
        </Field>
        <Field v-slot="{ field }" name="monthlyBalanceReportEmail">
          <PfFormInputCheckbox
            id="monthlyBalanceReportEmail"
            v-bind="field"
            :checked="field.value"
            :label="t('monthly-balance-report-email')" />
        </Field>

        <Field v-slot="{ field }" name="monthlyCardBalanceReportEmail">
          <PfFormInputCheckbox
            id="monthlyCardBalanceReportEmail"
            v-bind="field"
            :checked="field.value"
            :label="t('monthly-card-balance-report-email')" />
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

const props = defineProps({
  user: {
    type: Object,
    required: true
  }
});

const initialFormValues = computed(() => {
  return {
    createdCardPdfEmail: props.user.emailOptIn.createdCardPdfEmail,
    monthlyBalanceReportEmail: props.user.emailOptIn.monthlyBalanceReportEmail,
    monthlyCardBalanceReportEmail: props.user.emailOptIn.monthlyCardBalanceReportEmail,
    subscriptionExpirationEmail: props.user.emailOptIn.subscriptionExpirationEmail
  };
});

const { mutate: changeEmailOptIn } = useMutation(
  gql`
    mutation ChangeEmailOptIn($input: ChangeEmailOptInInput!) {
      changeEmailOptIn(input: $input) {
        user {
          id
          emailOptIn {
            createdCardPdfEmail
            monthlyBalanceReportEmail
            monthlyCardBalanceReportEmail
            subscriptionExpirationEmail
          }
        }
      }
    }
  `
);

async function onSubmitChangeEmailOptIn({
  createdCardPdfEmail,
  monthlyBalanceReportEmail,
  monthlyCardBalanceReportEmail,
  subscriptionExpirationEmail
}) {
  await changeEmailOptIn({
    input: { createdCardPdfEmail, monthlyBalanceReportEmail, monthlyCardBalanceReportEmail, subscriptionExpirationEmail }
  });
  addSuccess(t("change-email-opt-in-standby-notification"));
  router.push({ name: URL_ROOT });
}
</script>
