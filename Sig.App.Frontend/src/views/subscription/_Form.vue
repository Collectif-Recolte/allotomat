<i18n>
{
	"en": {
		"add-product-group-subscription-type": "Add a product group",
    "product-group-select": "Product group",
    "product-group-delete": "Delete product group",
		"cancel": "Cancel",
    "add-subscription-type": "Add a participant category",
		"empty-product-group-subscription-types-list-error": "The list «Subscription product group» must contain at least 1 element",
    "fifteenth-day-of-the-month": "The 15th day of the month",
		"first-day-of-the-month": "The first day of the month",
    "first-and-fifteenth-day-of-the-month": "The first and 15th day of the month",
		"monthly-payment-moment": "Send monthly payment",
		"subscription-end-date": "End of payment period",
		"subscription-end-date-error": "The “End date” field must be greater than the “Start date” field",
		"subscription-name": "Subscription name",
		"subscription-name-placeholder": "Ex. Winter 2022",
		"subscription-start-date": "Beginning of payment period",
    "subscription-funds-expiration-date": "Fund expiry date",
    "subscription-funds-expiration-date-error": "The “Fund expiry date” field  must be later than the “End of payment period” field",
    "subscription-type-amount": "Amount",
    "subscription-type-category": "Participant category",
    "subscription-funds-accumulable": "Cumulative funds",
    "previous": "Previous",
    "set-period": "Set period",
    "set-amounts": "Set amounts",
    "subscription-funds-accumulable-disabled": "Disabled",
    "subscription-funds-accumulable-enabled": "Enabled",
    "subscription-funds-accumulable-desc-deactivated": "Whenever scheduled funds are added to cards via this Subscription, the previous funds will expire. (Gift Card amounts, funds from other Subscriptions and funds added manually will not be affected.)",
    "subscription-funds-accumulable-desc-activated": "All of the scheduled funds from this Subscription will expire on the date selected below.",
    "subscription-type-category-must-be-unique": "The “Participant Category” field must be unique within a “Product Group”",
    "subscription-product-group-must-be-unique": "The “Product Group” field must be unique"
	},
	"fr": {
		"add-product-group-subscription-type": "Ajouter un groupe de produits",
    "product-group-select": "Groupe de produits",
    "product-group-delete": "Supprimer le groupe de produits",
		"cancel": "Annuler",
    "add-subscription-type": "Ajouter une catégorie de participant",
		"empty-product-group-subscription-types-list-error": "La liste «Groupe de produit par abonnement» doit contenir au moins 1 élément",
    "fifteenth-day-of-the-month": "Le 15e jour du mois",
		"first-day-of-the-month": "Le premier jour du mois",
    "first-and-fifteenth-day-of-the-month": "Le premier et le 15e jour du mois",
		"monthly-payment-moment": "Envoi du paiement mensuel",
		"subscription-end-date": "Fin période de versements",
		"subscription-end-date-error": "Le champ «Date de fin» doit être ultérieur au champ «Date de début»",
		"subscription-name": "Nom de l'abonnement",
		"subscription-name-placeholder": "Ex. Hiver 2022",
		"subscription-start-date": "Début période de versements",
		"subscription-type-amount": "Montant",
    "subscription-funds-expiration-date": "Date d'expiration des fonds",
    "subscription-funds-expiration-date-error": "Le champ «Date d'expiration des fonds» doit être ultérieur à la fin de la période de versements.",
    "subscription-type-category": "Catégorie de participant",
    "subscription-funds-accumulable": "Fonds accumulables",
    "previous": "Précédent",
    "set-period": "Définir la période",
    "set-amounts": "Définir les montants",
    "subscription-funds-accumulable-disabled": "Désactivé",
    "subscription-funds-accumulable-enabled": "Activé",
    "subscription-funds-accumulable-desc-deactivated": "Chaque fois que des fonds programmés sont ajoutés aux cartes à partir de cet Abonnement, les fonds précédents expirent. (Les montants Cartes-Cadeaux, les fonds d'autres Abonnements et les fonds ajoutés manuellement ne seront pas affectés).",
    "subscription-funds-accumulable-desc-activated": "Tous les fonds programmés de cet abonnement expireront à la date sélectionnée ci-dessous.",
    "subscription-type-category-must-be-unique": "Le champ «Catégorie de participant» doit être unique à l'intérieur d'un «Groupe de produits»",
    "subscription-product-group-must-be-unique": "Le champ «Groupe de produits» doit être unique"
	}
}
</i18n>

