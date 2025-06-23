<i18n>
{
	"en": {
		"cancel": "Cancel",
		"project-name": "Name",
		"project-name-placeholder": "Ex. Proximity card",
    "project-url": "Website",
    "project-url-description": "Ex. https://carteproximite.org/",
    "project-allow-organizations-assign-cards": "Allow groups to assign cards",
    "project-beneficiaries-are-anonymous": "Anonymous beneficiaries",
    "project-administration-subscriptions-off-platform": "The administration of participants is done off-platform",
    "reconciliation-report-date": "Reconciliation report date",
    "one-month": "Last month",
    "one-week": "Last week",
    "one-day": "Last day"
	},
	"fr": {
		"cancel": "Annuler",
		"project-name": "Nom",
		"project-name-placeholder": "Ex. Carte proximité",
    "project-url": "Site web",
    "project-url-description": "Ex. https://carteproximite.org/",
    "project-allow-organizations-assign-cards": "Permettre aux groupes d’assigner des cartes",
    "project-beneficiaries-are-anonymous": "Participant-e-s anonymes",
    "project-administration-subscriptions-off-platform": "L'administration des participant-e-s se fait hors plateforme",
    "reconciliation-report-date": "Date de réconciliation",
    "one-month": "Le dernier mois",
    "one-week": "La dernière semaine",
    "one-day": "La dernière journée"
  }
}
</i18n>

<template>
  <Form
    v-slot="{ isSubmitting, errors: formErrors }"
    :validation-schema="validationSchema || baseValidationSchema"
    :initial-values="initialValues || baseInitialValues"
    @submit="onSubmit">
    <PfForm
      has-footer
      can-cancel
      :disable-submit="Object.keys(formErrors).length > 0"
      :submit-label="props.submitBtn"
      :cancel-label="t('cancel')"
      :processing="isSubmitting"
      :warning-message="
        props.administrationSubscriptionsOffPlatform ? t('project-administration-subscriptions-off-platform') : ''
      "
      @cancel="closeModal">
      <PfFormSection>
        <Field v-slot="{ field, errors: fieldErrors }" name="name">
          <PfFormInputText
            id="name"
            v-bind="field"
            :label="t('project-name')"
            :placeholder="t('project-name-placeholder')"
            :errors="fieldErrors"
            col-span-class="sm:col-span-4" />
        </Field>
        <Field v-slot="{ field, errors: fieldErrors }" name="url">
          <PfFormInputText
            id="url"
            v-bind="field"
            :label="t('project-url')"
            :errors="fieldErrors"
            :description="t('project-url-description')"
            col-span-class="sm:col-span-4" />
        </Field>
        <Field v-slot="{ field }" name="beneficiariesAreAnonymous">
          <PfFormInputCheckbox
            id="beneficiariesAreAnonymous"
            v-bind="field"
            :label="t('project-beneficiaries-are-anonymous')"
            :checked="field.value"
            col-span-class="sm:col-span-4" />
        </Field>
        <Field v-slot="{ field }" name="allowOrganizationsAssignCards">
          <PfFormInputCheckbox
            id="allowOrganizationsAssignCards"
            v-bind="field"
            :label="t('project-allow-organizations-assign-cards')"
            :checked="field.value"
            col-span-class="sm:col-span-4" />
        </Field>
        <Field v-if="isNewProject" v-slot="{ field }" name="administrationSubscriptionsOffPlatform">
          <PfFormInputCheckbox
            id="administrationSubscriptionsOffPlatform"
            v-bind="field"
            :label="t('project-administration-subscriptions-off-platform')"
            :checked="field.value"
            col-span-class="sm:col-span-4" />
        </Field>
        <Field v-slot="{ field }" name="reconciliationReportDate">
          <PfFormInputSelect
            id="reconciliationReportDate"
            v-bind="field"
            :errors="errors"
            :label="t('reconciliation-report-date')"
            :options="reconciliationReportDateOptions"
            col-span-class="sm:col-span-4" />
        </Field>
      </PfFormSection>
      <slot></slot>
    </PfForm>
  </Form>
</template>

<script setup>
import { defineEmits, defineProps, computed } from "vue";
import { useI18n } from "vue-i18n";
import { string, object } from "yup";

const { t } = useI18n();
const emit = defineEmits(["submit", "closeModal"]);

const props = defineProps({
  title: {
    type: String,
    default: ""
  },
  submitBtn: {
    type: String,
    default: ""
  },
  name: {
    type: String,
    default: ""
  },
  url: {
    type: String,
    default: ""
  },
  allowOrganizationsAssignCards: {
    type: Boolean,
    default: false
  },
  beneficiariesAreAnonymous: {
    type: Boolean,
    default: false
  },
  administrationSubscriptionsOffPlatform: {
    type: Boolean,
    default: false
  },
  reconciliationReportDate: {
    type: String,
    default: ""
  },
  initialValues: {
    type: Object,
    default: null
  },
  validationSchema: {
    type: Object,
    default: null
  },
  isNewProject: {
    type: Boolean,
    default: false
  },
  isNew: {
    type: Boolean,
    default: false
  }
});

const baseInitialValues = {
  name: props.name,
  url: props.url,
  allowOrganizationsAssignCards: props.allowOrganizationsAssignCards,
  beneficiariesAreAnonymous: props.beneficiariesAreAnonymous,
  administrationSubscriptionsOffPlatform: props.administrationSubscriptionsOffPlatform,
  reconciliationReportDate: props.reconciliationReportDate
};

const reconciliationReportDateOptions = [
  {
    label: t("one-month"),
    value: "ONE_MONTH"
  },
  {
    label: t("one-week"),
    value: "ONE_WEEK"
  },
  {
    label: t("one-day"),
    value: "ONE_DAY"
  }
];

const baseValidationSchema = computed(() =>
  object({
    name: string().label(t("project-name")).required(),
    url: string().label(t("project-url")).url()
  })
);

function closeModal() {
  emit("closeModal");
}

async function onSubmit(values) {
  emit("submit", values);
}
</script>
