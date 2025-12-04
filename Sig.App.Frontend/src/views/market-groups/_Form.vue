<i18n>
{
	"en": {
		"cancel": "Cancel",
		"market-group-name": "Name",
		"market-group-name-placeholder": "Ex. Central market"
	},
	"fr": {
		"cancel": "Annuler",
		"market-group-name": "Nom",
		"market-group-name-placeholder": "Ex. Commerce du Marché centrale",
	}
}
</i18n>

<template>
  <Form
    v-slot="{ isSubmitting, meta }"
    :validation-schema="validationSchema || baseValidationSchema"
    :initial-values="initialValues"
    @submit="onSubmit">
    <PfForm
      has-footer
      can-cancel
      :disable-submit="!meta.valid"
      :submit-label="props.submitBtn"
      :cancel-label="t('cancel')"
      :processing="isSubmitting"
      @cancel="closeModal">
      <PfFormSection>
        <Field v-slot="{ field, errors: fieldErrors }" name="marketGroupName">
          <PfFormInputText
            id="marketGroupName"
            :model-value="field.value"
            :label="t('market-group-name')"
            :placeholder="t('market-group-name-placeholder')"
            :errors="fieldErrors"
            @update:modelValue="field.onChange" />
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
  submitBtn: {
    type: String,
    default: ""
  },
  marketGroupName: {
    type: String,
    default: ""
  },
  initialValues: {
    type: Object,
    default(rawProps) {
      return { marketGroupName: rawProps.marketGroupName };
    }
  },
  validationSchema: {
    type: Object,
    default: null
  },
  isNew: {
    type: Boolean,
    default: false
  }
});

const baseValidationSchema = computed(() =>
  object({
    marketGroupName: string().label(t("market-group-name")).required()
  })
);

function closeModal() {
  emit("closeModal");
}

async function onSubmit(values) {
  emit("submit", values);
}
</script>
