<i18n>
{
	"en": {
    "budget-allowance-organization": "Organization",
    "submit-btn": "OK",
    "cancel-btn": "Cancel",
    "amount-label": "Amount",
    "budget-allowance-minimum-error": "The amount is less than the promised amount.",
    "edit-allowance": "Edit",
    "delete-allowance": "Delete"
	},
	"fr": {
    "budget-allowance-organization": "Organisme",
    "submit-btn": "OK",
    "cancel-btn": "Annuler",
    "amount-label": "Montant",
    "budget-allowance-minimum-error": "Le montant est inférieur au montant promis.",
    "edit-allowance": "Modifier",
    "delete-allowance": "Supprimer"
	}
}
</i18n>

<template>
  <Form
    v-slot="{ isSubmitting, errors: formErrors, handleReset }"
    :validation-schema="validationSchema"
    :initial-values="props.budgetAllowance"
    @submit="saveBudget">
    <PfFormNested
      :is-disabled="!isInEdition"
      :submit-label="t('submit-btn')"
      :disable-submit="Object.keys(formErrors).length > 0"
      :processing="isSubmitting">
      <Field v-slot="{ field, errors }" name="organization">
        <PfFormInputSelect
          id="organization"
          v-bind="field"
          :errors="errors"
          :label="t('budget-allowance-organization')"
          :options="organizationsList"
          :disabled="!canEditOrganization" />
      </Field>
      <template #lastField>
        <Field v-slot="{ field, errors: fieldErrors }" name="originalFund">
          <PfFormInputText
            id="originalFund"
            v-bind="field"
            :label="t('amount-label')"
            :errors="fieldErrors"
            input-type="number"
            min="0"
            :disabled="!isInEdition">
            <template #trailingIcon>
              <UiDollarSign :errors="fieldErrors" />
            </template>
          </PfFormInputText>
        </Field>
      </template>

      <template #footer="{ processing, disableSubmit, submitLabel, loadingLabel }">
        <template v-if="!isInEdition">
          <UiButtonGroup :items="getBtnGroup()" tooltip-position="left" />
        </template>
        <template v-else>
          <PfButtonAction
            btn-type="reset"
            btn-style="link"
            size="sm"
            :label="t('cancel-btn')"
            :is-disabled="processing"
            @click="cancelEditBudget(handleReset)" />
          <div class="relative flex items-center">
            <PfButtonAction
              btn-type="submit"
              class="px-8"
              size="sm"
              :is-disabled="isFormDisabled || disableSubmit || processing"
              :label="submitLabel" />
            <div class="absolute -translate-y-1/2 top-1/2 right-1">
              <PfSpinner v-if="processing" text-color-class="text-white" :loading-label="loadingLabel" is-small />
            </div>
          </div>
        </template>
      </template>
    </PfFormNested>
  </Form>
</template>

<script setup>
import { defineProps, defineEmits, ref, computed } from "vue";
import { object, string, number } from "yup";
import { useI18n } from "vue-i18n";
import { useIsFormDirty, useIsFormValid } from "vee-validate";

import ICON_TRASH from "@/lib/icons/trash.json";
import ICON_PENCIL from "@/lib/icons/pencil.json";

const { t } = useI18n();
const inEdition = ref(null);

const props = defineProps({
  budgetAllowance: {
    type: Object,
    default: null
  },
  organizations: {
    type: Array,
    default() {
      return [];
    }
  },
  availableOrganizations: {
    type: Array,
    default() {
      return [];
    }
  }
});

const emit = defineEmits(["delete", "save"]);

const getBtnGroup = () => [
  {
    icon: ICON_PENCIL,
    label: t("edit-allowance"),
    onClick: () => editBudget()
  },
  {
    icon: ICON_TRASH,
    label: t("delete-allowance"),
    onClick: () => deleteBudget()
  }
];

const validationSchema = computed(() =>
  object({
    organization: string().nullable(true).label(t("budget-allowance-organization")).required(),
    originalFund: number()
      .nullable(true)
      .label(t("amount-label"))
      .required()
      .min(0.01)
      .test({
        name: "minAvailableFund",
        exclusive: false,
        params: {},
        message: t("budget-allowance-minimum-error"),
        test: function (value) {
          return value >= props.budgetAllowance.originalFund - props.budgetAllowance.availableFund;
        }
      })
  })
);

function editBudget() {
  inEdition.value = true;
}

function cancelEditBudget(reset) {
  reset();
  inEdition.value = false;
  if (props.budgetAllowance.isNew) {
    emit("delete", props.budgetAllowance);
  }
}

function deleteBudget() {
  emit("delete", props.budgetAllowance);
}

function saveBudget({ organization, originalFund }) {
  emit("save", {
    organization,
    originalFund: parseFloat(originalFund),
    budgetAllowanceId: props.budgetAllowance.isNew ? null : props.budgetAllowance.id
  });
  inEdition.value = false;
}

const canEditOrganization = computed(() => {
  return isInEdition.value && props.budgetAllowance.isNew;
});

const isInEdition = computed(() => {
  return inEdition.value || props.budgetAllowance.isNew;
});

const organizationsList = computed(() => {
  if (props.budgetAllowance.isNew) {
    return props.availableOrganizations;
  }

  return props.organizations;
});

const isFormDisabled = computed(() => {
  const isDirty = useIsFormDirty();
  const isValid = useIsFormValid();
  return !isDirty.value || !isValid.value;
});
</script>
