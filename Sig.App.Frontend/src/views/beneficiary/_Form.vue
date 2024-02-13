<i18n>
{
	"en": {
		"beneficiary-firstname": "Firstname",
		"beneficiary-firstname-placeholder": "Ex. John",
		"beneficiary-lastname": "Lastname",
		"beneficiary-lastname-placeholder": "Ex. Doe",
    "beneficiary-category": "Category",
		"cancel": "Cancel",
		"communication-means": "Means of communication",
		"communication-means-address": "Address",
		"communication-means-address-placeholder": "Ex. 123, example road",
    "communication-means-postal-code": "Postal code",
		"communication-means-postal-code-placeholder": "Ex. A0A 0A0",
    "communication-means-postal-code-error": "The postal code must be of the format \"A0A 0A0\"",
		"communication-means-email": "Email",
		"communication-means-email-placeholder": "Ex. john.doe{'@'}example.com",
		"communication-means-phone": "Phone",
		"communication-means-phone-placeholder": "Ex. 555-555-1234",
    "beneficiary-notes": "Notes",
    "unique-id-other-system": "Other System Unique Identifier",
    "unique-id-id1": "Unique Identifier 1",
    "unique-id-id2": "Unique Identifier 2"
	},
	"fr": {
		"beneficiary-firstname": "Prénom",
		"beneficiary-firstname-placeholder": "Ex. John",
		"beneficiary-lastname": "Nom de famille",
		"beneficiary-lastname-placeholder": "Ex. Doe",
    "beneficiary-category": "Catégorie",
		"cancel": "Annuler",
		"communication-means": "Moyen de communication",
		"communication-means-address": "Adresse",
		"communication-means-address-placeholder": "Ex. 123, rue de l'exemple",
    "communication-means-postal-code": "Code postal",
		"communication-means-postal-code-placeholder": "Ex. A0A 0A0",
    "communication-means-postal-code-error": "Le code postal doit être du format \"A0A 0A0\"",
		"communication-means-email": "Courriel",
		"communication-means-email-placeholder": "Ex. john.doe{'@'}exemple.com",
		"communication-means-phone": "Téléphone",
		"communication-means-phone-placeholder": "Ex. 555-555-1234",
    "beneficiary-notes": "Notes",
    "unique-id-other-system": "Identifiant unique d'autre système",
    "unique-id-id1": "Identifiant unique 1",
    "unique-id-id2": "Identifiant unique 2"
	}
}
</i18n>

<template>
  <Form
    v-slot="{ isSubmitting, errors: formErrors }"
    :validation-schema="validationSchema"
    :initial-values="initialValues"
    @submit="onSubmit">
    <PfForm
      has-footer
      can-cancel
      :disable-submit="Object.keys(formErrors).length > 0"
      :submit-label="props.submitBtn"
      :cancel-label="t('cancel')"
      :processing="isSubmitting"
      :warning-message="props.warningMessage"
      @cancel="closeModal">
      <PfFormSection>
        <Field v-slot="{ field, errors: fieldErrors }" name="firstname">
          <PfFormInputText
            id="firstname"
            v-bind="field"
            :label="t('beneficiary-firstname')"
            :placeholder="t('beneficiary-firstname-placeholder')"
            :errors="fieldErrors"
            col-span-class="sm:col-span-4" />
        </Field>
        <Field v-slot="{ field, errors: fieldErrors }" name="lastname">
          <PfFormInputText
            id="lastname"
            v-bind="field"
            :label="t('beneficiary-lastname')"
            :placeholder="t('beneficiary-lastname-placeholder')"
            :errors="fieldErrors"
            col-span-class="sm:col-span-4" />
        </Field>
        <Field v-slot="{ field, errors: fieldErrors }" name="notes">
          <PfFormInputTextarea
            id="notes"
            v-bind="field"
            :label="t('beneficiary-notes')"
            :errors="fieldErrors"
            col-span-class="sm:col-span-4" />
        </Field>
        <Field v-slot="{ field: inputField, errors: fieldErrors }" name="beneficiaryTypeId">
          <PfFormInputSelect
            id="beneficiaryTypeId"
            v-bind="inputField"
            :label="t('beneficiary-category')"
            :options="beneficiaryTypes"
            col-span-class="sm:col-span-3"
            :errors="fieldErrors" />
        </Field>
      </PfFormSection>
      <PfFormSection :title="t('communication-means')">
        <Field v-slot="{ field, errors: fieldErrors }" name="email">
          <PfFormInputText
            id="email"
            v-bind="field"
            :label="t('communication-means-email')"
            :placeholder="t('communication-means-email-placeholder')"
            :errors="fieldErrors"
            col-span-class="sm:col-span-4" />
        </Field>
        <Field v-slot="{ field, errors: fieldErrors }" name="phone">
          <PfFormInputText
            id="phone"
            v-bind="field"
            :label="t('communication-means-phone')"
            :placeholder="t('communication-means-phone-placeholder')"
            :errors="fieldErrors"
            col-span-class="sm:col-span-4" />
        </Field>
        <Field v-slot="{ field, errors: fieldErrors }" name="address">
          <PfFormInputText
            id="address"
            v-bind="field"
            :label="t('communication-means-address')"
            :placeholder="t('communication-means-address-placeholder')"
            :errors="fieldErrors"
            col-span-class="sm:col-span-4" />
        </Field>
        <Field v-slot="{ field, errors: fieldErrors }" name="postalCode">
          <PfFormInputText
            id="postalCode"
            v-bind="field"
            :label="t('communication-means-postal-code')"
            :placeholder="t('communication-means-postal-code-placeholder')"
            :errors="fieldErrors"
            col-span-class="sm:col-span-4" />
        </Field>
      </PfFormSection>
      <PfFormSection :title="t('unique-id-other-system')">
        <Field v-slot="{ field, errors: fieldErrors }" name="id1">
          <PfFormInputText
            id="id1"
            v-bind="field"
            :label="t('unique-id-id1')"
            :errors="fieldErrors"
            col-span-class="sm:col-span-4" />
        </Field>
        <Field v-slot="{ field, errors: fieldErrors }" name="id2">
          <PfFormInputText
            id="id2"
            v-bind="field"
            :label="t('unique-id-id2')"
            :errors="fieldErrors"
            col-span-class="sm:col-span-4" />
        </Field>
      </PfFormSection>
    </PfForm>
  </Form>
