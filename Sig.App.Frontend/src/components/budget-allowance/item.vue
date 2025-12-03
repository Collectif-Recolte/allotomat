<i18n>
{
	"en": {
    "budget-allowance-organization": "Group",
    "submit-btn": "OK",
    "cancel-btn": "Cancel",
    "amount-label": "Amount",
    "budget-allowance-minimum-error": "The amount is less than the promised amount.",
    "edit-allowance": "Edit",
    "available-fund-label": "Available fund",
    "delete-allowance": "Delete",
    "budget-allowance-move": "Transfer funds"
	},
	"fr": {
    "budget-allowance-organization": "Groupe",
    "submit-btn": "OK",
    "cancel-btn": "Annuler",
    "amount-label": "Montant",
    "budget-allowance-minimum-error": "Le montant est inférieur au montant promis.",
    "edit-allowance": "Modifier",
    "available-fund-label": "Fonds disponible",
    "delete-allowance": "Supprimer",
    "budget-allowance-move": "Transférer des fonds"
	}
}
</i18n>

<template>
  <Form
    v-slot="{ isSubmitting, handleReset, meta }"
    :validation-schema="validationSchema"
    :initial-values="props.budgetAllowance"
    @submit="saveBudget">
    <PfFormNested
      :is-disabled="!isInEdition"
      :submit-label="t('submit-btn')"
      :disable-submit="!meta.valid"
      :processing="isSubmitting">
      <Field v-slot="{ field, errors }" name="organization">
        <PfFormInputSelect
          id="organization"
          :model-value="field.value"
          :errors="errors"
          :label="t('budget-allowance-organization')"
          :options="organizationsList"
          :disabled="!canEditOrganization"
          @update:modelValue="field.onChange" />
      </Field>
      <template #lastField>
        <Field v-slot="{ field, errors: fieldErrors }" name="originalFund">
          <PfFormInputText
            id="originalFund"
            :model-value="field.value"
            :label="t('amount-label')"
            :errors="fieldErrors"
            input-type="number"
            min="0"
            :disabled="!isInEdition"
            @update:modelValue="field.onChange">
            <template #trailingIcon>
              <UiDollarSign :errors="fieldErrors" />
            </template>
          </PfFormInputText>
        </Field>
        <Field v-if="!props.budgetAllowance.isNew" v-slot="{ field, errors: fieldErrors }" name="availableFund">
          <PfFormInputText
            id="availableFund"
            :model-value="field.value"
            :label="t('available-fund-label')"
            :errors="fieldErrors"
            input-type="number"
            min="0"
            disabled="true">
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
              :is-disabled="disableSubmit || processing"
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

import ICON_TRASH from "@/lib/icons/trash.json";
import ICON_PENCIL from "@/lib/icons/pencil.json";
import ICON_MOVE from "@/lib/icons/arrow-ricochet.json";

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

const emit = defineEmits(["delete", "save", "move"]);

const getBtnGroup = () => [
  {
    icon: ICON_PENCIL,
    label: t("edit-allowance"),
    onClick: () => editBudget()
  },
  {
    icon: ICON_MOVE,
    label: t("budget-allowance-move"),
    onClick: () => moveBudget()
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

function moveBudget() {
  emit("move", props.budgetAllowance);
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
</script>