<template>
  <Form
    v-slot="{ isSubmitting, errors: formErrors, values, validateField, setFieldValue }"
    :initial-values="initialValues"
    :validation-schema="currentSchema"
    keep-values
    @submit="nextStep">
    <UiStepper
      class="mb-6"
      :step-label="currentStep === 0 ? t('set-period') : t('set-amounts')"
      :step-count="2"
      :step-number="currentStep + 1" />
    <PfForm
      v-if="currentStep === 0"
      has-footer
      can-cancel
      :disable-submit="Object.keys(formErrors).length > 0"
      :submit-label="t('set-amounts')"
      :cancel-label="t('cancel')"
      :processing="isSubmitting"
      @cancel="closeModal">
      <PfFormSection is-grid>
        <Field v-slot="{ field, errors: fieldErrors }" name="subscriptionName">
          <PfFormInputText
            id="subscriptionName"
            col-span-class="sm:col-span-12"
            v-bind="field"
            :label="t('subscription-name')"
            :placeholder="t('subscription-name-placeholder')"
            :errors="fieldErrors" />
        </Field>
        <Field name="isFundsAccumulable">
          <div class="flex sm:col-span-12">
            <span class="text-sm font-medium text-grey-900 dark:text-grey-200">{{ t("subscription-funds-accumulable") }}</span>
            <UiSwitch
              id="isFundsAccumulable"
              v-model="isFundsAccumulableValue"
              class="mx-auto mr-0"
              @update:modelValue="(e) => updateIsFundsAccumulable(setFieldValue, validateField)">
              <template #left>
                <span class="mr-2 text-p3 font-semibold">{{
                  isFundsAccumulableValue
                    ? t("subscription-funds-accumulable-enabled")
                    : t("subscription-funds-accumulable-disabled")
                }}</span>
              </template>
            </UiSwitch>
          </div>
          <div class="flex sm:col-span-12">
            <!-- eslint-disable vue/no-v-html @intlify/vue-i18n/no-v-html -->
            <span
              v-if="!isFundsAccumulableValue"
              class="text-sm text-grey-500 dark:text-grey-400"
              v-html="t('subscription-funds-accumulable-desc-deactivated')"></span>
            <span
              v-else
              class="text-sm text-grey-500 dark:text-grey-400"
              v-html="t('subscription-funds-accumulable-desc-activated')"></span>
            <!-- eslint-enable vue/no-v-html @intlify/vue-i18n/no-v-html -->
          </div>
        </Field>
        <Field v-slot="{ field: inputField, errors: fieldErrors }" name="startDate">
          <DatePicker
            id="startDate"
            class="sm:col-span-6"
            v-bind="inputField"
            :label="t('subscription-start-date')"
            :errors="fieldErrors"
            is-inside-modal
            @update:modelValue="forceValidation(values, validateField)" />
        </Field>
        <Field v-slot="{ field: inputField, errors: fieldErrors }" name="endDate">
          <DatePicker
            id="endDate"
            class="sm:col-span-6"
            v-bind="inputField"
            :label="t('subscription-end-date')"
            :errors="fieldErrors"
            is-inside-modal
            @update:modelValue="forceValidation(values, validateField)" />
        </Field>
        <Field v-slot="{ field, errors: fieldErrors }" name="monthlyPaymentMoment">
          <PfFormInputSelect
            id="monthlyPaymentMoment"
            class="sm:col-span-6 sm:col-start-1"
            v-bind="field"
            :label="t('monthly-payment-moment')"
            :options="monthlyPaymentMomentOptions"
            :errors="fieldErrors" />
        </Field>
        <Field v-slot="{ field: inputField, errors: fieldErrors }" v-model="fundsExpirationDateValue" name="fundsExpirationDate">
          <DatePicker
            id="fundsExpirationDate"
            class="sm:col-span-6"
            v-bind="inputField"
            :label="t('subscription-funds-expiration-date')"
            :errors="isFundsAccumulableValue ? fieldErrors : []"
            :disabled="!isFundsAccumulableValue"
            is-inside-modal
            @update:modelValue="forceValidation(values, validateField)" />
        </Field>
      </PfFormSection>
    </PfForm>

    <PfForm
      v-if="currentStep === 1"
      has-footer
      can-cancel
      :disable-submit="Object.keys(formErrors).length > 0"
      :submit-label="props.submitBtn"
      :cancel-label="t('previous')"
      :processing="isSubmitting"
      @cancel="prevStep">
      <PfFormSection>
        <FieldArray v-slot="{ fields, remove, push }" key-path="id" name="productGroupSubscriptionTypes">
          <UiFieldArray
            :block-layout="true"
            :fields="fields"
            :add-label="t('add-product-group-subscription-type')"
            :delete-label="t('product-group-delete')"
            :empty-list-error="t('empty-product-group-subscription-types-list-error')"
            :errors="formErrors[`productGroupSubscriptionTypes`]"
            @addField="() => createNewProductGroupSubscriptionTypes(push)"
            @removeField="(idx) => remove(idx)">
            <template #default="slotProps">
              <div class="sm:col-span-12 space-y-6 divide-y divide-grey-100">
                <Field
                  v-slot="{ field, errors: fieldErrors }"
                  :name="`productGroupSubscriptionTypes[${slotProps.idx}].productGroupId`">
                  <PfFormInputSelect
                    :key="`productGroupSubscriptionTypes[${slotProps.idx}].productGroupId`"
                    v-bind="field"
                    :label="t('product-group-select')"
                    :options="productGroups"
                    :errors="fieldErrors" />
                </Field>
                <FieldArray
                  v-slot="{ fields: fieldsChild, remove: removeChild, push: pushChild }"
                  key-path="id"
                  :name="`productGroupSubscriptionTypes[${slotProps.idx}].types`">
                  <UiFieldArray
                    class="pt-3"
                    :is-inside-block-layout="true"
                    :fields="fieldsChild"
                    :add-label="t('add-subscription-type')"
                    :empty-list-error="t('empty-product-group-subscription-types-list-error')"
                    :errors="formErrors[`productGroupSubscriptionTypes[${slotProps.idx}].types`]"
                    @addField="() => pushChild({ amount: '', type: '' })"
                    @removeField="(idx) => removeChild(idx)">
                    <template #default="slotPropsType">
                      <Field
                        v-slot="{ field: inputField, errors: fieldErrors }"
                        :name="`productGroupSubscriptionTypes[${slotProps.idx}].types[${slotPropsType.idx}].type`">
                        <PfFormInputSelect
                          :id="`productGroupSubscriptionTypes[${slotProps.idx}].types[${slotPropsType.idx}].type`"
                          class="grow"
                          v-bind="inputField"
                          :label="t('subscription-type-category')"
                          :options="beneficiaryTypes"
                          :errors="fieldErrors"
                          col-span-class="sm:col-span-6" />
                      </Field>
                      <Field
                        v-slot="{ field: inputField, errors: fieldErrors }"
                        :name="`productGroupSubscriptionTypes[${slotProps.idx}].types[${slotPropsType.idx}].amount`">
                        <PfFormInputText
                          :id="`productGroupSubscriptionTypes[${slotProps.idx}].types[${slotPropsType.idx}].amount`"
                          v-bind="inputField"
                          :label="t('subscription-type-amount')"
                          :errors="fieldErrors"
                          input-type="number"
                          min="0"
                          col-span-class="sm:col-span-6">
                          <template #trailingIcon>
                            <UiDollarSign :errors="fieldErrors" />
                          </template>
                        </PfFormInputText>
                      </Field>
                    </template>
                  </UiFieldArray>
                </FieldArray>
              </div>
            </template>
          </UiFieldArray>
        </FieldArray>
      </PfFormSection>
    </PfForm>
  </Form>