</template>

<script setup>
import gql from "graphql-tag";
import { useQuery, useResult } from "@vue/apollo-composable";
import { defineEmits, defineProps, computed } from "vue";
import { useI18n } from "vue-i18n";
import { string, object } from "yup";

const { t } = useI18n();
const emit = defineEmits(["submit", "closeModal"]);

const props = defineProps({
  submitBtn: {
    type: String,
    default: ""
  },
  firstname: {
    type: String,
    default: ""
  },
  lastname: {
    type: String,
    default: ""
  },
  email: {
    type: String,
    default: ""
  },
  phone: {
    type: String,
    default: ""
  },
  address: {
    type: String,
    default: ""
  },
  notes: {
    type: String,
    default: ""
  },
  postalCode: {
    type: String,
    default: ""
  },
  id1: {
    type: String,
    default: ""
  },
  id2: {
    type: String,
    default: ""
  },
  beneficiaryTypeId: {
    type: String,
    default: ""
  },
  organizationId: {
    type: String,
    required: true
  },
  warningMessage: {
    type: String,
    default: ""
  }
});

const initialValues = {
  firstname: props.firstname,
  lastname: props.lastname,
  email: props.email,
  phone: props.phone,
  address: props.address,
  notes: props.notes,
  postalCode: props.postalCode,
  id1: props.id1,
  id2: props.id2,
  beneficiaryTypeId: props.beneficiaryTypeId
};

const { result } = useQuery(
  gql`
    query Organization($id: ID!) {
      organization(id: $id) {
        id
        project {
          id
          beneficiaryTypes {
            id
            name
          }
        }
      }
    }
  `,
  {
    id: props.organizationId
  },
  () => ({
    enabled: props.organizationId !== null
  })
);
const beneficiaryTypes = useResult(result, null, (data) => {
  return data.organization.project.beneficiaryTypes.map((x) => ({ label: x.name, value: x.id }));
});

const validationSchema = computed(() =>
  object({
    firstname: string().label(t("beneficiary-firstname")).required(),
    lastname: string().label(t("beneficiary-lastname")).required(),
    email: string().label(t("communication-means-email")).email(),
    postalCode: string()
      .label("communication-means-postal-code")
      .test({
        name: "canadianPostalCode",
        exclusive: false,
        params: {},
        message: t("communication-means-postal-code-error"),
        test: function (value) {
          if (value === "" || value === undefined || value === null) {
            return true;
          }

          var regex = /^[ABCEGHJ-NPRSTVXY]\d[ABCEGHJ-NPRSTV-Z][ -]?\d[ABCEGHJ-NPRSTV-Z]\d$/i;
          return regex.test(value);
        }
      }),
    beneficiaryTypeId: string().label(t("beneficiary-category")).required(),
    id1: string().label(t("unique-id-id1")).required()
  })
);

function closeModal() {
  emit("closeModal");
}

async function onSubmit({ firstname, lastname, email, phone, address, notes, postalCode, id1, id2, beneficiaryTypeId }) {
  emit("submit", { firstname, lastname, email, phone, address, notes, postalCode, id1, id2, beneficiaryTypeId });
}
</script>