</template>

<script setup>
import gql from "graphql-tag";
import { useI18n } from "vue-i18n";
import { defineEmits, defineProps, computed, ref } from "vue";
import { FieldArray } from "vee-validate";
import { string, object, array, mixed, lazy } from "yup";
import { useQuery, useResult } from "@vue/apollo-composable";

import { PRODUCT_GROUP_LOYALTY } from "@/lib/consts/enums";
import {
  FIRST_DAY_OF_THE_MONTH,
  FIFTEENTH_DAY_OF_THE_MONTH,
  FIRST_AND_FIFTEENTH_DAY_OF_THE_MONTH
} from "@/lib/consts/monthly-payment-moment";

import DatePicker from "@/components/ui/date-picker.vue";

const { t } = useI18n();
const currentStep = ref(0);

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
  subscriptionName: {
    type: String,
    default: ""
  },
  isFundsAccumulable: {
    type: Boolean,
    default: false
  },
  monthlyPaymentMoment: {
    type: String,
    default: FIRST_DAY_OF_THE_MONTH
  },
  fundsExpirationDate: {
    type: Date,
    default: null
  },
  startDate: {
    type: Date,
    default: null
  },
  endDate: {
    type: Date,
    default: null
  },
  productGroupSubscriptionTypes: {
    type: Array,
    default() {
      return [{ productGroupId: "", types: [{ amount: "", type: "" }] }];
    }
  },
  projectId: {
    type: String,
    required: true
  }
});

const initialValues = {
  subscriptionName: props.subscriptionName,
  monthlyPaymentMoment: props.monthlyPaymentMoment,
  startDate: props.startDate,
  endDate: props.endDate,
  productGroupSubscriptionTypes: props.productGroupSubscriptionTypes
};

const isFundsAccumulableValue = ref(props.isFundsAccumulable);
const fundsExpirationDateValue = ref(props.fundsExpirationDate);

const monthlyPaymentMomentOptions = [
  {
    label: t("first-day-of-the-month"),
    value: FIRST_DAY_OF_THE_MONTH
  },
  {
    label: t("fifteenth-day-of-the-month"),
    value: FIFTEENTH_DAY_OF_THE_MONTH
  },
  {
    label: t("first-and-fifteenth-day-of-the-month"),
    value: FIRST_AND_FIFTEENTH_DAY_OF_THE_MONTH
  }
];

const { result } = useQuery(
  gql`
    query Project($id: ID!) {
      project(id: $id) {
        id
        beneficiaryTypes {
          id
          name
        }
        productGroups {
          id
          name
          color
          orderOfAppearance
        }
      }
    }
  `,
  {
    id: props.projectId
  }
);
const beneficiaryTypes = useResult(result, null, (data) => {
  return data.project.beneficiaryTypes.map((x) => ({ label: x.name, value: x.id }));
});

const productGroups = useResult(result, null, (data) => {
  return data.project.productGroups.filter((x) => x.name !== PRODUCT_GROUP_LOYALTY).map((x) => ({ label: x.name, value: x.id }));
});

// Form validation & steps management
const validationSchemas = computed(() => {
  return [
    object({
      subscriptionName: string().label(t("subscription-name")).required(),
      startDate: mixed().label(t("subscription-start-date")).required(),
      fundsExpirationDate: lazy(() => {
        if (isFundsAccumulableValue.value) {
          return mixed()
            .label(t("subscription-funds-expiration-date"))
            .test({
              name: "sameValue",
              exclusive: false,
              params: {},
              message: t("subscription-funds-expiration-date-error"),
              test: function (value, form) {
                return new Date(value) > new Date(form.parent.endDate);
              }
            })
            .required();
        } else {
          return mixed().test({
            test: function () {
              return true;
            }
          });
        }
      }),
      endDate: mixed()
        .label(t("subscription-end-date"))
        .test({
          name: "sameValue",
          exclusive: false,
          params: {},
          message: t("subscription-end-date-error"),
          test: function (value, form) {
            return new Date(value) > new Date(form.parent.startDate);
          }
        })
        .required()
    }),
    object({
      productGroupSubscriptionTypes: array()
        .min(1)
        .of(
          object({
            productGroupId: string().label(t("product-group-select")).required(),
            types: array()
              .min(1)
              .of(
                object({
                  amount: string().label(t("subscription-type-amount")).required(),
                  type: string().label(t("subscription-type-category")).required()
                })
              )
              .unique(t("subscription-type-category-must-be-unique"), (a) => a.type)
          })
        )
        .unique(t("subscription-product-group-must-be-unique"), (a) => a.productGroupId)
    })
  ];
});

const currentSchema = computed(() => {
  return validationSchemas.value[currentStep.value];
});

function createNewProductGroupSubscriptionTypes(push) {
  push({ productGroupId: "", types: [{ amount: "", type: "" }] });
}

function nextStep(values) {
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

function closeModal() {
  emit("closeModal");
}

async function onSubmit({
  subscriptionName,
  monthlyPaymentMoment,
  fundsExpirationDate,
  startDate,
  endDate,
  productGroupSubscriptionTypes
}) {
  emit("submit", {
    subscriptionName,
    monthlyPaymentMoment,
    fundsExpirationDate,
    startDate,
    endDate,
    productGroupSubscriptionTypes,
    isFundsAccumulable: isFundsAccumulableValue.value
  });
}

function updateIsFundsAccumulable(setFieldValue, validateField) {
  if (!isFundsAccumulableValue.value) {
    setFieldValue("fundsExpirationDate", null);
    fundsExpirationDateValue.value = null;
    validateField("fundsExpirationDate");
  }
}

async function forceValidation(values, validateField) {
  if (values.endDate) {
    validateField("endDate");
  }
  if (values.startDate) {
    validateField("startDate");
  }
  if (values.fundsExpirationDate) {
    validateField("fundsExpirationDate");
  }
}
</script>
